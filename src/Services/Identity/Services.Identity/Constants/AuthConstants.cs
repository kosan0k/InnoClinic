namespace Services.Identity.Constants;

/// <summary>
/// Authentication and authorization constants to avoid magic strings.
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// Configuration section names
    /// </summary>
    public static class ConfigSections
    {
        public const string AuthOptions = nameof(Configurations.AuthOptions);
        public const string RedisOptions = nameof(Configurations.RedisOptions);
        public const string ConnectionStrings = "ConnectionStrings";
    }
    
    /// <summary>
    /// Keycloak realm and client configuration
    /// </summary>
    public static class Keycloak
    {
        public const string DefaultRealm = "AppRealm";
        public const string DefaultClientId = "auth-service-api";
        
        /// <summary>
        /// JWT claim names used by Keycloak
        /// </summary>
        public static class Claims
        {
            public const string RealmAccess = "realm_access";
            public const string ResourceAccess = "resource_access";
            public const string Roles = "roles";
            public const string SessionId = "sid";
            public const string Subject = "sub";
            public const string PreferredUsername = "preferred_username";
            public const string Email = "email";
            public const string EmailVerified = "email_verified";
            public const string Name = "name";
            public const string GivenName = "given_name";
            public const string FamilyName = "family_name";
        }
    }
    
    /// <summary>
    /// Redis key prefixes
    /// </summary>
    public static class RedisKeys
    {
        public const string RevokedSession = "revoked_session:";
    }
    
    /// <summary>
    /// API route constants
    /// </summary>
    public static class Routes
    {
        public const string WebhooksBase = "api/webhooks";
        public const string BackChannelLogout = "api/logout/backchannel";
        public const string UsersBase = "api/users";
        public const string Health = "health";
    }
    
    /// <summary>
    /// Authentication scheme names
    /// </summary>
    public static class Schemes
    {
        public const string Bearer = "Bearer";
        public const string Cookie = "Cookies";
    }
    
    /// <summary>
    /// Policy names for authorization
    /// </summary>
    public static class Policies
    {
        public const string AdminOnly = "AdminOnly";
        public const string UserManagement = "UserManagement";
    }
    
    /// <summary>
    /// Role names
    /// </summary>
    public static class Roles
    {
        public const string Admin = "admin";
        public const string User = "user";
        public const string ServiceAccount = "service-account";
    }
}

