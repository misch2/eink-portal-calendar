using PortalCalendarServer.Models.ColorTypes;
using PortalCalendarServer.Models.Constants;

namespace PortalCalendarServer.Services
{
    public class DisplayTypeRegistry
    {
        private readonly Dictionary<string, IColorType> _colorTypes;

        public DisplayTypeRegistry()
        {
            _colorTypes = new Dictionary<string, IColorType>
            {
                [DisplayTypes.BW] = new BlackAndWhiteType(),
                [DisplayTypes.ThreeColor] = new ThreeColorType(),
            };
        }

        public IColorType? GetColorType(string code)
        {
            return _colorTypes.TryGetValue(code, out var colorType) ? colorType : null;
        }

        public IEnumerable<IColorType> GetAllColorTypes()
        {
            return _colorTypes.Values;
        }
    }
}