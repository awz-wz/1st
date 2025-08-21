using PIWebAPIApp.Interfaces;
using PIWebAPIApp.Models.DTOs;
using PIWebAPIApp.Utilities;
using System.Net;
using System.Net.Mail;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using PIWebAPIApp.Models; // Для PIConfiguration и EmailConfiguration

namespace PIWebAPIApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly PIConfiguration _piConfig; // PI конфигурация
        private readonly EmailConfiguration _emailConfig; // Email конфигурация
        private readonly ITagService _tagService;

        // Обновляем конструктор: принимаем обе конфигурации
        public NotificationService(PIConfiguration piConfig, EmailConfiguration emailConfig, ITagService tagService)
        {
            _piConfig = piConfig ?? throw new ArgumentNullException(nameof(piConfig));
            _emailConfig = emailConfig ?? throw new ArgumentNullException(nameof(emailConfig));
            _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
            
            // Логируем для отладки (временно)
            // Logger.Info($"NotificationService created with EmailConfig: Server={_emailConfig.SmtpServer}, Port={_emailConfig.SmtpPort}, Email={_emailConfig.SenderEmail}, Password Length={(_emailConfig.SenderPassword ?? "").Length}");
        }

        public async Task<bool> SendEmailNotificationAsync(string toEmail, string tagName, string oldState, string newState, string user, string justification)
        {
            try
            {
                // Проверяем, настроена ли SMTP полностью
                if (!_emailConfig.IsSmtpConfigured())
                {
                     Logger.Warn("SMTP configuration is incomplete (Server, Port, Email, or Password is missing). Email notification will not be sent.");
                     return false;
                }

                using var client = new SmtpClient(_emailConfig.SmtpServer, _emailConfig.SmtpPort);
                client.EnableSsl = true;
                // Используем логин/пароль из _emailConfig
                client.Credentials = new NetworkCredential(_emailConfig.SenderEmail, _emailConfig.SenderPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailConfig.SenderEmail, _emailConfig.SenderDisplayName),
                    Subject = $"PI Tag Change Notification: {tagName}",
                    Body = $@"
<html>
<body>
    <h2>PI Tag Value Changed</h2>
    <p><strong>Tag Name:</strong> {tagName}</p>
    <p><strong>Previous State:</strong> {oldState}</p>
    <p><strong>New State:</strong> {newState}</p>
    <p><strong>Changed By:</strong> {user}</p>
    <p><strong>Justification:</strong> {justification}</p>
    <p><strong>Timestamp:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
</body>
</html>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);
                await client.SendMailAsync(mailMessage);
                Logger.Info($"Email успешно отправлен на {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при отправке email: {ex.Message}");
                // Лучше логировать внутреннее исключение, если есть
                if(ex is SmtpException smtpEx)
                {
                    Logger.Error($"SMTP Error Details: {smtpEx.StatusCode} - {smtpEx.Message}");
                }
                return false;
            }
        }
        // Исправляем сигнатуру метода для соответствия интерфейсу
        // ... внутри метода замените _config на _piConfig если переименовали ...
        public async Task<byte[]> GeneratePdfReportAsync(ExportPdfRequest request)
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            document.Open();
            var fontFactory = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12);
            var titleFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16, Font.BOLD);
            var boldFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12, Font.BOLD);
            var title = new Paragraph("PI Tag Report", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);
            var infoTable = new PdfPTable(2) { WidthPercentage = 100 };
            infoTable.SetWidths(new float[] { 1, 2 });
            // Исправляем ошибки с Font - передаем текст отдельно
            infoTable.AddCell(new PdfPCell(new Phrase("User:", boldFont)) { Border = Rectangle.NO_BORDER });
            infoTable.AddCell(new PdfPCell(new Phrase(request.User ?? "Unknown", fontFactory)) { Border = Rectangle.NO_BORDER });
            infoTable.AddCell(new PdfPCell(new Phrase("Email:", boldFont)) { Border = Rectangle.NO_BORDER });
            infoTable.AddCell(new PdfPCell(new Phrase(request.Email ?? "Not specified", fontFactory)) { Border = Rectangle.NO_BORDER });
            infoTable.AddCell(new PdfPCell(new Phrase("Export Date:", boldFont)) { Border = Rectangle.NO_BORDER });
            infoTable.AddCell(new PdfPCell(new Phrase(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), fontFactory)) { Border = Rectangle.NO_BORDER });
            if (!string.IsNullOrEmpty(request.Justification))
            {
                infoTable.AddCell(new PdfPCell(new Phrase("Justification:", boldFont)) { Border = Rectangle.NO_BORDER });
                infoTable.AddCell(new PdfPCell(new Phrase(request.Justification, fontFactory)) { Border = Rectangle.NO_BORDER });
            }
            document.Add(infoTable);
            document.Add(new Paragraph(" ") { SpacingAfter = 10 });
            var table = new PdfPTable(5) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 2, 1, 1, 2, 2 }); // TagName, Status, Good, Previous Status, Export Date
            // Исправляем ошибки с Font - используем правильные заголовки
            table.AddCell(new PdfPCell(new Phrase("Tag Name", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Status", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Good", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Previous Status", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER }); // Изменено
            table.AddCell(new PdfPCell(new Phrase("Export Date", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER }); // Изменено
            // Предполагается, что oldState передается как часть request или как-то еще.
            // Для демонстрации, добавим фиктивное oldState. В реальном приложении это должно быть частью логики.
            // Если oldState не доступен здесь, его нужно передавать в request или получать другим способом.
            // Пока используем значение из GetTagValueAsync как "предыдущее".
            if (request.Tags != null)
            {
                foreach (var tagInfo in request.Tags)
                {
                    if (!string.IsNullOrEmpty(tagInfo.TagName))
                    {
                        // Используем _piConfig вместо _config
                        var tagValue = await _tagService.GetTagValueAsync(_piConfig.DataServerWebId, tagInfo.TagName);
                        table.AddCell(new PdfPCell(new Phrase(tagInfo.TagName, fontFactory)));
                        table.AddCell(new PdfPCell(new Phrase(tagInfo.NewState ?? "N/A", fontFactory)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        var isGood = tagValue?.Good.ToString() ?? "N/A";
                        table.AddCell(new PdfPCell(new Phrase(isGood, fontFactory)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        // Используем GetDisplayValue как "Previous Status"
                        var previousStatus = tagValue?.GetDisplayValue() ?? "N/A";
                        table.AddCell(new PdfPCell(new Phrase(previousStatus, fontFactory)));
                        // Используем текущую дату как "Export Date"
                        var exportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        table.AddCell(new PdfPCell(new Phrase(exportDate, fontFactory)));
                    }
                }
            }
            document.Add(table);
            document.Add(new Paragraph(" ") { SpacingBefore = 20 });
            // Исправляем ошибку с Font - создаем новый Font правильно
            var italicFont = FontFactory.GetFont("Arial", 10, Font.ITALIC);
            var generatedByParagraph = new Paragraph("Generated by PI Tag Manager", italicFont)
            {
                Alignment = Element.ALIGN_RIGHT
            };
            document.Add(generatedByParagraph);
            document.Close();
            return memoryStream.ToArray();
        }
    }
}