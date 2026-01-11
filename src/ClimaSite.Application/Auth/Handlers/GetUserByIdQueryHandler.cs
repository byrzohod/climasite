using ClimaSite.Application.Auth.DTOs;
using ClimaSite.Application.Auth.Queries;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ClimaSite.Application.Auth.Handlers;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUserByIdQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return Result<UserDto>.Failure("User not found");
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.EmailConfirmed,
            roles.FirstOrDefault() ?? "Customer",
            user.PreferredLanguage,
            user.PreferredCurrency,
            user.CreatedAt,
            user.LastLoginAt
        ));
    }
}
