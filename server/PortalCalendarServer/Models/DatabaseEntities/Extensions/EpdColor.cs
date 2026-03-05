using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PortalCalendarServer.Models.DatabaseEntities
{
    public partial class EpdColor
    {
        public bool IsWhite { get { return Code == "white"; } }
        public bool IsBlack { get { return Code == "black"; } }
        public bool IsRed { get { return Code == "red"; } }
        public bool IsYellow { get { return Code == "yellow"; } }

        private Rgba32? _epdPreviewRgba32;
        // Parsed RGBA32 representation of HexValue for fast pixel comparison.
        public Rgba32 EpdPreviewRgba32Value => _epdPreviewRgba32 ??= Color.ParseHex(EpdPreviewHexValue).ToPixel<Rgba32>();
    }
}
