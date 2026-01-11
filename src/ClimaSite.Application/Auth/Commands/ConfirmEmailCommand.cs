using ClimaSite.Application.Common.Models;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

public record ConfirmEmailCommand(
    string Token,
    string Email
) : IRequest<Result<bool>>;
