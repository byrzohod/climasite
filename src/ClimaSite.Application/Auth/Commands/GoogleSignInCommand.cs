using ClimaSite.Application.Auth.DTOs;
using ClimaSite.Application.Common.Models;
using MediatR;

namespace ClimaSite.Application.Auth.Commands;

/// <summary>
/// Signs a user in with a Google Identity Services ID token. On success it returns the same
/// <see cref="LoginResponseDto"/> as password login (app JWT + rotated refresh token + user).
/// </summary>
public record GoogleSignInCommand(string IdToken) : IRequest<Result<LoginResponseDto>>;
