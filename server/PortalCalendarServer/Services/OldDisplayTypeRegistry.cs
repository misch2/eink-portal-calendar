using PortalCalendarServer.Models.ColorTypes;
using PortalCalendarServer.Models.Constants;

namespace PortalCalendarServer.Services
{
    public class OldDisplayTypeRegistry
    {
        private readonly Dictionary<string, IOldColorType> _oldColorTypes;

        public OldDisplayTypeRegistry()
        {
            _oldColorTypes = new Dictionary<string, IOldColorType>
            {
                [OldDisplayTypes.BW] = new OldBlackAndWhiteType(),
                [OldDisplayTypes.ThreeColor] = new OldThreeColorType(),
            };
        }

        public IOldColorType? GetOldColorType(string code)
        {
            return _oldColorTypes.TryGetValue(code, out var colorType) ? colorType : null;
        }

        public IEnumerable<IOldColorType> GetAllOldColorTypes()
        {
            return _oldColorTypes.Values;
        }
    }
}