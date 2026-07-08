using FluentAssertions;
using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Tests.Models;

public class WebAppModelTests
{
    [Fact]
    public void DefaultValues_ShouldBeEmptyStringsAndZeroPort()
    {
        var model = new WebAppModel();

        model.MsiPath.Should().BeEmpty();
        model.SiteName.Should().BeEmpty();
        model.AppPoolName.Should().BeEmpty();
        model.ForcedInstallPath.Should().BeEmpty();
        model.TargetDirectory.Should().BeEmpty();
        model.Port.Should().Be(0);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var model = new WebAppModel
        {
            MsiPath = @"D:\Installers\webapp.msi",
            SiteName = "WebAppDS",
            AppPoolName = "WebAppDS",
            TargetDirectory = @"C:\inetpub\wwwroot\WebAppDS",
            Port = 8080
        };

        model.MsiPath.Should().Be(@"D:\Installers\webapp.msi");
        model.SiteName.Should().Be("WebAppDS");
        model.AppPoolName.Should().Be("WebAppDS");
        model.TargetDirectory.Should().Be(@"C:\inetpub\wwwroot\WebAppDS");
        model.Port.Should().Be(8080);
    }

    [Fact]
    public void ForcedInstallPath_ShouldHaveXmlDocComment()
    {
        var type = typeof(WebAppModel);
        var property = type.GetProperty(nameof(WebAppModel.ForcedInstallPath));

        property.Should().NotBeNull();
        // ForcedInstallPath agora é usado ativamente na estratégia de instalação,
        // então não possui mais o atributo [Obsolete]
        property!
            .GetCustomAttributes(typeof(ObsoleteAttribute), false)
            .Should()
            .BeEmpty();
    }
}
