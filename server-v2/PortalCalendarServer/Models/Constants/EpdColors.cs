namespace PortalCalendarServer.Models.Constants
{
    public class ColorPreviewPair
    {
        public required string Preview { get; init; }
        public required string Original { get; init; }
    }

    public static class EpdColors
    {
        public static string EPD_Black => "epd_black";
        public static string EPD_White => "epd_white";
        public static string EPD_Red => "epd_red";
        public static string EPD_Yellow => "epd_yellow";


        public static readonly Dictionary<string, ColorPreviewPair> Definitions = new()
        {
            [EPD_Black] = new ColorPreviewPair { Preview = "#111111", Original = "#000000" },
            [EPD_White] = new ColorPreviewPair { Preview = "#dddddd", Original = "#ffffff" },
            [EPD_Red] = new ColorPreviewPair { Preview = "#aa0000", Original = "#ff0000" },
            [EPD_Yellow] = new ColorPreviewPair { Preview = "#dddd00", Original = "#ffff00" }
        };

        public static Dictionary<string, string> GetColorMap(bool forPreview = false)
        {
            var colors = new Dictionary<string, string>();
            foreach (var (key, colorPair) in Definitions)
            {
                colors[key] = forPreview ? colorPair.Preview : colorPair.Original;
            }
            return colors;
        }
    }
}