using EntertainmentDocs.Contracts.Authentication;
using EntertainmentDocs.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace EntertainmentDocs.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication")
            .RequireRateLimiting("api");

        group.MapPost("/login", async (LoginRequest request, UserManager<ApplicationUser> users, ITokenService tokens) =>
        {
            var user = await users.FindByEmailAsync(request.Email);
            if (user is null || !user.IsActive || !await users.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();

            var roles = await users.GetRolesAsync(user);
            var response = new LoginResponse(
                tokens.CreateAccessToken(user, roles.ToArray()),
                new AuthenticatedUserResponse(user.Id, user.DisplayName, user.Email),
                roles.ToArray());

            return Results.Ok(response);
        })
        .WithName("Login")
        .WithSummary("Authenticate a platform user and issue a JWT access token.")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
