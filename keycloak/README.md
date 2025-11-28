# Keycloak Configuration

## Directory Structure

```
keycloak/
├── providers/          # Custom Keycloak providers (JARs)
│   └── keycloak-webhook-x.x.x.jar
├── themes/            # Custom login/account themes
└── README.md
```

## Webhook Plugin Setup

1. Download the vymalo/keycloak-webhook plugin from:
   https://github.com/vymalo/keycloak-webhook/releases

2. Place the JAR file in the `providers/` directory

3. The plugin is configured via environment variables in docker-compose.yaml:
   - `WEBHOOK_HTTP_BASE_PATH`: The base URL for webhook callbacks
   - `WEBHOOK_EVENTS_TAKEN`: Comma-separated list of events to send

## Realm Configuration

### Create the AppRealm

1. Log into Keycloak Admin Console at http://localhost:8180
2. Create a new realm named `AppRealm`

### Create the Auth Service Client

1. Go to Clients → Create Client
2. Set Client ID: `auth-service-api`
3. Enable Client Authentication (for service account)
4. Set Valid Redirect URIs: `http://localhost:5000/*`
5. Enable Service Account Roles for Admin API access

### Configure Back-Channel Logout

1. Go to Clients → auth-service-api → Settings
2. Set Backchannel Logout URL: `http://auth-service:8080/api/logout/backchannel`
3. Enable "Backchannel Logout Session Required"

### Create Realm Roles

Create the following realm roles:
- `admin`
- `user`

### Client Roles

Create client roles for `auth-service-api`:
- `user-management`

### Service Account Setup

1. Go to Clients → auth-service-api → Service Account Roles
2. Assign `realm-management` → `manage-users` role
3. Assign `realm-management` → `view-users` role
4. Assign `realm-management` → `manage-realm` role (for role management)

## Testing

### Get a Token

```bash
# Get an access token using client credentials
curl -X POST "http://localhost:8180/realms/AppRealm/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=auth-service-api" \
  -d "client_secret=your-client-secret"
```

### Call the Auth Service

```bash
# Get current user info
curl -X GET "http://localhost:5000/api/users/me" \
  -H "Authorization: Bearer <access_token>"
```

