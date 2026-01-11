using ClimaSite.Application.Common.Models;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest<Result<bool>>;
