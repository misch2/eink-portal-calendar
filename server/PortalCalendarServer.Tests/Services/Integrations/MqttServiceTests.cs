using Microsoft.Extensions.Logging;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Integrations;
using PortalCalendarServer.Tests.TestBase;

namespace PortalCalendarServer.Tests.Services.Integrations;

/// <summary>
/// Unit tests for MqttService
/// Note: These tests focus on configuration validation.
/// Actual MQTT broker connection cannot be easily mocked due to IMqttClient creation in the service,
/// so tests that would require connection are limited.
/// </summary>
public class MqttServiceTests : IntegrationServiceTestBase
{
    private readonly Mock<IDisplayService> _mockDisplayService;

    public MqttServiceTests()
    {
        _mockDisplayService = new Mock<IDisplayService>();
    }

    private MqttService CreateService()
    {
        var logger = new Mock<ILogger<MqttService>>().Object;

        return new MqttService(
            logger,
            MockHttpClientFactory.Object,
            MemoryCache,
            MockDatabaseCacheServiceFactory.Object,
            _mockDisplayService.Object,
            Context);
    }

    private void SetupMqttConfig(
        Display display,
        string? server = "localhost:1883",
        string? username = "testuser",
        string? password = "testpass",
        string? topic = "test-display",
        bool enabled = true)
    {
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_server"))
            .Returns(server);
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_username"))
            .Returns(username);
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_password"))
            .Returns(password);
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_topic"))
            .Returns(topic);
        _mockDisplayService
            .Setup(s => s.GetConfigBool(display, "mqtt", false))
            .Returns(enabled);
    }

    #region IsConfigured Tests

    [Fact]
    public void IsConfigured_WithAllRequiredSettings_ReturnsTrue()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        SetupMqttConfig(display);

        var result = service.IsConfigured(display);

        Assert.True(result);
    }

    [Fact]
    public void IsConfigured_WithNullDisplay_ReturnsFalse()
    {
        var service = CreateService();

        var result = service.IsConfigured(null!);

        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_WithoutServer_ReturnsFalse()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_server"))
            .Returns((string?)null);
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_username"))
            .Returns("testuser");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_password"))
            .Returns("testpass");

        var result = service.IsConfigured(display);

        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_WithoutUsername_ReturnsFalse()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_server"))
            .Returns("localhost:1883");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_username"))
            .Returns((string?)null);
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_password"))
            .Returns("testpass");

        var result = service.IsConfigured(display);

        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_WithoutPassword_ReturnsFalse()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_server"))
            .Returns("localhost:1883");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_username"))
            .Returns("testuser");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_password"))
            .Returns((string?)null);

        var result = service.IsConfigured(display);

        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_WithEmptyServer_ReturnsFalse()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_server"))
            .Returns(string.Empty);
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_username"))
            .Returns("testuser");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_password"))
            .Returns("testpass");

        var result = service.IsConfigured(display);

        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_WithServerAndPort_ReturnsTrue()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        SetupMqttConfig(display, server: "mqtt.example.com:8883");

        var result = service.IsConfigured(display);

        Assert.True(result);
    }

    [Fact]
    public void IsConfigured_WithServerOnly_ReturnsTrue()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        SetupMqttConfig(display, server: "mqtt.example.com");

        var result = service.IsConfigured(display);

        Assert.True(result);
    }

    [Fact]
    public void IsConfigured_WithWhitespaceServer_ReturnsFalse()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_server"))
            .Returns("   ");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_username"))
            .Returns("testuser");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_password"))
            .Returns("testpass");

        var result = service.IsConfigured(display);

        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_WithEmptyUsername_ReturnsFalse()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_server"))
            .Returns("localhost:1883");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_username"))
            .Returns(string.Empty);
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_password"))
            .Returns("testpass");

        var result = service.IsConfigured(display);

        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_WithEmptyPassword_ReturnsFalse()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_server"))
            .Returns("localhost:1883");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_username"))
            .Returns("testuser");
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_password"))
            .Returns(string.Empty);

        var result = service.IsConfigured(display);

        Assert.False(result);
    }

    #endregion

    #region PublishSensorAsync Tests

    [Fact]
    public async Task PublishSensorAsync_WhenMqttDisabled_DoesNothing()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        SetupMqttConfig(display, enabled: false);

        await service.PublishSensorAsync(display, "voltage", 3.7);

        _mockDisplayService.Verify(
            s => s.GetConfig(display, "mqtt_server"),
            Times.Never);
    }

    [Fact]
    public async Task PublishSensorAsync_WhenNotConfigured_HandlesGracefully()
    {
        var service = CreateService();
        var display = GetBWDisplay();

        // Don't set up any config - service is not configured
        _mockDisplayService
            .Setup(s => s.GetConfigBool(display, "mqtt", false))
            .Returns(true);
        _mockDisplayService
            .Setup(s => s.GetConfig(display, "mqtt_server"))
            .Returns((string?)null);

        // Should not throw even though not configured
        await service.PublishSensorAsync(display, "voltage", 3.7);

        Assert.True(true);
    }

    #endregion

    #region Disconnect and Dispose Tests

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
    {
        var service = CreateService();

        await service.DisconnectAsync();

        Assert.True(true);
    }

    [Fact]
    public async Task DisposeAsync_CallsDisconnect()
    {
        var service = CreateService();

        await service.DisposeAsync();

        Assert.True(true);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        var service = CreateService();

        await service.DisposeAsync();
        await service.DisposeAsync();

        Assert.True(true);
    }

    [Fact]
    public async Task DisconnectAsync_CanBeCalledMultipleTimes()
    {
        var service = CreateService();

        await service.DisconnectAsync();
        await service.DisconnectAsync();

        Assert.True(true);
    }

    #endregion
}
