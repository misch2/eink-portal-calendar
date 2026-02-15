using PortalCalendarServer.Models.DTOs;
using PortalCalendarServer.Services.Integrations;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

public class WeightComponent
{
    private readonly ILogger<PageGeneratorService> _logger;
    private readonly GoogleFitIntegrationService _googleFitService;
    private readonly bool _isAvailable;

    public WeightComponent(
        ILogger<PageGeneratorService> logger,
        GoogleFitIntegrationService googleFitService)
    {
        _logger = logger;
        _googleFitService = googleFitService;
        _isAvailable = googleFitService.IsConfigured();
    }

    public decimal? GetLastWeight()
    {
        if (!_isAvailable)
        {
            _logger.LogDebug("Google Fit not available, returning null for LastWeight");
            return null;
        }

        _logger.LogDebug("Computing LastWeight from Google Fit");

        try
        {
            var task = _googleFitService.GetLastKnownWeightAsync();
            task.Wait();
            return task.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last weight from Google Fit");
            return null;
        }
    }

    public List<WeightDataPoint> GetWeightSeries(DateTime date)
    {
        if (!_isAvailable)
        {
            _logger.LogDebug("Google Fit not available, returning empty WeightSeries");
            return new List<WeightDataPoint>();
        }

        _logger.LogDebug("Computing WeightSeries from Google Fit");

        try
        {
            var task = _googleFitService.GetWeightSeriesAsync();
            task.Wait();
            return task.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weight series from Google Fit");
            return new List<WeightDataPoint>();
        }
    }
}
