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
    private readonly PairingModeService _pairingMode;

    public ApiControllerTests()
    {
        _mockDisplayService = new Mock<IDisplayService>();
        _mockMqttService = new Mock<IMqttService>();
        _pairingMode = new PairingModeService();

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
            _mockMqttService.Object,
            _pairingMode);

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

    /// <summary>
    /// Sets up the display service mocks for the Config endpoint's post-creation/post-update flow.
    /// </summary>
    private void SetupDisplayServiceForConfig()
    {
        _mockDisplayService.Setup(s => s.GetMissedConnects(It.IsAny<Display>())).Returns(0);
        _mockDisplayService.Setup(s => s.GetNextWakeupTime(It.IsAny<Display>(), It.IsAny<DateTime?>())).Returns(MakeWakeUpInfo());
        _mockDisplayService.Setup(s => s.GetVoltage(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetBatteryPercent(It.IsAny<Display>())).Returns((decimal?)null);
        _mockDisplayService.Setup(s => s.GetConfigBool(It.IsAny<Display>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _mockDisplayService.Setup(s => s.GetConfig(It.IsAny<Display>(), It.IsAny<string>())).Returns((string?)null);
    }

    /// <summary>
    /// Seeds the database with a default theme, display type, and color variant for new-display tests.
    /// </summary>
    private async Task SeedNewDisplayPrerequisites(
        int themeId = 1,
        string displayTypeCode = "BW",
        string displayTypeName = "Black & White",
        int numColors = 1,
        string colorVariantCode = "BW",
        string colorVariantName = "Black & White")
    {
        Context.Themes.Add(new Theme { Id = themeId, DisplayName = "Default", FileName = "Default", IsDefault = true, IsActive = true });
        Context.DisplayTypes.Add(new DisplayType { Code = displayTypeCode, Name = displayTypeName, NumColors = numColors });
        Context.ColorVariants.Add(new ColorVariant { Code = colorVariantCode, Name = colorVariantName, DisplayTypeCode = displayTypeCode });
        await Context.SaveChangesAsync();
    }

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
            _mockMqttService.Object,
            new PairingModeService());

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
            mac: null, fw: null, w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Config_WithEmptyMac_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Config(
            mac: "   ", fw: null, w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Config — new display creation

    [Fact]
    public async Task Config_WithUnknownMac_CreatesNewDisplayAndReturnsOk()
    {
        var newMac = "11:22:33:44:55:66";
        await SeedNewDisplayPrerequisites();

        SetupDisplayServiceForConfig();

        _pairingMode.Activate();
        var controller = CreateController();
        var result = await controller.Config(
            mac: newMac, fw: "2.3.0", w: 800, h: 480, c: "BW", rotation: 0,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

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
        await SeedNewDisplayPrerequisites(
            themeId: 2,
            displayTypeCode: "3C", displayTypeName: "3 Color", numColors: 3,
            colorVariantCode: "BWR", colorVariantName: "B+W+R");

        SetupDisplayServiceForConfig();

        _pairingMode.Activate();
        var controller = CreateController();
        await controller.Config(
            mac: newMac, fw: "2.3.0", w: 1200, h: 960, c: "3C", rotation: 2,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        var created = await Context.Displays.FirstOrDefaultAsync(d => d.Mac == newMac);
        Assert.NotNull(created);
        Assert.Equal(1200, created.Width);
        Assert.Equal(960, created.Height);
        Assert.Equal("3C", created.DisplayTypeCode);
    }

    [Fact]
    public async Task Config_WithMissingParameters_ReturnsBadRequest()
    {
        var newMac = "bb:22:33:44:55:66";
        await SeedNewDisplayPrerequisites(themeId: 3);

        SetupDisplayServiceForConfig();

        _pairingMode.Activate();
        var controller = CreateController();
        var result = await controller.Config(
            mac: newMac, fw: null, w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        Assert.Equal(400, (result as BadRequestObjectResult)?.StatusCode);

        var created = await Context.Displays.FirstOrDefaultAsync(d => d.Mac == newMac);
        Assert.Null(created);
    }

    #endregion

    #region Config — existing display update

    [Fact]
    public async Task Config_WithKnownMac_UpdatesFirmwareAndReturnsOk()
    {
        var display = CreateTestDisplay(mac: "cc:dd:ee:ff:00:11");

        SetupDisplayServiceForConfig();

        var controller = CreateController();
        var result = await controller.Config(
            mac: display.Mac, fw: "2.5", w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

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
            mac: display.Mac, fw: null, w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

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

        SetupDisplayServiceForConfig();

        var controller = CreateController();
        // Supply MAC in upper-case — controller should lower-case it before looking up
        var result = await controller.Config(
            mac: "EE:FF:00:11:22:33", fw: null, w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        Assert.IsType<OkObjectResult>(result);
        // Should NOT have created a second display
        var count = await Context.Displays.CountAsync(d => d.Mac == "ee:ff:00:11:22:33");
        Assert.Equal(1, count);
    }

    #endregion

    #region Config — pairing mode

    [Fact]
    public async Task Config_WithUnknownMac_WhenPairingModeInactive_ReturnsUnauthorized()
    {
        var newMac = "ff:00:11:22:33:44";
        await SeedNewDisplayPrerequisites(themeId: 10);

        SetupDisplayServiceForConfig();

        // Do NOT activate pairing mode
        var controller = CreateController();
        var result = await controller.Config(
            mac: newMac, fw: "2.3.0", w: 800, h: 480, c: "BW", rotation: 0,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Config_WithUnknownMac_DeactivatesPairingModeAfterSuccess()
    {
        var newMac = "ff:00:11:22:33:55";
        await SeedNewDisplayPrerequisites(themeId: 11);

        SetupDisplayServiceForConfig();

        _pairingMode.Activate();
        Assert.True(_pairingMode.IsActive);

        var controller = CreateController();
        await controller.Config(
            mac: newMac, fw: "2.3.0", w: 800, h: 480, c: "BW", rotation: 0,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        Assert.False(_pairingMode.IsActive);
    }

    #endregion

    #region Config — API key

    [Fact]
    public async Task Config_NewDisplay_WithOldFirmware_ReturnsBadRequest()
    {
        var newMac = "a1:b1:c1:d1:e1:f1";
        await SeedNewDisplayPrerequisites(themeId: 20);

        SetupDisplayServiceForConfig();

        _pairingMode.Activate();
        var controller = CreateController();
        var result = await controller.Config(
            mac: newMac, fw: "1.0.0", w: 800, h: 480, c: "BW", rotation: 0,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        Assert.IsType<BadRequestObjectResult>(result);

        // Display should NOT have been persisted
        var created = await Context.Displays.FirstOrDefaultAsync(d => d.Mac == newMac);
        Assert.Null(created);
    }

    [Fact]
    public async Task Config_NewDisplay_WithSupportedFirmware_ReturnsApiKeyInResponse()
    {
        var newMac = "a2:b2:c2:d2:e2:f2";
        await SeedNewDisplayPrerequisites(themeId: 21);

        SetupDisplayServiceForConfig();

        _pairingMode.Activate();
        var controller = CreateController();
        var result = await controller.Config(
            mac: newMac, fw: "2.3.0", w: 800, h: 480, c: "BW", rotation: 0,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var apiKeyProp = ok.Value!.GetType().GetProperty("api_key");
        Assert.NotNull(apiKeyProp);
        var apiKeyValue = apiKeyProp.GetValue(ok.Value) as string;
        Assert.NotNull(apiKeyValue);
        Assert.NotEmpty(apiKeyValue);

        // Verify the key was persisted
        var display = await Context.Displays.FirstOrDefaultAsync(d => d.Mac == newMac);
        Assert.NotNull(display);
        Assert.Equal(apiKeyValue, display.ApiKey);
    }

    [Fact]
    public async Task Config_ExistingDisplay_WithNoApiKey_OldFirmware_PassesThrough()
    {
        // Existing display with old firmware (1.0.0) and no API key — should be allowed
        var display = CreateTestDisplay(mac: "a3:b3:c3:d3:e3:f3");
        Assert.Null(display.ApiKey);

        SetupDisplayServiceForConfig();

        var controller = CreateController();
        var result = await controller.Config(
            mac: display.Mac, fw: null, w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Config_ExistingDisplay_WithNoApiKey_NewFirmware_AssignsKeyInGracePeriod()
    {
        // Existing display with no API key gets firmware updated to a version that supports keys
        var display = CreateTestDisplay(mac: "a4:b4:c4:d4:e4:f4");
        Assert.Null(display.ApiKey);

        SetupDisplayServiceForConfig();

        var controller = CreateController();
        var result = await controller.Config(
            mac: display.Mac, fw: "2.5.0", w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        var ok = Assert.IsType<OkObjectResult>(result);

        // Response should contain the newly assigned API key
        var apiKeyProp = ok.Value!.GetType().GetProperty("api_key");
        Assert.NotNull(apiKeyProp);
        var apiKeyValue = apiKeyProp.GetValue(ok.Value) as string;
        Assert.NotNull(apiKeyValue);
        Assert.NotEmpty(apiKeyValue);

        // Verify persistence
        var updated = await Context.Displays.FindAsync(display.Id);
        Assert.Equal(apiKeyValue, updated!.ApiKey);
    }

    [Fact]
    public async Task Config_ExistingDisplay_WithApiKey_CorrectKeyProvided_ReturnsOk()
    {
        var display = CreateTestDisplay(mac: "a5:b5:c5:d5:e5:f5");
        display.ApiKey = "test-api-key-12345";
        display.Firmware = "2.3.0";
        Context.Update(display);
        await Context.SaveChangesAsync();

        SetupDisplayServiceForConfig();

        var controller = CreateController();
        var result = await controller.Config(
            mac: display.Mac, fw: "2.3.0", w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: "test-api-key-12345");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Config_ExistingDisplay_WithApiKey_WrongKeyProvided_ReturnsUnauthorized()
    {
        var display = CreateTestDisplay(mac: "a6:b6:c6:d6:e6:f6");
        display.ApiKey = "correct-key-xyz";
        display.Firmware = "2.3.0";
        Context.Update(display);
        await Context.SaveChangesAsync();

        SetupDisplayServiceForConfig();

        var controller = CreateController();
        var result = await controller.Config(
            mac: display.Mac, fw: "2.3.0", w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: "wrong-key");

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Config_ExistingDisplay_WithApiKey_NoKeyProvided_ReturnsUnauthorized()
    {
        var display = CreateTestDisplay(mac: "a7:b7:c7:d7:e7:f7");
        display.ApiKey = "some-assigned-key";
        display.Firmware = "2.3.0";
        Context.Update(display);
        await Context.SaveChangesAsync();

        SetupDisplayServiceForConfig();

        var controller = CreateController();
        var result = await controller.Config(
            mac: display.Mac, fw: "2.3.0", w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Config_ExistingDisplay_ApiKeyIsCaseSensitive()
    {
        var display = CreateTestDisplay(mac: "a8:b8:c8:d8:e8:f8");
        display.ApiKey = "CaseSensitiveKey";
        display.Firmware = "2.3.0";
        Context.Update(display);
        await Context.SaveChangesAsync();

        SetupDisplayServiceForConfig();

        var controller = CreateController();

        // Correct case → OK
        var resultCorrect = await controller.Config(
            mac: display.Mac, fw: "2.3.0", w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: "CaseSensitiveKey");
        Assert.IsType<OkObjectResult>(resultCorrect);

        // Wrong case → Unauthorized
        var resultWrongCase = await controller.Config(
            mac: display.Mac, fw: "2.3.0", w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: "casesensitivekey");
        Assert.IsType<UnauthorizedObjectResult>(resultWrongCase);
    }

    [Fact]
    public async Task Config_NewDisplay_ResponseApiKeyDoesNotChangeOnSubsequentRequests()
    {
        var newMac = "a9:b9:c9:d9:e9:f9";
        await SeedNewDisplayPrerequisites(themeId: 22);

        SetupDisplayServiceForConfig();

        // First request: pair the display
        _pairingMode.Activate();
        var controller = CreateController();
        var result1 = await controller.Config(
            mac: newMac, fw: "2.3.0", w: 800, h: 480, c: "BW", rotation: 0,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: null);

        var ok1 = Assert.IsType<OkObjectResult>(result1);
        var apiKey1 = ok1.Value!.GetType().GetProperty("api_key")!.GetValue(ok1.Value) as string;
        Assert.NotNull(apiKey1);

        // Second request: existing display with correct key
        var result2 = await controller.Config(
            mac: newMac, fw: "2.3.0", w: null, h: null, c: null, rotation: null,
            voltage_raw: null, v: null, vmin: null, vmax: null,
            vlmin: null, vlmax: null, reset: null, wakeup: null, key: apiKey1);

        var ok2 = Assert.IsType<OkObjectResult>(result2);
        // api_key should be null on subsequent requests (key already assigned, not newly assigned)
        var apiKey2 = ok2.Value!.GetType().GetProperty("api_key")!.GetValue(ok2.Value);
        Assert.Null(apiKey2);

        // Stored key should remain unchanged
        var display = await Context.Displays.FirstOrDefaultAsync(d => d.Mac == newMac);
        Assert.NotNull(display);
        Assert.Equal(apiKey1, display.ApiKey);
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

    [Fact]
    public async Task BitmapEpaper_WithApiKey_CorrectKey_Succeeds()
    {
        var display = CreateTestDisplay(mac: "b1:23:45:67:89:ab");
        display.ApiKey = "bitmap-key-123";
        Context.Update(display);
        await Context.SaveChangesAsync();

        _mockDisplayService
            .Setup(b => b.ConvertExistingRawBitmap(
                display.Id,
                It.IsAny<OutputFormat>(),
                It.IsAny<DisplayRotation?>(), It.IsAny<string?>()
                ))
            .Returns(new BitmapResult { ErrorMessage = "no bitmap" });

        var controller = CreateController();
        var result = await controller.BitmapEpaper(mac: display.Mac, key: "bitmap-key-123");

        // Should reach the bitmap logic (NotFound because no bitmap, not Unauthorized)
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task BitmapEpaper_WithApiKey_WrongKey_ReturnsUnauthorized()
    {
        var display = CreateTestDisplay(mac: "b2:23:45:67:89:ab");
        display.ApiKey = "bitmap-key-456";
        Context.Update(display);
        await Context.SaveChangesAsync();

        var controller = CreateController();
        var result = await controller.BitmapEpaper(mac: display.Mac, key: "wrong-key");

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task BitmapEpaper_WithApiKey_NoKeyProvided_ReturnsUnauthorized()
    {
        var display = CreateTestDisplay(mac: "b3:23:45:67:89:ab");
        display.ApiKey = "bitmap-key-789";
        Context.Update(display);
        await Context.SaveChangesAsync();

        var controller = CreateController();
        var result = await controller.BitmapEpaper(mac: display.Mac, key: null);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task BitmapEpaper_WithoutApiKey_NoKeyRequired_Succeeds()
    {
        // Display has no API key set — should not require one
        var display = CreateTestDisplay(mac: "b4:23:45:67:89:ab");
        Assert.Null(display.ApiKey);

        _mockDisplayService
            .Setup(b => b.ConvertExistingRawBitmap(
                display.Id,
                It.IsAny<OutputFormat>(),
                It.IsAny<DisplayRotation?>(), It.IsAny<string?>()
                ))
            .Returns(new BitmapResult { ErrorMessage = "no bitmap" });

        var controller = CreateController();
        var result = await controller.BitmapEpaper(mac: display.Mac);

        // Should reach the bitmap logic (NotFound because no bitmap, not Unauthorized)
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion
}
