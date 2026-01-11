using ClimaSite.Application.Common.Models;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

public record RevokeTokenCommand(
    string RefreshToken,
    string? IpAddress = null
) : IRequest<Result<bool>>;
