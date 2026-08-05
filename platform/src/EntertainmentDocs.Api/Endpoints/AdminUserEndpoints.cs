using EntertainmentDocs.Api.Authorization;
using EntertainmentDocs.Contracts.Common;
using EntertainmentDocs.Contracts.Users;
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
        {
            var entities = await users.Users
                .AsNoTracking()
                .OrderBy(x => x.DisplayName)
                .ToListAsync(ct);

            var response = new List<UserSummaryResponse>(entities.Count);
            foreach (var user in entities)
            {
                var roles = await users.GetRolesAsync(user);
                response.Add(new UserSummaryResponse(
                    user.Id,
                    user.DisplayName,
                    user.Email,
                    user.IsActive,
                    roles.ToArray()));
            }

            return Results.Ok(response);
        })
        .WithName("ListUsers")
        .WithSummary("List platform users with their active state and assigned roles.")
        .Produces<IReadOnlyList<UserSummaryResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/", async (CreateUserRequest request, UserManager<ApplicationUser> users) =>
        {
            var requestedRoles = (request.Roles ?? [])
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var invalidRoles = requestedRoles.Except(SystemRoles.All, StringComparer.Ordinal).ToArray();
            if (invalidRoles.Length > 0)
                return Results.BadRequest(new ApiErrorResponse($"Unsupported roles: {string.Join(", ", invalidRoles)}."));

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                DisplayName = request.DisplayName,
                EmailConfirmed = true
            };

            var result = await users.CreateAsync(user, request.TemporaryPassword);
            if (!result.Succeeded)
            {
                var details = string.Join(" ", result.Errors.Select(error => error.Description));
                return Results.BadRequest(new ApiErrorResponse(details));
            }

            if (requestedRoles.Length > 0)
                await users.AddToRolesAsync(user, requestedRoles);

            return Results.Created($"/api/v1/admin/users/{user.Id}", new CreatedUserResponse(user.Id));
        })
        .WithName("CreateUser")
        .WithSummary("Create a platform user and assign one or more system roles.")
        .Produces<CreatedUserResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}/roles", async (Guid id, UpdateUserRolesRequest request, UserManager<ApplicationUser> users) =>
        {
            var user = await users.FindByIdAsync(id.ToString());
            if (user is null)
                return Results.NotFound();

            var requestedRoles = (request.Roles ?? [])
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var invalidRoles = requestedRoles.Except(SystemRoles.All, StringComparer.Ordinal).ToArray();
            if (invalidRoles.Length > 0)
                return Results.BadRequest(new ApiErrorResponse($"Unsupported roles: {string.Join(", ", invalidRoles)}."));

            var currentRoles = await users.GetRolesAsync(user);
            var removeResult = await users.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                return Results.BadRequest(new ApiErrorResponse("Current roles could not be removed."));

            if (requestedRoles.Length > 0)
            {
                var addResult = await users.AddToRolesAsync(user, requestedRoles);
                if (!addResult.Succeeded)
                    return Results.BadRequest(new ApiErrorResponse("The requested roles could not be assigned."));
            }

            return Results.NoContent();
        })
        .WithName("UpdateUserRoles")
        .WithSummary("Replace all roles assigned to a platform user.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
