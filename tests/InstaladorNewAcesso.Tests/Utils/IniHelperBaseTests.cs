using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

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

    // ── Section-aware ─────────────────────

    [Fact]
    public void SetIniKey_WithSection_UpdatesKeyInSection()
    {
        var lines = new List<string>
        {
            "[MySection]",
            "MyKey = OldValue",
            "[Other]",
            "MyKey = OtherValue"
        };

        IniHelperBase.SetIniKey(lines, "MyKey", "NewValue", section: "MySection");

        lines.Should().Contain(l => l.Contains("NewValue"));
        lines.Should().Contain(l => l.Contains("OtherValue")); // other section untouched
    }

    [Fact]
    public void SetIniKey_WithSection_CaseInsensitiveSectionMatch()
    {
        var lines = new List<string>
        {
            "[mysection]",
            "MyKey = OldValue"
        };

        IniHelperBase.SetIniKey(lines, "MyKey", "NewValue", section: "MySection");

        lines.Should().Contain(l => l.Contains("NewValue"));
    }

    [Fact]
    public void SetIniKey_WithSection_DoesNotTouchKeyOutsideSection()
    {
        var lines = new List<string>
        {
            "MyKey = GlobalValue",
            "[Target]",
            "OtherKey = Something"
        };

        var modified = IniHelperBase.SetIniKey(lines, "MyKey", "NewValue", section: "Target");

        modified.Should().BeFalse();
        lines.Should().Contain(l => l.Contains("GlobalValue"));
    }

    [Fact]
    public void SetIniKey_WithSection_AddsKeyIfMissingInSection()
    {
        var lines = new List<string>
        {
            "[MySection]",
            "ExistingKey = OK"
        };

        var modified = IniHelperBase.SetIniKey(lines, "NewKey", "Value", section: "MySection");

        modified.Should().BeTrue();
        lines.Should().Contain(l => l.Contains("NewKey") && l.Contains("Value"));
    }

    [Fact]
    public void SetIniKey_WithSection_DoesNotAddKeyIfSectionMissing()
    {
        var lines = new List<string>
        {
            "[OtherSection]",
            "ExistingKey = OK"
        };

        var modified = IniHelperBase.SetIniKey(lines, "MyKey", "Value", section: "MySection");

        modified.Should().BeFalse();
        lines.Should().NotContain(l => l.Contains("MyKey"));
    }

    [Fact]
    public void SetIniKey_WithoutSection_IgnoresSections_FlatBehavior()
    {
        // Backward-compatible: no section parameter = original flat behavior
        var lines = new List<string>
        {
            "[SomeSection]",
            "MyKey = OldValue"
        };

        IniHelperBase.SetIniKey(lines, "MyKey", "NewValue");

        lines.Should().Contain(l => l.Contains("NewValue"));
    }
}
