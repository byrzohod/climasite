using ClimaSite.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual email sending using SMTP or a service like SendGrid
        _logger.LogInformation("Sending email to {To} with subject {Subject}", to, subject);

        // Placeholder - in production, implement with actual email service
        await Task.CompletedTask;
    }

    public async Task SendEmailAsync(string to, string subject, string templateName, object model, CancellationToken cancellationToken = default)
    {
        // TODO: Implement template-based email sending
        _logger.LogInformation("Sending templated email to {To} with template {Template}", to, templateName);
        await Task.CompletedTask;
    }

    public async Task SendWelcomeEmailAsync(string to, string firstName, CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to ClimaSite!";
        var body = $@"
            <h1>Welcome, {firstName}!</h1>
            <p>Thank you for creating an account with ClimaSite.</p>
            <p>We're excited to help you find the perfect HVAC solutions for your needs.</p>
        ";

        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://climasite.local";
        var resetUrl = $"{baseUrl}/reset-password?token={resetToken}";

        var subject = "Reset Your Password - ClimaSite";
        var body = $@"
            <h1>Password Reset Request</h1>
            <p>You requested to reset your password. Click the link below to proceed:</p>
            <p><a href='{resetUrl}'>Reset Password</a></p>
            <p>If you didn't request this, please ignore this email.</p>
            <p>This link will expire in 24 hours.</p>
        ";

        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task SendOrderConfirmationEmailAsync(string to, Guid orderId, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://climasite.local";
        var orderUrl = $"{baseUrl}/orders/{orderId}";

        var subject = $"Order Confirmation - ClimaSite #{orderId:N}";
        var body = $@"
            <h1>Thank You for Your Order!</h1>
            <p>Your order has been confirmed.</p>
            <p>Order Number: {orderId}</p>
            <p><a href='{orderUrl}'>View Order Details</a></p>
        ";

        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task SendOrderShippedEmailAsync(string to, Guid orderId, string trackingNumber, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://climasite.local";
        var orderUrl = $"{baseUrl}/orders/{orderId}";

        var subject = $"Your Order Has Shipped - ClimaSite #{orderId:N}";
        var body = $@"
            <h1>Your Order is On Its Way!</h1>
            <p>Great news! Your order has been shipped.</p>
            <p>Order Number: {orderId}</p>
            <p>Tracking Number: {trackingNumber}</p>
            <p><a href='{orderUrl}'>View Order Details</a></p>
        ";

        await SendEmailAsync(to, subject, body, cancellationToken);
    }
}
