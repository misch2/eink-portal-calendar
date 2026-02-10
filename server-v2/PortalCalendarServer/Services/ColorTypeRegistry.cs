using PortalCalendarServer.Models.ColorTypes;

namespace PortalCalendarServer.Services
{
    public class ColorTypeRegistry
    {
        private readonly Dictionary<string, IColorType> _colorTypes;

        public ColorTypeRegistry()
        {
            _colorTypes = new Dictionary<string, IColorType>
            {
                ["BW"] = new BlackAndWhiteColorType(),
                ["3C"] = new ThreeColorColorType(),
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