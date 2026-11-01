# Worker — инструкции агенту

Действует на `backend/src/Worker/**` вместе с корневым `AGENTS.md`. **Обязательно** прочитай `docs/MESSAGING.md` и `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`, если задача касается API → Worker, RabbitMQ, фоновых заданий, событий или новых consumers.

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
5. Не делай ссылку на `Api.csproj`, `KabanDbContext` API, HTTP controllers, request DTO, `IWeatherRepository` API. Для доступов к данным используй владеющий сервис и собственные адаптеры Worker.
6. Если Worker публикует результат, сначала обеспечь завершение/фиксацию эффекта; при необходимости используй outbox. Синхронизируй контракт с потенциальным получателем в API/Realtime.
7. Добавь тесты нормальной обработки, дубля, временной ошибки и перезапуска; актуализируй `docs/MESSAGING.md`.

**Не реализовано пока:** бизнес-обработка Weather, idempotency/inbox, outbox, custom retry policy, SignalR-публикация. Никогда не утверждай обратного на основании наличия `IConsumer`.
