# Правила разработки API — Kaban

Действует для `backend/src/Api/**` вместе с корневым `AGENTS.md`. Перед задачами API ↔ Worker прочитай `docs/MESSAGING.md`, `docs/WORKER_DATABASE_BOUNDARY.md` и `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`.

## Граница сервиса (ОБЯЗАТЕЛЬНО для новых фич)

**API = HTTP + MediatR + FluentValidation + MassTransit; Worker = бизнес-обработка + PostgreSQL.**

```text
HTTP POST → Controller → IMediator.Send(Command)
  → ValidationBehavior / AbstractValidator<Command>
  → Application Handler → MassTransit Send(...Requested)
  → RabbitMQ → Worker Consumer → Worker DB
HTTP GET → Controller → IMediator.Send(Query)
  → Request/Response через MassTransit → Worker DB → DTO
```

- В API **НЕ** добавляй новые `DbContext`, EF Core repositories, SQL, миграции, `IUnitOfWork` или зависимость от PostgreSQL для новых операций. Не регистрируй новую persistence-модель в `Api/Infrastructure/Persistence`.
- В `Presentation` — только HTTP DTO, маршруты, проверенный auth context и преобразования; никаких бизнес-операций, RabbitMQ-вызовов из controller, SQL или `SaveChanges`.
- В `Application` — MediatR commands/queries/handlers/validators/output DTO. Handler работает с абстракцией отправки или MassTransit, но не с БД. Не импортируй MVC/HttpContext, SQL, EF Core, `DbContext` и конкретную persistence-infrastructure в новые handlers.
- В `Domain` API храни только независимые от БД правила/value objects, если действительно нужны. Сохраняемые сущности и DB-инварианты новых фич принадлежат Worker.
- В feature-local `Infrastructure` API — адаптеры RabbitMQ и регистрация DI, если нужны. Репозитории и EF configurations новых фич размещай в Worker.
- Идентификатор автора/пользователя получай только из проверенного контекста аутентификации; не доверяй `UserId` из тела. Worker независимо проверяет реальные права и состояние БД.

## Модули и именование

Пример новой операции:

```text
Features/<Feature>/
  Presentation/<Feature>Controller.cs
  Presentation/<Operation>Request.cs
  Application/<Operation>/Command.cs
  Application/<Operation>/Validator.cs
  Application/<Operation>/Handler.cs
  Application/DTO/<Operation>Response.cs    # при необходимости
  Application/Abstractions/I<Name>Dispatcher.cs # только если нужна абстракция
  Infrastructure/<Feature>MessagingRegistration.cs # при необходимости
```

- Сохраняй краткие имена файлов внутри папки операции (`Command.cs`, `Handler.cs`, `Validator.cs`, `Query.cs`), но полные имена классов (`CreateTaskCommand`, `CreateTaskHandler`). Придерживайся namespace по пути `Kaban.Api.Features.<Feature>.Application.<Operation>`, file-scoped namespace, nullable, четыре пробела.
- `Presentation/*Request` — входящий HTTP DTO; `Application/*Command` или `*Query` — MediatR use case; `backend/Contracts/*Requested` — транспортное сообщение. Это **разные** типы и границы. Не отдавай EF/domain entity в HTTP или RabbitMQ.
- Records подходят для неизменяемых сообщений/DTO, `sealed` — для типов без наследования. Не создавай бесполезный интерфейс для каждого класса.

## FluentValidation и MediatR

- Контроллер вызывает `IMediator.Send(command, ct)`. Валидация выполняется **до** отправки сообщения через существующий `Common/Validation/ValidationBehavior` и `AbstractValidator<TCommand>` рядом с операцией; не дублируй ручной вызов валидатора в controller.
- В API валидируй форму, обязательность, длины, диапазоны, форматы и независимые от БД правила. **Не делай DB-запросы из `FluentValidation`**, включая `MustAsync`, для проверок существования, уникальности, владения, текущего статуса. Эти авторитетные проверки и ограничения БД выполняет Worker.
- Не ослабляй валидацию Worker лишь потому, что API уже проверил JSON: сообщение может быть доставлено повторно или отправлено другим внутренним producer.
- `AddApplication()` сканирует сборку API для handlers/validators. Пока сканирование validators привязано к `CreateForecastValidator`; если удалишь legacy Weather, поменяй marker на сохраняемый тип сборки.
- Передавай `CancellationToken` во все асинхронные операции; не используй `.Result`, `.Wait()`, `Task.Run` или fire-and-forget для гарантированной обработки.

## Постановка задания и результат

- Новый command handler создаёт `JobId`, строит DTO `...Requested` из `backend/Contracts` и `await`-ом отправляет его в RabbitMQ через MassTransit. Для задания одному Worker используй адресное `Send` на **согласованный стабильный endpoint**, зарегистрированный Worker. Не считай старый `Publish(ForecastCreatedEvent)` командой.
- Для долгой работы верни HTTP `202 Accepted` + `JobId`, по возможности — URL ресурса статуса, **только если он действительно реализован**. Это означает отправку задания, а не DB commit Worker. При ошибке MQ не отвечай `202`. Для подтверждения долговечного приёма задания проектируй короткий ack через Worker request/response, если это требуется.
- Для GET чтение из PostgreSQL делает только Worker: `IRequestClient<TQuery>`/`GetResponse<TResponse>` с конечным тайм-аутом. API переводит недоступность Worker/MQ в корректный HTTP ответ, не скрывает её как 404/200. Не передавай `IQueryable` и Entity.
- В новых фичах API **не** создаёт Job/status/outbox в собственной БД; если нужен долговечный статус — хранит Worker. Согласованность записи Worker и событий решай на стороне Worker (outbox/идемпотентность).
- На входящие проверки: невалидный HTTP → 400 `ValidationProblemDetails` через существующий `ValidationExceptionHandler`, без сообщения в MQ; валидный запрос → задание/`JobId`; завершение/ошибка Worker → отдельный результат/статус.

## Legacy Weather: не копировать

Существующий `Features/Weather/Application/CreateForecast/Handler.cs` сейчас вызывает `IWeatherRepository.AddAsync`, `IUnitOfWork.SaveChangesAsync` на API `KabanDbContext`, и затем `IPublishEndpoint.Publish(ForecastCreatedEvent)`. `Infrastructure/Persistence` и `WeatherRepository` нужны **только старой учебной фиче**; `EnsureCreatedAsync()` вызывается лишь в Development. Это сохранённый пример старой реализации, **не шаблон для новых функций**. Удаление API persistence, перенос Weather на Worker, изменение GET и миграции PostgreSQL выполняй отдельной задачей, не ломая базу и клиентов. `backend/Database` пока не интегрирован в solution.

## HTTP, безопасность и ошибки

- Сохраняй существующие HTTP-маршруты/контракты при рефакторинге, если пользователь не запросил изменение. Legacy синхронный Weather возвращает 201 + Location; **новый асинхронный POST** — 202 + JobId. Отсутствие ресурса — 404, конфликт состояния — 409, ошибка валидации — 400. Не маскируй сбой DB/MQ или timeout под успешный ответ.
- Авторизации в текущем учебном API нет. При реализации User/Task добавляй JWT и проверку пользователя/прав; не передавай refresh JWT как access. Не раскрывай stack trace, SQL или секреты в HTTP или логах.
- Не отправляй RabbitMQ-сообщения прямо из controller или доменной сущности. В проекте нет гарантий exactly-once; учитывай повторы и события о результате.

## Проверки из корня

```powershell
dotnet build backend/Kaban.sln
dotnet test backend/tests/Tests/Tests.csproj
```

- Для legacy Weather доступен фильтр `--filter FullyQualifiedName~CreateForecastHandlerTests`. Новые обработчики проверяй на то, что невалидная команда не отправляет сообщение, валидная отправляет ожидаемый контракт, а GET корректно обрабатывает timeout и ответ Worker.
- Интеграционные тесты нужны для DI/validation pipeline/RabbitMQ/Worker/PostgreSQL; прямые unit-тесты handlers и in-memory БД не доказывают реальную доставку/ограничения. При изменении контрактов обновляй `docs/MESSAGING.md`, `AGENTS.md` и соответствующий skill.
