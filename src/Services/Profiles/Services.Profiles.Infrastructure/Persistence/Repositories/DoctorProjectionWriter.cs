using Microsoft.EntityFrameworkCore;
using Services.Profiles.Application.Common.Persistence;
using Services.Profiles.Domain.Enums;
using Services.Profiles.Infrastructure.Persistence.ReadModels;

namespace Services.Profiles.Infrastructure.Persistence.Repositories;

public class DoctorProjectionWriter : IDoctorProjectionWriter
{
    private readonly ReadDbContext _context;

    public DoctorProjectionWriter(ReadDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
    {
        return await _context.Doctors.AnyAsync(d => d.Id == id, ct);
    }

    public async Task CreateAsync(
        Guid id,
        string firstName,
        string lastName,
        string? middleName,
        DateTime dateOfBirth,
        string email,
        string? photoUrl,
        int careerStartYear,
        DoctorStatus status,
        Guid specializationId,
        string specializationName,
        List<string> services,
        CancellationToken cancellationToken)
    {
        var readModel = new DoctorReadModel
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            DateOfBirth = dateOfBirth,
            Email = email,
            PhotoUrl = photoUrl,
            CareerStartYear = careerStartYear,
            Status = status,
            SpecializationId = specializationId,
            SpecializationName = specializationName,
            Services = services
        };

        await _context.Doctors.AddAsync(readModel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Guid id,
        string firstName,
        string lastName,
        string? middleName,
        DateTime dateOfBirth,
        string email,
        string? photoUrl,
        int careerStartYear,
        DoctorStatus status,
        Guid specializationId,
        string specializationName,
        List<string> services,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (existing is null)
        {
            // Fallback to Create if entity doesn't exist
            await CreateAsync(
                id,
                firstName,
                lastName,
                middleName,
                dateOfBirth,
                email,
                photoUrl,
                careerStartYear,
                status,
                specializationId,
                specializationName,
                services,
                cancellationToken);
            return;
        }

        existing.FirstName = firstName;
        existing.LastName = lastName;
        existing.MiddleName = middleName;
        existing.DateOfBirth = dateOfBirth;
        existing.Email = email;
        existing.PhotoUrl = photoUrl;
        existing.CareerStartYear = careerStartYear;
        existing.Status = status;
        existing.SpecializationId = specializationId;
        existing.SpecializationName = specializationName;
        existing.Services = services;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid id, DoctorStatus status, CancellationToken ct)
    {
        await _context.Doctors
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(calls => calls.SetProperty(d => d.Status, status), ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _context.Doctors
            .Where(d => d.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
