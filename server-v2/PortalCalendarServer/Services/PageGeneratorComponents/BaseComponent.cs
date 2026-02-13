namespace PortalCalendarServer.Services.PageGeneratorComponents
{
    public class BaseComponent
    {
        protected readonly ILogger<PageGeneratorService> _logger;
        protected readonly DisplayService _displayService;

        public BaseComponent(
            ILogger<PageGeneratorService> logger,
            DisplayService displayService)
        {
            _logger = logger;
            _displayService = displayService;
        }
    }
}
