using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Identity.Constants;
using Services.Identity.Models;
using Services.Identity.Services;

namespace Services.Identity.Api.Controllers;

/// <summary>
/// API controller for user management operations.
/// </summary>
[ApiController]
[Route(AuthConstants.Routes.UsersBase)]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly ILocalUserService _localUserService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IIdentityService identityService,
        ILocalUserService localUserService,
        ILogger<UsersController> logger)
    {
        _identityService = identityService;
        _localUserService = localUserService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user in Keycloak and creates a local entity.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user information.</returns>
    [HttpPost("register")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<IActionResult> RegisterUser(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering new user {Username}", request.Username);

        // Create user in Keycloak
        var keycloakResult = await _identityService.RegisterUserAsync(request, cancellationToken);
        
        if (keycloakResult.IsFailure)
        {
            _logger.LogError("Failed to create user in Keycloak: {Error}", keycloakResult.Error);
            return BadRequest(new { error = "keycloak_error", message = keycloakResult.Error });
        }

        var keycloakUserId = keycloakResult.Value;

        // Create local user entity
        var localResult = await _localUserService.CreateUserAsync(
            keycloakUserId,
            request.Username,
            request.Email,
            request.FirstName,
            request.LastName,
            cancellationToken);

        if (localResult.IsFailure)
        {
            _logger.LogWarning(
                "User created in Keycloak but failed to create local entity: {Error}",
                localResult.Error);
            
            // User exists in Keycloak, so we return success with a warning
            return Ok(new
            {
                keycloakUserId,
                username = request.Username,
                email = request.Email,
                warning = "Local entity creation failed"
            });
        }

        return CreatedAtAction(
            nameof(GetUser),
            new { userId = keycloakUserId },
            new
            {
                keycloakUserId,
                localUserId = localResult.Value.Id,
                username = request.Username,
                email = request.Email
            });
    }

    /// <summary>
    /// Gets a user by their Keycloak ID.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{userId}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<IActionResult> GetUser(
        string userId,
        CancellationToken cancellationToken)
    {
        var keycloakResult = await _identityService.GetUserByIdAsync(userId, cancellationToken);
        var localResult = await _localUserService.GetByKeycloakIdAsync(userId, cancellationToken);

        if (keycloakResult.IsFailure)
        {
            return NotFound(new { error = "user_not_found", message = $"User {userId} not found in Keycloak" });
        }

        return Ok(new
        {
            keycloak = keycloakResult.Value,
            local = localResult.IsSuccess ? localResult.Value : null
        });
    }

    /// <summary>
    /// Assigns a realm role to a user.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <param name="roleName">The role name to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{userId}/roles/realm/{roleName}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<IActionResult> AssignRealmRole(
        string userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Assigning realm role {RoleName} to user {UserId}", roleName, userId);

        var result = await _identityService.AssignRealmRoleAsync(userId, roleName, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = "role_assignment_failed", message = result.Error });
        }

        return Ok(new { message = $"Role '{roleName}' assigned successfully" });
    }

    /// <summary>
    /// Assigns a client role to a user.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <param name="clientId">The client ID.</param>
    /// <param name="roleName">The role name to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{userId}/roles/clients/{clientId}/{roleName}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<IActionResult> AssignClientRole(
        string userId,
        string clientId,
        string roleName,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assigning client role {ClientId}/{RoleName} to user {UserId}",
            clientId, roleName, userId);

        var result = await _identityService.AssignClientRoleAsync(userId, clientId, roleName, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = "role_assignment_failed", message = result.Error });
        }

        return Ok(new { message = $"Client role '{clientId}/{roleName}' assigned successfully" });
    }

    /// <summary>
    /// Deletes a user from Keycloak and the local database.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{userId}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<IActionResult> DeleteUser(
        string userId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting user {UserId}", userId);

        // Delete from local database first
        var localResult = await _localUserService.DeleteByKeycloakIdAsync(userId, cancellationToken);
        if (localResult.IsFailure)
        {
            _logger.LogWarning("Failed to delete local user: {Error}", localResult.Error);
        }

        // Delete from Keycloak
        var keycloakResult = await _identityService.DeleteUserAsync(userId, cancellationToken);
        
        if (keycloakResult.IsFailure)
        {
            return BadRequest(new { error = "deletion_failed", message = keycloakResult.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Gets the current authenticated user's information.
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(AuthConstants.Keycloak.Claims.Subject)?.Value;
        var username = User.FindFirst(AuthConstants.Keycloak.Claims.PreferredUsername)?.Value;
        var email = User.FindFirst(AuthConstants.Keycloak.Claims.Email)?.Value;
        var sessionId = User.FindFirst(AuthConstants.Keycloak.Claims.SessionId)?.Value;
        
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            userId,
            username,
            email,
            sessionId,
            roles,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false
        });
    }
}

