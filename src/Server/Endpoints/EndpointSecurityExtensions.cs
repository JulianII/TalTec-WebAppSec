using Microsoft.AspNetCore.Antiforgery;

public static class EndpointSecurityExtensions
{
    public static RouteHandlerBuilder RequireCsrfProtection(this RouteHandlerBuilder endpoint)
    {
        return endpoint.AddEndpointFilter(async (context, next) =>
        {
            try
            {
                var antiforgery = context.HttpContext.RequestServices
                    .GetRequiredService<IAntiforgery>();
                await antiforgery.ValidateRequestAsync(context.HttpContext);
                return await next(context);
            }
            catch (AntiforgeryValidationException)
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Antiforgery");
                logger.LogWarning(
                    "Rejected request with an invalid anti-forgery token. TraceId: {TraceId}",
                    context.HttpContext.TraceIdentifier);
                return Results.BadRequest(new
                {
                    message = "The request could not be verified. Refresh the page and try again."
                });
            }
        });
    }
}
