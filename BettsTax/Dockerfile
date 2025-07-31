# Multi-stage build for BettsTax production deployment
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for better Docker layer caching
COPY ["BettsTax.Web/BettsTax.Web.csproj", "BettsTax.Web/"]
COPY ["BettsTax.Core/BettsTax.Core.csproj", "BettsTax.Core/"]
COPY ["BettsTax.Data/BettsTax.Data.csproj", "BettsTax.Data/"]
COPY ["BettsTax.Shared/BettsTax.Shared.csproj", "BettsTax.Shared/"]

# Restore dependencies
RUN dotnet restore "BettsTax.Web/BettsTax.Web.csproj"

# Copy source code
COPY . .
WORKDIR "/src/BettsTax.Web"

# Build application
RUN dotnet build "BettsTax.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BettsTax.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Copy published application from build stage
COPY --from=publish /app/publish .

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Labels for maintainability
LABEL maintainer="BettsTax Development Team"
LABEL version="1.0.0"
LABEL description="BettsTax Client Tax Information System for Sierra Leone"

ENTRYPOINT ["dotnet", "BettsTax.Web.dll"]