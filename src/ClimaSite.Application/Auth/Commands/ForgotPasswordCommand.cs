using ClimaSite.Application.Common.Models;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

public record ForgotPasswordCommand(
    string Email
) : IRequest<Result<bool>>;
