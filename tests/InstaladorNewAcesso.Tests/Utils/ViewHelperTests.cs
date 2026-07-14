using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class ViewHelperTests
{
    // ── ParseIndices ────────────────────────

    [Fact]
    public void ParseIndices_NullInput_ReturnsEmptyList()
    {
        var result = ViewHelper.ParseIndices(null, 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseIndices_EmptyInput_ReturnsEmptyList()
    {
        var result = ViewHelper.ParseIndices("", 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseIndices_WhitespaceInput_ReturnsEmptyList()
    {
        var result = ViewHelper.ParseIndices("   ", 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseIndices_SingleValidIndex_ReturnsZeroBased()
    {
        var result = ViewHelper.ParseIndices("1", 5);

        result.Should().ContainSingle().Which.Should().Be(0);
    }

    [Fact]
    public void ParseIndices_MultipleValidIndices_ReturnsZeroBased()
    {
        var result = ViewHelper.ParseIndices("1,3,5", 5);

        result.Should().BeEquivalentTo([0, 2, 4], options => options.WithStrictOrdering());
    }

    [Fact]
    public void ParseIndices_IndexOutOfRange_IsExcluded()
    {
        var result = ViewHelper.ParseIndices("1,10", 5);

        result.Should().ContainSingle().Which.Should().Be(0);
    }

    [Fact]
    public void ParseIndices_IndexZero_IsExcluded()
    {
        var result = ViewHelper.ParseIndices("0,1", 5);

        result.Should().ContainSingle().Which.Should().Be(0);
    }

    [Fact]
    public void ParseIndices_NonNumericInput_IsExcluded()
    {
        var result = ViewHelper.ParseIndices("abc,1,xyz", 5);

        result.Should().ContainSingle().Which.Should().Be(0);
    }

    [Fact]
    public void ParseIndices_DuplicateIndices_ReturnsDistinct()
    {
        var result = ViewHelper.ParseIndices("1,1,3,3", 5);

        result.Should().BeEquivalentTo([0, 2], options => options.WithStrictOrdering());
    }

    [Fact]
    public void ParseIndices_AllIndicesValid_ReturnsAll()
    {
        var result = ViewHelper.ParseIndices("1,2,3", 3);

        result.Should().BeEquivalentTo([0, 1, 2], options => options.WithStrictOrdering());
    }

    [Fact]
    public void ParseIndices_ReturnsInAscendingOrder()
    {
        var result = ViewHelper.ParseIndices("3,1,2", 5);

        result.Should().BeEquivalentTo([0, 1, 2], options => options.WithStrictOrdering());
    }

    [Fact]
    public void ParseIndices_HandlesSpacesAroundNumbers()
    {
        var result = ViewHelper.ParseIndices(" 1 , 3 , 5 ", 5);

        result.Should().BeEquivalentTo([0, 2, 4], options => options.WithStrictOrdering());
    }

    [Fact]
    public void ParseIndices_MaxIsOne_OnlyIndexOneIsValid()
    {
        var result = ViewHelper.ParseIndices("1,2", 1);

        result.Should().ContainSingle().Which.Should().Be(0);
    }
}
