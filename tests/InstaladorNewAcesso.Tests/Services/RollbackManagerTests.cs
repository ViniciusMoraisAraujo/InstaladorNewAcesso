using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Core.Services;
using NSubstitute;
using Xunit;

namespace InstaladorNewAcesso.Tests.Services;

public class RollbackManagerTests
{
    private readonly IUIService _ui = Substitute.For<IUIService>();

    [Fact]
    public void Push_IncreasesCount_AndHasActionsIsTrue()
    {
        // Arrange
        var manager = new RollbackManager(_ui);

        // Act
        manager.Push(() => Task.CompletedTask);

        // Assert
        manager.Count.Should().Be(1);
        manager.HasActions.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteRollbackAsync_ExecutesActionsInLifoOrder()
    {
        // Arrange
        var manager = new RollbackManager(_ui);
        var executionOrder = new List<int>();

        manager.Push(() =>
        {
            executionOrder.Add(1);
            return Task.CompletedTask;
        });

        manager.Push(() =>
        {
            executionOrder.Add(2);
            return Task.CompletedTask;
        });

        manager.Push(() =>
        {
            executionOrder.Add(3);
            return Task.CompletedTask;
        });

        // Act
        await manager.ExecuteRollbackAsync();

        // Assert
        executionOrder.Should().Equal(3, 2, 1);
        manager.Count.Should().Be(0);
        manager.HasActions.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteRollbackAsync_WhenActionThrows_ContinuesExecutingRemainingActions()
    {
        // Arrange
        var manager = new RollbackManager(_ui);
        var remainingExecuted = false;

        manager.Push(() =>
        {
            remainingExecuted = true;
            return Task.CompletedTask;
        });

        manager.Push(() => throw new InvalidOperationException("Falha simulada na ação de rollback"));

        // Act
        var act = async () => await manager.ExecuteRollbackAsync();

        // Assert
        await act.Should().NotThrowAsync();
        remainingExecuted.Should().BeTrue();
        manager.Count.Should().Be(0);
        _ui.Received().WriteWarning(Arg.Is<string>(s => s.Contains("Falha simulada")));
    }

    [Fact]
    public void Clear_EmptiesAllRegisteredActions()
    {
        // Arrange
        var manager = new RollbackManager(_ui);
        manager.Push(() => Task.CompletedTask);
        manager.Push(() => Task.CompletedTask);

        // Act
        manager.Clear();

        // Assert
        manager.Count.Should().Be(0);
        manager.HasActions.Should().BeFalse();
    }
}
