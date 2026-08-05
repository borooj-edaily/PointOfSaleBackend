using Dapper;
using MediatR;
using Pos.Api.Common;
using Pos.Api.Enums;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.StockMovements.Deduct
{
    public class DeductStockHandler : IRequestHandler<DeductStockCommand, int>
    {
        private readonly IPosDatabase _database;

        public DeductStockHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<int> Handle(DeductStockCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string getProductSql = @"
                SELECT StockInPieces, PiecesPerPackage
                FROM Products
                WHERE Id = @ProductId;";

            var product = await connection.QueryFirstOrDefaultAsync<(int StockInPieces, int? PiecesPerPackage)?>(
                getProductSql, new { request.ProductId });

            if (product is null)
                throw new NotFoundException($"لا يوجد صنف بالمعرف {request.ProductId}");

            var quantityInPieces = UnitConversionHelper.ConvertToPieces(
                request.Quantity, request.IsPackage, product.Value.PiecesPerPackage);

            var balanceBefore = product.Value.StockInPieces;

            // منع الرصيد السالب — قاعدة أساسية
            if (quantityInPieces > balanceBefore)
                throw new BusinessException(
                    $"لا يمكن خصم {quantityInPieces} حبة، الرصيد الحالي هو {balanceBefore} حبة فقط.");

            var balanceAfter = balanceBefore - quantityInPieces;

            using var transaction = connection.BeginTransaction();
            try
            {
                const string updateStockSql = @"
                    UPDATE Products
                    SET StockInPieces = @BalanceAfter,
                        UpdatedAt = UTC_TIMESTAMP(6),
                        UpdatedByUserId = @CreatedByUserId
                    WHERE Id = @ProductId;";

                await connection.ExecuteAsync(updateStockSql, new
                {
                    BalanceAfter = balanceAfter,
                    request.ProductId,
                    request.CreatedByUserId
                }, transaction);

                const string insertMovementSql = @"
                    INSERT INTO StockMovements
                        (ProductId, Type, QuantityInPieces, BalanceBefore, BalanceAfter,
                         Reason, ReferenceInvoiceId, CreatedAt, CreatedByUserId)
                    VALUES
                        (@ProductId, @Type, @QuantityInPieces, @BalanceBefore, @BalanceAfter,
                         @Reason, NULL, UTC_TIMESTAMP(6), @CreatedByUserId);
                    SELECT LAST_INSERT_ID();";

                var movementId = await connection.ExecuteScalarAsync<int>(insertMovementSql, new
                {
                    request.ProductId,
                    Type = (int)StockMovementType.ManualDeduction,
                    QuantityInPieces = quantityInPieces,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    request.Reason,
                    request.CreatedByUserId
                }, transaction);

                transaction.Commit();
                return movementId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}