using EntertainmentDocs.Application.Abstractions;
using FoundationKit.Application.Messaging;
using FoundationKit.Application.Results;

namespace EntertainmentDocs.Application.Documents;

public sealed record AddDocumentVersionCommand(Guid DocumentId, string Version, string Content) : ICommand<Guid>;

public sealed class AddDocumentVersionCommandHandler(
    IDocumentRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : ICommandHandler<AddDocumentVersionCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        AddDocumentVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
            return Result<Guid>.Failure(DocumentErrors.AuthenticationRequired);

        var document = await repository.GetWithVersionsAsync(command.DocumentId, cancellationToken);
        if (document is null)
            return Result<Guid>.Failure(DocumentErrors.NotFound);

        try
        {
            var version = document.AddVersion(command.Version, command.Content, userId, clock.UtcNow);
            await repository.AddVersionAsync(version, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(version.Id);
        }
        catch (ArgumentException exception)
        {
            return Result<Guid>.Failure(Error.Validation("Documents.InvalidVersion", exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<Guid>.Failure(DocumentErrors.BusinessRule(exception.Message));
        }
    }
}
