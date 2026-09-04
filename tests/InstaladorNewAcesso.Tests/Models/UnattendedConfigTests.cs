using System.Text.Json;
using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;
using Xunit;

namespace InstaladorNewAcesso.Tests.Models;

public class UnattendedConfigTests
{
    [Fact]
    public void UnattendedConfig_CanBeSerializedAndDeserialized()
    {
        // Arrange
        var config = new UnattendedConfig
        {
            BasePath = "D:\\TestSoftPrime",
            InstallersPath = "D:\\TestSoftPrime\\Installers",
            InstallWindowsFeatures = false,
            CreateDirectories = true,
            ConfigureIIS = true,
            MsisToInstall = new List<string> { "Controller.msi", "CoreWs.msi" },
            InstallWebApps = true,
            Database = new DatabaseConfig
            {
                Server = "db-server",
                User = "admin",
                Password = "secretpassword"
            },
            TaskScheduler = new TaskSchedulerConfig
            {
                Install = true,
                TaskName = "SyncTask",
                ExecutablePath = "C:\\SoftPrime\\task.exe",
                IntervalMinutes = "15"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<UnattendedConfig>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.BasePath.Should().Be("D:\\TestSoftPrime");
        deserialized.MsisToInstall.Should().Contain("Controller.msi");
        deserialized.Database.Server.Should().Be("db-server");
        deserialized.TaskScheduler.TaskName.Should().Be("SyncTask");
    }
}
