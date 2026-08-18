using Dapper;
using MediatR;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Customers.Update
{
    public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, Unit>
    {
        private readonly IPosDatabase _database;

        public UpdateCustomerHandler(IPosDatabase database)
        {
            _database = database;
        }

        public async Task<Unit> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            using var connection = _database.Open();

            const string checkExistsSql = "SELECT COUNT(1) FROM Customers WHERE Id = @Id;";
            var exists = await connection.ExecuteScalarAsync<int>(checkExistsSql, new { request.Id });

            if (exists == 0)
                throw new NotFoundException($"لا يوجد زبون بالمعرف {request.Id}");

            const string updateSql = @"
                UPDATE Customers
                SET Name = @Name,
                    Phone = @Phone,
                    Notes = @Notes,
                    UpdatedAt = UTC_TIMESTAMP(6),
                    UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id;";

            await connection.ExecuteAsync(updateSql, new
            {
                request.Id,
                request.Name,
                request.Phone,
                request.Notes,
                request.UpdatedByUserId
            });

            return Unit.Value;
        }
    }
}
