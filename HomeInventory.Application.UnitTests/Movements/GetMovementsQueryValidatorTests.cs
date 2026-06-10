using FluentValidation.TestHelper;
using HomeInventory.Application.Movements.Queries.GetMovements;
using Xunit;

namespace HomeInventory.Application.UnitTests.Movements;

public class GetMovementsQueryValidatorTests
{
    private readonly GetMovementsQueryValidator _validator = new();

    [Fact]
    public void Valid_query_passes()
    {
        var result = _validator.TestValidate(new GetMovementsQuery(Page: 1, PageSize: 20));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_below_one_fails(int page)
    {
        var result = _validator.TestValidate(new GetMovementsQuery(Page: page));

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Page_size_out_of_range_fails(int pageSize)
    {
        var result = _validator.TestValidate(new GetMovementsQuery(PageSize: pageSize));

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Date_to_before_date_from_fails()
    {
        var result = _validator.TestValidate(
            new GetMovementsQuery(
                DateFrom: new DateTime(2026, 6, 1), DateTo: new DateTime(2026, 1, 1)));

        result.ShouldHaveValidationErrorFor(x => x.DateTo);
    }
}
