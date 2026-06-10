using FluentValidation;

namespace HomeInventory.Application.Expirations.Queries.GetKitchenOverview;

public sealed class GetKitchenOverviewQueryValidator : AbstractValidator<GetKitchenOverviewQuery>
{
    public GetKitchenOverviewQueryValidator()
    {
        RuleFor(x => x.WithinDays)
            .GreaterThanOrEqualTo(0);
    }
}
