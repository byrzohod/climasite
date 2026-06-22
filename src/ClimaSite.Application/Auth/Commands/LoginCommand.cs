using ClimaSite.Application.Auth.DTOs;
using ClimaSite.Application.Common.Models;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null
) : IRequest<Result<LoginResponseDto>>;

public record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    UserDto User
);
