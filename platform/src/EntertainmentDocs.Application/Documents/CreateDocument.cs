using EntertainmentDocs.Application.Abstractions;
using EntertainmentDocs.Domain.Documents;
using FoundationKit.Application.Messaging;
using FoundationKit.Application.Results;

namespace EntertainmentDocs.Application.Documents;

public sealed record CreateDocumentCommand(string Reference, string Slug, string Title) : ICommand<Guid>;

public sealed class CreateDocumentCommandHandler(
    IDocumentRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : ICommandHandler<CreateDocumentCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
            return Result<Guid>.Failure(DocumentErrors.AuthenticationRequired);
        if (await repository.ReferenceExistsAsync(command.Reference, cancellationToken))
            return Result<Guid>.Failure(DocumentErrors.ReferenceAlreadyExists);
        if (await repository.SlugExistsAsync(command.Slug, cancellationToken))
            return Result<Guid>.Failure(DocumentErrors.SlugAlreadyExists);

        var document = DocumentationDocument.Create(
            command.Reference,
            command.Slug,
            command.Title,
            userId,
            clock.UtcNow);

        await repository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(document.Id);
    }
}
