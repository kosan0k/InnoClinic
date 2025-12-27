using MediatR;
using Microsoft.AspNetCore.Mvc;
using Services.Profiles.Api.Contracts;
using Services.Profiles.Application.Features.Doctors.Commands.ChangeStatus;
using Services.Profiles.Application.Features.Doctors.Commands.Create;
using Services.Profiles.Application.Features.Doctors.Commands.Edit;
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
    public async Task<ActionResult<IReadOnlyList<DoctorListItemVm>>> GetAll(
        CancellationToken cancellationToken)
    {
        var query = new GetDoctorsListQuery();
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get doctor profile by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DoctorProfileVm), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorProfileVm>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetDoctorProfileQuery { DoctorId = id };
        var result = await _sender.Send(query, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new doctor
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        var doctorId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = doctorId }, doctorId);
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

        await _sender.Send(command, cancellationToken);

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

        await _sender.Send(command, cancellationToken);

        return NoContent();
    }
}
