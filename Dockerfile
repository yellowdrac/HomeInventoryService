# HomeInventory backend (ASP.NET Core Web API) container image.
# Build context is this directory (the backend solution root, where HomeInventory.sln lives).

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-wide build configuration first so restore can be cached.
COPY global.json ./
COPY Directory.Build.props ./

# Copy only the project files needed by the Web API and restore. Keeping this
# separate from the source copy lets Docker reuse the (slow) restore layer until
# a .csproj actually changes.
COPY HomeInventory.Api/HomeInventory.Api.csproj ./HomeInventory.Api/
COPY HomeInventory.Application/HomeInventory.Application.csproj ./HomeInventory.Application/
COPY HomeInventory.Domain/HomeInventory.Domain.csproj ./HomeInventory.Domain/
COPY HomeInventory.Infrastructure/HomeInventory.Infrastructure.csproj ./HomeInventory.Infrastructure/
RUN dotnet restore HomeInventory.Api/HomeInventory.Api.csproj

# Copy the rest of the source and publish in Release mode. The migrations live in
# HomeInventory.Infrastructure, which the API references, so they ship in the output.
COPY . .
RUN dotnet publish HomeInventory.Api/HomeInventory.Api.csproj \
    -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Default listening port. The entrypoint honors $PORT (Render/Cloud Run convention)
# and falls back to 8080. No secrets are baked into the image: the connection string
# (ConnectionStrings:Default), JWT signing key (Jwt:SigningKey) and S3 credentials
# (Storage:S3:*) are injected as environment variables at runtime.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as the non-root user shipped with the .NET runtime image.
USER app

ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://+:${PORT:-8080}; exec dotnet HomeInventory.Api.dll"]
