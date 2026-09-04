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
        lines.Should().Contain(l => l.Contains("\'MyValue\'"));
    }

    [Fact]
    public void SetIniKey_OnEmptyList_WithoutQuotes()
    {
        var lines = new List<string>();

        IniHelperBase.SetIniKey(lines, "MyKey", "MyValue", useQuotes: false);

        lines.Should().Contain(l => l.Contains("MyKey") && l.Contains("MyValue") && !l.Contains("\'"));
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
        lines.Count.Should().Be(1); // nao adiciona linha extra
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

        lines[0].Should().Be("; MyKey = Commented"); // nao altera comentario
        lines.Should().Contain(l => l.Contains("Updated"));
    }

    [Fact]
    public void SetIniKey_SkipsCommentLines_Hash()
    {
        var lines = new List<string> { "# MyKey = Commented", "MyKey = Real" };

        IniHelperBase.SetIniKey(lines, "MyKey", "Updated");

        lines[0].Should().Be("# MyKey = Commented"); // nao altera comentario
    }

    // ── Quoted values ──────────────────────

    [Fact]
    public void SetIniKey_StripsSingleQuotes_WhenReading()
    {
        var lines = new List<string> { "MyKey = \'OldValue\'" };

        IniHelperBase.SetIniKey(lines, "MyKey", "NewValue");

        lines.Should().Contain(l => l.Contains("\'NewValue\'"));
    }

    [Fact]
    public void SetIniKey_StripsDoubleQuotes_WhenReading()
    {
        var lines = new List<string> { "MyKey = \"OldValue\"" };

        IniHelperBase.SetIniKey(lines, "MyKey", "NewValue");

        lines.Should().Contain(l => l.Contains("\'NewValue\'"));
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
        lines[1].Should().Contain("Second");  // nao mexe no segundo
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
    public void SetIniKey_WithSection_InsertsBeforeNextSection()
    {
        var lines = new List<string>
        {
            "[TargetSection]",
            "ExistingKey = 123",
            "[NextSection]",
            "OtherKey = 456"
        };

        var modified = IniHelperBase.SetIniKey(lines, "NewKey", "789", section: "TargetSection");

        modified.Should().BeTrue();
        // A nova chave deve estar antes de [NextSection]
        var targetIdx = lines.IndexOf("[TargetSection]");
        var newKeyIdx = lines.FindIndex(l => l.Contains("NewKey"));
        var nextSecIdx = lines.IndexOf("[NextSection]");

        newKeyIdx.Should().BeGreaterThan(targetIdx);
        newKeyIdx.Should().BeLessThan(nextSecIdx);
    }

    [Fact]
    public void SetIniKey_WithSection_CreatesSectionIfMissing()
    {
        var lines = new List<string>
        {
            "[OtherSection]",
            "ExistingKey = OK"
        };

        var modified = IniHelperBase.SetIniKey(lines, "MyKey", "Value", section: "MySection");

        modified.Should().BeTrue();
        lines.Should().Contain("[MySection]");
        lines.Should().Contain(l => l.Contains("MyKey") && l.Contains("Value"));
    }

    [Fact]
    public void UpdateKeyIfExists_WhenKeyExists_UpdatesAndReturnsTrue()
    {
        var lines = new List<string>
        {
            "[GERAL]",
            "PathDataSouce_NewAcessoConnectionRecord = \'OldPath\'"
        };

        var modified = IniHelperBase.UpdateKeyIfExists(lines, "PathDataSouce_NewAcessoConnectionRecord", "NewPath", section: "GERAL");

        modified.Should().BeTrue();
        lines.Should().Contain(l => l.Contains("NewPath"));
    }

    [Fact]
    public void UpdateKeyIfExists_WhenKeyDoesNotExist_DoesNotAddAndReturnsFalse()
    {
        var lines = new List<string>
        {
            "[GERAL]",
            "PathDataSource_NewAcessoConnectionRecord = \'ValidPath\'"
        };

        var modified = IniHelperBase.UpdateKeyIfExists(lines, "PathDataSouce_NewAcessoConnectionRecord", "NewPath", section: "GERAL");

        modified.Should().BeFalse();
        lines.Should().NotContain(l => l.Contains("PathDataSouce"));
    }

    [Fact]
    public void RemoveKey_RemovesAllOccurrences()
    {
        var lines = new List<string>
        {
            "Dupe = 1",
            "Keep = 2",
            "Dupe = 3"
        };

        var removed = IniHelperBase.RemoveKey(lines, "Dupe");

        removed.Should().BeTrue();
        lines.Should().HaveCount(1);
        lines[0].Should().Be("Keep = 2");
    }
}