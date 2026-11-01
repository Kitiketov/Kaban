# Worker — инструкции агенту

Действует на `backend/src/Worker/**` вместе с корневым `AGENTS.md`. **Обязательно** прочитай `docs/MESSAGING.md` и `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`, если задача касается API → Worker, RabbitMQ, фоновых заданий, событий или новых consumers.

## Обязательная целевая архитектура — Worker владеет PostgreSQL

**Для новых фич только Worker читает и изменяет прикладную БД.** API ограничивается HTTP, MediatR, FluentValidation, авторизацией без прямых обращений к таблицам и отправкой заданий MassTransit в RabbitMQ. Конфигурация подключения к PostgreSQL, EF Core `DbContext`, репозитории, Unit of Work, миграции, записи и чтения должны быть на стороне Worker или в принадлежащей ему библиотеке Persistence/Database. Одна физическая PostgreSQL-инстанция допустима; право доступа определяется сервисом, а не только сервером БД.

- `IConsumer<TRequested>` получает **задание**, вызывает Worker Application handler/service, проверяет права и бизнес-инварианты, зависящие от БД, сохраняет результат и публикует `...Completed`/`...Failed` при необходимости.
- Входящую форму и базовые ограничения параметров API проверяет FluentValidation **до** отправки в очередь. Повторную/авторитетную проверку бизнес-правил и состояния БД выполняет Worker; потребители могут запускаться не только через HTTP.
- Примеры новых структур: `Worker/Features/<Feature>/Application`, `Domain`, `Infrastructure`, `Consumers`; `Worker/Infrastructure/Persistence` с Worker-owned `DbContext`. Создавай только необходимые каталоги и интерфейсы; избегай глобального мусора. Согласуй, подключать ли `backend/Database` как библиотеку Worker (сейчас она не входит в solution), или переносить модели по фичам.
- Не используй `Kaban.Api.Infrastructure.Persistence.KabanDbContext`, `IWeatherRepository` API или `Api.csproj` даже временно для новых задач. Не настраивай подключения к базе в API при появлении новой фичи.
- Чтение: если внешний HTTP GET требует данные, API использует согласованный `...Query` / `...Response` через MassTransit request/response с тайм-аутом, а запрос к PostgreSQL выполняется в Worker. Для долгих операций — JobId и асинхронный статус, также принадлежащий Worker. Не путай транспортную доступность с бизнес-успехом.
- Для операций с БД обеспечивай идемпотентность через уникальный ключ `JobId`, атомарную фиксацию изменения и статуса обработки, корректный `ack` только после успешной фиксации. Выходящие события при необходимости публикуй через outbox Worker.

**Факт о текущем коде:** Worker ещё **не** подключён к PostgreSQL и ещё не создаёт Weather в БД; это политика для будущей реализации, а не описание готового поведения. Подробнее — `docs/WORKER_DATABASE_BOUNDARY.md`.

## Что уже сделано

- `Worker.csproj` — самостоятельный исполняемый .NET 10 web host с `MassTransit.RabbitMQ` и ссылкой на `backend/Contracts/Contracts.csproj`.
- `Program.cs` вызывает `AddMassTransit`, `AddConsumer<ForecastCreatedConsumer>()`, `UsingRabbitMq` и `cfg.ConfigureEndpoints(context)`.
- `Features/Weather/ForecastCreatedConsumer.cs` получает `ForecastCreatedEvent` и **только логирует** его. Не называй это выполнением фоновой бизнес-задачи.
- RabbitMQ-настройки берутся из `RabbitMq` section; в Docker host — `rabbitmq`, локально — `localhost`.
- `health/live` и `health/ready` сейчас зарегистрированы, но не подтверждают работу брокера.

## Как добавлять настоящую фоновую работу

1. Определи задачу и её владельца, отдели `...Requested` (поручение) от `...Completed` (факт); контракт — в `backend/Contracts`, не в API.
2. Зарегистрируй `IConsumer<TRequested>` и его зависимости через DI. Для команды одному Worker используй согласованный **стабильный receive endpoint**; не полагайся на неописанные auto-queue названия.
3. `Consume` должен запускать **саму работу** через прикладной сервис Worker, проверять идемпотентность по `JobId`/идентификатору операции, передавать `context.CancellationToken`, фиксировать результат и лишь затем возвращаться успешно.
4. Не глоти исключения; настраивай обработку ошибок, ограниченные retry/redelivery, наблюдаемость `_error` очередей и логирование correlation IDs.
5. Не делай ссылку на `Api.csproj`, `KabanDbContext` API, HTTP controllers, request DTO, `IWeatherRepository` API. Для доступов к данным реализуй Worker-owned EF Core контекст/репозитории и DI в Worker (а не в API); проверь, что его контейнер имеет нужные переменные подключения.
6. Если Worker публикует результат, сначала обеспечь завершение/фиксацию эффекта; при необходимости используй outbox. Синхронизируй контракт с потенциальным получателем в API/Realtime.
7. Добавь тесты нормальной обработки, бизнес-валидации с БД, дубля, временной ошибки и перезапуска; актуализируй `docs/MESSAGING.md`.

**Не реализовано пока:** бизнес-обработка Weather, idempotency/inbox, outbox, custom retry policy, SignalR-публикация. Никогда не утверждай обратного на основании наличия `IConsumer`.
