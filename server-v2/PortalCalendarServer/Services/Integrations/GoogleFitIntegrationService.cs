using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.Entities;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Example integration service for Google Fit API.
/// Demonstrates how to use IntegrationServiceBase for external API calls with caching.
/// </summary>
public class GoogleFitIntegrationService : IntegrationServiceBase
{
    private const string GoogleOAuth2TokenUrl = "https://oauth2.googleapis.com/token";
    private const string GoogleFitDataUrl = "https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate";
    private const int FetchDays = 90;
    private const int FetchDaysDuringSingleCall = 30;

    public GoogleFitIntegrationService(
        ILogger<GoogleFitIntegrationService> logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        CalendarContext context,
        Display? display = null,
        int minimalCacheExpiry = 0)
        : base(logger, httpClientFactory, memoryCache, context, display, minimalCacheExpiry)
    {
    }

    protected override int HttpMaxCacheAge => 60 * 60; // 1 hour

    public bool IsAvailable()
    {
        var accessToken = GetConfigValue("_googlefit_access_token");
        var refreshToken = GetConfigValue("_googlefit_refresh_token");
        return !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken);
    }

    public async Task<string?> GetNewAccessTokenFromRefreshTokenAsync(
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting new access token from refresh token");

        var clientId = GetConfigValue("googlefit_client_id");
        var clientSecret = GetConfigValue("googlefit_client_secret");
        var redirectUri = GetConfigValue("googlefit_auth_callback");
        var refreshToken = GetConfigValue("_googlefit_refresh_token");

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) ||
            string.IsNullOrEmpty(redirectUri) || string.IsNullOrEmpty(refreshToken))
        {
            Logger.LogError("Missing required OAuth2 configuration");
            return null;
        }

        var httpClient = GetHttpClient();
        var parameters = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await httpClient.PostAsync(GoogleOAuth2TokenUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Logger.LogError("Error refreshing access token: {StatusCode} {Content}",
                response.StatusCode, errorContent);
            return null;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

        if (tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
        {
            var newAccessToken = accessTokenElement.GetString();
            SetConfigValue("_googlefit_access_token", newAccessToken);
            await Context.SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Access token refreshed successfully");
            return newAccessToken;
        }

        return null;
    }

    private async Task<HttpResponseMessage?> PerformAuthenticatedRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
        {
            Logger.LogWarning("Google Fit integration not available - missing tokens");
            return null;
        }

        var accessToken = GetConfigValue("_googlefit_access_token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var httpClient = GetHttpClient();
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogError("Unsuccessful response: {StatusCode}", response.StatusCode);

            var newAccessToken = await GetNewAccessTokenFromRefreshTokenAsync(cancellationToken);
            if (!string.IsNullOrEmpty(newAccessToken) && newAccessToken != accessToken)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                response = await httpClient.SendAsync(request, cancellationToken);
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Logger.LogError("Request failed: {StatusCode} {Content}",
                response.StatusCode, errorContent);
        }

        return response;
    }

    public async Task<GoogleFitAggregateResponse?> FetchFromWebAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
        {
            return null;
        }

        var dbCache = GetDatabaseCache();
        dbCache.MaxAge = 60 * 60; // 1 hour cache

        var globalDtStart = DateTime.UtcNow.AddDays(-(FetchDays - 1)).Date;
        var globalDtEnd = DateTime.UtcNow;

        return await dbCache.GetOrSetAsync(
            async () => await FetchFromWebInternalAsync(globalDtStart, globalDtEnd, cancellationToken),
            new { start = globalDtStart, end = globalDtEnd.Date },
            cancellationToken);
    }

    private async Task<GoogleFitAggregateResponse?> FetchFromWebInternalAsync(
        DateTime globalDtStart,
        DateTime globalDtEnd,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("Requesting Google Fit data from {Start} to {End} for {Days} days",
            globalDtStart, globalDtEnd, FetchDays);

        var globalResponse = new GoogleFitAggregateResponse { Bucket = new List<GoogleFitBucket>() };

        var dtStart = globalDtStart;
        var dtEnd = dtStart.AddDays(FetchDaysDuringSingleCall).AddSeconds(-1);

        if (dtEnd > globalDtEnd)
        {
            dtEnd = globalDtEnd;
        }

        while (dtStart < globalDtEnd)
        {
            Logger.LogTrace("Fetching data for {Start} - {End}", dtStart, dtEnd);

            var requestBody = new
            {
                aggregateBy = new[]
                {
                    new
                    {
                        dataSourceId = "derived:com.google.weight:com.google.android.gms:merge_weight"
                    }
                },
                bucketByTime = new
                {
                    period = new
                    {
                        type = "day",
                        value = 1,
                        timeZoneId = "Europe/Prague"
                    }
                },
                startTimeMillis = (long)(dtStart - DateTime.UnixEpoch).TotalMilliseconds,
                endTimeMillis = (long)(dtEnd - DateTime.UnixEpoch).TotalMilliseconds
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, GoogleFitDataUrl)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            var response = await PerformAuthenticatedRequestAsync(request, cancellationToken);
            if (response == null || !response.IsSuccessStatusCode)
            {
                var errorContent = response != null
                    ? await response.Content.ReadAsStringAsync(cancellationToken)
                    : "No response";
                Logger.LogError("Error fetching Google Fit data: {StatusCode} {Content}",
                    response?.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to fetch Google Fit data: {response?.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var batchResponse = JsonSerializer.Deserialize<GoogleFitAggregateResponse>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (batchResponse?.Bucket != null)
            {
                globalResponse.Bucket.AddRange(batchResponse.Bucket);
            }

            dtStart = dtStart.AddDays(FetchDaysDuringSingleCall);
            dtEnd = dtEnd.AddDays(FetchDaysDuringSingleCall);

            if (dtEnd > globalDtEnd)
            {
                dtEnd = globalDtEnd;
            }
        }

        return globalResponse;
    }

    public async Task<List<WeightDataPoint>> GetWeightSeriesAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
        {
            return new List<WeightDataPoint>();
        }

        var data = await FetchFromWebAsync(cancellationToken);
        if (data?.Bucket == null)
        {
            return new List<WeightDataPoint>();
        }

        Logger.LogDebug("Parsing Google Fit weight data...");

        var result = new List<WeightDataPoint>();

        foreach (var bucket in data.Bucket)
        {
            var start = DateTimeOffset.FromUnixTimeMilliseconds(bucket.StartTimeMillis).DateTime;

            var weight = bucket.Dataset?
                .FirstOrDefault()?.Point?
                .FirstOrDefault()?.Value?
                .FirstOrDefault()?.FpVal;

            if (weight.HasValue)
            {
                result.Add(new WeightDataPoint
                {
                    Date = start.Date,
                    Weight = (decimal)weight.Value
                });
            }
        }

        return result;
    }

    public async Task<decimal?> GetLastKnownWeightAsync(
        CancellationToken cancellationToken = default)
    {
        var series = await GetWeightSeriesAsync(cancellationToken);

        for (int i = series.Count - 1; i >= 0; i--)
        {
            if (series[i].Weight > 0)
            {
                return series[i].Weight;
            }
        }

        return null;
    }

    private string? GetConfigValue(string name)
    {
        if (Display == null)
        {
            return null;
        }

        return Display.Configs?.FirstOrDefault(c => c.Name == name)?.Value;
    }

    private void SetConfigValue(string name, string? value)
    {
        if (Display == null)
        {
            return;
        }

        var config = Context.Configs.FirstOrDefault(c =>
            c.DisplayId == Display.Id && c.Name == name);

        if (config != null)
        {
            config.Value = value;
        }
        else
        {
            Context.Configs.Add(new Config
            {
                DisplayId = Display.Id,
                Name = name,
                Value = value
            });
        }
    }
}

public class WeightDataPoint
{
    public DateTime Date { get; set; }
    public decimal Weight { get; set; }
}

public class GoogleFitAggregateResponse
{
    public List<GoogleFitBucket>? Bucket { get; set; }
}

public class GoogleFitBucket
{
    private long _startTimeMillis;
    private long _endTimeMillis;

    [JsonPropertyName("startTimeMillis")]
    public object? StartTimeMillisRaw { get; set; }

    [JsonPropertyName("endTimeMillis")]
    public object? EndTimeMillisRaw { get; set; }

    [JsonIgnore]
    public long StartTimeMillis
    {
        get => GoogleFitTimestampMillisConverter.ParseTimestampMillis(StartTimeMillisRaw, _startTimeMillis);
        set => _startTimeMillis = value;
    }

    [JsonIgnore]
    public long EndTimeMillis
    {
        get => GoogleFitTimestampMillisConverter.ParseTimestampMillis(EndTimeMillisRaw, _endTimeMillis);
        set => _endTimeMillis = value;
    }

    public List<GoogleFitDataset>? Dataset { get; set; }

}

public class GoogleFitDataset
{
    public List<GoogleFitPoint>? Point { get; set; }
}

public class GoogleFitPoint
{
    public List<GoogleFitValue>? Value { get; set; }
}

public class GoogleFitValue
{
    public double? FpVal { get; set; }
}

public class GoogleFitTimestampMillisConverter
{
    public static long ParseTimestampMillis(object? raw, long defaultValue)
    {
        if (raw == null) return defaultValue;

        return raw switch
        {
            JsonElement element => ParseJsonElement(element),
            string str when long.TryParse(str, out var result) => result,
            long longVal => longVal,
            _ => defaultValue
        };
    }

    private static long ParseJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetInt64(),
            JsonValueKind.String when long.TryParse(element.GetString(), out var result) => result,
            _ => 0
        };
    }

}