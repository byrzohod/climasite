using System.Net;
using System.Net.Mail;
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
        // Check if placeholder mode is enabled (for development)
        var usePlaceholder = _configuration.GetValue<bool>("Email:UsePlaceholder", true);
        
        if (usePlaceholder)
        {
            _logger.LogInformation(
                "Email placeholder mode: To={To}, Subject={Subject}, Body length={BodyLength} chars",
                to, subject, body.Length);
            return;
        }

        await SendEmailViaSMTPAsync(to, subject, body, cancellationToken);
    }

    public async Task SendEmailAsync(string to, string subject, string templateName, object model, CancellationToken cancellationToken = default)
    {
        // For now, we'll generate HTML from the model
        // In production, consider using a templating engine like RazorLight or Scriban
        var body = GenerateHtmlFromTemplate(templateName, model);
        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(string to, string firstName, CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to ClimaSite!";
        var body = GenerateWelcomeEmailHtml(firstName);
        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://climasite.local";
        var resetUrl = $"{baseUrl}/reset-password?token={resetToken}";

        var subject = "Reset Your Password - ClimaSite";
        var body = GeneratePasswordResetEmailHtml(resetUrl);
        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task SendOrderConfirmationEmailAsync(string to, Guid orderId, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://climasite.local";
        var orderUrl = $"{baseUrl}/account/orders/${orderId}";

        var subject = $"Order Confirmation - ClimaSite #{orderId.ToString()[..8].ToUpper()}";
        var body = GenerateOrderConfirmationEmailHtml(orderId, orderUrl);
        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task SendOrderShippedEmailAsync(string to, Guid orderId, string trackingNumber, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://climasite.local";
        var orderUrl = $"{baseUrl}/account/orders/${orderId}";

        var subject = $"Your Order Has Shipped - ClimaSite #{orderId.ToString()[..8].ToUpper()}";
        var body = GenerateOrderShippedEmailHtml(orderId, orderUrl, trackingNumber);
        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    private async Task SendEmailViaSMTPAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPortStr = _configuration["Email:SmtpPort"];
        var smtpUsername = _configuration["Email:Username"];
        var smtpPassword = _configuration["Email:Password"];
        var fromAddress = _configuration["Email:From"];
        var fromName = _configuration["Email:FromName"] ?? "ClimaSite";
        var enableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortStr))
        {
            _logger.LogError("SMTP configuration is missing. Email to {To} was NOT sent.", to);
            throw new InvalidOperationException("SMTP configuration is incomplete. Check Email:SmtpHost and Email:SmtpPort settings.");
        }

        if (!int.TryParse(smtpPortStr, out var smtpPort))
        {
            _logger.LogError("Invalid SMTP port configuration: {Port}", smtpPortStr);
            throw new InvalidOperationException($"Invalid SMTP port: {smtpPortStr}");
        }

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = enableSsl;
            
            // Set credentials if provided
            if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
            {
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            }
            else
            {
                client.UseDefaultCredentials = true;
            }

            var fromMailAddress = new MailAddress(fromAddress ?? "noreply@climasite.local", fromName);
            var toMailAddress = new MailAddress(to);

            using var message = new MailMessage(fromMailAddress, toMailAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await client.SendMailAsync(message, cancellationToken);
            
            _logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'", to, subject);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error while sending email to {To}: {Message}", to, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Message}", to, ex.Message);
            throw;
        }
    }

    private string GenerateHtmlFromTemplate(string templateName, object model)
    {
        // Simple template generation - in production use a proper templating engine
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>{GetEmailStyles()}</style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>ClimaSite</h1>
                    </div>
                    <div class='content'>
                        <p>Template: {templateName}</p>
                        <pre>{System.Text.Json.JsonSerializer.Serialize(model, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}</pre>
                    </div>
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }

    private string GenerateWelcomeEmailHtml(string firstName)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>{GetEmailStyles()}</style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>ClimaSite</h1>
                    </div>
                    <div class='content'>
                        <h2>Welcome, {EscapeHtml(firstName)}!</h2>
                        <p>Thank you for creating an account with ClimaSite.</p>
                        <p>We're excited to help you find the perfect HVAC solutions for your needs.</p>
                        <p>With your new account, you can:</p>
                        <ul>
                            <li>Browse our extensive catalog of air conditioners and heating systems</li>
                            <li>Save your favorite products to your wishlist</li>
                            <li>Track your orders easily</li>
                            <li>Get personalized recommendations</li>
                        </ul>
                        <a href='{_configuration["AppSettings:BaseUrl"] ?? "https://climasite.local"}/products' class='button'>
                            Start Shopping
                        </a>
                    </div>
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }

    private string GeneratePasswordResetEmailHtml(string resetUrl)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>{GetEmailStyles()}</style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>ClimaSite</h1>
                    </div>
                    <div class='content'>
                        <h2>Password Reset Request</h2>
                        <p>You requested to reset your password. Click the button below to proceed:</p>
                        <a href='{EscapeHtml(resetUrl)}' class='button'>Reset Password</a>
                        <p class='note'>If the button doesn't work, copy and paste this link into your browser:</p>
                        <p class='link'>{EscapeHtml(resetUrl)}</p>
                        <hr>
                        <p class='warning'>If you didn't request this password reset, please ignore this email. Your password will remain unchanged.</p>
                        <p class='note'>This link will expire in 24 hours.</p>
                    </div>
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }

    private string GenerateOrderConfirmationEmailHtml(Guid orderId, string orderUrl)
    {
        var orderIdShort = orderId.ToString()[..8].ToUpper();
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>{GetEmailStyles()}</style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>ClimaSite</h1>
                    </div>
                    <div class='content'>
                        <h2>Thank You for Your Order!</h2>
                        <p>Your order has been confirmed and is being processed.</p>
                        <div class='order-box'>
                            <p><strong>Order Number:</strong> #{orderIdShort}</p>
                        </div>
                        <p>We'll send you another email when your order ships.</p>
                        <a href='{EscapeHtml(orderUrl)}' class='button'>View Order Details</a>
                        <p class='note'>You can also track your order status in your account dashboard.</p>
                    </div>
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }

    private string GenerateOrderShippedEmailHtml(Guid orderId, string orderUrl, string trackingNumber)
    {
        var orderIdShort = orderId.ToString()[..8].ToUpper();
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>{GetEmailStyles()}</style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>ClimaSite</h1>
                    </div>
                    <div class='content'>
                        <h2>Your Order is On Its Way!</h2>
                        <p>Great news! Your order has been shipped and is on its way to you.</p>
                        <div class='order-box'>
                            <p><strong>Order Number:</strong> #{orderIdShort}</p>
                            <p><strong>Tracking Number:</strong> {EscapeHtml(trackingNumber)}</p>
                        </div>
                        <a href='{EscapeHtml(orderUrl)}' class='button'>Track Your Order</a>
                        <p class='note'>Delivery times may vary. Please allow 3-7 business days for delivery.</p>
                    </div>
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }

    private static string GetEmailStyles()
    {
        return @"
            body {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
                line-height: 1.6;
                color: #333;
                background-color: #f5f5f5;
                margin: 0;
                padding: 20px;
            }
            .container {
                max-width: 600px;
                margin: 0 auto;
                background-color: #ffffff;
                border-radius: 8px;
                overflow: hidden;
                box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            }
            .header {
                background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
                color: white;
                padding: 30px;
                text-align: center;
            }
            .header h1 {
                margin: 0;
                font-size: 28px;
            }
            .content {
                padding: 30px;
            }
            .content h2 {
                color: #1f2937;
                margin-top: 0;
            }
            .button {
                display: inline-block;
                background-color: #2563eb;
                color: white !important;
                text-decoration: none;
                padding: 12px 24px;
                border-radius: 6px;
                font-weight: 600;
                margin: 20px 0;
            }
            .button:hover {
                background-color: #1d4ed8;
            }
            .order-box {
                background-color: #f3f4f6;
                border-radius: 6px;
                padding: 20px;
                margin: 20px 0;
            }
            .order-box p {
                margin: 5px 0;
            }
            .note {
                color: #6b7280;
                font-size: 14px;
            }
            .warning {
                color: #dc2626;
                font-size: 14px;
            }
            .link {
                word-break: break-all;
                color: #2563eb;
                font-size: 12px;
            }
            .footer {
                background-color: #f9fafb;
                padding: 20px 30px;
                text-align: center;
                font-size: 12px;
                color: #6b7280;
                border-top: 1px solid #e5e7eb;
            }
            .footer a {
                color: #2563eb;
                text-decoration: none;
            }
            hr {
                border: none;
                border-top: 1px solid #e5e7eb;
                margin: 20px 0;
            }
            ul {
                padding-left: 20px;
            }
            li {
                margin-bottom: 8px;
            }
        ";
    }

    private string GetEmailFooter()
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://climasite.local";
        return $@"
            <div class='footer'>
                <p>This email was sent by ClimaSite</p>
                <p>
                    <a href='{baseUrl}'>Visit our website</a> |
                    <a href='{baseUrl}/account'>Manage your account</a>
                </p>
                <p>If you have any questions, please contact our support team.</p>
            </div>";
    }

    private static string EscapeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        return System.Web.HttpUtility.HtmlEncode(input);
    }
}
