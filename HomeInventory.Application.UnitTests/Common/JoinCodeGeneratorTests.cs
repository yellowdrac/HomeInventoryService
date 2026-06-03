using System.Text.RegularExpressions;
using FluentAssertions;
using HomeInventory.Application.Common.Identity;
using Xunit;

namespace HomeInventory.Application.UnitTests.Common;

public class JoinCodeGeneratorTests
{
    private readonly JoinCodeGenerator _generator = new();

    [Fact]
    public void Generate_produces_eight_unambiguous_uppercase_characters()
    {
        var codes = Enumerable.Range(0, 500).Select(_ => _generator.Generate()).ToList();

        codes.Should().OnlyContain(code => Regex.IsMatch(code, "^[A-HJ-NP-Z2-9]{8}$"));
    }

    [Fact]
    public void Generate_is_effectively_unique_across_many_calls()
    {
        var codes = Enumerable.Range(0, 1000).Select(_ => _generator.Generate()).ToList();

        // With ~32^8 possibilities, 1000 codes should not realistically collide.
        codes.Distinct().Count().Should().Be(codes.Count);
    }
}
