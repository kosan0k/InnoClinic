using CSharpFunctionalExtensions;
using MediatR;

namespace Services.Profiles.Application.Features.Doctors.Queries.GetDoctorsList;

public sealed record GetDoctorsListQuery : IRequest<Result<IReadOnlyList<DoctorListItemVm>, Exception>>;

