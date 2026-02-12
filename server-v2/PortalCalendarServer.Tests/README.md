# PortalCalendarServer.Tests

This project contains unit tests for the PortalCalendarServer application.

## Test Framework

- **xUnit** - Testing framework
- **Moq** - Mocking framework for creating test doubles
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database provider for testing

## Project Structure

```
PortalCalendarServer.Tests/
??? TestBase/
?   ??? IntegrationServiceTestBase.cs    # Base class for integration service tests
??? TestData/
?   ??? SampleIcsData.cs                 # Sample ICS calendar data for testing
??? Services/
?   ??? Integrations/
?       ??? ICalIntegrationServiceTests.cs   # Tests for ICalIntegrationService
??? PortalCalendarServer.Tests.csproj
```

## Using IntegrationServiceTestBase

The `IntegrationServiceTestBase` class provides common setup for testing integration services:

### Features

- **In-Memory Database**: Each test gets a fresh in-memory database
- **Mock HTTP Client**: HttpClient responses can be easily mocked
- **Memory Cache**: Real `IMemoryCache` instance for testing caching behavior
- **Logger**: Pre-configured logger for tests

### Example Usage

```csharp
public class MyIntegrationServiceTests : IntegrationServiceTestBase
{
    [Fact]
    public async Task MyTest()
    {
        // Arrange - Setup mock HTTP response
        SetupHttpResponse("https://api.example.com/data", "{\"key\":\"value\"}");
        
        // Create test display if needed
        var display = CreateTestDisplay();
        
        // Create service instance
        var service = new MyIntegrationService(
            Logger,
            MockHttpClientFactory.Object,
            MemoryCache,
            Context,
            display);
        
        // Act
        var result = await service.GetDataAsync();
        
        // Assert
        Assert.NotNull(result);
        
        // Verify HTTP was called
        VerifyHttpRequest("https://api.example.com/data", Times.Once());
    }
}
```

### Available Helper Methods

#### HTTP Mocking

- `SetupHttpResponse(url, content, statusCode)` - Mock a response for a specific URL
- `SetupHttpResponseForAnyUrl(content, statusCode)` - Mock a response for any URL
- `SetupHttpException(url, exception)` - Setup an HTTP request to throw an exception

#### Database

- `CreateTestDisplay(name, mac, width, height, colorType)` - Create a test display entity

#### Verification

- `VerifyHttpRequest(url, times)` - Verify an HTTP request was made to a specific URL
- `VerifyAnyHttpRequest(times)` - Verify any HTTP request was made

### Protected Properties

- `Context` - CalendarContext (in-memory database)
- `MockHttpClientFactory` - Mocked IHttpClientFactory
- `MockHttpMessageHandler` - Mocked HttpMessageHandler for fine-grained control
- `MemoryCache` - Real IMemoryCache instance
- `Logger` - ILogger instance
- `TestDisplay` - Optional test Display entity

## Running Tests

### Visual Studio
- Open Test Explorer (Test > Test Explorer)
- Click "Run All" to run all tests

### Command Line
```bash
dotnet test
```

### With Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Writing New Tests

1. Create a new test class that inherits from `IntegrationServiceTestBase`
2. Use the helper methods to set up mocks and test data
3. Follow the Arrange-Act-Assert pattern
4. Use descriptive test method names that explain what is being tested

### Test Naming Convention

Use the format: `MethodName_Scenario_ExpectedResult`

Examples:
- `GetEventsBetweenAsync_WithSimpleCalendar_ReturnsEvents`
- `GetEventsBetweenAsync_WithHttpError_ThrowsException`
- `GetEventsBetweenAsync_UsesCaching`

## Test Data

The `TestData` folder contains sample data for testing:

- `SampleIcsData.cs` - Various ICS calendar formats for testing

You can add more test data files as needed for different integration services.

## Best Practices

1. **Isolation**: Each test should be independent and not rely on other tests
2. **Cleanup**: The base class handles cleanup via `Dispose()`, but ensure you don't leak resources
3. **Clear Assertions**: Use descriptive assertion messages when helpful
4. **Test One Thing**: Each test should verify one specific behavior
5. **Mock External Dependencies**: Always mock HTTP calls and external services
6. **Use Test Data Constants**: Store test URLs and other constants to avoid magic strings

## Coverage Goals

Aim for:
- **Line Coverage**: > 80%
- **Branch Coverage**: > 70%
- **Critical Paths**: 100% coverage for critical business logic

## Troubleshooting

### Test Database Issues
If you see database-related errors, ensure:
- Each test uses a unique database name (automatically handled by the base class)
- Database context is properly disposed

### HTTP Mocking Issues
If HTTP requests aren't being mocked:
- Verify the URL matches exactly (including query parameters)
- Check that `SetupHttpResponse` is called before the service method
- Use `SetupHttpResponseForAnyUrl` if URL matching is problematic

### Cache Issues
If caching tests are flaky:
- Ensure each test uses a fresh `MemoryCache` instance (automatically handled)
- Clear cache between operations if testing cache invalidation
- Be aware that memory cache uses size limits (10MB by default)
