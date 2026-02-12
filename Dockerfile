# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY PaymentCore.sln .
COPY src/PaymentCore.Domain/PaymentCore.Domain.csproj src/PaymentCore.Domain/
COPY src/PaymentCore.Application/PaymentCore.Application.csproj src/PaymentCore.Application/
COPY src/PaymentCore.Infrastructure/PaymentCore.Infrastructure.csproj src/PaymentCore.Infrastructure/
COPY src/PaymentCore.API/PaymentCore.API.csproj src/PaymentCore.API/

# Restore dependencies
RUN dotnet restore

# Copy remaining source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/PaymentCore.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

# Copy published files
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PaymentCore.API.dll"]
