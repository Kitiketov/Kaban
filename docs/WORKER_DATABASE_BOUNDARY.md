# Kaban: API ждёт Worker, Worker владеет PostgreSQL

> Сверено с `main` commit `f6b110e9f2d59efc6251060a9ab9680202d24722` (2026-11-01). Это архитектурное решение для новых фич; существующая Tasks частично соответствует ему, но имеет ошибки wiring/надёжности.

## Ответственность

| Ответственность | API | Worker | Realtime |
| --- | --- | --- | --- |
| HTTP endpoints, HTTP DTO | да | нет | нет |
| MediatR + FluentValidation входа | да | собственная проверка при необходимости | нет |
| Аутентификация запроса | да | повторные domain/DB-permission checks по доверенной identity | SignalR auth, когда появится |
| RabbitMQ request client и ожидание response | **да** | **RespondAsync** после выполнения | нет |
| EF Core, DbContext, repository, миграции, PostgreSQL | **нет** | **да** | нет |
| Выполнение бизнес-операции и фиксация результата | нет | **да** | нет |
| Публикация события `...Completed` подписчикам | нет (по умолчанию) | по необходимости, отдельно от ответа API | получает (будущее) |
| Идемпотентность записи / повтор после timeout | передаёт постоянный ключ | гарантирует единственный бизнес-эффект | idempotent notifications, если нужны |

## Операция записи: синхронный ответ поверх очереди

1. Клиент вызывает `POST /api/...` и ожидает завершения; API Controller собирает MediatR Command.
2. `AbstractValidator<TCommand>` **зарегистрирован в DI**, `ValidationBehavior` отвергает неверные поля до обращения к RabbitMQ. API не использует SQL, даже для `MustAsync`.
3. API MediatR handler вызывает `IRequestClient<TRequest>.GetResponse<TSuccess, TFailure>(request, ct)` с конфигурируемым конечным timeout. В contract передаёт `OperationId`/idempotency key для записи, проверенного actor identity и нужные поля, **без секретов**.
4. Worker consumer выполняет DB-валидацию и операцию через Worker-owned EF Core, фиксацию и дедупликацию. Если OperationId уже встречался, возвращает сохранённый ответ без второй записи.
5. **После commit** Worker вызывает `ConsumeContext.RespondAsync<TSuccess>(...)` для ожидающего API. API возвращает данные Worker в HTTP-ответе. Документация не задаёт фиксированный успешный HTTP status; важно именно дождаться результата.
6. При ожидаемом domain reject Worker отвечает `TFailure`; при техническом исключении MassTransit посылает fault. API сопоставляет их с корректным HTTP статусом.
7. Если Worker также должен уведомить Realtime, публикует **дополнительное** `...Completed` событие после фиксации; outbox по необходимости. Это не замена response.
8. Если истёк timeout, API возвращает согласованную ошибку ожидания, а Worker может ещё работать. Повтор клиента — **тот же** OperationId, а не новая незащищённая запись.

В стандартной схеме `CreateTask` API ждёт итоговый ответ от Worker. Не заменяй ожидание выдачей JobId/подтверждения очереди; другая модель возможна только при отдельном изменении требований.

## GET/Query

Для чтения API также отправляет MassTransit request Worker с timeout и возвращает DTO после response. Бизнес-БД не читается из API. `IQueryable`/EF entities не переходят в контракты.

## Текущее состояние Tasks и проблемы

Сейчас есть `IRequestClient<TaskCreateEvent>.GetResponse<TaskCreateSuccess>` в API, `IConsumer<TaskCreateEvent>` в Worker и `SaveChangesAsync -> RespondAsync<TaskCreateSuccess>` в consumer, а `KabanDatabaseContext` зарегистрирован в Worker. Это **уже соответствует главному пути исполнения**. Но:

- Worker импортирует `Contracts.Tasks`, хотя contract определён в `Contracts.Features.Tasks`.
- Worker ссылается на `Contracts.dll` через `HintPath` из Debug-каталога API вместо `ProjectReference`.
- API зарегистрировал ValidationBehavior, но **нет FluentValidation-validator CreateTask и его регистрации**.
- `CreatedAtAction(nameof(Create))` не указывает на существующий GET-by-id endpoint.
- Docker Compose передаёт адрес БД в API, а не в Worker; Worker использует localhost из `appsettings.json`, который внутри контейнера не указывает на postgres service.
- Не показаны идемпотентность, явная timeout config и корректная классификация всех faults.

Это **зафиксированные проблемы**, а не изменения выполненного C#-кода. При отдельном рефакторинге чинить по одной границе с тестами и не удалять данные БД.

## Готовность

Успешная интеграционная проверка должна подтвердить: валидация API → request; Worker commit до reply → API получает Id в HTTP DTO; отсутствие Worker/DB/брокера → ожидаемая ошибка/timeout; повтор с тем же OperationId не дублирует запись; отдельное событие публикуется только когда действительно нужно. Тестировать с реальными RabbitMQ и PostgreSQL, не предполагать exactly-once.

Подробности — `docs/MESSAGING.md` и `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`.
