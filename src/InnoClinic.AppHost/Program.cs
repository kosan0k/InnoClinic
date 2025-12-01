var builder = DistributedApplication.CreateBuilder(args);

// ============================================
// Infrastructure Resources
// ============================================

// PostgreSQL database server with persistent volume
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("innoclinic-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent);

// Identity database for the Identity service
var identityDb = postgres.AddDatabase("identitydb", "identity");

// Redis for caching and session management
var redis = builder.AddRedis("redis")
    .WithDataVolume("innoclinic-redis-data")
    .WithLifetime(ContainerLifetime.Persistent);

// Keycloak for authentication and authorization
var keycloakImportPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "KeycloakConfiguration");
var keycloak = builder.AddKeycloakContainer("keycloak", port: 8180)
    .WithDataVolume("innoclinic-keycloak-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithBindMount(Path.GetFullPath(keycloakImportPath), "/opt/keycloak/data/import");

// ============================================
// Application Services
// ============================================

var identityApi = builder.AddProject<Projects.Services_Identity_Api>("identity-api")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("AuthOptions__KeycloakBaseUrl", keycloak.GetEndpoint("http"))
    .WithEnvironment("AuthOptions__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/AppRealm"))
    .WithEnvironment("Keycloak__auth-server-url", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__realm", "AppRealm")
    .WithExternalHttpEndpoints();

// ============================================
// Build and Run
// ============================================

builder.Build().Run();
