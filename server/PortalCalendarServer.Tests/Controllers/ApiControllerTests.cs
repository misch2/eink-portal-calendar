using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortalCalendarServer.Controllers;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.POCOs;
using PortalCalendarServer.Models.POCOs.Bitmap;
using PortalCalendarServer.Models.POCOs.Board;
using PortalCalendarServer.Services;
using PortalCalendarServer.Services.Integrations;
using PortalCalendarServer.Tests.TestBase;

namespace PortalCalendarServer.Tests.Controllers;

public class ApiControllerTests : IntegrationServiceTestBase
{
    private readonly Mock<IDisplayService> _mockDisplayService;
    private readonly Mock<IMqttService> _mockMqttService;

    public ApiControllerTests()
    {
        _mockDisplayService = new Mock<IDisplayService>();
        _mockMqttService = new Mock<IMqttService>();

        // Default MQTT setup — most tests don't care about MQTT internals
        _mockMqttService
            .Setup(m => m.PublishSensorAsync(It.IsAny<Display>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _mockMqttService
            .Setup(m => m.DisconnectAsync())
            .Returns(Task.CompletedTask);
    }

    private ApiController CreateController()
    {
        var logger = Mock.Of<ILogger<ApiController>>();
        var themeService = new ThemeService(Context);

        // PageGeneratorService is only needed for the Bitmap endpoint; supply a stub
        var pageGenService = new Mock<PageGeneratorService>(
            Mock.Of<ILogger<PageGeneratorService>>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
            Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(),
            _mockDisplayService.Object,
            Mock.Of<IWeb2PngService>(),
            new InternalTokenService(),
            Mock.Of<Microsoft.AspNetCore.Routing.LinkGenerator>(),
            new Modules.ModuleRegistry(),
            Mock.Of<IServiceProvider>()).Object;

        var controller = new ApiController(
            Context,
            logger,
            _mockDisplayService.Object,
            pageGenService,
            themeService,
            Mock.Of<IWeb2PngService>(),
            _mockMqttService.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private WakeUpInfo MakeWakeUpInfo(int sleepSeconds = 3600) => new()
    {
        NextWakeup = DateTime.UtcNow.AddSeconds(sleepSeconds),
        SleepInSeconds = sleepSeconds,
        Schedule = "0 * * * *"
    };

    #region Ping

    [Fact]
    public void Ping_ReturnsOk()
    {
        var controller = CreateController();

        var result = controller.Ping();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    #endregion

    #region Health

    [Fact]
    public void Health_WithDisplaysInDatabase_ReturnsOkHealthy()
    {
        CreateTestDisplay();
        var controller = CreateController();

        var result = controller.Health();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public void Health_WithEmptyDatabase_Returns503()
    {
        var emptyOptions = new DbContextOptionsBuilder<CalendarContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var emptyContext = new CalendarContext(emptyOptions);

        var stubPageGenService = new Mock<PageGeneratorService>(
            Mock.Of<ILogger<PageGeneratorService>>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
            Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(),
            _mockDisplayService.Object,
            Mock.Of<IWeb2PngService>(),
            new InternalTokenService(),
            Mock.Of<Microsoft.AspNetCore.Routing.LinkGenerator>(),
            new Modules.ModuleRegistry(),
            Mock.Of<IServiceProvider>()).Object;

        var controller = new ApiController(
            emptyContext,
            Mock.Of<ILogger<ApiController>>(),
            _mockDisplayService.Object,
            stubPageGenService,
            new ThemeService(emptyContext),
            Mock.Of<IWeb2PngService>(),
            _mockMqttService.Object);

        var result = controller.Health();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, status.StatusCode);
    }

    #endregion

    #region Config — validation

    [Fact]
    public async Task Config_WithMissingMac_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Config(
            mac: null, fw: null, w: null, h: null, c: null,
            adc: null, voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Config_WithEmptyMac_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Config(
            mac: "   ", fw: null, w: null, h: null, c: null,
            adc: null, voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Config — new display creation

    [Fact]
    public async Task Config_WithUnknownMac_CreatesNewDisplayAndReturnsOk()
    {
        var newMac = "11:22:33:44:55:66";
        var theme = new Theme { Id = 1, DisplayName = "Default", FileName = "Default", IsDefault = true, IsActive = true };
        Context.Themes.Add(theme);
        await Context.SaveChangesAsync();

        _mockDisplayService.Setup(s => s.GetMissedConnects(It.IsAny<Display>())).Returns(0);
        _mockDisplayService.Setup(s => s.GetNextWakeupTime(It.IsAny<Display>(), It.IsAny<DateTime?>())).Returns(MakeWakeUpInfo());
        _mockDisplayService.Setup(s => s.GetVoltage(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetBatteryPercent(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetConfigBool(It.IsAny<Display>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _mockDisplayService.Setup(s => s.GetConfig(It.IsAny<Display>(), It.IsAny<string>())).Returns((string?)null);

        var controller = CreateController();
        var result = await controller.Config(
            mac: newMac, fw: "1.0", w: 800, h: 480, c: "BW",
            adc: null, voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null);

        Assert.IsType<OkObjectResult>(result);
        var created = await Context.Displays.FirstOrDefaultAsync(d => d.Mac == newMac);
        Assert.NotNull(created);
        Assert.Equal(800, created.Width);
        Assert.Equal(480, created.Height);
    }

    [Fact]
    public async Task Config_WithUnknownMac_SetsDisplayDimensions()
    {
        var newMac = "aa:11:22:33:44:55";
        var theme = new Theme { Id = 2, DisplayName = "Default", FileName = "Default", IsDefault = true, IsActive = true };
        Context.Themes.Add(theme);
        await Context.SaveChangesAsync();

        _mockDisplayService.Setup(s => s.GetMissedConnects(It.IsAny<Display>())).Returns(0);
        _mockDisplayService.Setup(s => s.GetNextWakeupTime(It.IsAny<Display>(), It.IsAny<DateTime?>())).Returns(MakeWakeUpInfo());
        _mockDisplayService.Setup(s => s.GetVoltage(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetBatteryPercent(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetConfigBool(It.IsAny<Display>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _mockDisplayService.Setup(s => s.GetConfig(It.IsAny<Display>(), It.IsAny<string>())).Returns((string?)null);

        var controller = CreateController();
        await controller.Config(
            mac: newMac, fw: null, w: 1200, h: 960, c: "3C",
            adc: null, voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null);

        var created = await Context.Displays.FirstOrDefaultAsync(d => d.Mac == newMac);
        Assert.NotNull(created);
        Assert.Equal(1200, created.Width);
        Assert.Equal(960, created.Height);
        Assert.Equal("3C", created.DisplayTypeCode);
    }

    [Fact]
    public async Task Config_WithUnknownMac_UsesFallbackDimensionsWhenNotProvided()
    {
        var newMac = "bb:22:33:44:55:66";
        var theme = new Theme { Id = 3, DisplayName = "Default", FileName = "Default", IsDefault = true, IsActive = true };
        Context.Themes.Add(theme);
        await Context.SaveChangesAsync();

        _mockDisplayService.Setup(s => s.GetMissedConnects(It.IsAny<Display>())).Returns(0);
        _mockDisplayService.Setup(s => s.GetNextWakeupTime(It.IsAny<Display>(), It.IsAny<DateTime?>())).Returns(MakeWakeUpInfo());
        _mockDisplayService.Setup(s => s.GetVoltage(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetBatteryPercent(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetConfigBool(It.IsAny<Display>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _mockDisplayService.Setup(s => s.GetConfig(It.IsAny<Display>(), It.IsAny<string>())).Returns((string?)null);

        var controller = CreateController();
        await controller.Config(
            mac: newMac, fw: null, w: null, h: null, c: null,
            adc: null, voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null);

        var created = await Context.Displays.FirstOrDefaultAsync(d => d.Mac == newMac);
        Assert.NotNull(created);
        Assert.Equal(800, created.Width);
        Assert.Equal(480, created.Height);
    }

    #endregion

    #region Config — existing display update

    [Fact]
    public async Task Config_WithKnownMac_UpdatesFirmwareAndReturnsOk()
    {
        var display = CreateTestDisplay(mac: "cc:dd:ee:ff:00:11");

        _mockDisplayService.Setup(s => s.GetMissedConnects(It.IsAny<Display>())).Returns(0);
        _mockDisplayService.Setup(s => s.GetNextWakeupTime(It.IsAny<Display>(), It.IsAny<DateTime?>())).Returns(MakeWakeUpInfo());
        _mockDisplayService.Setup(s => s.GetVoltage(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetBatteryPercent(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetConfigBool(It.IsAny<Display>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _mockDisplayService.Setup(s => s.GetConfig(It.IsAny<Display>(), It.IsAny<string>())).Returns((string?)null);

        var controller = CreateController();
        var result = await controller.Config(
            mac: display.Mac, fw: "2.5", w: null, h: null, c: null,
            adc: null, voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null);

        Assert.IsType<OkObjectResult>(result);
        var updated = await Context.Displays.FindAsync(display.Id);
        Assert.Equal("2.5", updated!.Firmware);
    }

    [Fact]
    public async Task Config_WithKnownMac_ReturnsExpectedResponseShape()
    {
        var display = CreateTestDisplay(mac: "dd:ee:ff:00:11:22");

        _mockDisplayService.Setup(s => s.GetMissedConnects(It.IsAny<Display>())).Returns(0);
        _mockDisplayService.Setup(s => s.GetNextWakeupTime(It.IsAny<Display>(), It.IsAny<DateTime?>()))
            .Returns(new WakeUpInfo { NextWakeup = DateTime.UtcNow.AddSeconds(7200), SleepInSeconds = 7200, Schedule = "0 * * * *" });
        _mockDisplayService.Setup(s => s.GetVoltage(It.IsAny<Display>())).Returns(3.7m);
        _mockDisplayService.Setup(s => s.GetBatteryPercent(It.IsAny<Display>())).Returns(80m);
        _mockDisplayService.Setup(s => s.GetConfigBool(It.IsAny<Display>(), "ota_mode", It.IsAny<bool>())).Returns(false);
        _mockDisplayService.Setup(s => s.GetConfigBool(It.IsAny<Display>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _mockDisplayService.Setup(s => s.GetConfig(It.IsAny<Display>(), It.IsAny<string>())).Returns((string?)null);

        var controller = CreateController();
        var result = await controller.Config(
            mac: display.Mac, fw: null, w: null, h: null, c: null,
            adc: null, voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var propNames = ok.Value!.GetType().GetProperties().Select(p => p.Name).ToArray();
        Assert.Contains("sleep", propNames);
        Assert.Contains("battery_percent", propNames);
        Assert.Contains("ota_mode", propNames);
    }

    [Fact]
    public async Task Config_MacIsCaseInsensitive_MatchesExistingDisplay()
    {
        var display = CreateTestDisplay(mac: "ee:ff:00:11:22:33");

        _mockDisplayService.Setup(s => s.GetMissedConnects(It.IsAny<Display>())).Returns(0);
        _mockDisplayService.Setup(s => s.GetNextWakeupTime(It.IsAny<Display>(), It.IsAny<DateTime?>())).Returns(MakeWakeUpInfo());
        _mockDisplayService.Setup(s => s.GetVoltage(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetBatteryPercent(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetConfigBool(It.IsAny<Display>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _mockDisplayService.Setup(s => s.GetConfig(It.IsAny<Display>(), It.IsAny<string>())).Returns((string?)null);

        var controller = CreateController();
        // Supply MAC in upper-case — controller should lower-case it before looking up
        var result = await controller.Config(
            mac: "EE:FF:00:11:22:33", fw: null, w: null, h: null, c: null,
            adc: null, voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null);

        Assert.IsType<OkObjectResult>(result);
        // Should NOT have created a second display
        var count = await Context.Displays.CountAsync(d => d.Mac == "ee:ff:00:11:22:33");
        Assert.Equal(1, count);
    }

    #endregion

    #region BitmapEpaper

    [Fact]
    public async Task BitmapEpaper_WithUnknownMac_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.BitmapEpaper(mac: "ff:ff:ff:ff:ff:ff");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task BitmapEpaper_WithMissingMac_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.BitmapEpaper(mac: null);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task BitmapEpaper_WhenBitmapServiceReturnsError_ReturnsNotFound()
    {
        var display = CreateTestDisplay(mac: "12:34:56:78:9a:bc");
        string? errMsg = "No rendered bitmap available for this display yet";

        _mockDisplayService
            .Setup(b => b.ConvertExistingRawBitmap(
                display.Id,
                It.IsAny<OutputFormat>(),
                It.IsAny<DisplayRotation?>(), It.IsAny<string?>()
                ))
            .Returns(new BitmapResult { ErrorMessage = errMsg });

        var controller = CreateController();
        var result = await controller.BitmapEpaper(mac: display.Mac);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion
}
