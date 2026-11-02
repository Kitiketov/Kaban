# Архитектура Kaban: API — RabbitMQ — Worker — PostgreSQL

> По `Kitiketov/Kaban` `main`, commit `f6b110e9f2d59efc6251060a9ab9680202d24722` (2026-11-01). Старые документы с `Features/Weather` больше не описывают актуальный код.

## Границы трёх процессов

**Api (`backend/src/Api`)**: ASP.NET Core HTTP/Swagger, Controller, MediatR Command/Query, FluentValidation (`ValidationBehavior` + зарегистрированные validators), request client MassTransit, преобразование ответа Worker в HTTP. **Нет прямых операций с PostgreSQL для новых фич.**

**Worker (`backend/src/Worker`)**: RabbitMQ MassTransit consumer, прикладные операции, проверка бизнес-инвариантов с использованием данных, EF Core `KabanDatabaseContext`, репозитории, транзакции, PostgreSQL, idempotency, `RespondAsync` для API. Отдельный `Publish` для будущих subscribers по необходимости.

**Realtime (`backend/src/Realtime`)**: отдельный host для будущего защищённого SignalR, подписки на `...Completed` события; сейчас только health endpoints. Не обрабатывает команду создания задач, не пишет в бизнес-БД и не является reply endpoint для API.

**Contracts (`backend/src/Contracts`)**: общие типы transport request/response/event. Отдельная библиотека, **не четвёртый сервис**, без EF и business logic.

## По умолчанию — запрос с ожиданием результата

```text
HTTP client
    |
    v
API Controller -> MediatR -> FluentValidation
    |
    v
IRequestClient<CreateTaskRequest>.GetResponse<TaskCreatedResult>(request, timeout, ct)
    |
    v
RabbitMQ -> Worker IConsumer<...>
    |
    v
Worker business checks -> EF Core -> PostgreSQL commit
    |
    v
context.RespondAsync<...Result>(created result)
    |
    v
API -> HTTP response with actual Worker result
```

Типы `CreateTaskRequest` и `TaskCreatedResult` в схеме — **иллюстративные имена**, в реальном текущем коде это `TaskCreateEvent` и `TaskCreateSuccess`. На данный момент Worker сохраняет задачу и отвечает через `RespondAsync`, API ждёт `GetResponse`; API не подтверждает успешное выполнение до ответа Worker. Тайм-аут конечен и настраиваем; timeout **не гарантирует** отмены уже запущенного Worker, поэтому нужна idempotency на мутациях.

`context.RespondAsync` — прямой ответ request client. `IPublishEndpoint.Publish` — отдельное широковещательное событие для Realtime/аналитики; **не заменяет** адресный ответ. Для долгих операций, не укладывающихся в HTTP-budget, отдельно согласуй альтернативный протокол JobId/status, но не внедряй его по умолчанию.

## Размещение файлов

- `backend/src/Api/Features/Tasks/Presentation/TaskController.cs` — HTTP маршрут.
- `backend/src/Api/Features/Tasks/CreateTask/Command.cs`, `Handler.cs` — MediatR use case. Для полноценной валидации нужны также `Validator.cs` и его DI-регистрация.
- `backend/src/Contracts/Features/Tasks/TaskCreateEvent.cs`, `TaskCreateSuccess.cs` — message request/response.
- `backend/src/Worker/Features/Tasks/TaskCreateConsumer.cs` — consumer с DB и response.
- `backend/src/Worker/Infrastructure/Database/Program.cs` — Worker `KabanDatabaseContext` и mapping.
- `backend/src/Api/Program.cs`, `backend/src/Worker/Program.cs` — MassTransit DI и transport.

Для новых операций предпочитай folder `Features/<Feature>/<Operation>/Command.cs`, `Handler.cs`, `Validator.cs`; полные имена C#-типов (`CreateTaskHandler`). HTTP DTO, MediatR Command и Transport DTO — разные типы. Не добавляй ссылки Worker на API или EF entity в Contracts.

## Состояние и известные ограничения

API действительно ждёт `TaskCreateSuccess`, Worker действительно вызывает SaveChanges/RespondAsync, но repo пока содержит несогласованность namespace (`Contracts.Tasks` vs `Contracts.Features.Tasks`), `Worker.csproj` с DLL HintPath, не зарегистрированные FluentValidation validators, некорректный `CreatedAtAction(nameof(Create))` и отсутствие адреса контейнерной DB для Worker. Не представляй текущую сборку/контейнерный сценарий как проверенные без фактического запуска. Подробнее — `docs/MESSAGING.md`.

Три host-а включают базовые логирующие scopes (`TraceId` и `SpanId`); законченной трассировки OpenTelemetry, outbox/inbox, durable job status, SignalR ещё нет. Health endpoint не равнозначен готовности RabbitMQ/БД.

## Проверка

Из корня: `dotnet restore backend/Kaban.sln`, `dotnet build backend/Kaban.sln`, `dotnet test backend/Kaban.sln`; при изменении Compose: `docker compose config --quiet`. Для операций через RabbitMQ нужны интеграционные проверки успешного ответа Worker **после commit**, business failure, timeout, fault, повтора после timeout и DB state. Без реального запуска не утверждай, что эти проверки выполнены.

См. `.agents/skills/kaban-rabbitmq-workflow/SKILL.md` и `docs/WORKER_DATABASE_BOUNDARY.md`.
