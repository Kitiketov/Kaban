# Kaban: взаимодействие сервисов через RabbitMQ

> Проверено по `Kitiketov/Kaban`, `main`, commit `4136d1973521c7f4337ea51bddfdc126025f8a34` (2026-11-01). Это описание **того, что в коде есть сейчас**, и отдельно — **правил для будущих задач**, а не утверждение о production-ready обработке.

## Состав solution и владельцы ответственности

| Компонент | Проект | Реальная роль на указанном commit |
| --- | --- | --- |
| API | `backend/src/Api/Api.csproj` | ASP.NET Core контроллеры, MediatR/CQRS, FluentValidation, EF Core / PostgreSQL, MassTransit **publisher** |
| Worker | `backend/src/Worker/Worker.csproj` | Отдельный ASP.NET Core host, MassTransit **consumer**; пока только логирует событие Weather |
| Realtime | `backend/src/Realtime/Realtime.csproj` | Отдельный host с health endpoints; SignalR и RabbitMQ обработчиков **пока нет** |
| Contracts | `backend/Contracts/Contracts.csproj` | Общие контракты сообщений, **библиотека, не четвёртый микросервис** |
| Database | `backend/Database/Database.csproj` | Отдельный экспериментальный проект модели данных; сейчас **не включён** в `backend/Kaban.sln`, API использует `KabanDbContext` |
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

1. API **сам выполняет** создание прогноза и сохранение в PostgreSQL. Worker не создаёт прогноз — он **узнаёт о совершившемся событии**. Это event notification, а не фоновая команда.
2. API вызывает `SaveChangesAsync()` и **только потом** `Publish()`. Между ними нет атомарности: сохранение может пройти, публикация — упасть. **Transactional outbox пока не настроен.**
3. В примере нет бизнес-идемпотентности, явного retry/redelivery policy, fault handling / мониторинга ошибок и persist результата Worker. Не заявляй exactly-once.
4. В Realtime нет SignalR Hub, Kafka/RabbitMQ consumer, JWT-конфигурации или отправки клиентам.
5. `/health/live` и `/health/ready` сейчас **не подтверждают** наличие активного RabbitMQ consumer или успешного подключения к БД.
6. В `docker-compose.yml` credentials заданы для локальной разработки. В production — только переменные окружения/секреты. Подключение с хоста: RabbitMQ `localhost:5672`, с контейнера: `rabbitmq:5672`. PostgreSQL с хоста `localhost:55432`, из сети compose `postgres:5432`.

## Как реализовывать новые фоновые задачи: API даёт задание, Worker выполняет

**Это проектное правило для следующих фич, не реализованный в репозитории код.** Сначала определи «что является работой» и кто владеет её результатом. Нельзя автоматически считать `ForecastCreatedEvent` подходящим командным контрактом.

### Рекомендуемый сценарий

```text
Client
  | POST /api/.../jobs
  v
Api Controller -> IMediator.Send(Start...Command)
  | validate / authorize
  | create JobId and durable job/outbox record when needed
  | commit transaction with outbox entry
  | HTTP 202 Accepted + JobId/status URL
  v
MassTransit -> RabbitMQ -> worker-owned queue
  |
  v
Worker Consumer<...Requested>
  | deduplicate by JobId/MessageId
  | execute background work (not in HTTP request)
  | persist own outcome and/or call owner through explicit contract
  | publish ...Completed or ...Failed
  v
(optionally) Realtime consumer -> authorized SignalR clients
```

Правило: **API не выполняет тяжёлую работу в контроллере**, не вызывает `Task.Run` для гарантированного задания и не делает HTTP-запрос ожидающим завершения Worker. HTTP DTO — только на стороне API; транспортное сообщение — в `backend/Contracts`.

### Command vs event; Send vs Publish

- `...Requested` / `Process...` — **задание с одним владельцем**. Если один конкретный Worker отвечает за работу, предпочитай адресное `Send` к **явно названному worker receive endpoint**. Это нужно отдельно настроить в Worker и записать в документацию. Допустима схема `Publish` при одном подписчике, но при добавлении ещё одного подписчика событие обработается каждым подписчиком.
- `...Created`, `...Completed`, `...Failed` — **факты**, уже произошедшие в системе. Для них обычно подходит `IPublishEndpoint.Publish`, который позволяет нескольким заинтересованным потребителям получить копию события.
- `ForecastCreatedEvent` сейчас относится к **второму** типу: задача сохранения выполнена API, Worker наблюдает результат.
- Не превращай `ForecastCreatedEvent` в команду только за счёт изменения названия consumer. Согласуй новый контракт по семантике сценария.

### Требования к контракту нового задания

- Название: `GenerateReportRequested`, `ProcessTaskRequested` и т.п. **по настоящей предметной операции**.
- Содержимое: стабильный `JobId` (`Guid`) для учёта работы; необходимые идентификаторы и аргументы; время события/версия при необходимости. Используй `ConsumeContext.MessageId`/`CorrelationId` как transport metadata, а `JobId` — как бизнес-ключ повторной обработки.
- Не сериализуй EF entity, `DbContext`, HTTP request DTO, токены, пароли и секреты.
- Договорись, кто владеет статусом задания. Worker не пишет напрямую в API-owned таблицы без явного архитектурного решения; результат может возвращаться через событие API-owner-у.
- Контракты версионируй совместимо: при изменении полей учитывай старых consumers; существенные breaking changes согласовывай и тестируй обе стороны.

### Надёжность

- Публикация + DB commit: если они должны быть атомарно согласованы, добавь **outbox** (например MassTransit EF outbox или свой). Текущий `SaveChanges` затем `Publish` недостаточен.
- Worker должен быть **идемпотентен**: RabbitMQ/MassTransit могут доставить сообщение повторно. Проверяй `JobId` и атомарно фиксируй состояние выполненной работы.
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
| Worker делает работу | Сервис Worker со своими зависимостями и состоянием, без ссылки на `Api.csproj` |
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
