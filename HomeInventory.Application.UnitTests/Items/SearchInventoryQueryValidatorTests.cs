using FluentValidation.TestHelper;
using HomeInventory.Application.Items.Queries.SearchInventory;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class SearchInventoryQueryValidatorTests
{
    private readonly SearchInventoryQueryValidator _validator = new();

    [Fact]
    public void Valid_query_passes()
    {
        var result = _validator.TestValidate(new SearchInventoryQuery("milk", Page: 1, PageSize: 20));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public void Query_too_short_fails(string query)
    {
        var result = _validator.TestValidate(new SearchInventoryQuery(query));

        result.ShouldHaveValidationErrorFor(x => x.Query);
    }

    [Fact]
    public void Page_below_one_fails()
    {
        var result = _validator.TestValidate(new SearchInventoryQuery("milk", Page: 0));

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Page_size_out_of_range_fails(int pageSize)
    {
        var result = _validator.TestValidate(new SearchInventoryQuery("milk", PageSize: pageSize));

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}
