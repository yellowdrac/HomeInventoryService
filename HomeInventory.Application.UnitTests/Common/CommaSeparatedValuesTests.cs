using FluentAssertions;
using HomeInventory.Application.Common.Text;
using Xunit;

namespace HomeInventory.Application.UnitTests.Common;

public class CommaSeparatedValuesTests
{
    [Fact]
    public void Splits_on_commas()
    {
        CommaSeparatedValues.Parse("https://a.com,https://b.com")
            .Should().Equal("https://a.com", "https://b.com");
    }

    [Fact]
    public void Trims_surrounding_whitespace_on_each_entry()
    {
        CommaSeparatedValues.Parse("  https://a.com ,\thttps://b.com  ")
            .Should().Equal("https://a.com", "https://b.com");
    }

    [Fact]
    public void Drops_empty_and_whitespace_only_entries()
    {
        CommaSeparatedValues.Parse("https://a.com, ,,  ,https://b.com,")
            .Should().Equal("https://a.com", "https://b.com");
    }

    [Fact]
    public void Returns_single_entry_when_there_are_no_commas()
    {
        CommaSeparatedValues.Parse("http://localhost:3000")
            .Should().Equal("http://localhost:3000");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" , ,, ")]
    public void Returns_empty_for_null_empty_or_blank_input(string? value)
    {
        CommaSeparatedValues.Parse(value).Should().BeEmpty();
    }
}
