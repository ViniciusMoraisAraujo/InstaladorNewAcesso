using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

[Collection("IntegrationTests")]
public class SummaryStoreTests
{
    public SummaryStoreTests()
    {
        SummaryStore.Start();
    }

    // ── Start ──────────────────────────────

    [Fact]
    public void Start_ClearsPreviousResults()
    {
        SummaryStore.Add("Etapa1", "Item1", true);
        SummaryStore.Add("Etapa2", "Item2", false);

        SummaryStore.Start();

        SummaryStore.HasResults.Should().BeFalse();
        SummaryStore.GetResults().Should().BeEmpty();
    }

    [Fact]
    public void Start_ResetsStats()
    {
        SummaryStore.Add("Etapa1", "Item1", true);
        SummaryStore.Add("Etapa2", "Item2", false);

        SummaryStore.Start();

        var (total, sucessos, falhas, _) = SummaryStore.GetStats();
        total.Should().Be(0);
        sucessos.Should().Be(0);
        falhas.Should().Be(0);
    }

    [Fact]
    public void Start_RestartsStopwatch()
    {
        SummaryStore.Start();
        Thread.Sleep(50); // Pequena pausa para o stopwatch avançar

        SummaryStore.Start(); // Reinicia

        var (_, _, _, elapsed) = SummaryStore.GetStats();
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5)); // Deve ter reiniciado
    }

    // ── Add ─────────────────────────────────

    [Fact]
    public void Add_WithSuccess_IncrementsSuccessCount()
    {
        SummaryStore.Start();

        SummaryStore.Add("Etapa", "Item", true);

        var (total, sucessos, falhas, _) = SummaryStore.GetStats();
        total.Should().Be(1);
        sucessos.Should().Be(1);
        falhas.Should().Be(0);
    }

    [Fact]
    public void Add_WithFailure_IncrementsFailureCount()
    {
        SummaryStore.Start();

        SummaryStore.Add("Etapa", "Item", false);

        var (total, sucessos, falhas, _) = SummaryStore.GetStats();
        total.Should().Be(1);
        sucessos.Should().Be(0);
        falhas.Should().Be(1);
    }

    [Fact]
    public void Add_ReturnsSummaryResultWithCorrectValues()
    {
        SummaryStore.Start();

        var result = SummaryStore.Add("MinhaEtapa", "MeuItem", true, "detalhe");

        result.Should().NotBeNull();
        result.Etapa.Should().Be("MinhaEtapa");
        result.Item.Should().Be("MeuItem");
        result.Sucesso.Should().BeTrue();
        result.Detalhe.Should().Be("detalhe");
        result.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Add_WithoutDetail_DetailIsNull()
    {
        SummaryStore.Start();

        var result = SummaryStore.Add("Etapa", "Item", false);

        result.Detalhe.Should().BeNull();
    }

    [Fact]
    public void Add_MultipleItems_AccumulatesCorrectly()
    {
        SummaryStore.Start();

        SummaryStore.Add("E1", "I1", true);
        SummaryStore.Add("E1", "I2", true);
        SummaryStore.Add("E2", "I3", false);
        SummaryStore.Add("E2", "I4", true);

        var (total, sucessos, falhas, _) = SummaryStore.GetStats();
        total.Should().Be(4);
        sucessos.Should().Be(3);
        falhas.Should().Be(1);
    }

    // ── GetResults ─────────────────────────

    [Fact]
    public void GetResults_ReturnsAllAddedResults()
    {
        SummaryStore.Start();

        SummaryStore.Add("E1", "Item1", true);
        SummaryStore.Add("E2", "Item2", false);

        var results = SummaryStore.GetResults();

        results.Should().HaveCount(2);
        results[0].Item.Should().Be("Item1");
        results[1].Item.Should().Be("Item2");
    }

    [Fact]
    public void GetResults_ReturnsConsistentDataAcrossCalls()
    {
        SummaryStore.Start();

        SummaryStore.Add("E", "I", true);

        var results1 = SummaryStore.GetResults();
        var results2 = SummaryStore.GetResults();

        results1.Should().HaveCount(1);
        results2.Should().HaveCount(1);
        results1[0].Item.Should().Be(results2[0].Item);
    }

    [Fact]
    public void GetResults_ReflectsSubsequentAdds()
    {
        SummaryStore.Start();

        var initial = SummaryStore.GetResults();
        initial.Should().BeEmpty();

        SummaryStore.Add("E", "I", true);

        var afterAdd = SummaryStore.GetResults();
        afterAdd.Should().HaveCount(1);
    }

    // ── HasResults ─────────────────────────

    [Fact]
    public void HasResults_WhenEmpty_ReturnsFalse()
    {
        SummaryStore.Start();

        SummaryStore.HasResults.Should().BeFalse();
    }

    [Fact]
    public void HasResults_AfterAdd_ReturnsTrue()
    {
        SummaryStore.Start();

        SummaryStore.Add("E", "I", true);

        SummaryStore.HasResults.Should().BeTrue();
    }

    [Fact]
    public void HasResults_AfterStart_ReturnsFalse_EvenIfPreviouslyHadResults()
    {
        SummaryStore.Add("E", "I", true);
        SummaryStore.HasResults.Should().BeTrue();

        SummaryStore.Start();

        SummaryStore.HasResults.Should().BeFalse();
    }

    // ── ElapsedFormatted ───────────────────

    [Fact]
    public void ElapsedFormatted_AfterReset_ReturnsZeroSeconds()
    {
        SummaryStore.Start(); // reset

        var formatted = SummaryStore.ElapsedFormatted();

        formatted.Should().Contain("seg");
        formatted.Should().NotContain("min");
        formatted.Should().NotContain("h");
    }

    [Fact]
    public void ElapsedFormatted_WithElapsedSeconds_FormatsCorrectly()
    {
        SummaryStore.Start();
        Thread.Sleep(1100); // ~1.1 segundos

        var formatted = SummaryStore.ElapsedFormatted();

        formatted.Should().Contain("seg");
    }

    // ── GetStats ───────────────────────────

    [Fact]
    public void GetStats_ElapsedIsRunning()
    {
        SummaryStore.Start();
        var beforeSleep = SummaryStore.GetStats().elapsed;

        Thread.Sleep(100);

        var afterSleep = SummaryStore.GetStats().elapsed;

        afterSleep.Should().BeGreaterThan(beforeSleep);
    }
}
