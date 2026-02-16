namespace PortalCalendarServer.Models.Entities
{
    public partial class Display
    {
        public bool IsDefault()
        {
            return Id == 0;
        }

        public int VirtualWidth()
        {
            return Rotation % 180 == 0 ? Width : Height;
        }

        public int VirtualHeight()
        {
            return Rotation % 180 == 0 ? Height : Width;
        }

        public Dictionary<string, string> CssColorMap(bool forPreview)
        {
            var variants = new Dictionary<string, (string preview, string pure)>
            {
                ["epd_black"] = ("#111111", "#000000"),
                ["epd_white"] = ("#dddddd", "#ffffff"),
                ["epd_red"] = ("#aa0000", "#ff0000"),
                ["epd_yellow"] = ("#dddd00", "#ffff00")
            };

            var colors = new Dictionary<string, string>();
            foreach (var (key, (preview, pure)) in variants)
            {
                colors[key] = forPreview ? preview : pure;
            }

            return colors;
        }
    }
}
