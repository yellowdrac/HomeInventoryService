using System.Text.RegularExpressions;
using FluentAssertions;
using HomeInventory.Application.Common.Identity;
using Xunit;

namespace HomeInventory.Application.UnitTests.Locations;

public class QrSlugGeneratorTests
{
    private readonly QrSlugGenerator _generator = new();

    [Fact]
    public void Slugifies_name_and_appends_a_random_suffix()
    {
        var slug = _generator.Generate("Mi Dormitorio");

        slug.Should().MatchRegex("^mi-dormitorio-[a-z0-9]{6}$");
    }

    [Fact]
    public void Strips_diacritics_and_collapses_separators()
    {
        var slug = _generator.Generate("  Salón / Café  ");

        slug.Should().MatchRegex("^salon-cafe-[a-z0-9]{6}$");
    }

    [Fact]
    public void Falls_back_to_suffix_only_when_name_has_no_alphanumerics()
    {
        var slug = _generator.Generate("***");

        slug.Should().MatchRegex("^[a-z0-9]{6}$");
    }

    [Fact]
    public void Successive_calls_produce_different_slugs()
    {
        var first = _generator.Generate("Kitchen");
        var second = _generator.Generate("Kitchen");

        first.Should().NotBe(second);
        Regex.IsMatch(first, "^kitchen-[a-z0-9]{6}$").Should().BeTrue();
    }
}
