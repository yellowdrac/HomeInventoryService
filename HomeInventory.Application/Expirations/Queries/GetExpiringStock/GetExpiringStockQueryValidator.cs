using FluentValidation;

namespace HomeInventory.Application.Expirations.Queries.GetExpiringStock;

public sealed class GetExpiringStockQueryValidator : AbstractValidator<GetExpiringStockQuery>
{
    public GetExpiringStockQueryValidator()
    {
        RuleFor(x => x.WithinDays)
            .GreaterThanOrEqualTo(0);
    }
}
