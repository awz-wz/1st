// src/PIWebAPIApp.Infrastructure/Configuration/EmailConfiguration.cs
namespace PIWebAPIApp.Models
{
    public class EmailConfiguration
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Проверяет, достаточно ли данных для настройки SMTP клиента.
        /// Для класса Email от ментора важен только адрес отправителя.
        /// </summary>
        public bool IsSmtpConfigured()
        {
            // Минимально необходимый параметр для класса Email - адрес отправителя
            // SMTP сервер и порт жестко заданы в классе Email.
            // Пароль не требуется из-за UseDefaultCredentials.
            return !string.IsNullOrWhiteSpace(SenderEmail);
            
            // Альтернативно, можно проверять и сервер/порт для полноты:
            // return !string.IsNullOrWhiteSpace(SmtpServer) && SmtpPort > 0 && !string.IsNullOrWhiteSpace(SenderEmail);
        }
    }
}