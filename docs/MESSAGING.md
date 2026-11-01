# Kaban: взаимодействие сервисов через RabbitMQ

> Проверено по `Kitiketov/Kaban`, `main`, tree SHA `3f128e84877ad96769594c329efcbab265ffe8f2` (2026-11-01). **Текущее Weather-поведение и целевая архитектура различаются**. Ниже это отражено явно.

## Целевая граница: БД принадлежит Worker

Для **новых фич** обязательная схема:

```text
HTTP client
    │ request JSON
    ▼
API: Controller → MediatR Command → FluentValidation
    │ (формат/поля/диапазоны; авторизация без прямого чтения БД)
    │ Send(...Requested, JobId) через MassTransit
    ▼
RabbitMQ: worker-owned durable queue / receive endpoint
    ▼
Worker: IConsumer<TRequested>
    │ проверка дубликата, бизнес-правил и прав по данным БД
    │ Worker Application/Domain → Worker Repository/DbContext → PostgreSQL
    │ commit результата и статуса JobId
    │ optional Publish(...Completed / ...Failed)
    ▼
Realtime (после реализации): SignalR уведомление адресату
```

**Ни одно новое создание/изменение/чтение бизнес-данных не должно происходить из API через EF Core или SQL.** API отвечает за HTTP, проверку входных данных через `FluentValidation` в MediatR `ValidationBehavior`, авторизацию на основании доверенных claims и отправку сообщения. Worker владеет EF Core, PostgreSQL, миграциями, репозиториями, Unit of Work, проверками бизнес-инвариантов и идемпотентностью. Проверки, требующие БД, **нельзя** подменять только формальной FluentValidation в API.

Для чтения HTTP GET: при единственном владельце БД API отправляет `...Query` в Worker и ожидает `...Response` через MassTransit request/response с конечным timeout, либо команда явно согласует асинхронную модель статуса/результата. Не вводи API read-repository без изменения архитектурного решения.

**Статус:** это целевая архитектура, **она ещё не реализована в текущем Weather**. Порядок безопасного переноса описан в `docs/WORKER_DATABASE_BOUNDARY.md`.

## Состав solution и владельцы ответственности

| Компонент | Проект | Реальная роль на указанном commit |
| --- | --- | --- |
| API | `backend/src/Api/Api.csproj` | Сейчас Weather использует EF Core + публикует событие; **цель для новых фич:** HTTP/MediatR/FluentValidation и отправка заданий без БД |
| Worker | `backend/src/Worker/Worker.csproj` | Пока MassTransit consumer только логирует Weather; **цель:** выполняет задания и владеет PostgreSQL |
| Realtime | `backend/src/Realtime/Realtime.csproj` | Отдельный host с health endpoints; SignalR и RabbitMQ обработчиков **пока нет** |
| Contracts | `backend/Contracts/Contracts.csproj` | Общие контракты сообщений, **библиотека, не четвёртый микросервис** |
| Database | `backend/Database/Database.csproj` | Экспериментальная модель, пока не подключена к Worker и не в solution; для целевой архитектуры её можно адаптировать как Worker-only библиотеку |
| Frontend | `frontend/` | Статический клиент; Nginx проксирует `/api/` в API; маршрута `/hubs/` пока нет |

Три микросервиса — **три независимо запускаемых процесса**. Количество проектов в solution больше трёх, это нормально. Worker **не** должен ссылаться на сборку API или импортировать `Kaban.Api.Infrastructure.Persistence.KabanDbContext`. Общая библиотека Contracts содержит только транспортные сообщения; всё остальное принадлежит конкретному сервису.

## Что работает в коде сейчас: событие о созданном прогнозе

```text
POST /api/weather
    |
    v
WeatherController
    | IMediator.Send(CreateForecastCommand)
    v
CreateForecastHandler
    | IWeatherRepository.AddAsync
    | IUnitOfWork.SaveChangesAsync -> PostgreSQL
    | IPublishEndpoint.Publish(ForecastCreatedEvent)
    v
RabbitMQ (MassTransit publish exchange по типу сообщения)
    |
    v
Worker: ForecastCreatedConsumer : IConsumer<ForecastCreatedEvent>
    | Consume(...): запись в ILogger, затем успешное завершение
    v
[настоящей фоновой работы/сохранения результата пока нет]

Realtime: [не подключён к этому потоку]
```

**Конкретные файлы:**
- API DI/шина: `backend/src/Api/Program.cs` — `AddMassTransit`, `UsingRabbitMq`.
- Создание: `backend/src/Api/Features/Weather/Presentation/WeatherController.cs` → `Application/CreateForecast/Handler.cs`.
- Контракт: `backend/Contracts/Weather/ForecastCreatedEvent.cs`: `Date`, `TemperatureC`, `Summary` (нет `ForecastId`/`JobId`).
- Worker DI/шина: `backend/src/Worker/Program.cs` — `AddConsumer<ForecastCreatedConsumer>()`, `cfg.ConfigureEndpoints(context)`.
- Consumer: `backend/src/Worker/Features/Weather/ForecastCreatedConsumer.cs` — **только логирование**.
- Realtime: `backend/src/Realtime/Program.cs` — только health endpoints.

**Важно про RabbitMQ «топики»:** текущая интеграция не задаёт `ExchangeType.Topic` и routing keys вручную. MassTransit публикует в exchange по **типу сообщения**, а `ConfigureEndpoints(context)` поднимает receive endpoint и необходимые привязки для consumer. В RabbitMQ MassTransit по умолчанию использует **fanout exchange**, а не `topic` exchange. Не придумывай фиксированные названия очереди или topic/routing key: в коде их нет. При необходимости явных имён/маршрутизации конфигурируй и документируй их отдельным изменением. Наличие producer без существующей consumer binding не гарантирует, что событие будет сохранено до запуска Worker.

### Ограничения текущей реализации

1. API **сам выполняет** создание прогноза и сохранение в PostgreSQL. Worker не создаёт прогноз — он **узнаёт о совершившемся событии**. Это event notification, а не фоновая команда. **Не продолжай этот паттерн для новых API-фич.**
2. API вызывает `SaveChangesAsync()` и **только потом** `Publish()`. Между ними нет атомарности: сохранение может пройти, публикация — упасть. **Transactional outbox пока не настроен.**
3. В примере нет бизнес-идемпотентности, явного retry/redelivery policy, fault handling / мониторинга ошибок и persist результата Worker. Не заявляй exactly-once.
4. В Realtime нет SignalR Hub, Kafka/RabbitMQ consumer, JWT-конфигурации или отправки клиентам.
5. `/health/live` и `/health/ready` сейчас **не подтверждают** наличие активного RabbitMQ consumer или успешного подключения к БД.
6. В `docker-compose.yml` credentials заданы для локальной разработки. В production — только переменные окружения/секреты. Подключение с хоста: RabbitMQ `localhost:5672`, с контейнера: `rabbitmq:5672`. PostgreSQL с хоста `localhost:55432`, из сети compose `postgres:5432`.

## Как реализовывать новые фоновые задачи: API даёт задание, Worker выполняет

**Это проектное правило для следующих фич, не реализованный в репозитории код.** Сначала определи «что является работой» и кто владеет её результатом. Нельзя автоматически считать `ForecastCreatedEvent` подходящим командным контрактом.

### Рекомендуемый сценарий

```text
Client → POST /api/.../jobs
  → Api/Presentation: HTTP DTO
  → Api/Application: MediatR Command + FluentValidation
  → MassTransit Send(...Requested, JobId) → RabbitMQ
  → Worker Consumer: состояние БД, авторитетные бизнес-правила,
      идемпотентность, запись данных/статуса в Worker-owned PostgreSQL
  → optional Publish(...Completed/...Failed) → Realtime
Client ← 202 Accepted, JobId (после успешной отправки)
```

**API не создаёт записи Job/outbox в PostgreSQL.** Существующее `SaveChangesAsync` в API — только старый Weather-пример, не образец для новых задач. `202` подтверждает приём/отправку задания на выбранной границе, но не то, что Worker уже завершил работу или записал статус; определите отдельно semantics durably accepted, наличие записи и endpoint статуса.

Если требуется гарантировать, что `JobId` уже сохранён в БД прежде чем вернуть 202, используйте короткое подтверждение от Worker после создания записи Job, например request/response, с тайм-аутом и явной обработкой недоступности Worker. Это **не** ожидание всей фоновой обработки. Если передача сообщения не удалась, возвращайте подходящую ошибку (например 503), а не фиктивный успех.

### Command vs event; Send vs Publish

- `...Requested` / `Process...` — **задание с одним владельцем**. Если один конкретный Worker отвечает за работу, предпочитай адресное `Send` к **явно названному worker receive endpoint**. Это нужно отдельно настроить в Worker и записать в документацию. Допустима схема `Publish` при одном подписчике, но при добавлении ещё одного подписчика событие обработается каждым подписчиком.
- `...Created`, `...Completed`, `...Failed` — **факты**, уже произошедшие в системе. Для них обычно подходит `IPublishEndpoint.Publish`, который позволяет нескольким заинтересованным потребителям получить копию события.
- `ForecastCreatedEvent` сейчас относится к **второму** типу: задача сохранения выполнена API, Worker наблюдает результат.
- Не превращай `ForecastCreatedEvent` в команду только за счёт изменения названия consumer. Согласуй новый контракт по семантике сценария.

### Требования к контракту нового задания

- Название: `GenerateReportRequested`, `ProcessTaskRequested` и т.п. **по настоящей предметной операции**.
- Содержимое: стабильный `JobId` (`Guid`) для учёта работы; необходимые идентификаторы и аргументы; время события/версия при необходимости. Используй `ConsumeContext.MessageId`/`CorrelationId` как transport metadata, а `JobId` — как бизнес-ключ повторной обработки.
- Не сериализуй EF entity, `DbContext`, HTTP request DTO, токены, пароли и секреты.
- Статус задания и прикладные таблицы принадлежат Worker. API не ведёт свою таблицу Job и не читает таблицы Worker напрямую; запрос состояния направляется Worker через контракт.
- Контракты версионируй совместимо: при изменении полей учитывай старых consumers; существенные breaking changes согласовывай и тестируй обе стороны.

### Надёжность

- В новой схеме API не делает DB commit. При сохранении результатов и публикации `...Completed`/`...Failed` **в Worker** используй transactional outbox (при требованиях к надёжности), чтобы результат и событие не расходились. Текущий `SaveChanges` затем `Publish` в API — несогласованный legacy Weather, не копировать.
- Worker, владелец БД, должен быть **идемпотентен**: RabbitMQ/MassTransit могут доставить сообщение повторно. Проверяй `JobId` и атомарно фиксируй состояние выполненной работы.
- Исключения consumer не глоти: вызывай их наружу, чтобы MassTransit мог обработать ошибку. Разделяй временные и постоянные сбои; настраивай ограниченный retry/redelivery и анализируй `_error` очереди. Не выполняй бесконечные повторения.
- Логируй `JobId`/correlation, тип сообщения, длительность и результат; **не** логируй секреты и чувствительные данные. Оповещение клиента отправляй только после подтверждения успешного результата.
- Прежде чем говорить «работает», проверь: начало/конец, отрицательные сценарии, повторную доставку, отсутствие Worker, отказ RabbitMQ, корректность DI и временных ограничений. Отдельно покрой Outbox/Consumer integration tests.

## Куда вносить изменения: чек-лист агента

| Задача | Изменяемая область |
| --- | --- |
| Новый HTTP endpoint | `Api/Features/<Feature>/Presentation` |
| Прикладная операция | `Api/Features/<Feature>/Application/<Operation>`: `Command.cs`/`Query.cs`, `Handler.cs`, `Validator.cs` |
| Схема сообщения | `backend/Contracts/<Feature>` и тест совместимости контракта |
| API → MQ | Прикладной handler/адаптер из API (не контроллер и не доменная сущность) + API DI в `Program.cs`/регистрации |
| Worker принимает задание | `Worker/Features/<Feature>/*Consumer.cs` + `Worker/Program.cs` (AddConsumer/ReceiveEndpoint/ConfigureEndpoints) |
| Worker делает работу и работает с БД | Worker Application/Domain + Worker-owned EF Core DbContext/Repository/UoW и миграции, без ссылки на `Api.csproj` |
| HTTP GET получает данные | API request client → `...Query` → Worker EF query → `...Response`, конечный timeout |
| Результат → клиент | Новый message contract, Реaltime consumer + авторизованный SignalR hub; пока **не реализовано** |
| Изменяется топология/сценарий | Обновить `docs/MESSAGING.md` и относящиеся `AGENTS.md` |

## Как проверить нынешний пример

Из корня репозитория с работающим Docker Desktop:

```powershell
docker compose up -d --build postgres rabbitmq worker api
docker compose ps
# затем POST /api/weather, например через Swagger на http://localhost:5080/swagger
docker compose logs -f worker
```

Ожидаемое поведение: API сохраняет прогноз и публикует `ForecastCreatedEvent`; Worker пишет лог о получении события. **Не ожидай** от него создания дополнительной сущности или обновления клиента через SignalR. Запуск API в профиле `Production` может не создать учебную таблицу Weather (`EnsureCreated` привязан к Development). 

Проверка логов RabbitMQ Management: `http://localhost:15672` с dev-учётной записью из compose/env. Смотри реальные queues/exchanges/bindings, а не предполагаемые названия. При нарушении конфигурации проверяй `RabbitMq:Host/Username/Password` и сетевой адрес (локальный хост vs Docker). Не используй `docker compose down -v`: это удалит данные PostgreSQL и RabbitMQ.

## Источники в репозитории

- `backend/src/Api/Features/Weather/Application/CreateForecast/Handler.cs`
- `backend/src/Api/Program.cs`
- `backend/src/Worker/Program.cs`
- `backend/src/Worker/Features/Weather/ForecastCreatedConsumer.cs`
- `backend/Contracts/Weather/ForecastCreatedEvent.cs`
- `backend/src/Realtime/Program.cs`
- `backend/Kaban.sln`, `docker-compose.yml`

Поведение exchange/receive endpoint по умолчанию: официальная документация MassTransit по RabbitMQ — https://masstransit.io/documentation/configuration/transports/rabbitmq .
