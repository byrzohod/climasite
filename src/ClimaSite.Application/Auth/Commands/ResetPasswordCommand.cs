using ClimaSite.Application.Common.Models;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

public record ResetPasswordCommand(
    string Token,
    string Email,
    string NewPassword
) : IRequest<Result<bool>>;
