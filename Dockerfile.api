# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["src/ClimaSite.Api/ClimaSite.Api.csproj", "ClimaSite.Api/"]
COPY ["src/ClimaSite.Application/ClimaSite.Application.csproj", "ClimaSite.Application/"]
COPY ["src/ClimaSite.Core/ClimaSite.Core.csproj", "ClimaSite.Core/"]
COPY ["src/ClimaSite.Infrastructure/ClimaSite.Infrastructure.csproj", "ClimaSite.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ClimaSite.Api/ClimaSite.Api.csproj"

# Copy all source files
COPY src/ .

# Build the application
WORKDIR "/src/ClimaSite.Api"
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "ClimaSite.Api.dll"]
