using CSharpFunctionalExtensions;
using Keycloak.Net.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Identity.Features.Users.Models;
using Services.Identity.Features.Users.Services;
using System.ComponentModel.DataAnnotations;

namespace Services.Identity.Features.Users;

public static class UsersActions
{
    public static Task<Microsoft.AspNetCore.Http.IResult> RegisterUserAsync(
        [FromBody] RegisterUserRequest userDto,
        IIdentityService identityService,
        CancellationToken cancellationToken)
    {
        var validationResult = Validate(userDto)
            .MapError(validationResults =>
            {
                var errors = validationResults.ToDictionary(
                    v => v.MemberNames.FirstOrDefault() ?? "Error",
                    v => new string[] { v.ErrorMessage! });

                return Results.BadRequest(new { Message = "Validation failed", Errors = errors });
            });

        return validationResult.IsFailure
            ? Task.FromResult(validationResult.Error)
            : identityService
                .RegisterUserAsync(userDto, cancellationToken)
                .Match(
                    success => Results.Created(),
                    failure => Results.Problem(detail: failure.Message));
    }

    private static UnitResult<List<ValidationResult>> Validate(RegisterUserRequest userDto)
    {
        var validationResults = new List<ValidationResult>();

        var context = new ValidationContext(userDto);

        return UnitResult
            .SuccessIf(
                Validator.TryValidateObject(userDto, context, validationResults, true),
                validationResults);
    }
}
