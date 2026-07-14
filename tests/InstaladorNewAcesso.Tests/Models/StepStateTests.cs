using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;

namespace InstaladorNewAcesso.Tests.Models;

public class StepStateTests
{
    [Fact]
    public void Enum_ShouldHaveFiveMembers()
    {
        var names = Enum.GetNames<StepState>();
        names.Should().HaveCount(5);
    }

    [Fact]
    public void Pending_ShouldBeDefaultValue()
    {
        var defaultValue = default(StepState);
        defaultValue.Should().Be(StepState.Pending);
    }

    [Fact]
    public void Pending_ShouldHaveValueZero()
    {
        ((int)StepState.Pending).Should().Be(0);
    }

    [Fact]
    public void Running_ShouldHaveValueOne()
    {
        ((int)StepState.Running).Should().Be(1);
    }

    [Fact]
    public void Success_ShouldHaveValueTwo()
    {
        ((int)StepState.Success).Should().Be(2);
    }

    [Fact]
    public void Failed_ShouldHaveValueThree()
    {
        ((int)StepState.Failed).Should().Be(3);
    }

    [Fact]
    public void Warning_ShouldHaveValueFour()
    {
        ((int)StepState.Warning).Should().Be(4);
    }
}
