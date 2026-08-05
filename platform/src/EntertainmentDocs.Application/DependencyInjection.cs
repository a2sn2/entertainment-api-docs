using EntertainmentDocs.Application.Documents;
using FoundationKit.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace EntertainmentDocs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateDocumentCommand, Guid>, CreateDocumentCommandHandler>();
        services.AddScoped<ICommandHandler<AddDocumentVersionCommand, Guid>, AddDocumentVersionCommandHandler>();
        services.AddScoped<ICommandHandler<SubmitDocumentForReviewCommand>, SubmitDocumentForReviewCommandHandler>();
        services.AddScoped<ICommandHandler<PublishDocumentCommand>, PublishDocumentCommandHandler>();
        services.AddScoped<IQueryHandler<ListPublishedDocumentsQuery, IReadOnlyList<DocumentSummaryDto>>, ListPublishedDocumentsQueryHandler>();
        services.AddScoped<IQueryHandler<GetPublishedDocumentQuery, DocumentDetailsDto>, GetPublishedDocumentQueryHandler>();
        return services;
    }
}
