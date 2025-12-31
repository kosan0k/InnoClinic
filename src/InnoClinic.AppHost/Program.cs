var builder = DistributedApplication.CreateBuilder(args);

#region Infrastructure Resources

// Redis for caching and session management
var redis = builder.AddRedis("redis")
    .WithDataVolume("innoclinic-redis-data")
    .WithLifetime(ContainerLifetime.Persistent);

// Define the PostgreSQL Server and a specific database for Keycloak
var postgresImportPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "PostgresConfiguration");
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("innoclinic-postgres-data") // Persist data between restarts
    .WithPgAdmin(pgAdmin => pgAdmin.WithLifetime(ContainerLifetime.Persistent))   // Optional: Adds a UI to manage the DB
    .WithBindMount(postgresImportPath, "/docker-entrypoint-initdb.d") // Mount the init script folder into the container
    .WithLifetime(ContainerLifetime.Persistent); 

// Keycloak for authentication and authorization
var keycloakDb = postgres.AddDatabase("keycloak-db");
var keycloakDbReference = ReferenceExpression.Create($"jdbc:postgresql://{postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host)}:{postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port)}/{keycloakDb.Resource.DatabaseName}");

var keycloakImportPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "KeycloakConfiguration");
var keycloak = builder.AddKeycloakContainer("keycloak", port: 8180)
    .WithReference(keycloakDb)
    .WaitFor(postgres)
    .WithDataVolume("innoclinic-keycloak-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithBindMount(Path.GetFullPath(keycloakImportPath), "/opt/keycloak/data/import")
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_USERNAME", "postgres") // Default admin user for the Postgres resource
    .WithEnvironment("KC_DB_PASSWORD", postgres.Resource.PasswordParameter)
    .WithEnvironment(
        name: "KC_DB_URL", 
        value: keycloakDbReference);

// Profiles Service databases (separate read/write for CQRS)
var profilesWriteDb = postgres.AddDatabase("profiles-write-db");
var profilesReadDb = postgres.AddDatabase("profiles-read-db");

#endregion

#region Application Services
var keycloakRealm = "AppRealm";

var identityApi = builder.AddProject<Projects.Services_Identity_Api>("services-identity-api")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("AuthOptions__KeycloakBaseUrl", keycloak.GetEndpoint("http"))
    .WithEnvironment("AuthOptions__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/{keycloakRealm}"))
    .WithEnvironment("Keycloak__auth-server-url", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__realm", keycloakRealm)
    .WithExternalHttpEndpoints();

var profilesApi = builder.AddProject<Projects.Services_Profiles_Api>("services-profiles-api")
    .WithReference(profilesWriteDb)
    .WithReference(profilesReadDb)
    .WaitFor(profilesWriteDb)
    .WaitFor(profilesReadDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WaitFor(identityApi)
    .WithEnvironment("AuthOptions__KeycloakBaseUrl", keycloak.GetEndpoint("http"))
    .WithEnvironment("AuthOptions__Realm", keycloakRealm)
    .WithEnvironment("AuthOptions__IdentityLoginUrl", ReferenceExpression.Create($"{identityApi.GetEndpoint("http")}/auth/login"));

#endregion

builder.Build().Run();
