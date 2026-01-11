using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Auth.DTOs;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

public record UpdateProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? PreferredLanguage,
    string? PreferredCurrency
) : IRequest<Result<UserDto>>;
