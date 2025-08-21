using PIWebAPIApp.Models.DTOs; // Добавляем using

namespace PIWebAPIApp.Interfaces
{
    public interface INotificationService
    {
        Task<bool> SendEmailNotificationAsync(string toEmail, string tagName, string oldState, string newState, string user, string justification);
        Task<byte[]> GeneratePdfReportAsync(ExportPdfRequest request); // Убедимся, что тип правильный
    }
}