using Dapper;
using MediatR;
using MySqlConnector;
using Pos.Api.Exceptions;
using Pos.Api.Interfaces;

namespace Pos.Api.Features.Shifts;

internal static class ShiftReader
{
    internal const string SelectSql = """
        SELECT
            s.Id,
            s.UserId,
            u.FullName AS EmployeeName,
            s.LoginAt,
            s.LogoutAt,
            TIMESTAMPDIFF(
                MINUTE,
                s.LoginAt,
                COALESCE(s.LogoutAt, UTC_TIMESTAMP())
            ) AS DurationMinutes,
            (
                SELECT COUNT(*)
                FROM Invoices i
                WHERE i.CashierId = s.UserId
                  AND i.CreatedAt >= s.LoginAt
                  AND i.CreatedAt < COALESCE(
                      s.LogoutAt,
                      UTC_TIMESTAMP()
                  )
            ) AS InvoiceCount,
            COALESCE((
                SELECT SUM(i.Total)
                FROM Invoices i
                WHERE i.CashierId = s.UserId
                  AND i.CreatedAt >= s.LoginAt
                  AND i.CreatedAt < COALESCE(
                      s.LogoutAt,
                      UTC_TIMESTAMP()
                  )
            ), 0) AS SalesTotal
        FROM Shifts s
        JOIN Users u ON u.Id = s.UserId
        """;
}

public sealed class CheckInHandler
    : IRequestHandler<CheckInCommand, ShiftDto>
{
    private readonly IPosDatabase _database;

    public CheckInHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<ShiftDto> Handle(
        CheckInCommand request,
        CancellationToken ct)
    {
        using var connection = _database.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var user = await connection.QuerySingleOrDefaultAsync<UserState>(
                new CommandDefinition(
                    """
                    SELECT Id, IsActive
                    FROM Users
                    WHERE Id = @UserId
                    FOR UPDATE;
                    """,
                    new { request.UserId },
                    transaction,
                    cancellationToken: ct));

            if (user is null)
                throw new NotFoundException(
                    $"User {request.UserId} was not found.");

            if (!user.IsActive)
                throw new BusinessException(
                    "Inactive users cannot start a shift.");

            var existingShiftId =
                await connection.QuerySingleOrDefaultAsync<int?>(
                    new CommandDefinition(
                        """
                        SELECT Id
                        FROM Shifts
                        WHERE UserId = @UserId
                          AND LogoutAt IS NULL
                        LIMIT 1
                        FOR UPDATE;
                        """,
                        new { request.UserId },
                        transaction,
                        cancellationToken: ct));

            if (existingShiftId.HasValue)
                throw new BusinessException(
                    "The user already has an open shift.");

            int shiftId;

            try
            {
                shiftId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO Shifts (UserId, LoginAt, LogoutAt)
                        VALUES (@UserId, UTC_TIMESTAMP(), NULL);

                        SELECT LAST_INSERT_ID();
                        """,
                        new { request.UserId },
                        transaction,
                        cancellationToken: ct));
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                throw new BusinessException(
                    "The user already has an open shift.");
            }

            transaction.Commit();

            return await GetShiftById(
                connection,
                shiftId,
                ct);
        }
        catch
        {
            if (transaction.Connection is not null)
                transaction.Rollback();

            throw;
        }
    }

    private static async Task<ShiftDto> GetShiftById(
        System.Data.IDbConnection connection,
        int shiftId,
        CancellationToken ct)
    {
        return await connection.QuerySingleAsync<ShiftDto>(
            new CommandDefinition(
                $"""
                {ShiftReader.SelectSql}
                WHERE s.Id = @ShiftId;
                """,
                new { ShiftId = shiftId },
                cancellationToken: ct));
    }

    private sealed class UserState
    {
        public int Id { get; init; }
        public bool IsActive { get; init; }
    }
}

public sealed class CheckOutHandler
    : IRequestHandler<CheckOutCommand, ShiftDto>
{
    private readonly IPosDatabase _database;

    public CheckOutHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<ShiftDto> Handle(
        CheckOutCommand request,
        CancellationToken ct)
    {
        using var connection = _database.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var shiftId =
                await connection.QuerySingleOrDefaultAsync<int?>(
                    new CommandDefinition(
                        """
                        SELECT Id
                        FROM Shifts
                        WHERE UserId = @UserId
                          AND LogoutAt IS NULL
                        ORDER BY LoginAt DESC
                        LIMIT 1
                        FOR UPDATE;
                        """,
                        new { request.UserId },
                        transaction,
                        cancellationToken: ct));

            if (!shiftId.HasValue)
                throw new BusinessException(
                    "The user does not have an open shift.");

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE Shifts
                    SET LogoutAt = UTC_TIMESTAMP()
                    WHERE Id = @ShiftId
                      AND LogoutAt IS NULL;
                    """,
                    new { ShiftId = shiftId.Value },
                    transaction,
                    cancellationToken: ct));

            transaction.Commit();

            return await connection.QuerySingleAsync<ShiftDto>(
                new CommandDefinition(
                    $"""
                    {ShiftReader.SelectSql}
                    WHERE s.Id = @ShiftId;
                    """,
                    new { ShiftId = shiftId.Value },
                    cancellationToken: ct));
        }
        catch
        {
            if (transaction.Connection is not null)
                transaction.Rollback();

            throw;
        }
    }
}

public sealed class GetCurrentShiftHandler
    : IRequestHandler<GetCurrentShiftQuery, ShiftDto?>
{
    private readonly IPosDatabase _database;

    public GetCurrentShiftHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<ShiftDto?> Handle(
        GetCurrentShiftQuery request,
        CancellationToken ct)
    {
        using var connection = _database.Open();

        return await connection.QuerySingleOrDefaultAsync<ShiftDto>(
            new CommandDefinition(
                $"""
                {ShiftReader.SelectSql}
                WHERE s.UserId = @UserId
                  AND s.LogoutAt IS NULL
                ORDER BY s.LoginAt DESC
                LIMIT 1;
                """,
                new { request.UserId },
                cancellationToken: ct));
    }
}

public sealed class GetMyShiftsHandler
    : IRequestHandler<GetMyShiftsQuery, List<ShiftDto>>
{
    private readonly IPosDatabase _database;

    public GetMyShiftsHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<List<ShiftDto>> Handle(
        GetMyShiftsQuery request,
        CancellationToken ct)
    {
        if (request.From.HasValue &&
            request.To.HasValue &&
            request.From.Value.Date > request.To.Value.Date)
        {
            throw new Pos.Api.Exceptions.ValidationException(
                "'from' cannot be after 'to'.");
        }

        var from = request.From?.Date;
        var toExclusive = request.To?.Date.AddDays(1);

        using var connection = _database.Open();

        var shifts = await connection.QueryAsync<ShiftDto>(
            new CommandDefinition(
                $"""
                {ShiftReader.SelectSql}
                WHERE s.UserId = @UserId
                  AND (@From IS NULL OR s.LoginAt >= @From)
                  AND (@ToExclusive IS NULL
                       OR s.LoginAt < @ToExclusive)
                ORDER BY s.LoginAt DESC;
                """,
                new
                {
                    request.UserId,
                    From = from,
                    ToExclusive = toExclusive
                },
                cancellationToken: ct));

        return shifts.ToList();
    }
}

public sealed class GetShiftReportHandler
    : IRequestHandler<GetShiftReportQuery, ShiftReportResponse>
{
    private readonly IPosDatabase _database;

    public GetShiftReportHandler(IPosDatabase database)
    {
        _database = database;
    }

    public async Task<ShiftReportResponse> Handle(
        GetShiftReportQuery request,
        CancellationToken ct)
    {
        if (request.From.Date > request.To.Date)
        {
            throw new Pos.Api.Exceptions.ValidationException(
                "'from' cannot be after 'to'.");
        }

        var from = request.From.Date;
        var toExclusive = request.To.Date.AddDays(1);

        using var connection = _database.Open();

        var shifts = (
            await connection.QueryAsync<ShiftDto>(
                new CommandDefinition(
                    $"""
                    {ShiftReader.SelectSql}
                    WHERE s.LoginAt >= @From
                      AND s.LoginAt < @ToExclusive
                      AND (@UserId IS NULL
                           OR s.UserId = @UserId)
                    ORDER BY s.LoginAt DESC;
                    """,
                    new
                    {
                        From = from,
                        ToExclusive = toExclusive,
                        request.UserId
                    },
                    cancellationToken: ct)))
            .ToList();

        return new ShiftReportResponse
        {
            From = from,
            To = request.To.Date,
            TotalShifts = shifts.Count,
            TotalMinutes = shifts.Sum(x => x.DurationMinutes),
            TotalInvoices = shifts.Sum(x => x.InvoiceCount),
            TotalSales = shifts.Sum(x => x.SalesTotal),
            Shifts = shifts
        };
    }
}