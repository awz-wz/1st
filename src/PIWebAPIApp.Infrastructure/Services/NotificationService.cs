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

        /// <summary>
        /// Генерирует PDF-отчет на основе данных запроса.
        /// </summary>
        /// <param name="request">Данные для экспорта в PDF.</param>
        /// <returns>Массив байтов, представляющий PDF-документ.</returns>
        public async Task<byte[]> GeneratePdfReportAsync(ExportPdfRequest request)
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            document.Open();
            
            // Определяем шрифты
            var fontFactory = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12);
            var titleFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 16, Font.BOLD);
            var boldFont = FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 12, Font.BOLD);
            var italicFont = FontFactory.GetFont("Arial", 10, Font.ITALIC); // Определяем заранее

            // Добавляем заголовок отчета
            var title = new Paragraph("PI Tag Report", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);

            // Добавляем таблицу с информацией о запросе (пользователь, email, дата, обоснование)
            var infoTable = new PdfPTable(2) { WidthPercentage = 100 };
            infoTable.SetWidths(new float[] { 1, 2 });
            
            infoTable.AddCell(new PdfPCell(new Phrase("User:", boldFont)) { Border = Rectangle.NO_BORDER });
            infoTable.AddCell(new PdfPCell(new Phrase(request.User ?? "Unknown", fontFactory)) { Border = Rectangle.NO_BORDER });
            
            infoTable.AddCell(new PdfPCell(new Phrase("Email:", boldFont)) { Border = Rectangle.NO_BORDER });
            infoTable.AddCell(new PdfPCell(new Phrase(request.Email ?? "Not specified", fontFactory)) { Border = Rectangle.NO_BORDER });
            
            // Изменено: "Export Date:" на "Timestamp:"
            infoTable.AddCell(new PdfPCell(new Phrase("Timestamp:", boldFont)) { Border = Rectangle.NO_BORDER });
            infoTable.AddCell(new PdfPCell(new Phrase(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), fontFactory)) { Border = Rectangle.NO_BORDER });
            
            if (!string.IsNullOrEmpty(request.Justification))
            {
                infoTable.AddCell(new PdfPCell(new Phrase("Justification:", boldFont)) { Border = Rectangle.NO_BORDER });
                infoTable.AddCell(new PdfPCell(new Phrase(request.Justification, fontFactory)) { Border = Rectangle.NO_BORDER });
            }
            document.Add(infoTable);

            // Добавляем отступ
            document.Add(new Paragraph(" ") { SpacingAfter = 10 });

            // Добавляем таблицу с данными тегов
            var table = new PdfPTable(5) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 2, 1, 1, 2, 2 }); // TagName, Status, Good, Previous Status, Timestamp

            // Заголовки столбцов таблицы тегов
            table.AddCell(new PdfPCell(new Phrase("Tag Name", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Status", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Good", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase("Previous Status", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            // Изменено: "Export Date" на "Timestamp"
            table.AddCell(new PdfPCell(new Phrase("Timestamp", boldFont)) { HorizontalAlignment = Element.ALIGN_CENTER });

            // Заполняем таблицу данными тегов
            if (request.Tags != null)
            {
                foreach (var tagInfo in request.Tags)
                {
                    if (!string.IsNullOrEmpty(tagInfo.TagName))
                    {
                        // Получаем текущее значение тега из PI
                        var tagValue = await _tagService.GetTagValueAsync(_piConfig.DataServerWebId, tagInfo.TagName);
                        
                        // 1. Имя тега
                        table.AddCell(new PdfPCell(new Phrase(tagInfo.TagName, fontFactory)));
                        
                        // 2. Новое состояние (Status) - из запроса
                        table.AddCell(new PdfPCell(new Phrase(tagInfo.NewState ?? "N/A", fontFactory)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        
                        // 3. Качество данных (Good)
                        var isGood = tagValue?.Good.ToString() ?? "N/A";
                        table.AddCell(new PdfPCell(new Phrase(isGood, fontFactory)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        
                        // 4. Предыдущее состояние (Previous Status) - из текущего значения в PI
                        var previousStatus = tagValue?.GetDisplayValue() ?? "N/A";
                        table.AddCell(new PdfPCell(new Phrase(previousStatus, fontFactory)));
                        
                        // 5. Дата экспорта/создания отчета (Timestamp) - текущая дата/время
                        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        table.AddCell(new PdfPCell(new Phrase(timestamp, fontFactory)));
                    }
                }
            }
            document.Add(table);

            // Добавляем отступ перед футером
            document.Add(new Paragraph(" ") { SpacingBefore = 20 });

            // Добавляем футер
            var generatedByParagraph = new Paragraph("Generated by PI Tag Manager", italicFont)
            {
                Alignment = Element.ALIGN_RIGHT
            };
            document.Add(generatedByParagraph);

            // Закрываем документ
            document.Close();
            
            // Возвращаем массив байтов PDF
            return memoryStream.ToArray();
        }
    }
}