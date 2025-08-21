namespace PIWebAPIApp.Models
{
    public class EmailConfiguration
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty; // Это будет заполнено из User Secrets
        public string SenderDisplayName { get; set; } = string.Empty;

        // Валидация конфигурации SMTP
        // Для Gmail и большинства SMTP серверов требуется сервер, порт, email и password.
        public bool IsSmtpConfigured()
        {
            return !string.IsNullOrWhiteSpace(SmtpServer) &&
                   SmtpPort > 0 &&
                   !string.IsNullOrWhiteSpace(SenderEmail) &&
                   !string.IsNullOrWhiteSpace(SenderPassword); // Пароль обязателен
        }
    }
}