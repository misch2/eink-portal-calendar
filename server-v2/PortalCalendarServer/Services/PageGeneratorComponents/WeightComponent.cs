namespace PortalCalendarServer.Services.PageGeneratorComponents;

public class WeightComponent(ILogger<PageGeneratorService> logger)
{
    private readonly ILogger<PageGeneratorService> _logger = logger;

    public decimal? GetLastWeight()
    {
        _logger.LogDebug("Computing LastWeight on demand");
        // TODO: Replace with actual Google Fit integration
        return 106.8m;
    }

    public List<WeightDataPoint> GetWeightSeries(DateTime date)
    {
        _logger.LogDebug("Computing WeightSeries on demand");
        // TODO: Replace with actual Google Fit integration
        Random random = new Random(date.GetHashCode());
        return Enumerable.Range(0, 90)
            .Select(i => new WeightDataPoint
            {
                Date = date.AddDays(-89 + i),
                Weight = 110m - i * 0.1m + (decimal)(random.NextDouble() * 2 - 1)
            })
            .ToList();
    }
}

public class WeightDataPoint
{
    public DateTime Date { get; set; }
    public decimal Weight { get; set; }
}
