using PortalCalendarServer.Tests.TestBase;
using PortalCalendarServer.Tests.TestData;

namespace PortalCalendarServer.Tests.Services.Integrations;

/// <summary>
/// Demonstration tests showing how to use pre-seeded displays and shared test data.
/// These tests serve as documentation and can be removed if not needed.
/// </summary>
public class SharedTestDataDemoTests : IntegrationServiceTestBase
{
    [Fact]
    public void Demo_AccessPreSeededBWDisplay()
    {
        // Get the pre-seeded BW (black & white) display
        var display = GetBWDisplay();

        // Verify it has the expected properties
        Assert.Equal(TestDataHelper.Displays.BlackAndWhite.Name, display.Name);
        Assert.Equal(TestDataHelper.Displays.BlackAndWhite.Mac, display.Mac);
        Assert.Equal(TestDataHelper.Displays.BlackAndWhite.ColorType, display.DisplayTypeCode);
        Assert.Equal(TestDataHelper.Displays.BlackAndWhite.Width, display.Width);
        Assert.Equal(TestDataHelper.Displays.BlackAndWhite.Height, display.Height);
    }

    [Fact]
    public void Demo_AccessPreSeeded3CDisplay()
    {
        // Get the pre-seeded 3C (three-color) display
        var display = Get3CDisplay();

        // Verify it has the expected properties
        Assert.Equal(TestDataHelper.Displays.ThreeColor.Name, display.Name);
        Assert.Equal(TestDataHelper.Displays.ThreeColor.Mac, display.Mac);
        Assert.Equal(TestDataHelper.Displays.ThreeColor.ColorType, display.DisplayTypeCode);
        Assert.Equal(TestDataHelper.Displays.ThreeColor.Width, display.Width);
        Assert.Equal(TestDataHelper.Displays.ThreeColor.Height, display.Height);
    }


    [Fact]
    public void Demo_AddConfigsToPreSeededDisplay()
    {
        // Get a pre-seeded display and add Google Fit configuration to it
        var display = GetBWDisplay();
        AddConfigsToDisplay(display, TestDataHelper.GoogleFit.StandardConfigs);

        // Verify configs were added
        Assert.NotEmpty(display.Configs);
        Assert.Contains(display.Configs, c => c.Name == TestDataHelper.GoogleFit.AccessTokenConfigName);
        Assert.Contains(display.Configs, c => c.Name == TestDataHelper.GoogleFit.RefreshTokenConfigName);
    }

    [Fact]
    public void Demo_CreateNewDisplayWithGoogleFitConfig()
    {
        // Create a new display with Google Fit configuration using factory method
        var display = CreateDisplayWithGoogleFitTokens();

        // Verify it has the expected configs
        Assert.NotEmpty(display.Configs);
        var accessToken = display.Configs.FirstOrDefault(c =>
            c.Name == TestDataHelper.GoogleFit.AccessTokenConfigName);
        Assert.NotNull(accessToken);
        Assert.Equal(TestDataHelper.GoogleFit.TestAccessToken, accessToken.Value);
    }

    [Fact]
    public void Demo_CreateNewDisplayWithCustomConfigs()
    {
        // Create a display with completely custom configurations
        var display = CreateDisplayWithConfigs(
            ("custom_setting_1", "value_1"),
            ("custom_setting_2", "value_2"),
            ("another_config", "another_value")
        );

        // Verify configs were added
        Assert.Equal(3, display.Configs.Count);
        Assert.Contains(display.Configs, c => c.Name == "custom_setting_1");
    }

    [Fact]
    public void Demo_UseTestDataHelperConstants()
    {
        // Create a display with standard Google Fit configuration
        var display = CreateTestDisplay();

        // Add configs using TestDataHelper constants for maintainability
        Context.Configs.Add(new Config
        {
            DisplayId = display.Id,
            Name = TestDataHelper.GoogleFit.AccessTokenConfigName,
            Value = TestDataHelper.GoogleFit.TestAccessToken
        });

        Context.SaveChanges();
        Context.Entry(display).Collection(d => d.Configs).Load();

        // Verify using the same constants
        var config = display.Configs.First();
        Assert.Equal(TestDataHelper.GoogleFit.AccessTokenConfigName, config.Name);
        Assert.Equal(TestDataHelper.GoogleFit.TestAccessToken, config.Value);
    }
}
