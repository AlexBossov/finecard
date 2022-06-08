using MailKit.Net.Smtp;
using MimeKit;

namespace LoyalWalletv2.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }
    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var emailMessage = new MimeMessage();
 
        emailMessage.From.Add(new MailboxAddress("Администрация сайта", "noreply@finecard.pro"));
        emailMessage.To.Add(new MailboxAddress("", email));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = message
        };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync("smtp.yandex.ru", 465, true);
            await client.AuthenticateAsync("noreply@finecard.pro", "umonrsdtybsfpwjj");
            await client.SendAsync(emailMessage);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred when registering user {Message}", e.Message);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}