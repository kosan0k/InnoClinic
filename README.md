# InnoClinic

A modern healthcare clinic management platform built with **.NET 10**, **.NET Aspire**, and **Keycloak** for identity management.

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [Identity Service](src/Services/Identity/README.md) | Authentication API, endpoints, configuration, and troubleshooting |
| [Keycloak Setup](keycloak/README.md) | Keycloak configuration, realm setup, and webhook integration |

## 🚀 Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire)

```bash
# Install Aspire workload
dotnet workload install aspire
```

### Run the Application

```bash
cd src/InnoClinic.AppHost
dotnet run
```

This will automatically:
- Start **Redis** cache
- Start **Keycloak** identity provider
- Open the **Aspire Dashboard**

### Access Services

| Service | URL | Credentials |
|---------|-----|-------------|
| Aspire Dashboard | https://localhost:17001 | - |
| Identity API | See Aspire Dashboard | - |
| Keycloak Admin | See Aspire Dashboard | admin / admin |

## 🔐 Authentication

The platform uses **Keycloak** for authentication with support for:

- **OpenID Connect** - Browser-based login flows
- **JWT Bearer** - API authentication
- **OAuth 2.0** - Token exchange for SPAs/Mobile apps
- **Session Revocation** - Immediate logout via Redis

### Default Test Users

| Username | Password | Roles |
|----------|----------|-------|
| admin | admin123 | admin, user |
| testdoctor | doctor123 | doctor, user |
| testpatient | patient123 | patient, user |

## 🛠️ Development

### Build the Solution

```bash
cd src
dotnet build InnoClinic.slnx
```

### Run Tests

```bash
dotnet test InnoClinic.slnx
```

### Add a New Service

1. Create service project under `src/Services/`
2. Add project reference to `InnoClinic.ServiceDefaults`
3. Register in `InnoClinic.AppHost/Program.cs`
4. Call `builder.AddServiceDefaults()` in the service

## 🔧 Configuration

### Environment Variables

When running with Aspire, connection strings are automatically injected. For standalone deployment:

```bash
# Redis
ConnectionStrings__redis=localhost:6379

# Keycloak
AuthOptions__Authority=http://localhost:8180/realms/AppRealm
AuthOptions__ClientId=identity-service-api
AuthOptions__ClientSecret=your-secret
```

## 📖 Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [OpenID Connect Specification](https://openid.net/connect/)

## 📄 License

Copyright © 2024 InnoClinic. All rights reserved.
