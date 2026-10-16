FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /workspace

COPY ["Directory.Build.props", "./"]
COPY ["src/FileProcessingService.Api/FileProcessingService.Api.csproj", "./"]
RUN dotnet restore "FileProcessingService.Api.csproj"

COPY ["src/FileProcessingService.Api/", "./"]
RUN dotnet publish "FileProcessingService.Api.csproj" --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.10 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "FileProcessingService.Api.dll"]
