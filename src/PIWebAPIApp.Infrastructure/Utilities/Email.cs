// PIWebAPIApp.Infrastructure/Utilities/Email.cs
using System;
using System.Net.Mail;
using PIWebAPIApp.Utilities; // Для Logger

namespace PIWebAPIApp.Infrastructure.Utilities // Или другое подходящее пространство имен
{
    public class Email
    {
        public string _subject, _message, _senderEmail;

        public Email(string subject = "", string message = "", string senderEmail = "") 
        {
            _subject = subject;
            _message = message;
            _senderEmail = senderEmail;
        }
        
        public void Send(string emailTo)
        {
            try
            {
                // Проверка обязательных параметров
                if (string.IsNullOrWhiteSpace(emailTo))
                {
                    Logger.Error("Email.Send: Recipient email (emailTo) is null or empty.");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(_senderEmail))
                {
                    Logger.Warn("Email.Send: Sender email (_senderEmail) is null or empty. This might cause delivery issues.");
                    // Можно установить значение по умолчанию или прервать отправку
                    // _senderEmail = "no-reply@yourcompany.com"; // Пример запасного варианта
                }

                MailMessage mailMessage = new MailMessage(_senderEmail, emailTo);
            
                mailMessage.Subject = _subject ?? ""; // Защита от null
                mailMessage.Body = _message ?? "";   // Защита от null
                mailMessage.IsBodyHtml = true;
                
                // Используем внутренний SMTP-сервер, как указано ментором
                SmtpClient smtpClient = new SmtpClient("mailhost-tco.tcoaty.chevrontexaco.net");           
                smtpClient.UseDefaultCredentials = true; // Ключевой момент - используем учетные данные процесса
                smtpClient.EnableSsl = false; // Как в примере ментора

                smtpClient.Send(mailMessage);
                Logger.Info($"Email Sent Successfully => {emailTo} (From: {_senderEmail})");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error sending email to {emailTo} (From: {_senderEmail}): {ex.Message}");
                // В реальном приложении, возможно, нужно пробрасывать исключение выше
                // или возвращать код результата/bool.
            }
        }
    }
}