using FluentValidation.TestHelper;
using HomeInventory.Application.Items.Queries.GetItems;
using Xunit;

namespace HomeInventory.Application.UnitTests.Items;

public class GetItemsQueryValidatorTests
{
    private readonly GetItemsQueryValidator _validator = new();

    [Fact]
    public void Valid_query_passes()
    {
        var result = _validator.TestValidate(new GetItemsQuery(Page: 1, PageSize: 20));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Page_below_one_fails()
    {
        var result = _validator.TestValidate(new GetItemsQuery(Page: 0, PageSize: 20));

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Page_size_out_of_range_fails(int pageSize)
    {
        var result = _validator.TestValidate(new GetItemsQuery(Page: 1, PageSize: pageSize));

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}
