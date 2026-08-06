using MediatR;

namespace Pos.Api.Features.Shifts;

public sealed class ShiftDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime LoginAt { get; init; }
    public DateTime? LogoutAt { get; init; }
    public int DurationMinutes { get; init; }
    public int InvoiceCount { get; init; }
    public decimal SalesTotal { get; init; }
    public bool IsOpen => LogoutAt is null;
}

public sealed class ShiftReportResponse
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public int TotalShifts { get; init; }
    public int TotalMinutes { get; init; }
    public int TotalInvoices { get; init; }
    public decimal TotalSales { get; init; }
    public List<ShiftDto> Shifts { get; init; } = new();
}

public sealed record CheckInCommand(int UserId) : IRequest<ShiftDto>;

public sealed record CheckOutCommand(int UserId) : IRequest<ShiftDto>;

public sealed record GetCurrentShiftQuery(int UserId)
    : IRequest<ShiftDto?>;

public sealed record GetMyShiftsQuery(
    int UserId,
    DateTime? From,
    DateTime? To) : IRequest<List<ShiftDto>>;

public sealed record GetShiftReportQuery(
    DateTime From,
    DateTime To,
    int? UserId) : IRequest<ShiftReportResponse>;