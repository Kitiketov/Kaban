---
name: kaban-rabbitmq-workflow
description: "Use for Kaban API-Worker MassTransit/RabbitMQ request-response, waiting for Worker DB completion with timeout, replies, public events, FluentValidation, idempotency and faults."
---

# Kaban: synchronous HTTP over asynchronous RabbitMQ — coding agent skill

## Когда использовать

Обязательно используй этот skill при задачах «API отправляет в Worker», «дождись результата в HTTP», «Worker публикует/возвращает результат», «timeout», «GetResponse», «RespondAsync», «MassTransit», «RabbitMQ», «PostgreSQL через Worker», «добавь consumer», «Realtime уведомления».

**Начальные чтения:** `AGENTS.md`, `backend/src/Api/AGENTS.md`, `backend/src/Worker/AGENTS.md`, `backend/src/Contracts/AGENTS.md`, `docs/MESSAGING.md`, `docs/ARCHITECTURE.md`, `docs/WORKER_DATABASE_BOUNDARY.md`, реальные `Program.cs` и contracts. Проверяй актуальность дерева: в нынешнем main живёт `backend/src/Contracts` и `Features/Tasks`, а **не** старый `backend/Contracts` и `Features/Weather`.

## Главное правило (по умолчанию)

Клиент ожидает завершения **короткой** операции Worker. API не отвечает подтверждением отправки в RabbitMQ вместо результата: он ждёт **ответ Worker с конечным timeout** и возвращает полученные данные клиенту. Конкретный успешный HTTP status этот skill не предписывает. API не работает с бизнес-БД. Worker выполняет DB-операцию и после commit отвечает.

```text
HTTP request
  -> API controller -> IMediator.Send(CreateTaskCommand)
  -> FluentValidation behavior + зарегистрированный Validator
  -> API handler: IRequestClient<TaskCreateEvent>.GetResponse<TaskCreateSuccess>(request, ct)
  -> RabbitMQ -> Worker IConsumer<TaskCreateEvent>
  -> DB business validation -> EF Core SaveChangesAsync
  -> Worker: ConsumeContext.RespondAsync<TaskCreateSuccess>(result)
  -> API: response.Message -> response DTO -> HTTP-клиент
```

**Техническая точность:** `RespondAsync` — **адресный ответ**, автоматически связываемый MassTransit с ожидающим `GetResponse` через request/response headers и reply endpoint. Он **не эквивалентен** `IPublishEndpoint.Publish`. Если пользователь говорит «Worker публикует результат, API ждёт», при существующем `IRequestClient` реализуй **RespondAsync для API** и при необходимости **дополнительный Publish** публичного `...Completed` event для Realtime/других подписчиков. Никогда не подменяй ответ только `Publish` и не городи ручной shared-memory waiters. Если отдельно требуется буквально ожидание *опубликованного* события, это другой протокол с durable correlation consumer, state-store, репликами API и очисткой timeout; не внедряй его молча.

## Шаги реализации

1. **Сверь базу**: `Api/Features/Tasks/CreateTask/Handler.cs`, `Worker/Features/Tasks/TaskCreateConsumer.cs`, `backend/src/Contracts/Features/Tasks/*`, DI в обоих `Program.cs`. Выдели текущие недочёты (см. ниже), не копируй их.
2. **Контракты**: один request/command (`...Requested` предпочтительнее `...Event` для новых операций), один success (`...Success`/`...Result`) и типизированные business failures (`...NotFound`, `...Conflict`, `...Forbidden`). Храни в `backend/src/Contracts/<Feature>`; одинаковый namespace и тип на обеих сторонах. В мутациях предусмотри `OperationId`/idempotency key для безопасного повторения после timeout.
3. **API валидация**: `Presentation` HTTP DTO -> MediatR `Command`/`Query`; `AbstractValidator<TCommand>` + регистрация `AddValidatorsFromAssembly...` (в текущем `AddApplication()` её нет). Входная валидность до вызова MQ; нельзя читать PostgreSQL из API или validator. Authentication/authorization actor из verified claims.
4. **API request/response**: внедри `IRequestClient<TRequest>` в MediatR handler; вызови `await client.GetResponse<TSuccess, TBusinessFailure>(request, ct)` с **конечным настраиваемым timeout** (MassTransit `RequestTimeout` при конфигурации клиента/вызова) и request cancellation. Не считай один только `CancellationToken` явной настройкой timeout; не создавай `Task.Run`/`TaskCompletionSource`/static cache для ожидания события. Направление request — в единственный Worker endpoint или default MassTransit request publish с одним ответственным consumer; документируй выбранную топологию.
5. **Worker**: consumer получает request, проверяет domain/DB правила, применяет идемпотентную операцию в Worker-owned PostgreSQL; после успешного commit `await context.RespondAsync<TSuccess>(...)`. При типизированном отказе (без DB commit) — `RespondAsync<TBusinessFailure>`. При системном сбое исключение не глоти: MassTransit направит `Fault<TRequest>`, request client выбросит `RequestFaultException`.
6. **Дополнительное событие** (если реально нужно): Worker после успешного commit может `Publish<TCompleted>` для Realtime/аудита. Не используй `Publish` вместо `RespondAsync`. Для надёжной DB+event публикации — Worker outbox; иначе отдельно описывай возможный сбой между commit и publish. Успешный ответ API должен отражать сохранённое состояние БД.
7. **Timeout и повтор**: сконфигурируй конечный бюджет ожидания (пример dev: 30 секунд — **пример, не существующая настройка**). При timeout API возвращает документированную ошибку без ложного результата и без раскрытия exception; RabbitMQ/Worker могут продолжить обработку. Повтор с тем же idempotency key должен вернуть тот же результат, не создавать вторую запись. Не обещай exactly-once и что отмена HTTP автоматически отменяет консюмер.
8. **Ответ HTTP-клиенту**: возвращай реальные DTO из `response.Message` **после** ответа Worker; не предписывай фиксированный успешный HTTP status для новых операций только из-за использования RabbitMQ. Для ошибок FluentValidation, типизированных бизнес-отказов, fault и timeout применяй согласованное отображение ошибок endpoint. Не делай глобальное `RequestFaultException => BadRequest`. Если контроллер формирует `Location`, он должен вести на существующий маршрут; не копируй слепо текущий `CreatedAtAction(nameof(Create))`.
9. **Проверка**: `dotnet build backend/Kaban.sln`, `dotnet test backend/Kaban.sln`, интеграционный тест с настоящими RabbitMQ/PostgreSQL: (a) invalid input не вызывает MQ; (b) API ждёт Worker commit и отвечает реальным результатом; (c) business failure; (d) fault/нет Worker; (e) timeout + поздний успех + повтор; (f) дубли в Worker; (g) event получен подписчиком, если он реализован. При отсутствии среды не утверждай, что проверка выполнена.
10. **Документация**: после изменения кода обнови `docs/MESSAGING.md`, `docs/ARCHITECTURE.md`, сервисные `AGENTS.md` и README по необходимости.

## Проверяемый текущий пример (не буквальный образец для копирования)

```csharp
// API — backend/src/Api/Features/Tasks/CreateTask/Handler.cs
var response = await requestClient.GetResponse<TaskCreateSuccess>(taskEvent, ct);
return new TaskCreateResponseDto(response.Message.TaskId, /* ... */);

// Worker — backend/src/Worker/Features/Tasks/TaskCreateConsumer.cs
await dbContext.AddAsync(entityTask, context.CancellationToken);
await dbContext.SaveChangesAsync(context.CancellationToken);
await context.RespondAsync<TaskCreateSuccess>(new { TaskId = entityTask.Id });
```

Код выше иллюстрирует **правильный порядок**, но в текущем main есть неполные проверки и ошибки wiring: (1) `using Contracts.Tasks` consumer vs `Contracts.Features.Tasks` contract, (2) `Worker.csproj` использует `HintPath` к `Api/bin/Debug/.../Contracts.dll` вместо `ProjectReference`, (3) нет зарегистрированных FluentValidation validators CreateTask, (4) `CreatedAtAction(nameof(Create))` не формирует корректный Location на GET resource, (5) Compose Worker не получает connection string с `Host=postgres`, (6) нет явного timeout и идемпотентности. Не заявляй, что они исправлены одной документацией.

## Definition of Done

- [ ] Из API валидация через **реально зарегистрированный** FluentValidation-validator, без DB и без отправки при ошибке.
- [ ] API `GetResponse` ждёт типизированный **`RespondAsync`** Worker с конечным timeout.
- [ ] Worker проверил business rules/идемпотентность и успешно сохранил в PostgreSQL **до** response.
- [ ] API возвращает клиенту DTO Worker **после получения ответа**; никакой фиксированный успешный HTTP status не навязан, fault/business error/timeout классифицированы.
- [ ] Поздний результат и повтор после timeout безопасны, отмена HTTP не представлена как DB rollback.
- [ ] `Publish` используется отдельно от ответа, если нужен Realtime/другие подписчики.
- [ ] Проверены реальные DI/контракты/build и тесты либо явно зафиксированы причины, почему не проверены.
