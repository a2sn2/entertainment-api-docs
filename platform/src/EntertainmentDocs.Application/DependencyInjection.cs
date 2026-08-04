using EntertainmentDocs.Application.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace EntertainmentDocs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddScoped<DocumentService>();
}
