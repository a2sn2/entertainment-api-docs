using FoundationKit.WebApi.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FoundationKit.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddFoundationWebApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["correlationId"] =
                    context.HttpContext.TraceIdentifier;
            };
        });

        return services;
    }

    public static IApplicationBuilder UseFoundationRequestPipeline(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
