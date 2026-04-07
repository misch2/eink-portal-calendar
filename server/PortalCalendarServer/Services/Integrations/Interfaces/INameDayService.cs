namespace PortalCalendarServer.Services.Integrations
{
    public interface INameDayService : IIntegrationService
    {
        NameDayInfo? GetNameDay(DateTime date, string countryCode);
        List<NameDayInfo> GetNameDaysForMonth(int year, int month, string countryCode);
    }

    /// <summary>
    /// Name day information for Czech calendar
    /// </summary>
    public class NameDayInfo
    {
        /// <summary>
        /// Date of the name day
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Name(s) celebrated on this day
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Additional description or alternative names
        /// </summary>
        public string? Description { get; set; }
    }
}
