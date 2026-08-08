using FoundationKit.WebApi.Results;
using Madar.Api.Security;
using Madar.Application.Organization;
using Madar.Contracts.Organization;

namespace Madar.Api;

public static class DepartmentAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapMadarDepartmentAdministrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var departments = endpoints
            .MapGroup(DepartmentAdminRoutes.Root)
            .RequireAuthorization()
            .WithTags("Department Administration");

        departments.MapGet(
                "/",
                async (
                    IDepartmentAdministrationManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.ListAsync(cancellationToken))
                        .ToHttpResult(Results.Ok))
            .WithName("ListMadarAdminDepartments")
            .Produces<IReadOnlyList<DepartmentAdminDto>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        departments.MapPost(
                "/",
                async (
                    CreateDepartmentRequest request,
                    IDepartmentAdministrationManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.CreateAsync(request, cancellationToken))
                        .ToHttpResult(value => Results.Created(
                            DepartmentAdminRoutes.ById(value.Id),
                            value)))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("CreateMadarDepartment")
            .Produces<DepartmentAdminDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        departments.MapPut(
                "/{departmentId:guid}",
                async (
                    Guid departmentId,
                    UpdateDepartmentRequest request,
                    IDepartmentAdministrationManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.UpdateAsync(
                        departmentId,
                        request,
                        cancellationToken))
                    .ToHttpResult(Results.Ok))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("UpdateMadarDepartment")
            .Produces<DepartmentAdminDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        departments.MapGet(
                "/{departmentId:guid}/members",
                async (
                    Guid departmentId,
                    IDepartmentAdministrationManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.ListMembersAsync(
                        departmentId,
                        cancellationToken))
                    .ToHttpResult(Results.Ok))
            .WithName("ListMadarDepartmentMembers")
            .Produces<IReadOnlyList<DepartmentMemberDto>>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        departments.MapPost(
                "/{departmentId:guid}/members",
                async (
                    Guid departmentId,
                    AddDepartmentMemberRequest request,
                    IDepartmentAdministrationManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.AddMemberAsync(
                        departmentId,
                        request,
                        cancellationToken))
                    .ToHttpResult(value => Results.Created(
                        DepartmentAdminRoutes.Member(
                            departmentId,
                            value.UserId),
                        value)))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("AddMadarDepartmentMember")
            .Produces<DepartmentMemberDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        departments.MapDelete(
                "/{departmentId:guid}/members/{userId:guid}",
                async (
                    Guid departmentId,
                    Guid userId,
                    IDepartmentAdministrationManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.RemoveMemberAsync(
                        departmentId,
                        userId,
                        cancellationToken))
                    .ToHttpResult())
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("RemoveMadarDepartmentMember")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
}
