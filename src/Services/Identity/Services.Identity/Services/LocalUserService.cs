using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Identity.Data;
using Services.Identity.Entities;

namespace Services.Identity.Services;

/// <summary>
/// Implementation of local user management operations.
/// </summary>
public class LocalUserService : ILocalUserService
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<LocalUserService> _logger;

    public LocalUserService(
        AuthDbContext dbContext,
        ILogger<LocalUserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<LocalUser>> CreateUserAsync(
        string keycloakUserId,
        string username,
        string email,
        string? firstName = null,
        string? lastName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

            if (existingUser != null)
            {
                _logger.LogWarning("User with Keycloak ID {KeycloakUserId} already exists", keycloakUserId);
                return Result.Success(existingUser);
            }

            var user = new LocalUser
            {
                Id = Guid.NewGuid(),
                KeycloakUserId = keycloakUserId,
                Username = username,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created local user {Username} with Keycloak ID {KeycloakUserId}",
                username, keycloakUserId);

            return Result.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create local user for Keycloak ID {KeycloakUserId}", keycloakUserId);
            return Result.Failure<LocalUser>($"Failed to create user: {ex.Message}");
        }
    }

    public async Task<Result<LocalUser>> GetByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

            if (user == null)
            {
                return Result.Failure<LocalUser>($"User with Keycloak ID '{keycloakUserId}' not found");
            }

            return Result.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get local user by Keycloak ID {KeycloakUserId}", keycloakUserId);
            return Result.Failure<LocalUser>($"Failed to get user: {ex.Message}");
        }
    }

    public async Task<Result<LocalUser>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null)
            {
                return Result.Failure<LocalUser>($"User with email '{email}' not found");
            }

            return Result.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get local user by email {Email}", email);
            return Result.Failure<LocalUser>($"Failed to get user: {ex.Message}");
        }
    }

    public async Task<Result> DeleteByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User with Keycloak ID {KeycloakUserId} not found for deletion", keycloakUserId);
                return Result.Success(); // Idempotent delete
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted local user with Keycloak ID {KeycloakUserId}", keycloakUserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete local user with Keycloak ID {KeycloakUserId}", keycloakUserId);
            return Result.Failure($"Failed to delete user: {ex.Message}");
        }
    }

    public async Task<Result<LocalUser>> UpdateUserAsync(
        string keycloakUserId,
        string? username = null,
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

            if (user == null)
            {
                return Result.Failure<LocalUser>($"User with Keycloak ID '{keycloakUserId}' not found");
            }

            if (!string.IsNullOrEmpty(username))
                user.Username = username;

            if (!string.IsNullOrEmpty(email))
                user.Email = email;

            if (firstName != null)
                user.FirstName = firstName;

            if (lastName != null)
                user.LastName = lastName;

            user.UpdatedAt = DateTime.UtcNow;
            user.LastSyncedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated local user with Keycloak ID {KeycloakUserId}", keycloakUserId);

            return Result.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update local user with Keycloak ID {KeycloakUserId}", keycloakUserId);
            return Result.Failure<LocalUser>($"Failed to update user: {ex.Message}");
        }
    }
}

