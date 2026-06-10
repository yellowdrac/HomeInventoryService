using FluentAssertions;
using HomeInventory.Application.Common.Text;
using Xunit;

namespace HomeInventory.Application.UnitTests.Common;

public class TrigramSimilarityTests
{
    [Fact]
    public void Identical_strings_are_fully_similar()
    {
        TrigramSimilarity.Compute("duracell", "duracell").Should().Be(1d);
    }

    [Fact]
    public void A_single_typo_stays_above_the_default_threshold()
    {
        // "duracel" (missing one 'l') must still be recognised as "duracell".
        var similarity = TrigramSimilarity.Compute("duracell", "duracel");

        similarity.Should().BeGreaterThanOrEqualTo(TrigramSimilarity.DefaultThreshold);
    }

    [Fact]
    public void Unrelated_strings_are_below_the_default_threshold()
    {
        var similarity = TrigramSimilarity.Compute("duracell", "hammer");

        similarity.Should().BeLessThan(TrigramSimilarity.DefaultThreshold);
    }

    [Fact]
    public void Is_symmetric()
    {
        TrigramSimilarity.Compute("platano", "plantano")
            .Should().Be(TrigramSimilarity.Compute("plantano", "platano"));
    }
}
