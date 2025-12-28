using MediatR;
using Microsoft.AspNetCore.Mvc;
using Services.Profiles.Api.Contracts;
using Services.Profiles.Application.Common.Exceptions;
using Services.Profiles.Application.Features.Doctors.Commands.ChangeStatus;
using Services.Profiles.Application.Features.Doctors.Commands.Create;
using Services.Profiles.Application.Features.Doctors.Commands.Edit;
using Services.Profiles.Application.Features.Doctors.Commands.SoftDelete;
using Services.Profiles.Application.Features.Doctors.Queries.GetDoctorProfile;
using Services.Profiles.Application.Features.Doctors.Queries.GetDoctorsList;

namespace Services.Profiles.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DoctorsController : ControllerBase
{
    private readonly ISender _sender;

    public DoctorsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get all doctors
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorListItemVm>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DoctorListItemVm>>> GetAll(
        CancellationToken cancellationToken)
    {
        var query = new GetDoctorsListQuery();
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get doctor profile by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DoctorProfileVm), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DoctorProfileVm>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetDoctorProfileQuery { DoctorId = id };
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new doctor
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDoctorCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            DateOfBirth = request.DateOfBirth,
            Email = request.Email,
            PhotoUrl = request.PhotoUrl,
            CareerStartYear = request.CareerStartYear,
            SpecializationId = request.SpecializationId
        };

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Update an existing doctor
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EditDoctorCommand
        {
            Id = id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            DateOfBirth = request.DateOfBirth,
            PhotoUrl = request.PhotoUrl,
            CareerStartYear = request.CareerStartYear,
            SpecializationId = request.SpecializationId,
            Status = request.Status
        };

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Change doctor status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeDoctorStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeDoctorStatusCommand
        {
            DoctorId = id,
            NewStatus = request.Status
        };

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Soft delete a doctor
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new SoftDeleteDoctorCommand
        {
            DoctorId = id
        };

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return NoContent();
    }

    private ActionResult HandleError(Exception error)
    {
        return error switch
        {
            NotFoundException notFoundEx => NotFound(notFoundEx.Message),
            _ => BadRequest(error.Message)
        };
    }
}
