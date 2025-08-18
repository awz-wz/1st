using Microsoft.AspNetCore.Mvc;
using PIWebAPIApp.Models;
using PIWebAPIApp.Services;
using PIWebAPIApp.Utilities;

namespace PIWebAPIApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly PITagService _tagService;
        private readonly PIConfiguration _config;

        public TagsController()
        {
            Logger.Info("Инициализация TagsController");
            
            // Создаем конфигурацию (убрал пробелы в BaseUrl!)
            _config = new PIConfiguration
            {
                BaseUrl = "https://piwebd2.tengizchevroil.com/piwebapi", // Убрал пробелы!
                DataServerWebId = "F1DSWCdJQqyHMkueMiGiQ3KQpwvKUjAAVENPR0lMU1RQSURcS1RMLUZXUy1LLTI2MC5NVg",
                UseDefaultCredentials = true,
                TimeoutSeconds = 30
            };

            // Создаем клиент и сервис
            var client = new PIWebApiClient(_config);
            _tagService = new PITagService(client);
            
            Logger.Info("TagsController инициализирован успешно");
        }

        [HttpGet("{tagName}")]
        public async Task<ActionResult<object>> GetTagValue(string tagName)
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
                
                // Форматируем данные для фронтенда
                var result = new
                {
                    value = value.GetDisplayValue(),
                    timestamp = value.Timestamp,
                    good = value.Good,
                    unitsAbbreviation = value.UnitsAbbreviation,
                    displayValue = value.GetDisplayValue()
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
                
                if (string.IsNullOrWhiteSpace(request.TagName) || string.IsNullOrWhiteSpace(request.NewState))
                {
                    return BadRequest(new { success = false, message = "Tag name and new state are required" });
                }
                
                var success = await _tagService.ChangeDigitalStateAsync(
                    _config.DataServerWebId, 
                    request.TagName, 
                    request.NewState);
                
                if (success)
                {
                    Logger.Info($"Успешно обновлено значение для тега: {request.TagName}");
                    return Ok(new { success = true, message = "Tag updated successfully" });
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
                
                // Список тегов для отображения
                var tags = new List<string>
                {
                    "KTL-FWS-K-260.MV",
                    // Добавьте сюда другие теги
                };
                
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

    public class UpdateTagRequest
    {
        public string TagName { get; set; }
        public string NewState { get; set; }
    }
}