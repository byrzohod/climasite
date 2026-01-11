using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Auth.DTOs;
using MediatR;

namespace ClimaSite.Application.Auth.Queries;

public record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;
