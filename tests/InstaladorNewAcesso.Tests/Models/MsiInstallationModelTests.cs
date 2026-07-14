using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;

namespace InstaladorNewAcesso.Tests.Models;

public class MsiInstallationModelTests
{
    [Fact]
    public void DefaultValues_ShouldBeEmptyStrings()
    {
        var model = new MsiInstallationModel();

        model.MsiPath.Should().BeEmpty();
        model.TargetDirectory.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var model = new MsiInstallationModel
        {
            MsiPath = @"D:\Installers\app.msi",
            TargetDirectory = @"C:\Program Files\App"
        };

        model.MsiPath.Should().Be(@"D:\Installers\app.msi");
        model.TargetDirectory.Should().Be(@"C:\Program Files\App");
    }

    [Fact]
    public void GenerateLog_DefaultValue_ShouldBeFalse()
    {
        var model = new MsiInstallationModel();
        model.GenerateLog.Should().BeFalse();
    }

    [Fact]
    public void GenerateLog_ShouldBeSettable()
    {
        var model = new MsiInstallationModel();
        model.GenerateLog = true;
        model.GenerateLog.Should().BeTrue();

        model.GenerateLog = false;
        model.GenerateLog.Should().BeFalse();
    }

    [Fact]
    public void TwoInstances_WithSameValues_ShouldHaveSameProperties()
    {
        var model1 = new MsiInstallationModel
        {
            MsiPath = @"C:\test.msi",
            TargetDirectory = @"C:\Target"
        };

        var model2 = new MsiInstallationModel
        {
            MsiPath = @"C:\test.msi",
            TargetDirectory = @"C:\Target"
        };

        model1.MsiPath.Should().Be(model2.MsiPath);
        model1.TargetDirectory.Should().Be(model2.TargetDirectory);
    }
}
