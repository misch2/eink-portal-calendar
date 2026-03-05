using PortalCalendarServer.Models.DatabaseEntities;
using SixLabors.ImageSharp;

namespace PortalCalendarServer.Models.POCOs.Bitmap
{
    public enum OutputFormat
    {
        Png,
        EpaperSpecific
    }

    public class BitmapOptions
    {
        public int Rotate { get; set; }
        public string Flip { get; set; } = string.Empty;
        public double Gamma { get; set; } = 1.0;
        public int NumColors { get; set; } = 256;
        /// <summary>
        /// Either "none" or "webmap". Originally used for the Perl Imager module.
        /// </summary>
        public List<Color> ColormapColors { get; set; } = new();
        public OutputFormat Format { get; set; } = OutputFormat.Png;
        public required DisplayType DisplayType { get; set; }
        public string? DitheringType { get; set; } = null;
    }

    public class BitmapResult
    {
        public string? ErrorMessage { get; set; } = null;
        public byte[] Data { get; set; } = [];
        public string ContentType { get; set; } = "x-unknown/x-unknown";
        public Dictionary<string, string>? Headers { get; set; }
    }
}