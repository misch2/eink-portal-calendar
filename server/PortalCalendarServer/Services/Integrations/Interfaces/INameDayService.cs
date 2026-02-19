namespace PortalCalendarServer.Services.Integrations
{
    public interface INameDayService : IIntegrationService
    {
        NameDayInfo? GetNameDay(DateTime date, string countryCode = "CZ");
        List<NameDayInfo> GetNameDaysForMonth(int year, int month, string countryCode = "CZ");
    }

    /// <summary>
    /// Name day information for Czech calendar
    /// </summary>
    public class NameDayInfo
    {
        /// <summary>
        /// Name(s) celebrated on this day
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Date of the name day
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Country code (e.g., "CZ" for Czech Republic)
        /// </summary>
        public string CountryCode { get; set; } = "CZ";

        /// <summary>
        /// Additional description or alternative names
        /// </summary>
        public string? Description { get; set; }
    }
}
