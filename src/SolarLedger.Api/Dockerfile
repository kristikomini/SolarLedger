# Build stage — authoritative .NET 8 build, independent of the host SDK.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore against the csproj graph first, for layer caching.
COPY Directory.Build.props ./
COPY src/SolarLedger.Domain/SolarLedger.Domain.csproj src/SolarLedger.Domain/
COPY src/SolarLedger.Application/SolarLedger.Application.csproj src/SolarLedger.Application/
COPY src/SolarLedger.Infrastructure/SolarLedger.Infrastructure.csproj src/SolarLedger.Infrastructure/
COPY src/SolarLedger.Api/SolarLedger.Api.csproj src/SolarLedger.Api/
RUN dotnet restore src/SolarLedger.Api/SolarLedger.Api.csproj

COPY . .
RUN dotnet publish src/SolarLedger.Api/SolarLedger.Api.csproj -c Release -o /app/publish

# Runtime stage.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# curl lets the compose healthcheck probe /health.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SolarLedger.Api.dll"]
