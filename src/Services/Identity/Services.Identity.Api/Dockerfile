# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY ["InnoClinic.slnx", "./"]
COPY ["Services/Identity/Services.Identity.Api/Services.Identity.Api.csproj", "Services/Identity/Services.Identity.Api/"]
COPY ["Services/Identity/Services.Identity/Services.Identity.csproj", "Services/Identity/Services.Identity/"]
COPY ["Services/Services.Shared/Services.Shared.csproj", "Services/Services.Shared/"]

# Restore dependencies
RUN dotnet restore "Services/Identity/Services.Identity.Api/Services.Identity.Api.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/Services/Identity/Services.Identity.Api"
RUN dotnet build "Services.Identity.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Services.Identity.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser
USER appuser

COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Services.Identity.Api.dll"]

