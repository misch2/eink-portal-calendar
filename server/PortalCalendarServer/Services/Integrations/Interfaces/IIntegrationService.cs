using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Services.Integrations
{
    public interface IIntegrationService
    {
        public bool IsConfigured(Display display);
    }
}
