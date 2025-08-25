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
// === РЕГИСТРАЦИЯ КОНФИГУРАЦИЙ ИЗ appsettings.json ===

// PI Configuration
builder.Services.AddSingleton<PIConfiguration>(sp =>
{
    // Читаем конфигурацию из appsettings.json
    var configSection = builder.Configuration.GetSection("PIConfiguration");
    var config = new PIConfiguration();
    configSection.Bind(config); // Заполняем объект данными из секции
    Logger.Info($"PIConfiguration loaded from appsettings.json - BaseUrl: {config.BaseUrl}, WebId: {config.DataServerWebId}");
    return config;
});

// Email Configuration
builder.Services.AddSingleton<EmailConfiguration>(sp =>
{
    var emailConfig = new EmailConfiguration();
    // Читаем конфигурацию из appsettings.json
    builder.Configuration.GetSection("EmailConfiguration").Bind(emailConfig);
    
    // Логируем для отладки (временно)
    Logger.Info($"EmailConfiguration loaded from appsettings.json - Server: {emailConfig.SmtpServer}, Port: {emailConfig.SmtpPort}, Email: {emailConfig.SenderEmail}, Password Length: {(emailConfig.SenderPassword ?? "").Length}");
    
    return emailConfig;
});
// === КОНЕЦ РЕГИСТРАЦИИ КОНФИГУРАЦИЙ ===

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