using FoundationKit.WebApi.Results;
using Madar.Api.Security;
using Madar.Application.Cases;
using Madar.Contracts.Cases;

namespace Madar.Api;

public static class CaseCommentEndpoints
{
    public static IEndpointRouteBuilder MapMadarCaseCommentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/cases/{caseId:guid}/comments",
                async (
                    Guid caseId,
                    ICaseCommentManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.ListAsync(caseId, cancellationToken))
                        .ToHttpResult(Results.Ok))
            .RequireAuthorization()
            .WithTags("Case Comments")
            .WithName("ListMadarCaseComments")
            .Produces<IReadOnlyList<CaseCommentDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost(
                "/api/cases/{caseId:guid}/comments",
                async (
                    Guid caseId,
                    AddCaseCommentRequest request,
                    ICaseCommentManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.AddAsync(caseId, request, cancellationToken))
                        .ToHttpResult(value => Results.Created(
                            $"/api/cases/{caseId:D}/comments",
                            value)))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithTags("Case Comments")
            .WithName("AddMadarCaseComment")
            .Produces<CaseCommentDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
