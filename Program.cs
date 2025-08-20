using PIWebAPIApp.Interfaces;
using PIWebAPIApp.Models;
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

builder.Services.AddSingleton<PIWebApiClient>(sp =>
{
    var config = sp.GetRequiredService<PIConfiguration>();
    return new PIWebApiClient(config);
});

builder.Services.AddSingleton<ITagService, PITagService>();
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