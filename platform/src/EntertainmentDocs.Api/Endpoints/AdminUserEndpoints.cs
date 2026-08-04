using EntertainmentDocs.Api.Authorization;
using EntertainmentDocs.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EntertainmentDocs.Api.Endpoints;

public static class AdminUserEndpoints
{
    public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/users")
            .RequireAuthorization(Policies.ManageUsers)
            .RequireRateLimiting("api")
            .WithTags("Admin Users");

        group.MapGet("/", async (UserManager<ApplicationUser> users, CancellationToken ct) =>
            Results.Ok(await users.Users.Select(x => new { x.Id, x.DisplayName, x.Email, x.IsActive }).ToListAsync(ct)));

        group.MapPost("/", async (CreateUserRequest request, UserManager<ApplicationUser> users) =>
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = request.Email, Email = request.Email, DisplayName = request.DisplayName, EmailConfirmed = true };
            var result = await users.CreateAsync(user, request.TemporaryPassword);
            if (!result.Succeeded) return Results.BadRequest(result.Errors);
            foreach (var role in request.Roles.Distinct())
                if (SystemRoles.All.Contains(role)) await users.AddToRoleAsync(user, role);
            return Results.Created($"/api/v1/admin/users/{user.Id}", new { user.Id });
        });

        group.MapPut("/{id:guid}/roles", async (Guid id, UpdateRolesRequest request, UserManager<ApplicationUser> users) =>
        {
            var user = await users.FindByIdAsync(id.ToString());
            if (user is null) return Results.NotFound();
            var current = await users.GetRolesAsync(user);
            await users.RemoveFromRolesAsync(user, current);
            await users.AddToRolesAsync(user, request.Roles.Where(role => SystemRoles.All.Contains(role)).Distinct());
            return Results.NoContent();
        });

        return app;
    }

    public sealed record CreateUserRequest(string Email, string DisplayName, string TemporaryPassword, string[] Roles);
    public sealed record UpdateRolesRequest(string[] Roles);
}
