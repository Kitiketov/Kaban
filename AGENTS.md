# Kaban — инструкции для кодовых агентов

> Проверено по `Kitiketov/Kaban`, `main` на commit `f6b110e9f2d59efc6251060a9ab9680202d24722` (2026-11-01). Сверяй с кодом при последующих изменениях: документация не заменяет проверку исходников.

## Область действия и чтение

Этот файл действует на весь монорепозиторий; вложенные `AGENTS.md` уточняют правила, прямые требования пользователя имеют приоритет. Перед изменениями читай `docs/ARCHITECTURE.md`, `docs/MESSAGING.md`, `docs/WORKER_DATABASE_BOUNDARY.md`, соответствующие вложенные `AGENTS.md` и, для RabbitMQ, **`.agents/skills/kaban-rabbitmq-workflow/SKILL.md`**. Читай также `docs/FILE_NAMING_AND_DTO.md`, если меняешь структуру фич.

Технологии: .NET 10, ASP.NET Core, MediatR, FluentValidation, MassTransit/RabbitMQ, PostgreSQL, EF Core/Npgsql, Docker Compose. Версии проверяй в `.csproj`; не обновляй без отдельной необходимости.

## ОБЯЗАТЕЛЬНЫЙ поток HTTP → Worker → HTTP

**Главное правило: HTTP-запрос остаётся ожидающим, пока Worker выполняет операцию через RabbitMQ. API ждёт адресный результат Worker с конечным тайм-аутом и возвращает полученные данные вызывающему клиенту. Не подменяй результат подтверждением постановки задания в очередь. Не предписывай фиксированный HTTP success status: это отдельная деталь контракта endpoint, а не задача агента по messaging.**

```text
Клиент HTTP POST/GET
  -> API Controller -> IMediator.Send(Command/Query)
  -> API FluentValidation pipeline (формат и независимые от БД правила)
  -> API IRequestClient<TRequest>.GetResponse<TSuccess[, TBusinessFailure]>(..., ct)
  -> RabbitMQ -> Worker IConsumer<TRequest>
  -> Worker проверяет бизнес-правила, читает/пишет PostgreSQL через EF Core
  -> успешный commit -> ConsumeContext.RespondAsync<TSuccess>(...)
  -> API получает response.Message, преобразует в DTO и возвращает результат клиенту
```

- **API** владеет HTTP, authentication/authorization по проверенным claims, MediatR, FluentValidation входа, MassTransit request client и HTTP-ответами; для новых фич **без DbContext, EF, Repository/UnitOfWork/SQL и подключения к PostgreSQL**.
- **Worker** владеет прикладной бизнес-обработкой, проверками по реальным данным, EF Core/DbContext, PostgreSQL, репозиториями, миграциями, идемпотентностью и ответом на сообщение. Worker не зависит от `Api.csproj`.
- **Контракт** для обеих сторон живёт в `backend/src/Contracts` (а НЕ в старом `backend/Contracts`); не передавай EF-сущности, JWT, пароли, `HttpContext`.
- **Результат для ожидающего API** — `RespondAsync` внутри Worker и `GetResponse` внутри API. **Публичное событие** о завершённом действии для Realtime/других подписчиков можно **дополнительно** разослать через `IPublishEndpoint.Publish<TCompleted>`, но публикация сама по себе **не завершает** `IRequestClient.GetResponse`. Не пытайся ждать произвольный `Publish` через статический `TaskCompletionSource` или подписку отдельного HTTP-запроса.
- **Тайм-аут обязателен:** настрой явно конечный и разумный бюджет ожидания (общая конфигурация, транспорт + HTTP); передавай `CancellationToken`. Тайм-аут означает, что **API перестал ждать, но Worker мог продолжить работу и успешно записать данные**. Для повторов клиентских запросов применяй стабильный `OperationId`/idempotency key и дедупликацию в Worker; не запускай повторную запись вслепую.
- **Успешный ответ клиенту** формируется только из подтверждённого Worker результата после commit. Не заменяй ожидание Worker ответом о том, что запрос поставлен в очередь, и не назначай всем операциям одинаковый HTTP status code.
- Бизнес-отказы Worker передавай типизированным ответом (not found, invalid state, conflict, forbidden), технические faults и timeouts обрабатывай отдельно. Ошибку очереди/БД не выдавай за успешное завершение.
- Message delivery по умолчанию не exactly-once. Для записи: идемпотентность, ограниченный retry, проверка `_error` queue, при необходимости outbox/inbox. При долгих задачах предпочитай отдельный асинхронный workflow вместо длительного открытого HTTP-запроса.

## Текущее состояние (НЕ путать с идеалом)

- **Текущая фича Tasks** уже использует `Api/Features/Tasks/CreateTask/Handler.cs` -> `IRequestClient<TaskCreateEvent>.GetResponse<TaskCreateSuccess>` -> `Worker/Features/Tasks/TaskCreateConsumer.cs` -> `KabanDatabaseContext.SaveChangesAsync()` -> `context.RespondAsync<TaskCreateSuccess>`. Это **request/response**, хотя тип запроса пока называется `TaskCreateEvent`.
- В API `Program.cs` зарегистрирован `AddRequestClient<TaskCreateEvent>()`; в Worker зарегистрирован consumer через `AddConsumer` и `ConfigureEndpoints`.
- **FluentValidation pipeline имеется, но для текущего `CreateTaskCommand` нет валидатора и в текущем `AddApplication()` не зарегистрировано сканирование validators.** Не заявляй, что создание Tasks уже проходит реальную FluentValidation-проверку; при добавлении валидатора зарегистрируй его.
- Существуют технические несогласованности: в Worker consumer указан `using Contracts.Tasks`, но контракт объявлен в `Contracts.Features.Tasks`; `Worker.csproj` использует ссылку на Debug-DLL вместо `ProjectReference`; `docker-compose.yml` не задаёт `ConnectionStrings__DefaultConnection` для Worker (его appsettings использует localhost); `CreatedAtAction(nameof(Create))` у API не указывает на корректный GET-by-id endpoint. Эти проблемы исправляй в отдельной задаче с проверкой сборки, **не копируй их как образец**.
- `Realtime` пока содержит только health endpoints, без SignalR/MQ и JWT; не изображай его работающим уведомляющим сервисом.

## Структура и правила

- `backend/src/Api` — HTTP/MediatR/FluentValidation/request client; правила в `backend/src/Api/AGENTS.md`.
- `backend/src/Worker` — consumer/бизнес-обработка/единственный владелец бизнес-БД; правила в `backend/src/Worker/AGENTS.md`.
- `backend/src/Contracts` — общие транспортные DTO и типизированные результаты; правила в `backend/src/Contracts/AGENTS.md`.
- `backend/src/Realtime` — отдельный host для будущего SignalR; `backend/src/Realtime/AGENTS.md`.
- `backend/tests` — тесты; `backend/tests/AGENTS.md`.
- `frontend` — отдельный статический клиент; `deploy/` и `docker-compose.yml` — инфраструктура.

Сохраняй feature-first, короткие имена файлов внутри папки операции (`Command.cs`, `Handler.cs`, `Validator.cs`) и полные имена типов; разделяй HTTP DTO, MediatR command/query и MQ-контракт. Не создавай лишние сервисы, глобальные слои, ненужные абстракции, ссылки Worker на API или собственный DbContext в API.

## Проверки и безопасность

Из корня: `dotnet restore backend/Kaban.sln`, `dotnet build backend/Kaban.sln`, `dotnet test backend/Kaban.sln`, `docker compose config --quiet` (последнее при изменении Compose). Для messaging обязателен интеграционный прогон API + RabbitMQ + Worker + PostgreSQL, включая timeout/дубль/fault. Если сборка не проходит из-за существующих ошибок — сообщи точно, не называй проверки успешными.

Не коммить секреты и реальные connection strings; не доверяй `UserId` из тела запроса; не раскрывай исключения Worker напрямую клиенту. Не используй `docker compose down -v`, не удаляй существующие данные и не запускай production-миграции. Не изменяй C#-код/контракты/HTTP-семантику только ради правки Markdown без отдельного поручения.
