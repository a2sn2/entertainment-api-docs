using EntertainmentDocs.Application.Abstractions;
using FoundationKit.Application.Messaging;
using FoundationKit.Application.Results;

namespace EntertainmentDocs.Application.Documents;

public sealed record SubmitDocumentForReviewCommand(Guid DocumentId) : ICommand;

public sealed class SubmitDocumentForReviewCommandHandler(
    IDocumentRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<SubmitDocumentForReviewCommand>
{
    public async Task<Result> HandleAsync(
        SubmitDocumentForReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var document = await repository.GetWithVersionsAsync(command.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure(DocumentErrors.NotFound);

        try
        {
            document.SubmitForReview(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure(DocumentErrors.BusinessRule(exception.Message));
        }
    }
}
