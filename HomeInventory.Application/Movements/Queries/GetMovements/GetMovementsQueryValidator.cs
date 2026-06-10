using FluentValidation;

namespace HomeInventory.Application.Movements.Queries.GetMovements;

public sealed class GetMovementsQueryValidator : AbstractValidator<GetMovementsQuery>
{
    public GetMovementsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}
