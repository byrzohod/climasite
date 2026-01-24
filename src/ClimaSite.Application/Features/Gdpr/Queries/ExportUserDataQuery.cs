using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Gdpr.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Gdpr.Queries;

public record ExportUserDataQuery : IRequest<Result<UserDataExportDto>>;

public class ExportUserDataQueryHandler : IRequestHandler<ExportUserDataQuery, Result<UserDataExportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ExportUserDataQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UserDataExportDto>> Handle(
        ExportUserDataQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<UserDataExportDto>.Failure("User not authenticated");
        }

        // Get user profile
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user == null)
        {
            return Result<UserDataExportDto>.Failure("User not found");
        }

        // Get addresses
        var addresses = await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId.Value)
            .Select(a => new UserAddressDataDto
            {
                Id = a.Id,
                FullName = a.FullName,
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                City = a.City,
                State = a.State,
                PostalCode = a.PostalCode,
                Country = a.Country,
                Phone = a.Phone,
                IsDefault = a.IsDefault,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Get orders with items
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId.Value)
            .Select(o => new UserOrderDataDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status.ToString(),
                Subtotal = o.Subtotal,
                ShippingCost = o.ShippingCost,
                TaxAmount = o.TaxAmount,
                Total = o.Total,
                PaymentMethod = o.PaymentMethod,
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new OrderItemDataDto
                {
                    ProductName = i.ProductName,
                    Sku = i.Sku,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.Quantity * i.UnitPrice
                }).ToList(),
                ShippingAddress = o.ShippingAddress,
                BillingAddress = o.BillingAddress
            })
            .ToListAsync(cancellationToken);

        // Get reviews
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Product)
            .Where(r => r.UserId == userId.Value)
            .Select(r => new UserReviewDataDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product.Name,
                Rating = r.Rating,
                Title = r.Title,
                Content = r.Content,
                Status = r.Status.ToString(),
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Get wishlist items
        var wishlistItems = await _context.WishlistItems
            .AsNoTracking()
            .Include(w => w.Product)
            .Where(w => w.Wishlist.UserId == userId.Value)
            .Select(w => new UserWishlistItemDataDto
            {
                ProductId = w.ProductId,
                ProductName = w.Product.Name,
                AddedAt = w.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Get questions and answers
        var questions = await _context.ProductQuestions
            .AsNoTracking()
            .Include(q => q.Product)
            .Include(q => q.Answers.Where(a => a.UserId == userId.Value))
            .Where(q => q.UserId == userId.Value)
            .Select(q => new UserQuestionDataDto
            {
                Id = q.Id,
                ProductId = q.ProductId,
                ProductName = q.Product.Name,
                QuestionText = q.QuestionText,
                CreatedAt = q.CreatedAt,
                Answers = q.Answers.Select(a => new UserAnswerDataDto
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    CreatedAt = a.CreatedAt
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        // Get notifications
        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value)
            .Select(n => new UserNotificationDataDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var export = new UserDataExportDto
        {
            Profile = new UserProfileDataDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                PreferredLanguage = user.PreferredLanguage,
                PreferredCurrency = user.PreferredCurrency,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            },
            Addresses = addresses,
            Orders = orders,
            Reviews = reviews,
            WishlistItems = wishlistItems,
            Questions = questions,
            Notifications = notifications,
            ExportedAt = DateTime.UtcNow,
            ExportVersion = "1.0"
        };

        return Result<UserDataExportDto>.Success(export);
    }
}
