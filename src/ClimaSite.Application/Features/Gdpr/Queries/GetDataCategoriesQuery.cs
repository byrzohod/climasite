using ClimaSite.Application.Features.Gdpr.DTOs;
using MediatR;

namespace ClimaSite.Application.Features.Gdpr.Queries;

public record GetDataCategoriesQuery : IRequest<List<DataCategoryDto>>;

public class GetDataCategoriesQueryHandler : IRequestHandler<GetDataCategoriesQuery, List<DataCategoryDto>>
{
    public Task<List<DataCategoryDto>> Handle(
        GetDataCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = new List<DataCategoryDto>
        {
            new()
            {
                Category = "Account Information",
                Description = "Your name, email address, phone number, and account preferences.",
                Purpose = "To create and manage your account, authenticate your identity, and personalize your experience.",
                LegalBasis = "Contract performance and legitimate interest",
                RetentionPeriod = "Until account deletion or 3 years after last activity"
            },
            new()
            {
                Category = "Contact Addresses",
                Description = "Shipping and billing addresses you have saved.",
                Purpose = "To deliver orders and process payments.",
                LegalBasis = "Contract performance",
                RetentionPeriod = "Until address deletion or account deletion"
            },
            new()
            {
                Category = "Order History",
                Description = "Records of your purchases including products, prices, and delivery information.",
                Purpose = "To fulfill orders, process returns, provide customer support, and for legal/accounting requirements.",
                LegalBasis = "Contract performance and legal obligation",
                RetentionPeriod = "7 years from order date for tax/legal compliance"
            },
            new()
            {
                Category = "Payment Information",
                Description = "Payment method details (stored securely by our payment processor Stripe).",
                Purpose = "To process payments for your orders.",
                LegalBasis = "Contract performance",
                RetentionPeriod = "Managed by payment processor; references kept for 7 years"
            },
            new()
            {
                Category = "Reviews and Ratings",
                Description = "Product reviews and ratings you have submitted.",
                Purpose = "To help other customers make informed purchasing decisions.",
                LegalBasis = "Consent and legitimate interest",
                RetentionPeriod = "Until review deletion or account deletion"
            },
            new()
            {
                Category = "Questions and Answers",
                Description = "Questions you have asked about products and answers you have provided.",
                Purpose = "To help customers get product information and assist other shoppers.",
                LegalBasis = "Consent and legitimate interest",
                RetentionPeriod = "Until deletion or account deletion"
            },
            new()
            {
                Category = "Wishlist",
                Description = "Products you have saved to your wishlist.",
                Purpose = "To allow you to save products for later consideration.",
                LegalBasis = "Contract performance",
                RetentionPeriod = "Until item removal or account deletion"
            },
            new()
            {
                Category = "Shopping Cart",
                Description = "Items currently in your shopping cart.",
                Purpose = "To enable you to complete purchases.",
                LegalBasis = "Contract performance",
                RetentionPeriod = "30 days of inactivity or until checkout"
            },
            new()
            {
                Category = "Notifications",
                Description = "System notifications about orders, promotions, and account activity.",
                Purpose = "To keep you informed about your orders and relevant offers.",
                LegalBasis = "Contract performance and consent",
                RetentionPeriod = "90 days or until read and dismissed"
            },
            new()
            {
                Category = "Usage Data",
                Description = "Information about how you use our website (pages visited, search queries).",
                Purpose = "To improve our services, personalize recommendations, and ensure security.",
                LegalBasis = "Legitimate interest",
                RetentionPeriod = "12 months in anonymized form"
            },
            new()
            {
                Category = "Communication Records",
                Description = "Records of customer support interactions and correspondence.",
                Purpose = "To provide customer support and improve our services.",
                LegalBasis = "Contract performance and legitimate interest",
                RetentionPeriod = "3 years from last interaction"
            }
        };

        return Task.FromResult(categories);
    }
}
