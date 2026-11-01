# Выбранный подход: feature-first, локальные слои, привычные CQRS/MediatR

**API** состоит из модулей (`Features/Weather`, в будущем `Features/Users` и `Features/Tasks`). Каждый модуль содержит `Presentation`, `Application`, `Domain`, `Infrastructure`. Внутри `Application` операции могут группироваться по `Command/Query` и handler, как в Boardify и Blueprint. В отличие от старой структуры, у каждого модуля свои входные DTO, DTO результата, интерфейсы репозиториев и EF-реализации **рядом**. В отдельном общем `Infrastructure/Persistence` остаётся DbContext, который принадлежит API-сервису, а не Weather.

HTTP request -> Controller -> mediator.Send(Command/Query) -> FluentValidation Behavior -> Handler -> IWeatherRepository / IUnitOfWork -> WeatherRepository / KabanDbContext -> PostgreSQL.

**Worker** и **Realtime** — отдельные запускаемые сервисы с health endpoints. Они не подключены к RabbitMQ и не знают про Weather. Команда добавит свою интеграцию позже. Это 3 микросервиса в одном `.sln`, плюс 1 тестовый проект. Предполагается один монорепозиторий с отдельно расположенным `frontend/`.

## Трассировка запросов

Все три host-а включают `TraceId`, `SpanId` и `ParentId` в logging scopes и вывод этих scopes в консоль. ASP.NET Core создаёт `Activity` для входящего HTTP-запроса и принимает W3C `traceparent`; поэтому связанные логи получают одинаковый `TraceId` и свой `SpanId`. Для будущих исходящих HTTP-вызовов используйте `HttpClient`/`IHttpClientFactory`: стандартный .NET handler переносит W3C trace context в `traceparent`. При обмене через MassTransit используйте его обычные `Publish`/`Send` и `ConsumeContext` — транспортная диагностика связывает контекст публикации и обработки. Не передавайте trace id вручную в DTO и не используйте его как бизнес-идентификатор.

Пример логирования в handler/consumer: `logger.LogInformation("Handled {EventType}", nameof(ForecastCreatedEvent));`. При активном `Activity` консольный лог содержит `TraceId` и `SpanId`; для поиска цепочки между сервисами фильтруйте по `TraceId`. Вне активного запроса/сообщения (например, автономная фоновая задача) `Activity.Current` может отсутствовать — для таких процессов позднее следует создавать собственную root `Activity`.

Это базовая корреляция логов, без экспортёра OpenTelemetry и backend-а хранения трасс. Метрики и экспортёр можно добавить отдельно, когда будут выбраны сборщик и система наблюдаемости.

Изменения относительно последней минимальной версии: заменены handlers вызываемые напрямую через DI на MediatR `IRequestHandler`, добавлены `Command`, `Query`, validation pipeline, `IUnitOfWork`, `FluentValidation`, перенос `DbContext` в общую инфраструктуру и конфигурации EF в Weather, улучшен тест handler-а. Схема обмена сообщениями не проектировалась.
