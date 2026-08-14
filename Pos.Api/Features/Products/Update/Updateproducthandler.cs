using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Products.Update
{
    public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Unit>
    {
        private readonly IPosDatabase _database;

        public UpdateProductHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string checkProductSql = "SELECT COUNT(1) FROM Products WHERE Id = @Id;";
            var exists = await connection.ExecuteScalarAsync<int>(checkProductSql, new { request.Id });

            if (exists == 0)
                throw new NotFoundException($"لا يوجد صنف بالمعرف {request.Id}");

            const string checkCategorySql = "SELECT IsActive FROM Categories WHERE Id = @CategoryId;";
            var categoryIsActive = await connection.QueryFirstOrDefaultAsync<bool?>(
                checkCategorySql, new { request.CategoryId });

            if (categoryIsActive is null)
                throw new NotFoundException($"لا يوجد كاتيجوري بالمعرف {request.CategoryId}");

            if (categoryIsActive == false)
                throw new BusinessException("لا يمكن ربط الصنف بكاتيجوري معطّلة");

            const string checkNameSql = @"
                SELECT COUNT(1) FROM Products
                WHERE CategoryId = @CategoryId AND Name = @Name AND Id <> @Id;";

            var nameExists = await connection.ExecuteScalarAsync<int>(
                checkNameSql, new { request.CategoryId, request.Name, request.Id });

            if (nameExists > 0)
                throw new DuplicateResourceException(
                    $"يوجد صنف بنفس الاسم '{request.Name}' مسبقاً بهذه الكاتيجوري");

            const string updateSql = @"
                UPDATE Products
                SET Name = @Name,
                    CategoryId = @CategoryId,
                    SellBy = @SellBy,
                    PiecesPerPackage = @PiecesPerPackage,
                    PricePerPiece = @PricePerPiece,
                    PricePerPackage = @PricePerPackage,
                    UpdatedAt = UTC_TIMESTAMP(6),
                    UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id;";

            await connection.ExecuteAsync(updateSql, new
            {
                request.Id,
                request.Name,
                request.CategoryId,
                SellBy = (int)request.SellBy,
                request.PiecesPerPackage,
                request.PricePerPiece,
                request.PricePerPackage,
                request.UpdatedByUserId
            });

            return Unit.Value;
        }
    }
}