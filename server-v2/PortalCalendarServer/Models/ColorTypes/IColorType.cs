namespace PortalCalendarServer.Models.ColorTypes
{
    public interface IColorType
    {
        string Code { get; }
        string PrettyName { get; }
        int NumColors { get; }
        List<string> ColorPalette(bool forPreview);
    }
}