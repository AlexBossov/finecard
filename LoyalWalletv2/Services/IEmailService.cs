namespace LoyalWalletv2.Services;

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string message);
}