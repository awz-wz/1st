using PIWebAPIApp.Interfaces;
using PIWebAPIApp.Models; // Убедитесь, что это есть
using PIWebAPIApp.Services;
using PIWebAPIApp.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Добавляем CORS для разрешения запросов с фронтенда
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Добавляем контроллеры
builder.Services.AddControllers();

// Добавляем сервисы
// Конфигурация PI - оставляем как было, это работает
builder.Services.AddSingleton<PIConfiguration>(sp =>
{
    return new PIConfiguration
    {
        BaseUrl = "https://piwebd2.tengizchevroil.com/piwebapi",
        DataServerWebId = "F1DSWCdJQqyHMkueMiGiQ3KQpwvKUjAAVENPR0lMU1RQSURcS1RMLUZXUy1LLTI2MC5NVg",
        UseDefaultCredentials = true,
        TimeoutSeconds = 30
    };
});

// === ИСПРАВЛЕННАЯ РЕГИСТРАЦИЯ EmailConfiguration ===
// 1. Регистрируем EmailConfiguration как Singleton, заполняя его данными из appsettings.json
builder.Services.AddSingleton<EmailConfiguration>(sp =>
{
    var emailConfig = new EmailConfiguration();
    // Читаем конфигурацию из appsettings.json
    builder.Configuration.GetSection("EmailConfiguration").Bind(emailConfig);
    
    // Логируем для отладки (временно)
    Logger.Info($"EmailConfiguration loaded from appsettings.json - Server: {emailConfig.SmtpServer}, Port: {emailConfig.SmtpPort}, Email: {emailConfig.SenderEmail}, Password Length: {(emailConfig.SenderPassword ?? "").Length}");
    
    return emailConfig;
});
// === КОНЕЦ ИСПРАВЛЕННОЙ РЕГИСТРАЦИИ EmailConfiguration ===

builder.Services.AddSingleton<PIWebApiClient>(sp =>
{
    var config = sp.GetRequiredService<PIConfiguration>();
    return new PIWebApiClient(config);
});

builder.Services.AddSingleton<ITagService, PITagService>();
// Регистрируем NotificationService, который теперь зависит от EmailConfiguration
builder.Services.AddSingleton<INotificationService, NotificationService>();

var app = builder.Build();

// Включаем CORS
app.UseCors("AllowAll");

// Разрешаем обслуживание статических файлов
app.UseStaticFiles();

// Добавляем маршруты API
app.MapControllers();

// Добавляем fallback для SPA
app.MapFallbackToFile("index.html");

// Запускаем приложение
Logger.Info("Веб-приложение запущено");
app.Run();