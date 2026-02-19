namespace PortalCalendarServer.Tests.TestData;

/// <summary>
/// Example tests demonstrating how to use the shared test data infrastructure.
/// This file serves as documentation and can be removed if not needed.
/// </summary>
public class TestDataUsageExamples
{
    // Example 1: Using pre-seeded displays
    public void Example_UsingPreSeededDisplays()
    {
        /*
        public class MyServiceTests : IntegrationServiceTestBase
        {
            [Fact]
            public async Task TestWithBWDisplay()
            {
                // Get the pre-seeded BW display
                var display = GetBWDisplay();
                
                Assert.Equal("BW", display.ColorType);
                Assert.Equal(800, display.Width);
                Assert.Equal(480, display.Height);
                
                // Use it in your test...
            }
            
            [Fact]
            public async Task TestWith3CDisplay()
            {
                // Get the pre-seeded 3C display
                var display = Get3CDisplay();
                
                Assert.Equal("3C", display.ColorType);
                
                // Use it in your test...
            }
            
            [Fact]
            public async Task TestWithBothDisplays()
            {
                // Test that verifies behavior for both display types
                var bwDisplay = GetBWDisplay();
                var colorDisplay = Get3CDisplay();
                
                // Compare bitmap generation for both types...
            }
        }
        */
    }

    // Example 2: Creating display with Google Fit config
    public void Example_GoogleFitConfiguration()
    {
        /*
        public class GoogleFitTests : IntegrationServiceTestBase
        {
            [Fact]
            public async Task TestGoogleFitIntegration()
            {
                // Creates a display with all Google Fit tokens pre-configured
                var display = CreateDisplayWithGoogleFitTokens();
                
                var service = new GoogleFitIntegrationService(...);
                var result = await service.GetWeightSeriesAsync(display);
                
                // Test the result...
            }
            
            [Fact]
            public async Task TestGoogleFitWith3CDisplay()
            {
                // Creates a 3C display with Google Fit tokens
                var display = CreateDisplayWithGoogleFitTokens(colorType: "3C");
                
                // Use it in your test...
            }
        }
        */
    }

    // Example 3: Creating display with custom configs
    public void Example_CustomConfiguration()
    {
        /*
        public class CustomConfigTests : IntegrationServiceTestBase
        {
            [Fact]
            public async Task TestWithCustomConfig()
            {
                // Create display with specific configurations
                var display = CreateDisplayWithConfigs(
                    ("custom_setting", "value1"),
                    ("another_setting", "value2")
                );
                
                // Use it in your test...
            }
            
            [Fact]
            public async Task TestWithCustom3CDisplay()
            {
                // Create 3C display with custom configurations
                var display = CreateDisplayWithConfigs(
                    "My3CDisplay",
                    "aa:bb:cc:dd:ee:99",
                    "3C",
                    ("setting1", "value1"),
                    ("setting2", "value2")
                );
                
                // Use it in your test...
            }
        }
        */
    }

    // Example 4: Using TestDataHelper constants
    public void Example_UsingConstants()
    {
        /*
        public class ConstantsTests : IntegrationServiceTestBase
        {
            [Fact]
            public async Task TestWithStandardConfig()
            {
                var display = CreateTestDisplay();
                
                // Use standard Google Fit configs
                AddConfigsToDisplay(display, TestDataHelper.GoogleFit.StandardConfigs);
                
                // Or use individual constants
                Context.Configs.Add(new Config
                {
                    DisplayId = display.Id,
                    Name = TestDataHelper.GoogleFit.AccessTokenConfigName,
                    Value = TestDataHelper.GoogleFit.TestAccessToken
                });
                
                Context.SaveChanges();
            }
            
            [Fact]
            public async Task TestWithICalConfig()
            {
                var display = CreateDisplayWithICalConfig();
                
                // Or with custom URL
                var display2 = CreateDisplayWithICalConfig(
                    url: TestDataHelper.ICal.AlternativeTestUrl
                );
            }
        }
        */
    }

    // Example 5: Adding configs to pre-seeded displays
    public void Example_AddingConfigsToPreSeededDisplays()
    {
        /*
        public class ConfigModificationTests : IntegrationServiceTestBase
        {
            [Fact]
            public async Task TestBWDisplayWithGoogleFit()
            {
                // Get the pre-seeded BW display and add Google Fit configs
                var display = GetBWDisplay();
                AddConfigsToDisplay(display, TestDataHelper.GoogleFit.StandardConfigs);
                
                // Now the BW display has Google Fit configuration
                var service = new GoogleFitIntegrationService(...);
                var result = await service.GetWeightSeriesAsync(display);
            }
            
            [Fact]
            public async Task Test3CDisplayWithWeather()
            {
                // Get the pre-seeded 3C display and add weather configs
                var display = Get3CDisplay();
                AddConfigsToDisplay(display, TestDataHelper.Weather.StandardConfigs());
                
                // Now the 3C display has weather configuration
            }
        }
        */
    }

    // Example 6: Using common test dates
    public void Example_UsingCommonDates()
    {
        /*
        public class DateTests : IntegrationServiceTestBase
        {
            [Fact]
            public async Task TestWithStandardDateRange()
            {
                var service = new IcalIntegrationService(...);
                
                // Use standard test date range
                var events = await service.GetEventsBetweenAsync(
                    TestDataHelper.Dates.TestDateStart,
                    TestDataHelper.Dates.TestDateEnd
                );
                
                // Or use base test date
                var today = TestDataHelper.Dates.BaseTestDate;
                var todayEvents = await service.GetTodayEventsAsync(today);
            }
        }
        */
    }
}
