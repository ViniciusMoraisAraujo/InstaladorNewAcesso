using FluentAssertions;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class IniHelperBaseTests
{
    // ── Add new key ────────────────────────

    [Fact]
    public void SetIniKey_OnEmptyList_AddsKey_ReturnsTrue()
    {
        var lines = new List<string>();

        var modified = IniHelperBase.SetIniKey(lines, "MyKey", "MyValue", useQuotes: true);

        modified.Should().BeTrue();
        lines.Should().Contain(l => l.Contains("MyKey") && l.Contains("MyValue"));
        lines.Should().Contain(l => l.Contains("'MyValue'"));
    }

    [Fact]
    public void SetIniKey_OnEmptyList_WithoutQuotes()
    {
        var lines = new List<string>();

        IniHelperBase.SetIniKey(lines, "MyKey", "MyValue", useQuotes: false);

        lines.Should().Contain(l => l.Contains("MyKey") && l.Contains("MyValue") && !l.Contains("'"));
    }

    // ── Update existing key ────────────────

    [Fact]
    public void SetIniKey_WhenKeyExists_UpdatesValue_ReturnsTrue()
    {
        var lines = new List<string> { "ExistingKey = OldValue" };

        var modified = IniHelperBase.SetIniKey(lines, "ExistingKey", "NewValue");

        modified.Should().BeTrue();
        lines.Should().Contain(l => l.Contains("NewValue"));
        lines.Should().NotContain(l => l.Contains("OldValue"));
    }

    [Fact]
    public void SetIniKey_WhenKeyExists_SameValue_ReturnsFalse()
    {
        var lines = new List<string> { "MyKey = SameValue" };

        var modified = IniHelperBase.SetIniKey(lines, "MyKey", "SameValue");

        modified.Should().BeFalse();
        lines.Count.Should().Be(1); // não adiciona linha extra
    }

    // ── Case insensitive ───────────────────

    [Fact]
    public void SetIniKey_KeyMatch_IsCaseInsensitive()
    {
        var lines = new List<string> { "MYKEY = Old" };

        IniHelperBase.SetIniKey(lines, "mykey", "New");

        lines.Should().Contain(l => l.Contains("New"));
    }

    // ── Comments ───────────────────────────

    [Fact]
    public void SetIniKey_SkipsCommentLines_Semicolon()
    {
        var lines = new List<string> { "; MyKey = Commented", "MyKey = Real" };

        IniHelperBase.SetIniKey(lines, "MyKey", "Updated");

        lines[0].Should().Be("; MyKey = Commented"); // não altera comentário
        lines.Should().Contain(l => l.Contains("Updated"));
    }

    [Fact]
    public void SetIniKey_SkipsCommentLines_Hash()
    {
        var lines = new List<string> { "# MyKey = Commented", "MyKey = Real" };

        IniHelperBase.SetIniKey(lines, "MyKey", "Updated");

        lines[0].Should().Be("# MyKey = Commented"); // não altera comentário
    }

    // ── Quoted values ──────────────────────

    [Fact]
    public void SetIniKey_StripsSingleQuotes_WhenReading()
    {
        var lines = new List<string> { "MyKey = 'OldValue'" };

        IniHelperBase.SetIniKey(lines, "MyKey", "NewValue");

        lines.Should().Contain(l => l.Contains("'NewValue'"));
    }

    [Fact]
    public void SetIniKey_StripsDoubleQuotes_WhenReading()
    {
        var lines = new List<string> { "MyKey = \"OldValue\"" };

        IniHelperBase.SetIniKey(lines, "MyKey", "NewValue");

        lines.Should().Contain(l => l.Contains("'NewValue'"));
    }

    // ── Whitespace handling ────────────────

    [Fact]
    public void SetIniKey_HandlesSpacesAroundEquals()
    {
        var lines = new List<string> { "  MyKey   =   SomeValue  " };

        IniHelperBase.SetIniKey(lines, "MyKey", "Updated");

        lines.Should().Contain(l => l.Contains("Updated") && l.Contains("="));
    }

    // ── Multiple lines ─────────────────────

    [Fact]
    public void SetIniKey_OnlyFirstMatchIsUpdated()
    {
        var lines = new List<string> { "MyKey = First", "MyKey = Second" };

        IniHelperBase.SetIniKey(lines, "MyKey", "Updated");

        lines[0].Should().Contain("Updated"); // atualiza o primeiro
        lines[1].Should().Contain("Second");  // não mexe no segundo
    }
}
