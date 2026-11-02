# Worker — правила для кодового агента

Применяется к `backend/src/Worker/**` вместе с корневым `AGENTS.md`. Для новой операции вызови локальный skill `.agents/skills/kaban-rabbitmq-workflow/SKILL.md` и сверяйся с `docs/MESSAGING.md`.

## Ответственность

**Worker = единственный владелец бизнес-операций и PostgreSQL, а также отправитель результата ожидающему API.** API не пишет в БД; он отправляет MassTransit request и ждёт `RespondAsync`.

1. Зарегистрируй `IConsumer<TRequest>` и нужный receive endpoint в `Worker/Program.cs` через `AddMassTransit`, `AddConsumer<T>`, `ConfigureEndpoints` либо явно настроенный endpoint. Нельзя ссылаться на Api assembly, его handlers или модели.
2. В `Consume(ConsumeContext<TRequest>)` получи DTO из `backend/src/Contracts`, вызови application/service Worker, проверь бизнес-инварианты и разрешения по данным БД. Не доверяй входу только потому, что API использует FluentValidation; сообщения могут поступить от других producers.
3. Через Worker-owned `KabanDatabaseContext`/репозитории и EF Core выполни работу, учти транзакции, уникальные ограничения, конкурентные обновления и **идемпотентность по OperationId/JobId**. Используй `context.CancellationToken` для I/O, но не прерывай транзакцию без осмысленной политики при отмене HTTP-клиента.
4. **Только после успешного DB commit** вызови `await context.RespondAsync<TSuccess>(result)` с фактическим результатом (`TaskId`, данные DTO и т. п.). Это завершает `IRequestClient.GetResponse` в API. Не заменяй `RespondAsync` на `Publish` как единственный путь возврата ответа API.
5. При ожидаемых бизнес-ошибках ответь типизированным `TBusinessFailure` через `RespondAsync` (например, `NotFound`, `Conflict`, `Forbidden`). При технической ошибке не глотай исключение: MassTransit отправит fault, API должен классифицировать его как server error. Логи без секретов/SQL details.
6. Для Realtime и других подписчиков **дополнительно**, после commit, публикуй `...Completed` через `IPublishEndpoint.Publish`. Это уведомление, не ответ ожидающему API. Если необходима гарантированная публикация вместе с DB commit — outbox Worker. Не выдавай publication за завершение request client.
7. Настрой ограниченный retry/redelivery, обработку `_error` и идемпотентность, включая поздний ответ после HTTP timeout. Тайм-аут ожидания в API **не гарантирует отмены** сообщения и не защищает от дублей. Повтор с тем же OperationId должен возвращать тот же сохранённый результат, не делать вторую запись.

## Реальная реализация Tasks

- `Program.cs` регистрирует `AddDbContext<KabanDatabaseContext>(...UseNpgsql(...))` и `AddConsumer<TaskCreateConsumer>()`, `UsingRabbitMq`, `ConfigureEndpoints`.
- `Features/Tasks/TaskCreateConsumer.cs` строит сущность `Infrastructure.Database.Domain.Tasks`, вызывает `AddAsync` + `SaveChangesAsync` и **после сохранения** `context.RespondAsync<TaskCreateSuccess>({ TaskId = entityTask.Id })`. Именно этот порядок — базовый для новых операций.
- **Дефекты текущего кода, не копировать:** `using Contracts.Tasks;` не совпадает с контрактом `Contracts.Features.Tasks`; `Worker.csproj` ссылается на файл `../Api/bin/Debug/net10.0/Contracts.dll`, а не на проект Contracts; сохранение пока не использует `context.CancellationToken`; нет идемпотентности, явных retry/outbox, typed business failures.
- `Worker/appsettings.json` содержит `Host=localhost;Port=55432` для запуска на ноутбуке; в Docker compose для Worker **пока нет** `ConnectionStrings__DefaultConnection` с `Host=postgres;Port=5432`. При изменении запуска исправляй соответствующий env и проверяй `docker compose config --quiet`.
- В модели DB/контрактах могут отсутствовать или не совпадать поля. Проверяй фактически сохраняемые колонки и маппинг; не считай request полным отражением entity.

## Структура и тесты

Фичи создавай в `Features/<Feature>/` (consumer, локальные application/domain/infrastructure по необходимости). Общая persistence Worker находится в `Infrastructure/Database`. Контракты только в `backend/src/Contracts`; бизнес-сущности — в Worker. Проверяй unit + интеграцию с реальными RabbitMQ/PostgreSQL: успешный ответ после commit, отказ, fault, timeout + поздний ответ, повтор, concurrent requests, очередь `_error`, зависший Worker. Не заявляй exactly-once.
