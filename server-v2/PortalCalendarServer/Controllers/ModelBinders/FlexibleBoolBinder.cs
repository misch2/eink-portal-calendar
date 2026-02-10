using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PortalCalendarServer.Controllers.ModelBinders
{
    public class FlexibleBoolBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext context)
        {
            var value = context.ValueProvider.GetValue(context.ModelName).FirstValue;

            if (string.IsNullOrWhiteSpace(value))
            {
                context.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            var normalized = value.Trim().ToLowerInvariant();

            bool result = normalized switch
            {
                "1" => true,
                "0" => false,
                "true" => true,
                "false" => false,
                _ => false
            };

            context.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }
    }

    public class FlexibleBoolBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(bool) ||
                context.Metadata.ModelType == typeof(bool?))
            {
                return new FlexibleBoolBinder();
            }

            return null;
        }
    }
}
