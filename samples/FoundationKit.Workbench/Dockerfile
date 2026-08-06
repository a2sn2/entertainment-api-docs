FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore samples/FoundationKit.Workbench/FoundationKit.Workbench.csproj
RUN dotnet publish samples/FoundationKit.Workbench/FoundationKit.Workbench.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FoundationKit.Workbench.dll"]
