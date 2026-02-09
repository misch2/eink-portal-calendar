namespace PortalCalendarServer.Models.Web
{
    public class CSSColor
    {
        Dictionary<string, CSSColorPair> _variants;

        public CSSColor()
        {
            _variants = new Dictionary<string, CSSColorPair>
            {
                ["epd_black"] = new CSSColorPair { Preview = "#111111", Original = "#000000" },
                ["epd_white"] = new CSSColorPair { Preview = "#dddddd", Original = "#ffffff" },
                ["epd_red"] = new CSSColorPair { Preview = "#aa0000", Original = "#ff0000" },
                ["epd_yellow"] = new CSSColorPair { Preview = "#dddd00", Original = "#ffff00" }
            };
        }

        public class CSSColorPair
        {
            public required string Preview { get; set; }
            public required string Original { get; set; }
        };

        // Returns a dictionary of CSS color names to their hex values. If forPreview is true, returns the preview colors; otherwise, returns the original colors.
        public Dictionary<string, string> CssColorMap(bool forPreview = false)
        {

            var colors = new Dictionary<string, string>();
            foreach (var (key, color) in _variants)
            {
                colors[key] = forPreview ? color.Preview : color.Original;
            }

            return colors;
        }

    }
}
