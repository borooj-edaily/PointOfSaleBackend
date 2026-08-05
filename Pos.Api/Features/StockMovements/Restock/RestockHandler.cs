using Dapper;
using MediatR;
using Pos.Api.Common;
using Pos.Api.Enums;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.StockMovements.Restock
{
    public class RestockHandler : IRequestHandler<RestockCommand, int>
    {
        private readonly IPosDatabase _database;

        public RestockHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<int> Handle(RestockCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            // نجيب بيانات الصنف الحالية (الرصيد + عدد القطع بالعبوة إن وجد)
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
            var balanceAfter = balanceBefore + quantityInPieces;

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
                         NULL, NULL, UTC_TIMESTAMP(6), @CreatedByUserId);
                    SELECT LAST_INSERT_ID();";

                var movementId = await connection.ExecuteScalarAsync<int>(insertMovementSql, new
                {
                    request.ProductId,
                    Type = (int)StockMovementType.Restock,
                    QuantityInPieces = quantityInPieces,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
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