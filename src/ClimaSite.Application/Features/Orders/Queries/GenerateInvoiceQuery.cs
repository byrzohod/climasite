using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClimaSite.Application.Features.Orders.Queries;

public record GenerateInvoiceQuery : IRequest<Result<InvoiceResultDto>>
{
    public Guid OrderId { get; init; }
}

public class InvoiceResultDto
{
    public byte[] PdfContent { get; init; } = [];
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/pdf";
}

public class GenerateInvoiceQueryHandler : IRequestHandler<GenerateInvoiceQuery, Result<InvoiceResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GenerateInvoiceQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<InvoiceResultDto>> Handle(
        GenerateInvoiceQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Result<InvoiceResultDto>.Failure("Order not found");
        }

        // Verify user owns the order
        if (userId.HasValue && order.UserId != userId && !_currentUserService.IsAdmin)
        {
            return Result<InvoiceResultDto>.Failure("Access denied");
        }

        // Get product images for the invoice
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Images)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;

        var document = new InvoiceDocument(order, products);
        var pdfBytes = document.GeneratePdf();

        return Result<InvoiceResultDto>.Success(new InvoiceResultDto
        {
            PdfContent = pdfBytes,
            FileName = $"Invoice-{order.OrderNumber}.pdf",
            ContentType = "application/pdf"
        });
    }
}

internal class InvoiceDocument : IDocument
{
    private readonly Core.Entities.Order _order;
    private readonly List<Core.Entities.Product> _products;

    public InvoiceDocument(Core.Entities.Order order, List<Core.Entities.Product> products)
    {
        _order = order;
        _products = products;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("ClimaSite")
                    .FontSize(24)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().Text("HVAC Solutions & Equipment")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);
            });

            row.RelativeItem().Column(column =>
            {
                column.Item().AlignRight().Text("INVOICE")
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Grey.Darken2);

                column.Item().AlignRight().Text($"#{_order.OrderNumber}")
                    .FontSize(12);

                column.Item().AlignRight().Text($"Date: {_order.CreatedAt:yyyy-MM-dd}")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Addresses row
            column.Item().Row(row =>
            {
                row.RelativeItem().Component(new AddressComponent("Bill To", GetShippingAddress()));
                row.ConstantItem(30);
                row.RelativeItem().Component(new AddressComponent("Ship To", GetShippingAddress()));
            });

            column.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Order info
            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Order Date: {_order.CreatedAt:MMMM dd, yyyy}");
                row.RelativeItem().AlignRight().Text($"Status: {_order.Status}");
            });

            column.Item().PaddingVertical(15);

            // Items table
            column.Item().Element(ComposeItemsTable);

            column.Item().PaddingVertical(15);

            // Summary
            column.Item().AlignRight().Width(250).Element(ComposeSummary);
        });
    }

    private void ComposeItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Product
                columns.RelativeColumn(1); // SKU
                columns.RelativeColumn(1); // Qty
                columns.RelativeColumn(1); // Price
                columns.RelativeColumn(1); // Total
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Product").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("SKU").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qty").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Price").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Total").Bold();
            });

            // Items
            foreach (var item in _order.Items)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                    .Column(col =>
                    {
                        col.Item().Text(item.ProductName);
                        if (!string.IsNullOrEmpty(item.VariantName))
                        {
                            col.Item().Text(item.VariantName).FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                    });
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                    .Text(item.Sku).FontSize(9);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                    .AlignRight().Text(item.Quantity.ToString());
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                    .AlignRight().Text($"{_order.Currency} {item.UnitPrice:N2}");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                    .AlignRight().Text($"{_order.Currency} {item.LineTotal:N2}");
            }
        });
    }

    private void ComposeSummary(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Subtotal:");
                row.RelativeItem().AlignRight().Text($"{_order.Currency} {_order.Subtotal:N2}");
            });

            if (_order.ShippingCost > 0)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Shipping:");
                    row.RelativeItem().AlignRight().Text($"{_order.Currency} {_order.ShippingCost:N2}");
                });
            }

            if (_order.TaxAmount > 0)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Tax (VAT 20%):");
                    row.RelativeItem().AlignRight().Text($"{_order.Currency} {_order.TaxAmount:N2}");
                });
            }

            if (_order.DiscountAmount > 0)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Discount:");
                    row.RelativeItem().AlignRight().Text($"-{_order.Currency} {_order.DiscountAmount:N2}");
                });
            }

            column.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Darken1).Row(row =>
            {
                row.RelativeItem().Text("Total:").Bold().FontSize(12);
                row.RelativeItem().AlignRight().Text($"{_order.Currency} {_order.Total:N2}").Bold().FontSize(12);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(text =>
            {
                text.Span("Thank you for your order!").FontColor(Colors.Grey.Darken1);
            });

            column.Item().AlignCenter().Text(text =>
            {
                text.Span("ClimaSite - HVAC Solutions | support@climasite.com | www.climasite.com")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
            });

            column.Item().PaddingTop(5).AlignCenter().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }

    private AddressData GetShippingAddress()
    {
        if (!_order.ShippingAddress.Any())
        {
            return new AddressData
            {
                Name = _order.CustomerEmail,
                Email = _order.CustomerEmail
            };
        }

        return new AddressData
        {
            Name = $"{_order.ShippingAddress.GetValueOrDefault("firstName")} {_order.ShippingAddress.GetValueOrDefault("lastName")}",
            AddressLine1 = _order.ShippingAddress.GetValueOrDefault("addressLine1")?.ToString() ?? "",
            AddressLine2 = _order.ShippingAddress.GetValueOrDefault("addressLine2")?.ToString(),
            City = _order.ShippingAddress.GetValueOrDefault("city")?.ToString() ?? "",
            State = _order.ShippingAddress.GetValueOrDefault("state")?.ToString(),
            PostalCode = _order.ShippingAddress.GetValueOrDefault("postalCode")?.ToString() ?? "",
            Country = _order.ShippingAddress.GetValueOrDefault("country")?.ToString() ?? "",
            Phone = _order.ShippingAddress.GetValueOrDefault("phone")?.ToString(),
            Email = _order.CustomerEmail
        };
    }
}

internal class AddressData
{
    public string Name { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

internal class AddressComponent : IComponent
{
    private readonly string _title;
    private readonly AddressData _address;

    public AddressComponent(string title, AddressData address)
    {
        _title = title;
        _address = address;
    }

    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text(_title).Bold().FontSize(10).FontColor(Colors.Grey.Darken2);
            column.Item().PaddingTop(5);

            column.Item().Text(_address.Name).Bold();

            if (!string.IsNullOrEmpty(_address.AddressLine1))
                column.Item().Text(_address.AddressLine1);

            if (!string.IsNullOrEmpty(_address.AddressLine2))
                column.Item().Text(_address.AddressLine2);

            var cityLine = string.Join(", ", new[]
            {
                _address.City,
                _address.State,
                _address.PostalCode
            }.Where(x => !string.IsNullOrEmpty(x)));

            if (!string.IsNullOrEmpty(cityLine))
                column.Item().Text(cityLine);

            if (!string.IsNullOrEmpty(_address.Country))
                column.Item().Text(_address.Country);

            if (!string.IsNullOrEmpty(_address.Phone))
                column.Item().Text($"Phone: {_address.Phone}").FontSize(9);

            if (!string.IsNullOrEmpty(_address.Email))
                column.Item().Text(_address.Email).FontSize(9).FontColor(Colors.Blue.Darken1);
        });
    }
}
