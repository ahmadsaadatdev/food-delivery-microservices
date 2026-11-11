using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace FoodDelivery.BuildingBlocks.Web;

public static class ExceptionHandling
{
    public static IApplicationBuilder UseFoodDeliveryExceptionHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = feature?.Error;

                context.Response.ContentType = "application/json";

                switch (exception)
                {
                    case ValidationException validation:
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            title = "Validation failed",
                            errors = validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
                        });
                        break;

                    case UnauthorizedAccessException:
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { title = "Unauthorized" });
                        break;

                    case InvalidOperationException invalid when invalid.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase):
                        context.Response.StatusCode = StatusCodes.Status409Conflict;
                        await context.Response.WriteAsJsonAsync(new { title = invalid.Message });
                        break;

                    default:
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            title = "Unexpected error",
                            detail = exception?.Message
                        });
                        break;
                }
            });
        });

        return app;
    }
}
