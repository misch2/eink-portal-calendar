namespace PortalCalendarServer.Services.PageGeneratorComponents
{
    public class BaseComponent
    {
        protected readonly ILogger<PageGeneratorService> _logger;
        protected readonly DisplayService? _displayService;
        protected readonly DateTime _date;

        public BaseComponent(
            ILogger<PageGeneratorService> logger,
            DisplayService? displayService,
            DateTime date)
        {
            _logger = logger;
            _displayService = displayService;
            _date = date;
        }
    }
}
