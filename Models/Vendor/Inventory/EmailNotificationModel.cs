using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;
using MailKit.Security;

namespace EAD_BE.Models.Vendor.Inventory
{
    public class EmailNotificationModel : IEmailSender
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _userEmail;
        private readonly string _userPassword; // If using app-specific password or OAuth token
        private readonly SecureSocketOptions _secureSocketOptions;

        public EmailNotificationModel(string smtpServer, int smtpPort, string userEmail, string userPassword, SecureSocketOptions secureSocketOptions)
        {
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _userEmail = userEmail;
            _userPassword = userPassword;
            _secureSocketOptions = secureSocketOptions;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_userEmail, _userEmail));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, _secureSocketOptions);

                // Authenticate with Gmail credentials or OAuth token
                await client.AuthenticateAsync(_userEmail, _userPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}