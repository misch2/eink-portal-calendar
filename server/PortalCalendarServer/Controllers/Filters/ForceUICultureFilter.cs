using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Globalization;

namespace PortalCalendarServer.Controllers.Filters;

public class ForceUICultureFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.HttpContext.Items.TryGetValue("ForcedUICulture", out var v) && v is string cultureName)
        {
            var culture = new CultureInfo(cultureName);

            //CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            context.HttpContext.Features.Set<IRequestCultureFeature>(
                new RequestCultureFeature(new RequestCulture(culture), new CookieRequestCultureProvider())
            );
        }

        await next(); // now the view result executes with the forced culture
    }
}