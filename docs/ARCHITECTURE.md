# Выбранный подход: feature-first, локальные слои, привычные CQRS/MediatR

**API** состоит из модулей (`Features/Weather`, в будущем `Features/Users` и `Features/Tasks`). Каждый модуль содержит `Presentation`, `Application`, `Domain`, `Infrastructure`. Внутри `Application` операции могут группироваться по `Command/Query` и handler, как в Boardify и Blueprint. В отличие от старой структуры, у каждого модуля свои входные DTO, DTO результата, интерфейсы репозиториев и EF-реализации **рядом**. В отдельном общем `Infrastructure/Persistence` остаётся DbContext, который принадлежит API-сервису, а не Weather.

HTTP request -> Controller -> mediator.Send(Command/Query) -> FluentValidation Behavior -> Handler -> IWeatherRepository / IUnitOfWork -> WeatherRepository / KabanDbContext -> PostgreSQL.

**Worker** и **Realtime** — отдельные запускаемые сервисы с health endpoints. Они не подключены к RabbitMQ и не знают про Weather. Команда добавит свою интеграцию позже. Это 3 микросервиса в одном `.sln`, плюс 1 тестовый проект. Предполагается один монорепозиторий с отдельно расположенным `frontend/`.

Изменения относительно последней минимальной версии: заменены handlers вызываемые напрямую через DI на MediatR `IRequestHandler`, добавлены `Command`, `Query`, validation pipeline, `IUnitOfWork`, `FluentValidation`, перенос `DbContext` в общую инфраструктуру и конфигурации EF в Weather, улучшен тест handler-а. Схема обмена сообщениями не проектировалась.
