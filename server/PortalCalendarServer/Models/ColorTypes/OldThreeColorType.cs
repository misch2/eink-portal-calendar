using PortalCalendarServer.Models.Constants;

namespace PortalCalendarServer.Models.ColorTypes
{
    public class OldThreeColorType : IOldColorType
    {
        public string Code => "3C";
        public string PrettyName => "Black & White & Color (red or yellow)";
        public int NumColors => 3;

        public List<string> ColorPalette(bool forPreview)
        {
            var cssColors = EPDColors.GetColorMap(forPreview);
            return [
                cssColors[EPDColors.EPD_Black],
                cssColors[EPDColors.EPD_White],
                cssColors[EPDColors.EPD_Red],
                cssColors[EPDColors.EPD_Yellow]
            ];
        }
    }
}