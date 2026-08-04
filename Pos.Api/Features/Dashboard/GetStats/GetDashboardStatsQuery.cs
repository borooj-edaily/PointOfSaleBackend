using MediatR;

namespace Pos.Api.Features.Dashboard.GetStats
{
    public class GetDashboardStatsQuery : IRequest<DashboardStatsDto>
    {
    }
}