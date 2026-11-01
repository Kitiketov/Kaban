# Архитектура Kaban — три микросервиса, feature-first

## Принятое решение для новых фич

**HTTP client → API (MediatR + FluentValidation) → RabbitMQ (MassTransit) → Worker (Application/Domain + EF Core) → PostgreSQL.**

- **API** принимает запрос, проверяет входящие поля через `ValidationBehavior`/FluentValidation, аутентифицирует/авторизует пользователя и отправляет `...Requested`. Для долгих операций отвечает `202 Accepted` + `JobId`; это подтверждает постановку, **не** завершение задачи. Не пишет бизнес-данные в PostgreSQL.
- **Worker** получает сообщение, повторно проверяет правила, требующие доступа к данным, выполняет работу, сохраняет результат и статус в **своей** базе и при необходимости публикует `...Completed`/`...Failed`. Worker должен обеспечивать идемпотентность по `JobId`, безопасные retry и согласованность результата и события (outbox при необходимости).
- **Чтение через API**: HTTP GET → MediatR Query → MassTransit request/response → Worker → EF Core/PostgreSQL → DTO. У запроса есть timeout; API не использует свой DbContext.
- **Realtime** — отдельный сервис для будущих SignalR-уведомлений об уже произошедших событиях. Пока SignalR и RabbitMQ consumer там отсутствуют.
- **Contracts** — общие message DTO для обмена, не микросервис. Не передавать DbContext, EF entity, JWT и пароли. Взаимодействие через Contracts и RabbitMQ, не ссылки на проект API из Worker.

Это **целевая архитектура для новых фич**. Не утверждай, что она полностью работает сегодня. Полные правила: [MESSAGING.md](MESSAGING.md), [WORKER_DATABASE_BOUNDARY.md](WORKER_DATABASE_BOUNDARY.md), [`SKILL.md`](../.agents/skills/kaban-rabbitmq-workflow/SKILL.md).

## Текущее состояние кода: Weather — legacy

Текущий учебный пример всё ещё работает по другому пути:

```text
POST /api/weather
  → WeatherController
  → IMediator.Send(CreateForecastCommand)
  → FluentValidation pipeline
  → CreateForecastHandler
  → IWeatherRepository + IUnitOfWork
  → Api/KabanDbContext → PostgreSQL
  → MassTransit.Publish(ForecastCreatedEvent)
  → RabbitMQ
  → Worker/ForecastCreatedConsumer → только ILogger
```

`Api/Infrastructure/Persistence/KabanDbContext` — существующий API DbContext старой фичи. Не добавляй в него новые бизнес-сущности. `backend/Database` — отдельный экспериментальный проект, пока не входит в `backend/Kaban.sln` и не подключён к Worker. Worker пока не имеет собственного DbContext, а Realtime — SignalR Hub. Миграция Weather на целевую схему — отдельная задача, не выполняемая простым изменением документации.

## Разделение кода

- `backend/src/Api/Features/<Feature>/Presentation` — HTTP request DTO, controller, маршруты.
- `backend/src/Api/Features/<Feature>/Application/<Operation>` — MediatR Command/Query, Handler, Validator; отправка задания или query к Worker через message adapter.
- `backend/src/Worker/Features/<Feature>` — consumer, application/domain, repository и EF mapping. Общий Worker persistence можно выделить в `Worker/Infrastructure/Persistence`.
- `backend/Contracts/<Feature>` — неизменяемые message DTO и ответы/события; без бизнес-логики.
- `backend/src/Realtime` — будущие hub и подписки на события, без хранения бизнес-данных.

Создавай только слои и файлы, которые нужны новой операции. Придерживайся коротких имён `Command.cs`, `Query.cs`, `Handler.cs`, `Validator.cs` в отдельных каталогах операций и полных имён C# типов, например `CreateTaskCommand`.

## Трассировка и диагностика

Текущие host-проекты включают `TraceId`, `SpanId`, `ParentId` в консольные logging scopes. ASP.NET Core принимает W3C `traceparent`; MassTransit распространяет транспортную диагностику при штатном `Send`/`Publish`. Не копируй trace id в бизнес-DTO и не используй его вместо `JobId`. Для длительных заданий логируй тип сообщения, `JobId`, correlation и итог; не логируй секреты. Экспорт OpenTelemetry/Jaeger и полноценные health readiness с проверкой брокера/БД ещё не реализованы.

## Готовность и тестирование

Реальные RabbitMQ/PostgreSQL, Docker и `.NET 10` проверяй интеграционными тестами; unit-тесты handler не доказывают, что consumer зарегистрирован и очередь работает. Проверяй отрицательную валидацию (сообщение не отправлено), успешную доставку, дубль, временный сбой, Worker unavailable и фактический результат в БД. Не обещай exactly-once, если нет идемпотентного применения операции.
