using Microsoft.AspNetCore.Mvc;
using PIWebAPIApp.Interfaces;
using PIWebAPIApp.Models.DTOs;
using PIWebAPIApp.Utilities;
using System.Security.Claims;
using PIWebAPIApp.Models; // Добавляем для PIConfiguration

namespace PIWebAPIApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;
        private readonly INotificationService _notificationService;
        private readonly PIConfiguration _config; // Используем правильный тип

        public TagsController(ITagService tagService, INotificationService notificationService, PIConfiguration config)
        {
            _tagService = tagService;
            _notificationService = notificationService;
            _config = config;
        }

        [HttpGet("current-user")]
        public IActionResult GetCurrentUser()
        {
            var user = HttpContext.User;
            string? email = null; // Делаем nullable

            if (user != null)
            {
                var claimsIdentity = user.Identity as ClaimsIdentity;
                email = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            }

            var username = user?.Identity?.Name;

            var result = !string.IsNullOrEmpty(email)
                ? email
                : !string.IsNullOrEmpty(username)
                    ? username
                    : "unknown";

            return Ok(new { success = true, user = result });
        }

        [HttpGet("search/{filter}")]
        public async Task<ActionResult<List<string>>> SearchTags(string filter)
        {
            try
            {
                Logger.Info($"Поиск тегов по фильтру: {filter}");
                
                var filteredTags = await _tagService.SearchTagsAsync(filter);

                Logger.Info($"Найдено {filteredTags.Count} тегов");
                return Ok(filteredTags);
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при поиске тегов: {ex.Message}");
                return StatusCode(500, new { error = $"Ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPost("export-pdf")]
        public async Task<IActionResult> ExportToPdf([FromBody] ExportPdfRequest request) // Используем правильный тип
        {
            try
            {
                Logger.Info("Начало экспорта в PDF");
                
                if (request?.Tags == null || !request.Tags.Any())
                {
                    return BadRequest(new { error = "No tags to export" });
                }

                var pdfBytes = await _notificationService.GeneratePdfReportAsync(request);
                var fileName = $"pi_tags_report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                Logger.Info("PDF успешно сгенерирован");
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при экспорте в PDF: {ex.Message}");
                Logger.Error($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Ошибка сервера: {ex.Message}" });
            }
        }

        [HttpGet("{tagName}")]
        public async Task<ActionResult<TagValueDto>> GetTagValue(string tagName)
        {
            try
            {
                Logger.Info($"Получение значения для тега: {tagName}");

                if (string.IsNullOrWhiteSpace(tagName))
                {
                    Logger.Error("Пустое имя тега");
                    return BadRequest(new { error = "Tag name is required" });
                }

                var value = await _tagService.GetTagValueAsync(_config.DataServerWebId, tagName);
                if (value == null)
                {
                    Logger.Error($"Тег '{tagName}' не найден");
                    return NotFound(new { error = $"Тег '{tagName}' не найден" });
                }

                var result = new TagValueDto
                {
                    Value = value.GetDisplayValue(),
                    Timestamp = value.Timestamp,
                    Good = value.Good,
                    UnitsAbbreviation = value.UnitsAbbreviation,
                    DisplayValue = value.GetDisplayValue()
                };

                Logger.Info($"Успешно получено значение для тега: {tagName}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при получении значения тега {tagName}: {ex.Message}");
                Logger.Error($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Ошибка сервера: {ex.Message}" });
            }
        }

        [HttpPost("update")]
        public async Task<ActionResult<object>> UpdateTagValue([FromBody] UpdateTagRequest request)
        {
            try
            {
                Logger.Info($"Обновление значения для тега: {request.TagName} на {request.NewState}");

                if (string.IsNullOrWhiteSpace(request.TagName) ||
                    string.IsNullOrWhiteSpace(request.NewState) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Justification))
                {
                    return BadRequest(new { success = false, message = "Tag name, new state, email, and justification are required" });
                }

                var currentValue = await _tagService.GetTagValueAsync(_config.DataServerWebId, request.TagName);
                var oldState = currentValue?.GetDisplayValue() ?? "Unknown";

                var success = await _tagService.ChangeDigitalStateAsync(
                    _config.DataServerWebId,
                    request.TagName,
                    request.NewState);

                if (success)
                {
                    Logger.Info($"Успешно обновлено значение для тега: {request.TagName}");
                    
                    var emailSent = await _notificationService.SendEmailNotificationAsync(
                        request.Email,
                        request.TagName,
                        oldState,
                        request.NewState,
                        request.User ?? "Unknown User",
                        request.Justification
                    );

                    Logger.Info($"Email notification sent: {emailSent}");

                    return Ok(new
                    {
                        success = true,
                        message = "Tag updated successfully",
                        emailSent = emailSent
                    });
                }
                else
                {
                    Logger.Error($"Не удалось обновить значение для тега: {request.TagName}");
                    return BadRequest(new { success = false, message = "Failed to update tag" });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при обновлении значения тега {request.TagName}: {ex.Message}");
                Logger.Error($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, error = $"Ошибка сервера: {ex.Message}" });
            }
        }

        [HttpGet("list")]
        public ActionResult<List<string>> GetTagList()
        {
            try
            {
                Logger.Info("Получение списка тегов");

                var tags = _tagService.GetAllTags().Take(20).ToList();

                Logger.Info("Список тегов успешно возвращен");
                return Ok(tags);
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при получении списка тегов: {ex.Message}");
                return StatusCode(500, new { error = $"Ошибка сервера: {ex.Message}" });
            }
        }
    }
}