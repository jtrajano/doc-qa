# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first so the restore layer is cached
COPY src/DocQa.API/*.csproj src/DocQa.API/
RUN dotnet restore src/DocQa.API

# Copy the rest of the source and publish
COPY src/DocQa.API/ src/DocQa.API/
RUN dotnet publish src/DocQa.API -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The official .NET images include a non-root user named "app"
USER app

# .NET 8+ container images listen on port 8080 by default
EXPOSE 8080
ENTRYPOINT ["dotnet", "DocQa.API.dll"]