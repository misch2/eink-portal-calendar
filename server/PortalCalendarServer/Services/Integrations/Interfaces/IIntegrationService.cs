using PortalCalendarServer.Models.Entities;

namespace PortalCalendarServer.Services.Integrations
{
    public interface IIntegrationService
    {
        public bool IsConfigured(Display display);
    }
}
