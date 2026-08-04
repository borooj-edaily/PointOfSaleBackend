using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Products.Create
{
    public class CreateProductHandler : IRequestHandler<CreateProductCommand, int>
    {
        private readonly IPosDatabase _database;

        public CreateProductHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            // تحقق: الكاتيجوري موجودة وفعّالة
            const string checkCategorySql = @"
                SELECT IsActive FROM Categories WHERE Id = @CategoryId;";

            var categoryIsActive = await connection.QueryFirstOrDefaultAsync<bool?>(
                checkCategorySql, new { request.CategoryId });

            if (categoryIsActive is null)
                throw new NotFoundException($"لا يوجد كاتيجوري بالمعرف {request.CategoryId}");

            if (categoryIsActive == false)
                throw new BusinessException("لا يمكن إضافة صنف لكاتيجوري معطّلة");

            // تحقق: اسم الصنف فريد جوا نفس الكاتيجوري (uq_products_category_name)
            const string checkNameSql = @"
                SELECT COUNT(1) FROM Products
                WHERE CategoryId = @CategoryId AND Name = @Name;";

            var nameExists = await connection.ExecuteScalarAsync<int>(
                checkNameSql, new { request.CategoryId, request.Name });

            if (nameExists > 0)
                throw new DuplicateResourceException(
                    $"يوجد صنف بنفس الاسم '{request.Name}' مسبقاً بهذه الكاتيجوري");

            const string insertSql = @"
                INSERT INTO Products
                    (Name, CategoryId, SellBy, PiecesPerPackage, PricePerPiece, PricePerPackage,
                     StockInPieces, IsActive, CreatedAt, CreatedByUserId)
                VALUES
                    (@Name, @CategoryId, @SellBy, @PiecesPerPackage, @PricePerPiece, @PricePerPackage,
                     @StockInPieces, TRUE, UTC_TIMESTAMP(6), @CreatedByUserId);
                SELECT LAST_INSERT_ID();";

            var newId = await connection.ExecuteScalarAsync<int>(insertSql, new
            {
                request.Name,
                request.CategoryId,
                SellBy = (int)request.SellBy,
                request.PiecesPerPackage,
                request.PricePerPiece,
                request.PricePerPackage,
                request.StockInPieces,
                request.CreatedByUserId
            });

            return newId;
        }
    }
}