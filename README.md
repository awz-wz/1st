# PIWebAPIApp

## Обзор
PIWebAPIApp предоставляет REST API и минимальный веб-интерфейс для поиска, чтения и массового обновления тегов в системе OSIsoft PI. Приложение также отправляет e‑mail уведомления и формирует PDF‑отчёты о произведённых изменениях.

## Архитектура
Проект разделён на четыре слоя:

### 1. Domain
Содержит базовые сущности:
- `PIPoint` — описание PI‑тега (WebId, имя, путь и т.д.)【F:src/PIWebAPIApp.Domain/Entities/PIPoint.cs】
- `PIValue` — значение тега c временной меткой и признаком качества; умеет преобразовывать и красиво отображать данные【F:src/PIWebAPIApp.Domain/Entities/PIValue.cs】

### 2. Application
Определяет модели и контракты:
- DTO `TagInfo` и `ExportPdfRequest`
- Интерфейс `ITagService` для чтения, записи и поиска тегов【F:src/PIWebAPIApp.Application/Interfaces/ITagService.cs】
- Интерфейс `INotificationService` (реализация см. ниже)

### 3. Infrastructure
Реализации сервисов и вспомогательные классы:
- `PIWebApiClient` — низкоуровневый клиент PI Web API (поиск точек, чтение и запись значений)【F:src/PIWebAPIApp.Infrastructure/Clients/ PIWebApiClient.cs】
- `PITagService` — высокоуровневые операции с тегами: изменение цифрового состояния, получение значения, поиск по фильтру и кэшированный список всех доступных тегов【F:src/PIWebAPIApp.Infrastructure/Services/PITagService.cs】
- `NotificationService` — отправка e‑mail уведомлений и генерация PDF‑отчётов о действиях пользователя【F:src/PIWebAPIApp.Infrastructure/Services/NotificationService.cs】
- Утилиты `Logger` и `Email`

### 4. Presentation
ASP.NET Core приложение с REST‑контроллером и статическими ресурсами:
- `Program.cs` настраивает CORS, регистрирует конфигурации и сервисы, обслуживает статические файлы【F:src/PIWebAPIApp.Presentation/Program.cs】【F:src/PIWebAPIApp.Presentation/Program.cs】
- `TagsController` предоставляет конечные точки API【F:src/PIWebAPIApp.Presentation/Controllers/TagsController.cs】
- В `wwwroot` размещён упрощённый фронтенд (`index.html`, `app.js`, `style.css`)【F:src/PIWebAPIApp.Presentation/wwwroot/index.html】

## Конфигурация
Настройки задаются в `appsettings.json`:
```json
{
  "PIConfiguration": {
    "BaseUrl": "https://piwebd2.tengizchevroil.com/piwebapi",
    "DataServerWebId": "F1DSWCdJQqyHMkueMiGiQ3KQpwvKUjAAVENPR0lMU1RQSURcS1RMLUZXUy1LLTI2MC5NVg",
    "TimeoutSeconds": 30,
    "UseDefaultCredentials": true
  },
  "EmailConfiguration": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "awzwz0401@gmail.com",
    "SenderPassword": "***"
  }
}
```
> **Важно:** реальные учётные данные SMTP должны храниться в секретах пользователя или переменных окружения, а не в репозитории.

## Запуск
1. Установите .NET 8 SDK.
2. В папке проекта выполните:
   ```bash
   dotnet restore
   dotnet run --project src/PIWebAPIApp.Presentation
   ```
3. По умолчанию фронтенд доступен по адресу `http://localhost:5276`.

## API
| Метод и путь | Назначение |
|--------------|------------|
| `GET /api/tags/current-user` | Возвращает имя текущего пользователя Windows【F:src/PIWebAPIApp.Presentation/Controllers/TagsController.cs】 |
| `GET /api/tags/search/{filter}` | Ищет теги по подстроке (мин. 2 символа)【F:src/PIWebAPIApp.Presentation/Controllers/TagsController.cs】 |
| `GET /api/tags/{tagName}` | Получает текущее значение тега【F:src/PIWebAPIApp.Presentation/Controllers/TagsController.cs】 |
| `POST /api/tags/update` | Изменяет цифровое состояние тега и отправляет e‑mail уведомление【F:src/PIWebAPIApp.Presentation/Controllers/TagsController.cs】 |
| `POST /api/tags/export-pdf` | Формирует PDF‑отчёт по набору тегов【F:src/PIWebAPIApp.Presentation/Controllers/TagsController.cs】 |

## Веб‑интерфейс
Статическая страница `index.html` позволяет:
- указать почту для уведомлений и обоснование изменения;
- добавлять до 10 тегов и выбирать для каждого новое состояние;
- сохранить изменения и экспортировать отчёт в PDF【F:src/PIWebAPIApp.Presentation/wwwroot/index.html†L16-L47】.

## Логирование и уведомления
- Все операции пишутся в консоль через статический `Logger`【F:src/PIWebAPIApp.Infrastructure/Utilities/Logger.cs】.
- При успешном обновлении тегов `NotificationService` отправляет письмо и добавляет запись в PDF‑отчёт с указанием пользователя, старого и нового состояния и временной метки【F:src/PIWebAPIApp.Infrastructure/Services/NotificationService.cs】.

## Известные проблемы и идеи развития
- В репозитории присутствуют артефакты сборки (`bin/`, `obj/`) и файл с реальными паролями — их следует удалить и добавить `.gitignore`.
- Реализовать `PITagRepository` для получения списка тегов из внешнего источника вместо жёстко прописанного массива.
- Перенести секреты в `UserSecrets`/переменные окружения и улучшить обработку ошибок.
