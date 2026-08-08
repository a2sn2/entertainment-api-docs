using FoundationKit.WebApi.Results;
using Madar.Api.Security;
using Madar.Application.Cases;
using Madar.Contracts.Cases;

namespace Madar.Api;

public static class CaseApprovalEndpoints
{
    public static IEndpointRouteBuilder MapMadarCaseApprovalEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/cases/{caseId:guid}/approvals",
                async (
                    Guid caseId,
                    ICaseApprovalManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.ListAsync(caseId, cancellationToken))
                        .ToHttpResult(Results.Ok))
            .RequireAuthorization()
            .WithTags("Case Approvals")
            .WithName("ListMadarCaseApprovals")
            .Produces<IReadOnlyList<CaseApprovalDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost(
                "/api/cases/{caseId:guid}/approvals",
                async (
                    Guid caseId,
                    ICaseApprovalManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.RequestAsync(caseId, cancellationToken))
                        .ToHttpResult(value => Results.Created(
                            $"/api/cases/{caseId:D}/approvals/{value.Id:D}",
                            value)))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithTags("Case Approvals")
            .WithName("RequestMadarCaseApproval")
            .Produces<CaseApprovalDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPost(
                "/api/cases/{caseId:guid}/approvals/{approvalId:guid}/decision",
                async (
                    Guid caseId,
                    Guid approvalId,
                    DecideCaseApprovalRequest request,
                    ICaseApprovalManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.DecideAsync(
                        caseId,
                        approvalId,
                        request,
                        cancellationToken))
                    .ToHttpResult(Results.Ok))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithTags("Case Approvals")
            .WithName("DecideMadarCaseApproval")
            .Produces<CaseApprovalDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
}
