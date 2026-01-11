using ClimaSite.Application.Common.Models;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null
) : IRequest<Result<TokenResponseDto>>;

public record TokenResponseDto(
    string AccessToken,
    string RefreshToken
);
