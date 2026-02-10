using PortalCalendarServer.Models.Constants;

namespace PortalCalendarServer.Models.ColorTypes
{
    public class BlackAndWhiteColorType : IColorType
    {
        public string Code => "BW";
        public string PrettyName => "Black & White";
        public int NumColors => 2;

        public List<string> GetColorPalette(bool forPreview)
        {
            var cssColors = EpdColors.GetColorMap(forPreview);
            return 
            [
                cssColors[EpdColors.EPD_Black],
                cssColors[EpdColors.EPD_White]
            ];
        }
    }
}