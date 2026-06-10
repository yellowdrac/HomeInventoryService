using FluentValidation.TestHelper;
using HomeInventory.Application.Expirations.Queries.GetExpiringStock;
using HomeInventory.Application.Expirations.Queries.GetKitchenOverview;
using Xunit;

namespace HomeInventory.Application.UnitTests.Expirations;

public class ExpirationQueryValidatorTests
{
    private readonly GetExpiringStockQueryValidator _expiringValidator = new();
    private readonly GetKitchenOverviewQueryValidator _overviewValidator = new();

    [Fact]
    public void Expiring_query_with_non_negative_within_days_passes()
    {
        _expiringValidator.TestValidate(new GetExpiringStockQuery(WithinDays: 0))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Expiring_query_with_negative_within_days_fails()
    {
        _expiringValidator.TestValidate(new GetExpiringStockQuery(WithinDays: -1))
            .ShouldHaveValidationErrorFor(x => x.WithinDays);
    }

    [Fact]
    public void Overview_query_with_negative_within_days_fails()
    {
        _overviewValidator.TestValidate(new GetKitchenOverviewQuery(WithinDays: -1))
            .ShouldHaveValidationErrorFor(x => x.WithinDays);
    }
}
