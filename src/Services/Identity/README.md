# InnoClinic Identity Service

The Identity Service provides authentication and authorization capabilities for the InnoClinic platform using **Keycloak** as the identity provider and **.NET Aspire** for orchestration.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Authentication Flows](#authentication-flows)
- [Keycloak Setup](#keycloak-setup)
- [Development](#development)
- [Troubleshooting](#troubleshooting)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     .NET Aspire AppHost                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│    ┌─────────────┐  ┌─────────────────────────┐                 │
│    │    Redis    │  │        Keycloak         │                 │
│    │   (cache)   │  │   (authentication)      │                 │
│    └──────┬──────┘  └───────────┬─────────────┘                 │
│           │                     │                               │
│           ┼─────────────────────┘                               │
│                          │                                      │
│                    ┌─────┴─────┐                                │
│                    │ Identity  │                                │
│                    │    API    │                                │
│                    └───────────┘                                │
└─────────────────────────────────────────────────────────────────┘
```

### Components

| Component | Purpose |
|-----------|---------|
| **Identity API** | REST API for authentication, user management, and token operations |
| **Keycloak** | OpenID Connect / OAuth 2.0 identity provider |
| **Redis** | Session revocation cache and distributed caching |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling)

Install Aspire workload:
```bash
dotnet workload install aspire
```

---

## Getting Started

### 1. Clone and Navigate

```bash
cd src/InnoClinic.AppHost
```

### 2. Run with Aspire

```bash
dotnet run
```

This will:
- Start Redis, and Keycloak containers
- Import the default Keycloak realm configuration
- Start the Identity API service
- Open the Aspire Dashboard in your browser

### 3. Access Services

| Service | URL |
|---------|-----|
| **Aspire Dashboard** | https://localhost:17001 |
| **Identity API** | http://localhost:{port}/ (see dashboard) |
| **Keycloak Admin** | http://localhost:{port}/admin (see dashboard) |

### 4. First-Time Keycloak Setup

After the first run, you need to configure the Keycloak client's redirect URIs:

```powershell
# Get the current Keycloak port from Docker
$port = (docker ps --filter "name=keycloak" --format "{{.Ports}}") -replace '.*:(\d+)->.*', '$1'

# Get admin token
$token = (Invoke-RestMethod -Uri "http://localhost:$port/realms/master/protocol/openid-connect/token" `
    -Method Post -ContentType "application/x-www-form-urlencoded" `
    -Body "grant_type=password&client_id=admin-cli&username=admin&password=admin").access_token

# Get client and update redirect URIs
$headers = @{ Authorization = "Bearer $token" }
$clients = Invoke-RestMethod -Uri "http://localhost:$port/admin/realms/AppRealm/clients?clientId=identity-service-api" -Headers $headers
$client = $clients[0]
$client.redirectUris = @("*")
$client.webOrigins = @("*")
$body = $client | ConvertTo-Json -Depth 10
Invoke-RestMethod -Uri "http://localhost:$port/admin/realms/AppRealm/clients/$($client.id)" -Method Put -Headers $headers -ContentType "application/json" -Body $body
```

---

## Configuration

### Application Settings

**appsettings.Development.json**

```json
{
  "Keycloak": {
    "realm": "AppRealm",
    "auth-server-url": "http://localhost:8180",
    "ssl-required": "none",
    "resource": "identity-service-api",
    "verify-token-audience": true,
    "credentials": {
      "secret": "dev-client-secret"
    }
  },
  
  "AuthOptions": {
    "Authority": "http://localhost:8180/realms/AppRealm",
    "ClientId": "identity-service-api",
    "ClientSecret": "dev-client-secret",
    "AdminClientId": "admin-cli",
    "AdminClientSecret": "dev-admin-secret",
    "Realm": "AppRealm",
    "KeycloakBaseUrl": "http://localhost:8180",
    "ValidAudiences": ["identity-service-api", "account"],
    "ClockSkewSeconds": 60
  },
  
  "RedisOptions": {
    "InstanceName": "IdentityService_Dev_",
    "SessionRevocationTtlMinutes": 15
  }
}
```

### Environment Variables (Aspire-injected)

When running with Aspire, these are automatically configured:

| Variable | Description |
|----------|-------------|
| `AuthOptions__Authority` | Keycloak realm URL |
| `AuthOptions__KeycloakBaseUrl` | Keycloak base URL |
| `Keycloak__auth-server-url` | Keycloak server URL |
| `Keycloak__realm` | Keycloak realm name |
| `ConnectionStrings__redis` | Redis connection string |


## Authentication Flows

### 1. Browser-Based Login (OpenID Connect)

```
User                    Identity API              Keycloak
  │                          │                        │
  │  GET /login              │                        │
  │─────────────────────────>│                        │
  │                          │                        │
  │  302 Redirect            │                        │
  │<─────────────────────────│                        │
  │                          │                        │
  │  GET /auth?client_id=... │                        │
  │──────────────────────────────────────────────────>│
  │                          │                        │
  │         Login Page       │                        │
  │<──────────────────────────────────────────────────│
  │                          │                        │
  │  POST credentials        │                        │
  │──────────────────────────────────────────────────>│
  │                          │                        │
  │  302 Redirect + code     │                        │
  │<──────────────────────────────────────────────────│
  │                          │                        │
  │  GET /callback?code=...  │                        │
  │─────────────────────────>│                        │
  │                          │  Exchange code         │
  │                          │───────────────────────>│
  │                          │                        │
  │                          │  Access + ID tokens    │
  │                          │<───────────────────────│
  │                          │                        │
  │  Set cookie + redirect   │                        │
  │<─────────────────────────│                        │
```

### 2. API Authentication (JWT Bearer)

```
Client                  Identity API              Keycloak
  │                          │                        │
  │  Request + Bearer token  │                        │
  │─────────────────────────>│                        │
  │                          │                        │
  │                          │  Validate JWT          │
  │                          │  (cached JWKS)         │
  │                          │                        │
  │  Response                │                        │
  │<─────────────────────────│                        │
```

### 3. Token Exchange (for SPAs/Mobile)

```
Client                  Identity API              Keycloak
  │                          │                        │
  │  GET /api/auth/authorize-url                      │
  │─────────────────────────>│                        │
  │                          │                        │
  │  { authorizeUrl: "..." } │                        │
  │<─────────────────────────│                        │
  │                          │                        │
  │  (User authenticates with Keycloak)               │
  │                          │                        │
  │  POST /api/auth/token    │                        │
  │  { code, redirect_uri }  │                        │
  │─────────────────────────>│                        │
  │                          │  Exchange code         │
  │                          │───────────────────────>│
  │                          │                        │
  │                          │  Tokens                │
  │                          │<───────────────────────│
  │                          │                        │
  │  { access_token, ... }   │                        │
  │<─────────────────────────│                        │
```

---

## Keycloak Setup

### Default Realm Configuration

The `AppRealm-realm.json` file in `InnoClinic.AppHost/KeycloakConfiguration/` contains:

#### Clients

| Client ID | Purpose | Auth Type |
|-----------|---------|-----------|
| `identity-service-api` | Identity API backend | Client credentials |
| `admin-cli` | Admin operations | Client credentials |

#### Roles

| Role | Description |
|------|-------------|
| `admin` | Full system administrator |
| `user` | Standard authenticated user |
| `doctor` | Medical professional |
| `patient` | Patient user |
| `receptionist` | Front desk staff |

#### Test Users

| Username | Password | Roles |
|----------|----------|-------|
| `admin` | `admin123` | admin, user |
| `testdoctor` | `doctor123` | doctor, user |
| `testpatient` | `patient123` | patient, user |

### Manual Client Configuration

If realm import fails, configure the client manually:

1. Access Keycloak Admin Console
2. Select **AppRealm**
3. Go to **Clients** → **identity-service-api**
4. Under **Settings**:
   - Valid Redirect URIs: `*` (or specific URIs for production)
   - Web Origins: `*`
5. Save

---

## Development
### Session Revocation

The service supports immediate session revocation via Redis:

```csharp
// Revoke a session
await _sessionRevocationService.RevokeSessionAsync(sessionId, TimeSpan.FromMinutes(15));

// Check if revoked
var isRevoked = await _sessionRevocationService.IsSessionRevokedAsync(sessionId);
```

---

## Troubleshooting

### Common Issues

#### 1. "Unable to retrieve document from .well-known/openid-configuration"

**Cause:** Identity API cannot reach Keycloak.

**Solutions:**
- Check if Keycloak container is running: `docker ps --filter "name=keycloak"`
- Verify the Keycloak port in the Aspire dashboard
- Ensure `AuthOptions:Authority` points to the correct URL

#### 2. "Invalid parameter: redirect_uri"

**Cause:** Redirect URI not registered in Keycloak client.

**Solutions:**
- Update client redirect URIs in Keycloak Admin Console
- Or use the PowerShell script in [Getting Started](#4-first-time-keycloak-setup)
- Set redirect URIs to `*` for development

#### 3. "Realm 'AppRealm' already exists. Import skipped"

**Cause:** Keycloak uses persistent volume and won't reimport.

**Solutions:**
```bash
# Delete Keycloak volume to force reimport
docker volume rm innoclinic-keycloak-data
# Restart Aspire
```

#### 4. "PUSHED_AUTHORIZATION_REQUEST_ERROR"

**Cause:** .NET 9+ uses PAR by default, which Keycloak may not support properly.

**Solution:** Disable PAR in OpenIdConnect configuration:
```csharp
options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
```

#### 5. Dynamic Keycloak Port

Aspire assigns dynamic ports. To get the current Keycloak port:
```powershell
docker ps --filter "name=keycloak" --format "{{.Ports}}"
```

### Logs

View Identity API logs in the Aspire Dashboard or:
```bash
# View Keycloak logs
docker logs $(docker ps -q --filter "name=keycloak")
```

### Health Checks

```bash
# Full health check
curl http://localhost:{port}/health

# Liveness
curl http://localhost:{port}/alive

# Readiness (includes DB and Redis)
curl http://localhost:{port}/ready
```

---

## Security Considerations

### Production Checklist

- [ ] Use HTTPS for all endpoints
- [ ] Set specific redirect URIs (not `*`)
- [ ] Use strong client secrets
- [ ] Enable HTTPS metadata requirement
- [ ] Configure proper CORS policies
- [ ] Enable brute force protection in Keycloak
- [ ] Use secure cookie policies
- [ ] Implement rate limiting
- [ ] Set up proper logging and monitoring

### Environment Variables for Production

```bash
AuthOptions__Authority=https://keycloak.yourdomain.com/realms/AppRealm
AuthOptions__ClientSecret=<strong-secret>
AuthOptions__KeycloakBaseUrl=https://keycloak.yourdomain.com
```

---

## License

Copyright © 2024 InnoClinic. All rights reserved.


