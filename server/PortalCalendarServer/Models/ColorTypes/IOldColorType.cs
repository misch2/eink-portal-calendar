namespace PortalCalendarServer.Models.ColorTypes
{
    public interface IOldColorType
    {
        string Code { get; }
        string PrettyName { get; }
        int NumColors { get; }
        List<string> ColorPalette(bool forPreview);
    }
}