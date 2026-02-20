using PortalCalendarServer.Models.Constants;

namespace PortalCalendarServer.Models.ColorTypes
{
    public class BlackAndWhiteType : IColorType
    {
        public string Code => "BW";
        public string PrettyName => "Black & White";
        public int NumColors => 2;

        public List<string> ColorPalette(bool forPreview)
        {
            var cssColors = EPDColors.GetColorMap(forPreview);
            return
            [
                cssColors[EPDColors.EPD_Black],
                cssColors[EPDColors.EPD_White]
            ];
        }
    }
}