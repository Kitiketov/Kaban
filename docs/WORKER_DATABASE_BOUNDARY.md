# Решение для новых фич: PostgreSQL принадлежит Worker

**Статус: целевая архитектура, утверждённая для новых разработок, пока НЕ реализована в текущем Weather (main, tree `3f128e84877ad96769594c329efcbab265ffe8f2`, 2026-11-01).**

## Кто за что отвечает

| Область | API | Worker | Realtime |
| --- | --- | --- | --- |
| HTTP endpoints / Swagger / transport DTO | ✅ | — | — |
| MediatR Command/Query, FluentValidation формы входа | ✅ | возможно свои обработчики/валидаторы | — |
| Авторизация клиента по JWT/claims | ✅ | **повторно** проверяет бизнес-права по данным БД / доверенному actor id | ✅ для hub, когда появится |
| Отправка задания через MassTransit/RabbitMQ | ✅ | принимает | получает только события о результате |
| EF Core, DbContext, PostgreSQL, миграции | **НЕТ для новых фич** | **ДА, владелец** | НЕТ |
| Бизнес-операция, проверки текущего состояния БД | НЕТ | **ДА** | НЕТ |
| Идемпотентность, Job status, сохранение результата | НЕТ | **ДА** | НЕТ |
| SignalR | НЕТ | НЕТ | будущая реализация |

## Новая операция записи

1. `Api/Features/<Feature>/Presentation`: контроллер разбирает HTTP DTO и вызывает `IMediator.Send(Start...Command)`.
2. `Api/Features/<Feature>/Application/<Operation>`: `Command.cs`, `Validator.cs`, `Handler.cs`. FluentValidation pipeline (`Common/Validation/ValidationBehavior`) проверяет поля, формат, диапазоны и независимые от БД правила. Авторизация использует подтверждённые identity claims, а не доверяет `UserId` от клиента.
3. Handler **не** внедряет `DbContext`, `IUnitOfWork`, EF repositories. Он создаёт `JobId`, формирует `Process...Requested` из `backend/Contracts/<Feature>` и `await`-ом отправляет через MassTransit **в согласованный Worker receive endpoint** (`Send` для адресной команды).
4. Если отправка в брокер не состоялась — вернуть ошибку; при успешной постановке в работу можно вернуть `202 Accepted` с `JobId` и ссылкой на статус. **Не обещать запись в БД или выполнение**: для подтверждения сохранённого статуса Job нужен короткий ack/request-response Worker.
5. `Worker/Features/<Feature>`: `IConsumer<Process...Requested>` вызывает Worker application service. Там проверяются права и предметные ограничения по БД, дубликаты по `JobId`, выполняется EF Core `SaveChangesAsync()` и при необходимости публикуется `...Completed`/`...Failed`.
6. Для гарантированной доставки результата используйте Worker-side outbox и не глотайте исключения consumers. MassTransit обеспечивает at-least-once в обычных конфигурациях; реализация должна быть идемпотентной. Realtime после реализации принимает событие о результате и доставляет клиентам.

**Важно:** `[Authorize]` и FluentValidation в API не гарантируют проверку существования сущности, роли, конкурентного состояния, уникальности — это отвечает Worker и ограничения PostgreSQL.

## Как получать данные для HTTP GET

API не открывает соединение к PostgreSQL. Для короткого запроса допустим `IRequestClient<Get...Query>` / `GetResponse<Get...Response>` через MassTransit к Worker с явным timeout и обработкой недоступности. Для долгих запросов — асинхронное JobId/status API через Worker; read-model в другом сервисе возможен лишь после отдельного архитектурного решения. В ответах/сообщениях только DTO, без EF entities и IQueryable.

## Что сейчас НЕ соответствует целевой архитектуре

- `backend/src/Api/Features/Weather/Application/CreateForecast/Handler.cs` в действительности вызывает `IWeatherRepository.AddAsync()` и `IUnitOfWork.SaveChangesAsync()` **из API**, а потом публикует `ForecastCreatedEvent`. Это демонстрационный legacy flow, не образец для новых фич.
- `backend/src/Api/Infrastructure/Persistence/*` содержит API `KabanDbContext`; подключение к PostgreSQL регистрируется в `backend/src/Api/Program.cs`. **Не расширяй** этот код новым доменом; переносить существующий Weather следует отдельным PR.
- `backend/src/Worker/Features/Weather/ForecastCreatedConsumer.cs` только логирует событие. Рабочего Worker-side DbContext/DI/миграций ещё нет.
- `backend/Database` — экспериментальный проект, пока не включён в solution. Можно подключить его **только к Worker** после аудита моделей, EF mapping и миграций; не расшаривать его context через API.
- `Realtime` пока не реализован; не описывать SignalR как работающий.

## Пошаговая миграция Weather в отдельной задаче

1. Утвердить `CreateForecastRequested(JobId, Date, TemperatureC, Summary, ActorId?)` в Contracts; `ActorId` только из проверенного контекста API, его права повторно проверяет Worker.
2. Перенести EF entity mapping/repository/DbContext/DI и подключение БД в Worker (или Worker-only library); добавить Worker migrations и тесты на PostgreSQL.
3. Добавить `CreateForecastRequestedConsumer` с проверкой входа, DB-правил, дедупликацией `JobId` и записью прогноза; при необходимости publish результата после commit/outbox.
4. Переписать API `CreateForecastHandler`: FluentValidation → Send → `202 Accepted + JobId`, убрать `IWeatherRepository`, `IUnitOfWork` и DB DI из новой операции.
5. Для `GET /api/weather` и `GET /api/weather/{id}` добавить запрос Worker через RabbitMQ request/response; обновить контроллер, тесты и Swagger. Не ломать текущие HTTP-контракты без решения команды.
6. Удалить или изолировать legacy Weather persistence API после перехода; обновить `.csproj`, Compose env, документацию и тесты. **Не удалять БД/volumes**.

## Проверки перед PR с кодом

- `dotnet build backend/Kaban.sln`, `dotnet test backend/Kaban.sln`.
- В API handler-ах новой фичи **нет** `DbContext`/EF/`SaveChanges` и dependency на DB. Проверить `Program.cs` и конфигурацию Compose — API не получает строку подключения к БД для новой фичи, Worker получает.
- HTTP невалидный → FluentValidation ошибка и **нет сообщения в MQ**; валидный → Request/Command в RabbitMQ; Worker меняет БД ровно один раз при дубле; проблемы DB/retry/stop/restart корректны; GET читает через Worker.
- Подтверждено `202` (постановка) и отдельно `Completed` (эффект); не утверждать exactly-once и готовность Realtime без фактических тестов.

См. также `docs/MESSAGING.md`, `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`, `AGENTS.md` и сервисные `AGENTS.md`.
