using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;

namespace InstaladorNewAcesso.Tests.Models;

public class StepStatusTests
{
    [Fact]
    public void DefaultValues_ShouldBePending()
    {
        var status = new StepStatus();

        status.StepName.Should().BeEmpty();
        status.Description.Should().BeEmpty();
        status.State.Should().Be(StepState.Pending);
        status.ErrorDetail.Should().BeNull();
        status.StartedAt.Should().BeNull();
        status.CompletedAt.Should().BeNull();
        status.Duration.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var status = new StepStatus
        {
            StepName = "Instalar MSI",
            Description = "Instalação do Controle de Acesso"
        };

        status.StepName.Should().Be("Instalar MSI");
        status.Description.Should().Be("Instalação do Controle de Acesso");
    }

    [Fact]
    public void Start_ShouldSetStateToRunningAndRecordStartedAt()
    {
        var status = new StepStatus();
        var before = DateTime.Now;

        status.Start();

        status.State.Should().Be(StepState.Running);
        status.StartedAt.Should().NotBeNull();
        status.StartedAt!.Value.Should().BeOnOrAfter(before);
        status.StartedAt!.Value.Should().BeOnOrBefore(DateTime.Now);
        status.CompletedAt.Should().BeNull();
        status.Duration.Should().BeNull();
    }

    [Fact]
    public void Complete_ShouldSetStateToSuccessAndRecordCompletedAt()
    {
        var status = new StepStatus();
        status.Start();
        var before = DateTime.Now;

        status.Complete();

        status.State.Should().Be(StepState.Success);
        status.CompletedAt.Should().NotBeNull();
        status.CompletedAt!.Value.Should().BeOnOrAfter(before);
        status.CompletedAt!.Value.Should().BeOnOrBefore(DateTime.Now);
    }

    [Fact]
    public void Complete_AfterStart_ShouldHavePositiveDuration()
    {
        var status = new StepStatus();
        status.Start();
        Thread.Sleep(1); // Garante pelo menos 1ms de diferença

        status.Complete();

        status.Duration.Should().NotBeNull();
        status.Duration!.Value.Should().BePositive();
    }

    [Fact]
    public void Fail_ShouldSetStateToFailedWithErrorDetail()
    {
        var status = new StepStatus();
        status.Start();

        status.Fail("Arquivo MSI não encontrado");

        status.State.Should().Be(StepState.Failed);
        status.ErrorDetail.Should().Be("Arquivo MSI não encontrado");
        status.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Fail_WithNullDetail_ShouldSetErrorDetailToNull()
    {
        var status = new StepStatus();
        status.Start();

        status.Fail();

        status.State.Should().Be(StepState.Failed);
        status.ErrorDetail.Should().BeNull();
        status.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Warn_ShouldSetStateToWarningWithDetail()
    {
        var status = new StepStatus();
        status.Start();

        status.Warn("Versão desatualizada");

        status.State.Should().Be(StepState.Warning);
        status.ErrorDetail.Should().Be("Versão desatualizada");
        status.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Warn_WithNullDetail_ShouldSetErrorDetailToNull()
    {
        var status = new StepStatus();
        status.Start();

        status.Warn();

        status.State.Should().Be(StepState.Warning);
        status.ErrorDetail.Should().BeNull();
    }

    [Fact]
    public void Duration_ShouldBeNull_WhenOnlyStarted()
    {
        var status = new StepStatus();
        status.Start();

        status.Duration.Should().BeNull();
    }

    [Fact]
    public void Duration_ShouldBeNull_WhenNotStarted()
    {
        var status = new StepStatus();

        status.Duration.Should().BeNull();
    }

    [Fact]
    public void Fail_WithoutStart_ShouldStillSetCompletedAt()
    {
        var status = new StepStatus();

        status.Fail("Erro sem start");

        status.State.Should().Be(StepState.Failed);
        status.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_WithoutStart_ShouldStillSetCompletedAt()
    {
        var status = new StepStatus();

        status.Complete();

        status.State.Should().Be(StepState.Success);
        status.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Warn_WithoutStart_ShouldStillSetCompletedAt()
    {
        var status = new StepStatus();

        status.Warn("Aviso sem start");

        status.State.Should().Be(StepState.Warning);
        status.CompletedAt.Should().NotBeNull();
    }
}
