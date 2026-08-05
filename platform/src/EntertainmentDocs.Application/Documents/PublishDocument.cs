using EntertainmentDocs.Application.Abstractions;
using FoundationKit.Application.Messaging;
using FoundationKit.Application.Results;

namespace EntertainmentDocs.Application.Documents;

public sealed record PublishDocumentCommand(Guid DocumentId) : ICommand;

public sealed class PublishDocumentCommandHandler(
    IDocumentRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<PublishDocumentCommand>
{
    public async Task<Result> HandleAsync(
        PublishDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var document = await repository.GetWithVersionsAsync(command.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure(DocumentErrors.NotFound);

        try
        {
            document.Publish(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure(DocumentErrors.BusinessRule(exception.Message));
        }
    }
}
