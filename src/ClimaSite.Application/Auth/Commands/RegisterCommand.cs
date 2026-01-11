using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Auth.DTOs;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone = null
) : IRequest<Result<UserDto>>;
