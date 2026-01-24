using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Features.Gdpr.Commands;

public record DeleteUserDataCommand : IRequest<Result<bool>>
{
    public string Password { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public bool ConfirmDeletion { get; init; }
}

public class DeleteUserDataCommandHandler : IRequestHandler<DeleteUserDataCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<DeleteUserDataCommandHandler> _logger;

    public DeleteUserDataCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<DeleteUserDataCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        DeleteUserDataCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<bool>.Failure("User not authenticated");
        }

        if (!request.ConfirmDeletion)
        {
            return Result<bool>.Failure("You must confirm the deletion request");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<bool>.Failure("Password is required to delete your account");
        }

        // Get user and verify password
        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
        {
            return Result<bool>.Failure("User not found");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            _logger.LogWarning("GDPR deletion failed - invalid password for user {UserId}", userId.Value);
            return Result<bool>.Failure("Invalid password");
        }

        var userEmail = user.Email;
        var userName = user.FullName;

        _logger.LogInformation(
            "GDPR deletion request initiated for user {UserId} ({Email}). Reason: {Reason}",
            userId.Value,
            userEmail,
            request.Reason ?? "Not provided");

        try
        {
            // Start transaction
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // 1. Hard delete sensitive data that shouldn't be retained

            // Delete notifications (no legal requirement to retain)
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId.Value)
                .ToListAsync(cancellationToken);
            _context.Notifications.RemoveRange(notifications);

            // Delete wishlist items
            var wishlist = await _context.Wishlists
                .Include(w => w.Items)
                .FirstOrDefaultAsync(w => w.UserId == userId.Value, cancellationToken);
            if (wishlist != null)
            {
                _context.WishlistItems.RemoveRange(wishlist.Items);
                _context.Wishlists.Remove(wishlist);
            }

            // Delete cart
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken);
            if (cart != null)
            {
                _context.CartItems.RemoveRange(cart.Items);
                _context.Carts.Remove(cart);
            }

            // Delete addresses
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId.Value)
                .ToListAsync(cancellationToken);
            _context.Addresses.RemoveRange(addresses);

            // Delete review votes
            var reviewVotes = await _context.ReviewVotes
                .Where(v => v.UserId == userId.Value)
                .ToListAsync(cancellationToken);
            _context.ReviewVotes.RemoveRange(reviewVotes);

            // 2. Anonymize data that has legal retention requirements

            // Anonymize reviews (keep for product integrity but remove personal info)
            var reviews = await _context.Reviews
                .Where(r => r.UserId == userId.Value)
                .ToListAsync(cancellationToken);
            foreach (var review in reviews)
            {
                // Keep the review content but mark as no longer verified
                // Using the proper method to update verified status
                review.SetVerifiedPurchase(false, null);
            }

            // 3. Soft-delete the user account
            user.IsActive = false;
            user.Email = $"deleted_{userId.Value}@deleted.local";
            user.NormalizedEmail = user.Email.ToUpperInvariant();
            user.UserName = $"deleted_{userId.Value}";
            user.NormalizedUserName = user.UserName.ToUpperInvariant();
            user.FirstName = "Deleted";
            user.LastName = "User";
            user.PhoneNumber = null;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.PasswordHash = null; // Prevent login
            user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate tokens
            user.SetUpdatedAt();

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "GDPR deletion completed for user {UserId}. Email: {Email}",
                userId.Value,
                userEmail);

            // Send confirmation email (to original email, before it was changed)
            if (!string.IsNullOrEmpty(userEmail))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        userEmail,
                        "Account Deletion Confirmation - ClimaSite",
                        $@"Dear {userName},

Your ClimaSite account has been successfully deleted as per your request.

What was deleted:
- Your account profile and login credentials
- Saved addresses
- Wishlist items
- Shopping cart
- Notifications

What was retained (as required by law):
- Order history (anonymized) - retained for 7 years for tax/legal compliance
- Reviews (anonymized) - retained for product information integrity

If you did not request this deletion, please contact our support team immediately.

Thank you for being a ClimaSite customer.

Best regards,
The ClimaSite Team",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the deletion
                    _logger.LogWarning(ex, "Failed to send deletion confirmation email to {Email}", userEmail);
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GDPR deletion failed for user {UserId}", userId.Value);
            return Result<bool>.Failure("An error occurred while deleting your data. Please contact support.");
        }
    }
}
