using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Fitness.v1;
using Google.Apis.Fitness.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DTOs;
using PortalCalendarServer.Models.Entities;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Google Fit integration service using official Google.Apis packages.
/// </summary>
public class GoogleFitIntegrationService : IntegrationServiceBase
{
    private const int FetchDays = 90;
    private const int FetchDaysDuringSingleCall = 30;
    private readonly IDisplayService _displayService;

    public GoogleFitIntegrationService(
        ILogger<GoogleFitIntegrationService> logger,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        IDatabaseCacheServiceFactory databaseCacheFactory,
        CalendarContext context,
        IDisplayService displayService,
        Display? display = null)
        : base(logger, httpClientFactory, memoryCache, databaseCacheFactory, context, display)
    {
        _displayService = displayService;
    }

    public override bool IsConfigured()
    {
        if (display == null)
        {
            return false;
        }

        var accessToken = _displayService.GetConfig(display, "_googlefit_access_token");
        var refreshToken = _displayService.GetConfig(display, "_googlefit_refresh_token");
        return !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken);
    }

    private UserCredential? GetUserCredential()
    {
        if (!IsConfigured() || display == null)
        {
            return null;
        }

        var clientId = _displayService.GetConfig(display, "googlefit_client_id");
        var clientSecret = _displayService.GetConfig(display, "googlefit_client_secret");
        var accessToken = _displayService.GetConfig(display, "_googlefit_access_token");
        var refreshToken = _displayService.GetConfig(display, "_googlefit_refresh_token");

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            logger.LogError("Missing Google Fit OAuth configuration");
            return null;
        }

        var tokenResponse = new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
            Scopes = new[] { FitnessService.Scope.FitnessBodyRead }
        });

        return new UserCredential(flow, "user", tokenResponse);
    }

    private async Task<FitnessService?> GetFitnessServiceAsync(CancellationToken cancellationToken = default)
    {
        var credential = GetUserCredential();
        if (credential == null || display == null)
        {
            return null;
        }

        // Force token refresh if needed and save the new token
        await credential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);

        // Check if token was refreshed
        if (credential.Token.AccessToken != _displayService.GetConfig(display, "_googlefit_access_token"))
        {
            _displayService.SetConfig(display, "_googlefit_access_token", credential.Token.AccessToken);
            if (!string.IsNullOrEmpty(credential.Token.RefreshToken))
            {
                _displayService.SetConfig(display, "_googlefit_refresh_token", credential.Token.RefreshToken);
            }
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Access token refreshed successfully");
        }

        return new FitnessService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "PortalCalendarServer"
        });
    }

    public async Task<AggregateResponse?> FetchFromWebAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return null;
        }

        var dbCacheService = databaseCacheFactory.Create(nameof(GoogleFitIntegrationService), TimeSpan.FromMinutes(60));

        var globalDtStart = DateTime.UtcNow.AddDays(-(FetchDays - 1)).Date;
        var globalDtEnd = DateTime.UtcNow;

        return await dbCacheService.GetOrSetAsync(
            async () => await FetchFromWebInternalAsync(globalDtStart, globalDtEnd, cancellationToken),
            new { start = globalDtStart, end = globalDtEnd.Date },
            cancellationToken);
    }

    private async Task<AggregateResponse?> FetchFromWebInternalAsync(
        DateTime globalDtStart,
        DateTime globalDtEnd,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Requesting Google Fit data from {Start} to {End} for {Days} days",
            globalDtStart, globalDtEnd, FetchDays);

        var service = await GetFitnessServiceAsync(cancellationToken);
        if (service == null)
        {
            logger.LogError("Failed to create Google Fit service");
            return null;
        }

        var globalResponse = new AggregateResponse { Bucket = new List<AggregateBucket>() };

        var dtStart = globalDtStart;
        var dtEnd = dtStart.AddDays(FetchDaysDuringSingleCall).AddSeconds(-1);

        if (dtEnd > globalDtEnd)
        {
            dtEnd = globalDtEnd;
        }

        while (dtStart < globalDtEnd)
        {
            logger.LogTrace("Fetching data for {Start} - {End}", dtStart, dtEnd);

            var aggregateRequest = new AggregateRequest
            {
                AggregateBy = new List<AggregateBy>
                {
                    new AggregateBy
                    {
                        DataSourceId = "derived:com.google.weight:com.google.android.gms:merge_weight"
                    }
                },
                BucketByTime = new BucketByTime
                {
                    Period = new BucketByTimePeriod
                    {
                        Type = "day",
                        Value = 1,
                        TimeZoneId = _displayService.GetTimeZoneInfo(display!).Id
                    }
                },
                StartTimeMillis = new DateTimeOffset(dtStart).ToUnixTimeMilliseconds(),
                EndTimeMillis = new DateTimeOffset(dtEnd).ToUnixTimeMilliseconds()
            };

            try
            {
                var request = service.Users.Dataset.Aggregate(aggregateRequest, "me");
                var response = await request.ExecuteAsync(cancellationToken);

                if (response?.Bucket != null)
                {
                    foreach (var bucket in response.Bucket)
                    {
                        globalResponse.Bucket.Add(bucket);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching Google Fit data");
                throw new HttpRequestException($"Failed to fetch Google Fit data: {ex.Message}", ex);
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
        var result = new List<WeightDataPoint>();

        if (!IsConfigured())
        {
            return result;
        }

        var data = await FetchFromWebAsync(cancellationToken);
        if (data?.Bucket == null)
        {
            return result;
        }

        logger.LogDebug("Parsing Google Fit weight data...");

        foreach (var bucket in data.Bucket)
        {
            var startMillis = bucket.StartTimeMillis ?? 0;
            var start = DateTimeOffset.FromUnixTimeMilliseconds(startMillis).DateTime;

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
}