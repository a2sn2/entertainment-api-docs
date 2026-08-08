using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Performance",
    "CA1861:Avoid constant arrays as arguments",
    Justification = "EF Core migration operations use immutable column-name arrays as schema metadata.",
    Scope = "namespaceanddescendants",
    Target = "~N:Madar.Infrastructure.Migrations")]
