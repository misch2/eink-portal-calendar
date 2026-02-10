using PortalCalendarServer.Models.Constants;

namespace PortalCalendarServer.Models.ColorTypes
{
    public class ThreeColorColorType : IColorType
    {
        public string Code => "3C";
        public string PrettyName => "Black & White & Color (red or yellow)";
        public int NumColors => 3;

        public List<string> GetColorPalette(bool forPreview)
        {
            var cssColors = EpdColors.GetColorMap(forPreview);
            return [
                cssColors[EpdColors.EPD_Black],
                cssColors[EpdColors.EPD_White],
                cssColors[EpdColors.EPD_Red],
                cssColors[EpdColors.EPD_Yellow]
            ];
        }
    }
}