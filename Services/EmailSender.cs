using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;

namespace SchoolPortal.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public EmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var smtp = new SmtpClient
        {
            Host = _config["Email:Smtp"],
            Port = int.Parse(_config["Email:Port"]!),
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _config["Email:Username"],
                _config["Email:Password"]
            )
        };

        var message = new MailMessage
        {
            From = new MailAddress(_config["Email:From"]!),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        message.To.Add(email);

        await smtp.SendMailAsync(message);
    }
}
