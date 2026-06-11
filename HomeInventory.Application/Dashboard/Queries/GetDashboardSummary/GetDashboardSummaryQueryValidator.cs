using FluentValidation;

namespace HomeInventory.Application.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryQueryValidator()
    {
        RuleFor(x => x.WithinDays)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.RecentMovementsCount)
            .InclusiveBetween(1, 100);
    }
}
