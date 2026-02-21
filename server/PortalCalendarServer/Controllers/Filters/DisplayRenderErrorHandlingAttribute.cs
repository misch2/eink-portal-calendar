namespace PortalCalendarServer.Controllers.Filters;

/// <summary>
/// Marks a controller action so that <see cref="PortalCalendarServer.Controllers.UiController"/>
/// intercepts view-rendering exceptions and renders the calendar error theme instead.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DisplayRenderErrorHandlingAttribute : Attribute { }
