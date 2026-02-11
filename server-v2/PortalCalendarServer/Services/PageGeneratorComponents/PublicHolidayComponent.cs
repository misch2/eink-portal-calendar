namespace PortalCalendarServer.Services.PageGeneratorComponents;

using PublicHoliday;

public class PublicHolidayComponent : BaseComponent
{
    public PublicHolidayComponent(
        ILogger<PageGeneratorService> logger,
        DisplayService? displayService,
        DateTime date)
        : base(logger, displayService, date)
    {
    }

    // FIXME CZ only for now
    public PublicHolidayInfo? GetPublicHolidayInfo()
    {
        var provider = new CzechRepublicPublicHoliday();

        if (!provider.IsPublicHoliday(_date))
        {
            return null;
        }

        var holidays = provider.PublicHolidaysInformation(_date.Year);

        var holiday = holidays.FirstOrDefault(h => h.HolidayDate == _date);
        if (holiday == null)
        {
            // FIXME - this should not happen, but if it does, we should handle it gracefully
            return null;
        }

        return new PublicHolidayInfo
        {
            Name = holiday.EnglishName,
            LocalName = holiday.Name,
            Date = holiday.HolidayDate,
            CountryCode = "CZ"
        };
    }
}

public class PublicHolidayInfo
{
    public string Name { get; set; } = string.Empty;
    public string LocalName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CountryCode { get; set; } = string.Empty;
}
