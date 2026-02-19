namespace PortalCalendarServer.Tests.TestData;

/// <summary>
/// Helper class containing shared test data constants and configuration values
/// </summary>
public static class TestDataHelper
{
    /// <summary>
    /// Common display configurations
    /// </summary>
    public static class Displays
    {
        /// <summary>
        /// Standard black and white e-ink display configuration
        /// </summary>
        public static class BlackAndWhite
        {
            public const string Name = "TestDisplay_BW";
            public const string Mac = "aa:bb:cc:dd:ee:01";
            public const int Width = 800;
            public const int Height = 480;
            public const string ColorType = "BW";
        }

        /// <summary>
        /// Three-color (black, white, red/yellow) e-ink display configuration
        /// </summary>
        public static class ThreeColor
        {
            public const string Name = "TestDisplay_3C";
            public const string Mac = "aa:bb:cc:dd:ee:02";
            public const int Width = 800;
            public const int Height = 480;
            public const string ColorType = "3C";
        }

        public static class Common
        {
            public const int DefaultRotation = 0;
            public const double DefaultGamma = 2.2;
            public const int DefaultBorder = 0;
            public const string DefaultFirmware = "1.0.0";
        }
    }

    /// <summary>
    /// Google Fit integration test data
    /// </summary>
    public static class GoogleFit
    {
        public const string AccessTokenConfigName = "_googlefit_access_token";
        public const string RefreshTokenConfigName = "_googlefit_refresh_token";
        public const string ClientIdConfigName = "googlefit_client_id";
        public const string ClientSecretConfigName = "googlefit_client_secret";
        public const string AuthCallbackConfigName = "googlefit_auth_callback";

        public const string TestAccessToken = "test_access_token_123";
        public const string TestRefreshToken = "test_refresh_token_456";
        public const string TestClientId = "test_client_id";
        public const string TestClientSecret = "test_client_secret";
        public const string TestAuthCallback = "https://example.com/callback";

        /// <summary>
        /// Returns standard Google Fit configuration as tuples
        /// </summary>
        public static (string name, string value)[] StandardConfigs => new[]
        {
            (AccessTokenConfigName, TestAccessToken),
            (RefreshTokenConfigName, TestRefreshToken),
            (ClientIdConfigName, TestClientId),
            (ClientSecretConfigName, TestClientSecret),
            (AuthCallbackConfigName, TestAuthCallback)
        };
    }

    /// <summary>
    /// iCal integration test data
    /// </summary>
    public static class ICal
    {
        public const string DefaultTestUrl = "https://example.com/calendar.ics";
        public const string AlternativeTestUrl = "https://test.example.com/events.ics";

        public const string UrlConfigName = "ical_url";

        /// <summary>
        /// Returns standard iCal configuration as tuples
        /// </summary>
        public static (string name, string value)[] StandardConfigs(string? url = null) => new[]
        {
            (UrlConfigName, url ?? DefaultTestUrl)
        };
    }

    /// <summary>
    /// Weather service test data
    /// </summary>
    public static class Weather
    {
        public const string LocationConfigName = "weather_location";
        public const string DefaultLatitude = "50.0755";
        public const string DefaultLongitude = "14.4378"; // Prague coordinates

        /// <summary>
        /// Returns standard weather configuration as tuples
        /// </summary>
        public static (string name, string value)[] StandardConfigs(string? latitude = null, string? longitude = null) => new[]
        {
            ("weather_latitude", latitude ?? DefaultLatitude),
            ("weather_longitude", longitude ?? DefaultLongitude)
        };
    }

    /// <summary>
    /// Common test dates and times
    /// </summary>
    public static class Dates
    {
        public static DateTime BaseTestDate => new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        public static DateTime TestDateStart => new(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        public static DateTime TestDateEnd => new(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// HTTP response test data
    /// </summary>
    public static class Http
    {
        public const string ErrorResponseUnauthorized = "{\"error\": \"unauthorized\"}";
        public const string ErrorResponseNotFound = "{\"error\": \"not_found\"}";
        public const string ErrorResponseServerError = "{\"error\": \"internal_server_error\"}";
    }
}
