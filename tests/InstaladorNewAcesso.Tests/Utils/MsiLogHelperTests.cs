using FluentAssertions;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class MsiLogHelperTests
{
    [Fact]
    public void GetLogDirectory_ShouldCreateAndReturnDirectory()
    {
        var dir = MsiLogHelper.GetLogDirectory();

        dir.Should().NotBeNullOrWhiteSpace();
        dir.Should().Contain("InstaladorNewAcesso");
        dir.Should().Contain("Logs");
        Directory.Exists(dir).Should().BeTrue();
    }

    [Fact]
    public void GenerateLogFilePath_ShouldReturnPathWithMsiNameAndTimestamp()
    {
        var msiPath = @"C:\Installers\WebAppDS.Setup.5.9.1.msi";
        var logPath = MsiLogHelper.GenerateLogFilePath(msiPath);

        logPath.Should().NotBeNullOrWhiteSpace();
        logPath.Should().Contain("WebAppDS.Setup.5.9.1");
        logPath.Should().EndWith(".log");
        logPath.Should().Contain("InstaladorNewAcesso");
        logPath.Should().Contain("Logs");
    }

    [Fact]
    public void GenerateLogFilePath_ShouldUseTimestampInFilename()
    {
        var before = DateTime.Now;
        var logPath = MsiLogHelper.GenerateLogFilePath(@"C:\test.msi");
        var after = DateTime.Now;

        var fileName = Path.GetFileNameWithoutExtension(logPath);
        // Formato esperado: test_yyyyMMdd_HHmmss
        fileName.Should().Match("test_????????_??????");
    }

    [Fact]
    public void GetLogDirectory_ShouldBeParentOfGeneratedLogPath()
    {
        var logDir = MsiLogHelper.GetLogDirectory();
        var logPath = MsiLogHelper.GenerateLogFilePath(@"C:\test.msi");

        Path.GetDirectoryName(logPath).Should().Be(logDir);
    }

    [Fact]
    public void MsiLogAnalysisResult_DefaultValues_ShouldBeCorrect()
    {
        var result = new MsiLogAnalysisResult();

        result.LogFilePath.Should().BeEmpty();
        result.HasCriticalError.Should().BeFalse();
        result.ReturnValue3Line.Should().BeNull();
        result.FailedCustomAction.Should().BeNull();
        result.ErrorSummary.Should().BeNull();
        result.ErrorContext.Should().BeNull();
        result.RelevantProperties.Should().BeNull();
    }

    [Fact]
    public void AnalyzeLog_WhenFileNotExists_ShouldReturnFileNotFound()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".log");
        var result = MsiLogHelper.AnalyzeLog(logPath);

        result.HasCriticalError.Should().BeFalse();
        result.ErrorSummary.Should().Contain("não encontrado");
    }

    [Fact]
    public void AnalyzeLog_WhenFileHasReturnValue3_ShouldDetectCriticalError()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(logPath, [
                "Line 1: Starting installation...",
                "Line 2: Property(S): TARGETDIR = C:\\inetpub\\wwwroot\\WebAppDS",
                "Line 3: CustomAction MyCustomAction returned actual error code 1603",
                "Line 4: Action ended 14:32:10: InstallExecute. Return value 3.",
                "Line 5: Installation failed."
            ]);

            var result = MsiLogHelper.AnalyzeLog(logPath);

            result.HasCriticalError.Should().BeTrue();
            result.ReturnValue3Line.Should().Be(4); // 1-based line number
            result.LogFilePath.Should().Be(logPath);
            result.ErrorSummary.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    [Fact]
    public void AnalyzeLog_WhenFileHasError1603_ShouldDetectCriticalError()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(logPath, [
                "Line 1: Starting...",
                "Line 2: Error 1603. Fatal error during installation.",
                "Line 3: MSI (s) (84!F4) [00:00:00:000]: I/O error occurred."
            ]);

            var result = MsiLogHelper.AnalyzeLog(logPath);

            result.HasCriticalError.Should().BeTrue();
            result.ErrorSummary.Should().Contain("ERRO CRÍTICO");
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    [Fact]
    public void AnalyzeLog_ShouldExtractErrorContextAroundReturnValue3()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".log");
        try
        {
            var lines = new List<string>();
            for (int i = 0; i < 20; i++)
                lines.Add($"Line {i + 1}: Some log data...");
            lines[12] = "Line 13: CustomAction InstallFiles returned actual error code 1603";
            lines[13] = "Line 14: Action ended 14:32:10: InstallExecute. Return value 3.";

            File.WriteAllLines(logPath, lines);

            var result = MsiLogHelper.AnalyzeLog(logPath);

            result.HasCriticalError.Should().BeTrue();
            result.ReturnValue3Line.Should().Be(14);
            result.ErrorContext.Should().NotBeNull();
            result.ErrorContext.Should().HaveCount(9); // 5 before + 1 (return value 3) + 3 after
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    [Fact]
    public void AnalyzeLog_ShouldDetectFailedCustomAction()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(logPath, [
                "MSI (s) (84!F4) [00:00:00:000]: CustomAction MSVBDPCADLL returned actual error code 1603.",
                "Action ended 14:32:10: InstallExecute. Return value 3."
            ]);

            var result = MsiLogHelper.AnalyzeLog(logPath);

            result.FailedCustomAction.Should().Be("MSVBDPCADLL");
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    [Fact]
    public void AnalyzeLog_ShouldExtractRelevantProperties()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(logPath, [
                "Property(S): TARGETDIR = C:\\inetpub\\wwwroot\\WebAppDS",
                "Property(S): TARGETSITE = WebAppDS",
                "Property(S): TARGETAPPPOOL = WebAppDS",
                "Property(S): CustomActionData = Some data",
                "Some other log line",
                "Action ended 14:32:10: InstallExecute. Return value 3."
            ]);

            var result = MsiLogHelper.AnalyzeLog(logPath);

            result.RelevantProperties.Should().NotBeNull();
            result.RelevantProperties.Should().HaveCount(4);
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    [Fact]
    public void AnalyzeLog_WhenNoErrors_ShouldReturnNoCriticalError()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".log");
        try
        {
            File.WriteAllLines(logPath, [
                "Property(S): TARGETDIR = C:\\inetpub\\wwwroot",
                "Installation completed successfully.",
                "Product: WebAppDS -- Installation completed successfully."
            ]);

            var result = MsiLogHelper.AnalyzeLog(logPath);

            result.HasCriticalError.Should().BeFalse();
            result.ErrorSummary.Should().Contain("Nenhum");
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    [Fact]
    public void AnalyzeLog_ShouldHandleLargeLogFile()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid() + ".log");
        try
        {
            var lines = new List<string>();
            for (int i = 0; i < 1000; i++)
                lines.Add($"MSI (s) ({i:D3}!00) [00:00:00.000]: Some log entry #{i}");
            lines.Add("Action ended 15:00:00: InstallExecute. Return value 3.");

            File.WriteAllLines(logPath, lines);

            var result = MsiLogHelper.AnalyzeLog(logPath);

            result.HasCriticalError.Should().BeTrue();
            result.ReturnValue3Line.Should().Be(1001);
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }
}
