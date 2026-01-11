namespace ClimaSite.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string to, string subject, string templateName, object model, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string to, string firstName, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string to, string resetToken, CancellationToken cancellationToken = default);
    Task SendOrderConfirmationEmailAsync(string to, Guid orderId, CancellationToken cancellationToken = default);
    Task SendOrderShippedEmailAsync(string to, Guid orderId, string trackingNumber, CancellationToken cancellationToken = default);
}
