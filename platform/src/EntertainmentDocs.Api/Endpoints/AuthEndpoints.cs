using EntertainmentDocs.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace EntertainmentDocs.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication").RequireRateLimiting("api");
        group.MapPost("/login", async (LoginRequest request, UserManager<ApplicationUser> users, ITokenService tokens) =>
        {
            var user = await users.FindByEmailAsync(request.Email);
            if (user is null || !user.IsActive || !await users.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();

            var roles = await users.GetRolesAsync(user);
            return Results.Ok(new { accessToken = tokens.CreateAccessToken(user, roles.ToArray()), user = new { user.Id, user.DisplayName, user.Email }, roles });
        });
        return app;
    }

    public sealed record LoginRequest(string Email, string Password);
}
