# Kaban — RabbitMQ/MassTransit: API ждёт результат Worker

> Актуализировано по `Kitiketov/Kaban` `main`, commit `f6b110e9f2d59efc6251060a9ab9680202d24722`, 2026-11-01. Здесь отдельно отмечены **проверенный код**, **принятая архитектура** и **недочёты**.

## Обязательный сценарий для коротких операций

```text
Client: POST /api/tasks
  |
  v
API: TaskController.Create -> IMediator.Send(CreateTaskCommand)
  | FluentValidation (зарегистрированный validator для Command)
  v
API: IRequestClient<TaskCreateEvent>.GetResponse<TaskCreateSuccess>(request, ct)
  |                              [ожидает; есть конечный timeout]
  v
RabbitMQ -> Worker: TaskCreateConsumer : IConsumer<TaskCreateEvent>
  |  проверить business rules и дубликаты
  |  сохранить Tasks через KabanDatabaseContext/PostgreSQL
  |  SaveChangesAsync (успех / commit)
  v
Worker: context.RespondAsync<TaskCreateSuccess>({ TaskId })
  |
  v
API: получить ответ, собрать TaskCreateResponseDto
  |
  v
Client: HTTP-ответ с данными Worker (включая TaskId)
```

**Ключевое правило:** API не завершает HTTP-запрос сразу после отправки сообщения; он ждёт ответа Worker либо истечения тайм-аута. Обработка физически идёт в отдельном процессе Worker. Для чтения данных используется тот же request/response, а не прямой доступ API к БД. **Схема обмена не предписывает конкретные HTTP-коды успеха.**

### Что значит «Worker публикует результат»

- Для **ожидающего запроса API** в существующей архитектуре применяется `RespondAsync<TSuccess>` в Worker и `GetResponse<TSuccess>` в API. MassTransit возвращает ответ на reply address и связывает его с request автоматически. Это **request/response**.
- `IPublishEndpoint.Publish<TCompleted>` — **публикация события** всем подписчикам, например будущему Realtime. Она **не** вызывает завершение `IRequestClient.GetResponse<TSuccess>`; подменять ответ публикацией нельзя.
- Если нужен и ответ клиенту, и событие: **Worker сохраняет в БД → отвечает API через RespondAsync → отдельно публикует Completed event** (или сначала надёжно фиксирует outbox и выпускает событие, затем отвечает). Протокол публикуемого события не должен зависеть от того, держит ли HTTP-клиент соединение.
- Не заводи in-memory словарь `TaskCompletionSource`, подписку на произвольный `Publish` внутри контроллера или глобальный state только одного экземпляра API. Это ломается на нескольких API replicas, рестартах и гонках. Если команда отдельно требует ожидания именно *broadcast event*, нужна самостоятельная durable correlation архитектура; default проекта — MassTransit request/response.

## Что реально есть в `main`

| Участник | Реализация |
| --- | --- |
| API HTTP | `backend/src/Api/Features/Tasks/Presentation/TaskController.cs`, `POST /api/tasks` |
| API MediatR | `Features/Tasks/CreateTask/Command.cs`, `Handler.cs`, `IRequestClient<TaskCreateEvent>.GetResponse<TaskCreateSuccess>` |
| API transport DI | `Api/Program.cs`: `AddRequestClient<TaskCreateEvent>()`, `UsingRabbitMq` |
| Контракты | `backend/src/Contracts/Features/Tasks/TaskCreateEvent.cs`, `TaskCreateSuccess.cs`; namespace `Contracts.Features.Tasks` |
| Worker transport DI | `Worker/Program.cs`: `AddConsumer<TaskCreateConsumer>()`, `UsingRabbitMq`, `ConfigureEndpoints(context)` |
| Worker DB | `Worker/Infrastructure/Database/Program.cs`: `KabanDatabaseContext`; `Worker/Program.cs`: `AddDbContext(...UseNpgsql)` |
| Worker обработчик | `Worker/Features/Tasks/TaskCreateConsumer.cs`: `AddAsync` -> `SaveChangesAsync` -> `RespondAsync<TaskCreateSuccess>` |
| Realtime | Пока только health; SignalR/MQ consumers отсутствуют |

**Терминологический нюанс:** `TaskCreateEvent` в текущем коде по смыслу *запрос на создание*, а не публичное событие о завершении. Для новых контрактов называй request `CreateTaskRequested`/`...Command`; при переименовании текущего типа учитывай двустороннее изменение namespaces, wiring и совместимость.

### Известные несогласованности, которые агент должен проверить

1. Worker использует `using Contracts.Tasks`, а реальный контракт находится в `namespace Contracts.Features.Tasks`. Без согласованного namespace/ссылки consumer и API не смогут корректно работать с одним типом.
2. `Worker.csproj` содержит `<Reference Include="Contracts"><HintPath>..\Api\bin\Debug\net10.0\Contracts.dll</HintPath>` вместо нормального `<ProjectReference Include="..\Contracts\Contracts.csproj" />`.
3. `Api/Common/ApplicationRegistration.cs` подключает MediatR и `ValidationBehavior`, **но не сканирует/регистрирует FluentValidation validators**; валидатора `CreateTaskCommand` нет. Поэтому реальная API-валидация CreateTask не реализована, хотя pipeline есть.
4. `TaskController.Create` вызывает `CreatedAtAction(nameof(Create))`, но GET-by-ID route отсутствует; при отдельной правке HTTP-контроллера проверить маршрут, не копировать этот вызов как обязательный шаблон.
5. `docker-compose.yml` не задаёт Worker `ConnectionStrings__DefaultConnection`. Локальный `Worker/appsettings.json` указывает на `localhost:55432`, что неверно внутри Worker Docker-контейнера; нужно `postgres:5432`, сетевой Compose доступ и правильные dependency/health проверки.
6. В коде нет явно заданного общего request timeout, transport/application idempotency key, inbox/outbox, полноценных типизированных business errors и гарантированной проверки повторов. Не утверждай, что они реализованы.

## Тайм-аут, ошибки и повторное выполнение

- Выбери конечный **настраиваемый** request timeout (dev-пример 30 сек; это пример, не текущая конфигурация) и передавай HTTP `CancellationToken`. Убедись, что timeout API/прокси согласован с бюджетом операции.
- `RequestTimeoutException`: API не получил Worker response вовремя. Возвращай документированную ошибку ожидания с безопасным ProblemDetails, **не** выдавай подтверждение отправки за выполненную работу. На текущем main exception handler использует 503, но это не настраивает тайм-аут само по себе.
- **Timeout не равен отмене операции.** Запрос мог остаться в очереди, Worker мог завершить запись в БД и ответить уже после того, как API перестал ждать. Поэтому мутация должна нести стабильный `OperationId`/idempotency key и Worker должен атомарно дедуплицировать и уметь повторно выдать результат.
- При ожидаемой бизнес-ошибке возвращай typed reply `NotFound`/`Conflict`/`Forbidden` и переводи его в правильный HTTP статус. Не кидай обычные domain errors просто ради HTTP 400 для всех.
- Техническое исключение consumer -> MassTransit `Fault<TRequest>` -> `RequestFaultException` в API. **Не** маскируй любую инфраструктурную аварию под 400; текущий handler для Fault так делает и требует доработки.
- Для Worker: limited retry/redelivery, `_error` queue, логирование correlation ID, outbox для согласования DB commit и дополнительного `Publish`, тесты на дубли. Никакой магии exactly-once.

## Как формируется ответ клиенту

- **Успешная операция:** API получает `TaskCreateSuccess` от Worker, извлекает `TaskId`, формирует `TaskCreateResponseDto` и возвращает DTO клиенту. Не выбирай HTTP success code исходя из самого факта использования брокера; это независимая деталь HTTP-контракта.
- **Невалидный ввод:** FluentValidation отклоняет запрос до RabbitMQ и возвращается ошибка валидации.
- **Бизнес-отказ Worker:** типизированный ответ об отсутствии данных, запрете или конфликте; API преобразует его в согласованный HTTP-ответ без маскировки под технический сбой.
- **Fault/timeout:** отдельная ошибка API; запрещено возвращать успешный DTO, если Worker не подтвердил результат.
- **Дополнительное событие:** `Publish<TCompleted>` служит подписчикам, не является ответом, который ожидает `GetResponse`.

В текущем `TaskController` используется `CreatedAtAction(nameof(Create))`; это реальная деталь исходного кода, **не правило для новых контроллеров**. В рамках этой документации HTTP-контракт и C#-контроллер не меняются.

## Топология и инфраструктура

`IRequestClient<T>` по умолчанию публикует request и ждёт ответ на response endpoint MassTransit; ожидается **один логический тип обработчика**, а несколько Worker-инстансов одной очереди балансируют запросы. Можно указать явный endpoint/адрес для направленного request. Не выдумывай RabbitMQ topic-routing keys или ручные exchange names: `cfg.ConfigureEndpoints(context)` использует соглашения MassTransit.

В Docker: API подключается к `rabbitmq:5672`; Worker — к `rabbitmq:5672`, PostgreSQL — к `postgres:5432`; с Windows-хоста брокер доступен по `localhost:5672`, PostgreSQL — `localhost:55432`, management — `localhost:15672`. Смотри актуальный `docker-compose.yml` прежде чем давать команды. Не запускай `docker compose down -v`.

## Проверка изменения фичи

Из корня: `dotnet build backend/Kaban.sln`, `dotnet test backend/Kaban.sln`, `docker compose config --quiet` при изменении Compose. Интеграционный сценарий: неверный input не попал в MQ, успешный POST ждал commit Worker и получил ID, бизнес-ошибка, технический fault, Worker недоступен, timeout + поздняя запись, повтор с тем же idempotency key, публикация отдельного события (если реализована). Фактически выполненные проверки отделяй от предложенных.

Официальное описание MassTransit request/response: https://masstransit.io/documentation/concepts/requests
