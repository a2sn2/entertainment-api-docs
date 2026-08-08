using Microsoft.AspNetCore.Antiforgery;

namespace Madar.Api.Security;

public sealed class AntiforgeryEndpointFilter(
    IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "InvalidAntiforgeryToken",
                detail: "تعذر التحقق من طلب الأمان. حدّث الصفحة وحاول مرة أخرى.");
        }

        return await next(context);
    }
}
