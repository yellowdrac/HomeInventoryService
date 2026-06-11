using FluentValidation.TestHelper;
using HomeInventory.Application.Dashboard.Queries.GetDashboardSummary;
using Xunit;

namespace HomeInventory.Application.UnitTests.Dashboard;

public class GetDashboardSummaryQueryValidatorTests
{
    private readonly GetDashboardSummaryQueryValidator _validator = new();

    [Fact]
    public void Valid_query_passes()
    {
        var result = _validator.TestValidate(
            new GetDashboardSummaryQuery(WithinDays: 7, RecentMovementsCount: 5));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Negative_within_days_fails()
    {
        var result = _validator.TestValidate(new GetDashboardSummaryQuery(WithinDays: -1));

        result.ShouldHaveValidationErrorFor(x => x.WithinDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Recent_movements_count_out_of_range_fails(int count)
    {
        var result = _validator.TestValidate(
            new GetDashboardSummaryQuery(RecentMovementsCount: count));

        result.ShouldHaveValidationErrorFor(x => x.RecentMovementsCount);
    }
}
