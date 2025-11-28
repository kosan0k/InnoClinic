using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Identity.Configurations;
using Services.Identity.Constants;

namespace Services.Identity.Authentication;

/// <summary>
/// Transforms Keycloak JWT claims by extracting roles from nested realm_access and resource_access claims
/// and flattening them into standard .NET ClaimTypes.Role claims.
/// </summary>
public class KeycloakRolesClaimsTransformer : IClaimsTransformation
{
    private readonly ILogger<KeycloakRolesClaimsTransformer> _logger;
    private readonly AuthOptions _authOptions;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public KeycloakRolesClaimsTransformer(
        ILogger<KeycloakRolesClaimsTransformer> logger,
        IOptions<AuthOptions> authOptions)
    {
        _logger = logger;
        _authOptions = authOptions.Value;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        // Check if we've already transformed this principal (avoid double-transformation)
        if (identity.HasClaim(c => c.Type == "roles_transformed" && c.Value == "true"))
        {
            return Task.FromResult(principal);
        }

        var roleClaims = new List<Claim>();

        // Extract realm roles from realm_access claim
        var realmAccessClaim = identity.FindFirst(AuthConstants.Keycloak.Claims.RealmAccess);
        if (realmAccessClaim != null)
        {
            var realmRoles = ExtractRolesFromJson(realmAccessClaim.Value);
            foreach (var role in realmRoles)
            {
                roleClaims.Add(new Claim(ClaimTypes.Role, role));
                _logger.LogDebug("Added realm role claim: {Role}", role);
            }
        }

        // Extract resource/client roles from resource_access claim
        var resourceAccessClaim = identity.FindFirst(AuthConstants.Keycloak.Claims.ResourceAccess);
        if (resourceAccessClaim != null)
        {
            var clientRoles = ExtractClientRolesFromJson(resourceAccessClaim.Value, _authOptions.ClientId);
            foreach (var role in clientRoles)
            {
                // Prefix client roles to distinguish them from realm roles
                var prefixedRole = $"{_authOptions.ClientId}:{role}";
                roleClaims.Add(new Claim(ClaimTypes.Role, prefixedRole));
                
                // Also add without prefix for simpler [Authorize(Roles = "...")] usage
                roleClaims.Add(new Claim(ClaimTypes.Role, role));
                _logger.LogDebug("Added client role claim: {Role}", role);
            }
        }

        // Add all extracted role claims to the identity
        foreach (var claim in roleClaims)
        {
            identity.AddClaim(claim);
        }

        // Mark as transformed to prevent double-transformation
        identity.AddClaim(new Claim("roles_transformed", "true"));

        _logger.LogDebug("Transformed {RoleCount} roles for user {Subject}",
            roleClaims.Count,
            identity.FindFirst(AuthConstants.Keycloak.Claims.Subject)?.Value ?? "unknown");

        return Task.FromResult(principal);
    }

    /// <summary>
    /// Extracts roles from the realm_access JSON structure.
    /// Expected format: { "roles": ["role1", "role2"] }
    /// </summary>
    private IEnumerable<string> ExtractRolesFromJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty(AuthConstants.Keycloak.Claims.Roles, out var rolesElement) &&
                rolesElement.ValueKind == JsonValueKind.Array)
            {
                return rolesElement
                    .EnumerateArray()
                    .Where(r => r.ValueKind == JsonValueKind.String)
                    .Select(r => r.GetString()!)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .ToList();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse realm_access claim: {Json}", json);
        }

        return [];
    }

    /// <summary>
    /// Extracts client-specific roles from the resource_access JSON structure.
    /// Expected format: { "client-id": { "roles": ["role1", "role2"] } }
    /// </summary>
    private IEnumerable<string> ExtractClientRolesFromJson(string json, string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty(clientId, out var clientElement) &&
                clientElement.TryGetProperty(AuthConstants.Keycloak.Claims.Roles, out var rolesElement) &&
                rolesElement.ValueKind == JsonValueKind.Array)
            {
                return rolesElement
                    .EnumerateArray()
                    .Where(r => r.ValueKind == JsonValueKind.String)
                    .Select(r => r.GetString()!)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .ToList();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse resource_access claim: {Json}", json);
        }

        return [];
    }
}

/// <summary>
/// DTO for parsing realm_access claim structure
/// </summary>
internal class RealmAccess
{
    public List<string> Roles { get; set; } = [];
}

/// <summary>
/// DTO for parsing resource_access claim structure
/// </summary>
internal class ResourceAccess
{
    public List<string> Roles { get; set; } = [];
}

