using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Identity.Constants;
using Services.Identity.Models;
using Services.Identity.Services;

namespace Services.Identity.Api.Controllers;

/// <summary>
/// Controller to handle webhook events from Keycloak (vymalo/keycloak-webhook plugin).
/// </summary>
[ApiController]
[Route(AuthConstants.Routes.WebhooksBase)]
[AllowAnonymous] // Webhooks come from Keycloak, not authenticated users
public class WebhookController : ControllerBase
{
    private readonly ILocalUserService _localUserService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        ILocalUserService localUserService,
        ILogger<WebhookController> logger)
    {
        _localUserService = localUserService;
        _logger = logger;
    }

    /// <summary>
    /// Receives webhook events from Keycloak.
    /// </summary>
    /// <param name="payload">The webhook event payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>OK if the event was processed successfully.</returns>
    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> HandleWebhook(
        [FromBody] WebhookPayload payload,
        CancellationToken cancellationToken)
    {
        if (payload == null || string.IsNullOrEmpty(payload.EventType))
        {
            _logger.LogWarning("Received invalid webhook payload");
            return BadRequest("Invalid webhook payload");
        }

        _logger.LogInformation(
            "Received Keycloak webhook event: {EventType} for user {UserId} from realm {RealmId}",
            payload.EventType,
            payload.UserId,
            payload.RealmId);

        try
        {
            return payload.EventType switch
            {
                WebhookEventTypes.Register => await HandleRegisterEventAsync(payload, cancellationToken),
                WebhookEventTypes.DeleteAccount => await HandleDeleteAccountEventAsync(payload, cancellationToken),
                _ => HandleUnknownEvent(payload)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventType} for user {UserId}",
                payload.EventType, payload.UserId);
            
            // Return OK to prevent Keycloak from retrying indefinitely
            // Log the error and handle it asynchronously if needed
            return Ok(new { status = "error_logged", eventType = payload.EventType });
        }
    }

    /// <summary>
    /// Handles user registration events.
    /// Creates a corresponding local user entity.
    /// </summary>
    private async Task<IActionResult> HandleRegisterEventAsync(
        WebhookPayload payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(payload.UserId))
        {
            _logger.LogWarning("REGISTER event received without userId");
            return BadRequest("UserId is required for REGISTER event");
        }

        var username = payload.Details?.Username ?? $"user_{payload.UserId[..8]}";
        var email = payload.Details?.Email ?? $"{payload.UserId}@placeholder.local";

        var result = await _localUserService.CreateUserAsync(
            keycloakUserId: payload.UserId,
            username: username,
            email: email,
            firstName: payload.Details?.FirstName,
            lastName: payload.Details?.LastName,
            cancellationToken: cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to create local user for Keycloak ID {KeycloakUserId}: {Error}",
                payload.UserId, result.Error);
            
            // Still return OK to prevent webhook retries
            return Ok(new { status = "creation_failed", error = result.Error });
        }

        _logger.LogInformation(
            "Successfully created local user {Username} (ID: {LocalUserId}) for Keycloak user {KeycloakUserId}",
            result.Value.Username,
            result.Value.Id,
            payload.UserId);

        return Ok(new 
        { 
            status = "user_created",
            localUserId = result.Value.Id,
            keycloakUserId = payload.UserId
        });
    }

    /// <summary>
    /// Handles account deletion events.
    /// Removes the corresponding local user entity.
    /// </summary>
    private async Task<IActionResult> HandleDeleteAccountEventAsync(
        WebhookPayload payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(payload.UserId))
        {
            _logger.LogWarning("DELETE_ACCOUNT event received without userId");
            return BadRequest("UserId is required for DELETE_ACCOUNT event");
        }

        var result = await _localUserService.DeleteByKeycloakIdAsync(
            payload.UserId,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to delete local user for Keycloak ID {KeycloakUserId}: {Error}",
                payload.UserId, result.Error);
            
            return Ok(new { status = "deletion_failed", error = result.Error });
        }

        _logger.LogInformation(
            "Successfully deleted local user for Keycloak user {KeycloakUserId}",
            payload.UserId);

        return Ok(new 
        { 
            status = "user_deleted",
            keycloakUserId = payload.UserId
        });
    }

    /// <summary>
    /// Handles unknown/unhandled event types.
    /// </summary>
    private IActionResult HandleUnknownEvent(WebhookPayload payload)
    {
        _logger.LogDebug(
            "Received unhandled webhook event type: {EventType} for user {UserId}",
            payload.EventType,
            payload.UserId);

        // Return OK to acknowledge receipt even for unhandled events
        return Ok(new 
        { 
            status = "event_acknowledged",
            eventType = payload.EventType,
            handled = false
        });
    }
}

