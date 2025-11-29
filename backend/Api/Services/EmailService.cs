using System.Net.Mail;
using System.Net;
using Api.Interfaces;
using DotNetEnv;

namespace Api.Service
{
    public class EmailService : IEmailService
    {   

        // U Program.cs sam uradio Env.Load() i zato ovde moze Env.GetString(...)
        public async Task SendEmailAsync(string primalac, string subject, string message)
        {
            var smtpClient = new SmtpClient(Env.GetString("SMTP_HOST"))
            {
                Port = int.Parse(Env.GetString("SMTP_PORT")),
                Credentials = new NetworkCredential(Env.GetString("SMTP_USERNAME"), Env.GetString("SMTP_PASSWORD")),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(Env.GetString("SMTP_FROM")),
                Subject = subject,
                Body = message,
                IsBodyHtml = true, // Zbog <a href > in SendEmailAsync u ForgotPassword, jer ocu clickable link da bude
            };

            mailMessage.To.Add(primalac);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
