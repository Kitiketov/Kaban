# API — правила для кодового агента

Действует на `backend/src/Api/**` вместе с корневым `AGENTS.md`. Для изменений очередей сначала прочитай `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`, `docs/MESSAGING.md` и `docs/WORKER_DATABASE_BOUNDARY.md`.

## Назначение и обязательный поток

**API = HTTP + MediatR + FluentValidation + MassTransit request/response. Бизнес-БД находится только в Worker.** Для коротких операций API **ожидает результат Worker** с конечным тайм-аутом, а затем возвращает его HTTP-клиенту, а не подтверждает одну лишь отправку сообщения.

```text
HTTP POST /api/tasks
 -> Controller / HTTP DTO
 -> IMediator.Send(CreateTaskCommand, ct)
 -> ValidationBehavior + AbstractValidator<CreateTaskCommand>
 -> Handler: IRequestClient<TaskCreateEvent>.GetResponse<TaskCreateSuccess>(..., ct)
 -> RabbitMQ -> Worker -> PostgreSQL -> RespondAsync
  -> API: response.Message -> response DTO -> HTTP-клиент
```

- `Presentation`: HTTP request/response DTO, маршруты, авторизация, mapping в MediatR. Controller не обращается к RabbitMQ, EF/SQL или Worker DB непосредственно.
- `Application/<Operation>`: `Command.cs`/`Query.cs`, `Handler.cs`, `Validator.cs`. Handler переводит проверенную команду в DTO из `backend/src/Contracts`, вызывает **`IRequestClient<T>`**, дожидается `GetResponse<TSuccess, TBusinessFailure>` и возвращает DTO без ожидания через fire-and-forget. Не добавляй EF Core, `DbContext`, DB repository, миграции, SQL и `SaveChangesAsync` в API.
- `Domain`: чистые правила/value objects без БД при необходимости; авторитетные инварианты с БД — в Worker. `Infrastructure`: message adapters/DI, только если нужны.
- Запрос на чтение (`GET`) тоже идёт через MassTransit request/response Worker → DTO. Не добавляй прямой read-repository API.

## Validation, authentication и обработка ошибок

- Используй существующий `Common/Validation/ValidationBehavior<TRequest,TResponse>`, `AbstractValidator<TCommand>` и `ValidateAsync`. Валидируй обязательность, длину, диапазоны, форматы и независимые от БД правила **до** MQ-вызова. Проверки существования, прав на ресурс, уникальности и конфликтов по БД — Worker.
- **Важно:** сейчас `AddApplication()` регистрирует MediatR и `ValidationBehavior`, **но не содержит** `AddValidatorsFromAssembly...`; валидатора `CreateTaskCommand` в текущем `main` нет. При реализации валидации добавь `Validator.cs` и явную регистрацию FluentValidation, проверь через DI/интеграционный тест. Не заявляй, что одна регистрация pipeline уже обеспечивает проверку полей.
- Авторизуй API через доверенные claims; при передаче actorId получай его из проверенного identity, не принимай без проверки `UserId` из body. Worker проверяет бизнес-права повторно.
- У `IRequestClient<T>` должно быть **настраиваемое конечное время ожидания** (`RequestTimeout`/конфигурация MassTransit) и `CancellationToken` HTTP-запроса. Не используй бесконечное ожидание, `.Result`, `.Wait()`, `Task.Run`, собственный in-memory `TaskCompletionSource` для опубликованных events.
- Успешный `RespondAsync<TSuccess>` после commit → API возвращает данные Worker клиенту. Типизированный отказ, fault и timeout обрабатывай раздельно по контракту HTTP endpoint; не считай отправку в очередь выполнением бизнес-операции.
- Тайм-аут **не отменяет гарантированно** уже запущенное задание Worker. Повтор клиента может создать дубликат. Для mutating request добавь стабильный idempotency key/OperationId, передавай через Contracts и требуй дедупликацию Worker. Не делай автоматические blind retries POST.
- Не раскрывай внутренние `RequestFaultException`/SQL/stack trace в HTTP. Имеющийся `RequestFaultExceptionHandler` сейчас переводит *любой* fault в 400; это **неправильный шаблон** для инфраструктурных ошибок. `RequestTimeoutExceptionHandler` сейчас возвращает 503; соблюдай единый выбранный status и ProblemDetails.

## Реальный пример Tasks, нюансы

- `Features/Tasks/CreateTask/Handler.cs` уже содержит `IRequestClient<TaskCreateEvent>.GetResponse<TaskCreateSuccess>`, а не `IPublishEndpoint.Publish`. `TaskCreateEvent` по факту команда-запрос, несмотря на имя.
- `TaskController.Create` сейчас использует `CreatedAtAction(nameof(Create))`, хотя корректного GET по ID нет. Это существующая деталь HTTP-слоя, а не шаблон схемы RabbitMQ. Не навязывай агенту конкретный успешный HTTP status; при отдельном изменении контроллера проверь корректность его маршрута и ответа.
- `CreateForecastHandler` как имя класса в папке `CreateTask` — существующее несоответствие; для нового кода используй точное `CreateTaskHandler` и не копируй ошибку.
- `CreateTaskCommand` пока не содержит реального FluentValidation-validator; `Api.csproj` имеет ProjectReference на `../Contracts/Contracts.csproj`. Смотри фактические namespaces (`Contracts.Features.Tasks`), не старую документацию о `backend/Contracts`/Weather.

## Схема файлов и команды

```text
Features/<Feature>/
  Presentation/<Feature>Controller.cs
  Presentation/<Operation>Request.cs  # или локальный DTO по существующему стилю
  Application/<Operation>/Command.cs (или Query.cs)
  Application/<Operation>/Handler.cs
  Application/<Operation>/Validator.cs
  Application/DTO/<Name>Dto.cs      # только если нужен
```

Если операция в текущей фиче размещена непосредственно в `Features/<Feature>/<Operation>`, соблюдай её локальный стиль и не делай бесполезную массовую перестройку папок. Namespace по реальному пути, file-scoped, четыре пробела, nullable, cancellation tokens. Не передавай EF-сущности в HTTP.

После изменений проверяй `dotnet build backend/Kaban.sln`, `dotnet test backend/Kaban.sln`; обязательно тестируй: невалидный input **не отправляет MQ**, успешный Worker response, business rejection, fault, timeout, late response и повтор с тем же OperationId. При изменении протокола актуализируй `docs/MESSAGING.md` и контракты.
