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