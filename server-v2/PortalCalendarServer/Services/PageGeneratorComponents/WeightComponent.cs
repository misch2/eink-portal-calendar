namespace PortalCalendarServer.Services.PageGeneratorComponents;

public class WeightComponent : BaseComponent
{
    public decimal? LastWeight { get; set; }
    public List<WeightDataPoint>? WeightSeries { get; set; }

    public WeightComponent(ILogger<PageGeneratorService> logger, DateTime date, Random random)
        : base(logger, null, date)
    {
        LastWeight = _getLastWeight();
        WeightSeries = _GetWeightSeries(random);
    }

    private decimal? _getLastWeight()
    {
        _logger.LogDebug("Computing LastWeight on demand");
        // TODO: Replace with actual Google Fit integration
        return 106.8m;
    }

    private List<WeightDataPoint>? _GetWeightSeries(Random random)
    {
        _logger.LogDebug("Computing WeightSeries on demand");
        // TODO: Replace with actual Google Fit integration
        return Enumerable.Range(0, 90)
            .Select(i => new WeightDataPoint
            {
                Date = _date.AddDays(-89 + i),
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
