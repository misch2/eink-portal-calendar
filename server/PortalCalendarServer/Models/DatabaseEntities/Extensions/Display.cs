using PortalCalendarServer.Models.POCOs;
using SixLabors.ImageSharp;

namespace PortalCalendarServer.Models.DatabaseEntities
{
    public partial class Display
    {
        public bool IsDefault()
        {
            return Id == 0;
        }

        public int VirtualWidth()
        {
            return Rotation is DisplayRotation.None or DisplayRotation.Rotate180 ? Width : Height;
        }

        public int VirtualHeight()
        {
            return Rotation is DisplayRotation.None or DisplayRotation.Rotate180 ? Height : Width;
        }

        public Dictionary<string, string> CssColorMap(bool forPreview)
        {
            return ColorVariant.ColorPaletteLinks.Select(link =>
                    (key: link.EpdColorCode, value: forPreview ? link.EffectiveEpdPreviewHexValue : link.EffectiveHexValue))
                .ToDictionary(x => x.key, x => $"#{x.value}");
        }

        public List<Color> ColorPalette(bool forPreview)
        {
            var hexColors = ColorVariant.ColorPaletteLinks
                .Select(link => forPreview ? link.EffectiveEpdPreviewHexValue : link.EffectiveHexValue)
                .ToList();

            var colors = new List<Color>();
            foreach (var hex in hexColors)
            {
                if (Color.TryParseHex(hex, out var color))
                {
                    colors.Add(color);
                }
            }

            return colors;
        }

    }
}
