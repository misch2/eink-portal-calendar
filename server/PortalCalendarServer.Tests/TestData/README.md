# Test Data Infrastructure

This document describes the shared test data infrastructure for integration service tests.

## Overview

The test infrastructure provides:
- **Pre-seeded test displays** (BW and 3C) for consistent testing
- **Factory methods** for creating displays with various configurations
- **Shared constants** for common test data values
- **Test isolation** - each test gets its own in-memory database

## Architecture

### Base Class: `IntegrationServiceTestBase`

All integration service tests inherit from this base class which provides:
- In-memory Entity Framework database (isolated per test)
- Mocked HttpClient with configurable responses
- Memory cache
- Database cache service
- Pre-seeded displays

### Helper Class: `TestDataHelper`

Contains organized constants and configuration templates for:
- Display configurations (BW and 3C)
- Google Fit integration settings
- iCal integration settings
- Weather service settings
- Common test dates and HTTP responses

## Using Pre-Seeded Displays

Every test automatically has access to two displays:

```csharp
public class MyServiceTests : IntegrationServiceTestBase
{
    [Fact]
    public async Task Test_WithBWDisplay()
    {
        var display = GetBWDisplay();
        // display.ColorType == "BW"
        // display.Width == 800
        // display.Height == 480
    }
    
    [Fact]
    public async Task Test_With3CDisplay()
    {
        var display = Get3CDisplay();
        // display.ColorType == "3C"
    }
    
    [Fact]
    public async Task Test_CompareBothDisplayTypes()
    {
        var bwDisplay = GetBWDisplay();
        var colorDisplay = Get3CDisplay();
        
        // Verify different bitmap output for each type
        var bwBitmap = GenerateBitmap(bwDisplay);
        var colorBitmap = GenerateBitmap(colorDisplay);
        
        Assert.NotEqual(bwBitmap, colorBitmap);
    }
}
```

**Benefits:**
- Consistent test data across all tests
- Easy to test display-type-specific behavior
- No setup needed for basic display scenarios

## Factory Methods

### Create Display with Specific Integration

```csharp
// Google Fit
var display = CreateDisplayWithGoogleFitTokens();
var display3C = CreateDisplayWithGoogleFitTokens(colorType: "3C");

// iCal
var display = CreateDisplayWithICalConfig();
var displayWithUrl = CreateDisplayWithICalConfig(url: "https://custom.com/cal.ics");

// Weather
var display = CreateDisplayWithWeatherConfig();
var customLocation = CreateDisplayWithWeatherConfig(
    latitude: "50.0755",
    longitude: "14.4378"
);
```

### Create Display with Custom Configs

```csharp
// Simple custom configs
var display = CreateDisplayWithConfigs(
    ("setting1", "value1"),
    ("setting2", "value2")
);

// Custom display properties + configs
var display = CreateDisplayWithConfigs(
    displayName: "CustomDisplay",
    mac: "aa:bb:cc:dd:ee:99",
    colorType: "3C",
    ("custom_setting", "custom_value")
);
```

### Add Configs to Existing Display

```csharp
// Add configs to pre-seeded display
var display = GetBWDisplay();
AddConfigsToDisplay(display, TestDataHelper.GoogleFit.StandardConfigs);

// Or add custom configs
AddConfigsToDisplay(display,
    ("key1", "value1"),
    ("key2", "value2")
);
```

## Using TestDataHelper Constants

### Display Configuration

```csharp
// Use standard display properties
var display = CreateTestDisplay(
    name: TestDataHelper.Displays.BlackAndWhite.Name,
    mac: TestDataHelper.Displays.BlackAndWhite.Mac,
    colorType: TestDataHelper.Displays.BlackAndWhite.ColorType
);
```

### Google Fit Configuration

```csharp
// Use all standard configs at once
AddConfigsToDisplay(display, TestDataHelper.GoogleFit.StandardConfigs);

// Or use individual constants
Context.Configs.Add(new Config
{
    DisplayId = display.Id,
    Name = TestDataHelper.GoogleFit.AccessTokenConfigName,
    Value = TestDataHelper.GoogleFit.TestAccessToken
});
```

### iCal Configuration

```csharp
// Standard URL
AddConfigsToDisplay(display, TestDataHelper.ICal.StandardConfigs());

// Custom URL
AddConfigsToDisplay(display, TestDataHelper.ICal.StandardConfigs(
    url: TestDataHelper.ICal.AlternativeTestUrl
));
```

### Weather Configuration

```csharp
// Default location (Prague)
AddConfigsToDisplay(display, TestDataHelper.Weather.StandardConfigs());

// Custom location
AddConfigsToDisplay(display, TestDataHelper.Weather.StandardConfigs(
    latitude: "51.5074",
    longitude: "-0.1278" // London
));
```

### Common Test Dates

```csharp
// Use standard test date range
var events = await service.GetEventsBetweenAsync(
    TestDataHelper.Dates.TestDateStart,    // 2024-01-15 00:00:00 UTC
    TestDataHelper.Dates.TestDateEnd        // 2024-01-17 00:00:00 UTC
);

// Use base test date
var today = TestDataHelper.Dates.BaseTestDate; // 2024-01-15 12:00:00 UTC
```

## Common Patterns

### Testing Integration Services

```csharp
public class GoogleFitIntegrationServiceTests : IntegrationServiceTestBase
{
    [Fact]
    public async Task GetWeightSeriesAsync_WhenNotConfigured_ReturnsEmpty()
    {
        // Use display without Google Fit config
        var display = GetBWDisplay();
        var service = CreateService();
        
        var result = await service.GetWeightSeriesAsync(display);
        
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task GetWeightSeriesAsync_WithValidConfig_ReturnsData()
    {
        // Use display with Google Fit config
        var display = CreateDisplayWithGoogleFitTokens();
        var service = CreateService();
        
        SetupHttpResponseForAnyUrl(mockData, HttpStatusCode.OK);
        
        var result = await service.GetWeightSeriesAsync(display);
        
        Assert.NotEmpty(result);
    }
}
```

### Testing Display-Type-Specific Behavior

```csharp
[Theory]
[InlineData("BW")]
[InlineData("3C")]
public async Task GenerateBitmap_DifferentOutputPerDisplayType(string colorType)
{
    var display = colorType == "BW" ? GetBWDisplay() : Get3CDisplay();
    
    var bitmap = await GenerateBitmapAsync(display);
    
    // Verify bitmap is correct for the display type
    Assert.Equal(display.ColorType, GetBitmapColorType(bitmap));
}
```

### Comparing BW and 3C Outputs

```csharp
[Fact]
public async Task API_ReturnsDifferentBitmapsForDifferentDisplayTypes()
{
    var bwDisplay = GetBWDisplay();
    var colorDisplay = Get3CDisplay();
    
    var bwBitmap = await apiClient.GetBitmapAsync(bwDisplay.Mac);
    var colorBitmap = await apiClient.GetBitmapAsync(colorDisplay.Mac);
    
    Assert.NotEqual(bwBitmap.Length, colorBitmap.Length);
    Assert.NotEqual(bwBitmap, colorBitmap);
}
```

## Best Practices

### ✅ DO

- Use pre-seeded displays (`GetBWDisplay()`, `Get3CDisplay()`) when you don't need special configuration
- Use factory methods for integration-specific tests
- Use `TestDataHelper` constants for maintainability
- Create new displays when you need custom configuration
- Add configs to pre-seeded displays when you want to combine scenarios

### ❌ DON'T

- Modify pre-seeded displays if it affects other tests (create new displays instead)
- Hardcode test values - use `TestDataHelper` constants
- Reuse displays across tests that modify state
- Skip `Context.Entry(display).Collection(d => d.Configs).Load()` when you need configs

## Adding New Test Data

### Adding New Integration Config

1. Add constants to `TestDataHelper`:

```csharp
public static class MyIntegration
{
    public const string ConfigName = "myintegration_config";
    public const string TestValue = "test_value";
    
    public static (string name, string value)[] StandardConfigs => new[]
    {
        (ConfigName, TestValue)
    };
}
```

2. Add factory method to `IntegrationServiceTestBase`:

```csharp
protected Display CreateDisplayWithMyIntegrationConfig(string? colorType = null)
{
    var display = CreateTestDisplay(colorType: colorType ?? "BW");
    AddConfigsToDisplay(display, TestDataHelper.MyIntegration.StandardConfigs);
    return display;
}
```

3. Use in tests:

```csharp
var display = CreateDisplayWithMyIntegrationConfig();
```

## Troubleshooting

### Display has no configs

```csharp
// Always reload configs before accessing them
Context.Entry(display).Collection(d => d.Configs).Load();
```

### Test affects other tests

- Each test gets its own database instance
- If tests are still interfering, ensure you're creating new displays, not reusing them

### Pre-seeded display is missing

```csharp
// This should never happen, but you can override SeedCommonTestData:
protected override void SeedCommonTestData()
{
    base.SeedCommonTestData();
    // Add custom seeding
}
```
