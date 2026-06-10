using FluentValidation;

namespace HomeInventory.Application.Items.Queries.SearchInventory;

public sealed class SearchInventoryQueryValidator : AbstractValidator<SearchInventoryQuery>
{
    /// <summary>Minimum length of a search term, to avoid returning the whole inventory.</summary>
    public const int MinQueryLength = 2;

    public SearchInventoryQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MinimumLength(MinQueryLength);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
