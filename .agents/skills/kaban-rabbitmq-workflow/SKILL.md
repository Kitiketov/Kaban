---
name: kaban-rabbitmq-workflow
description: "Use when changing or adding Kaban API-to-Worker asynchronous work, MassTransit/RabbitMQ contracts, consumers, publishing, retries, idempotency, or Realtime event delivery."
---

# Kaban RabbitMQ workflow — skill для кодового агента

## Область применения

Вызывай этот skill при запросах: «обработай через Worker», «отправь из API через RabbitMQ», «добавь consumer/event», «передай результат в Realtime», «проверь потери/дубли сообщений», а также при изменении MassTransit config.

**Перед началом:** прочитай `AGENTS.md`, вложенный `AGENTS.md` целевого сервиса, `docs/MESSAGING.md`, `docs/ARCHITECTURE.md` и существующие `backend/Contracts`/`Program.cs`. Не считай старые README источником истины, если он противоречит коду.

## Неизменяемое правило для новых фич

**API = HTTP + аутентификация/авторизация + FluentValidation + постановка задания в RabbitMQ. Worker = выполнение бизнес-операции + PostgreSQL + EF Core.** Не добавляй `DbContext`, `IUnitOfWork` для DB, EF-репозитории, миграции и SQL в новую фичу API. Старый Weather — антипример для новой схемы: он сохраняет прогноз в API и публикует лишь событие после изменения.

Новая операция записи: `HTTP → Controller → MediatR Command → FluentValidation pipeline → MassTransit Send(...Requested) → Worker Consumer → Worker Application/Domain → Worker Repository/DbContext → PostgreSQL → optional ...Completed / ...Failed`. API возвращает `202 Accepted` + `JobId` после успешной постановки в брокер; это **не** утверждение, что запись уже появилась в БД. Если нужен долговечный статус и гарантия приёма, Worker должен сохранить запись задачи и подтвердить её коротким request/response, либо должен быть отдельно спроектирован другой надёжный протокол. Не выдавай `202` за гарантию exactly-once.

Бизнес-валидация по БД (наличие сущности, роли/права, уникальность, допустимость перехода) выполняется **Worker**. FluentValidation в API остаётся для формы, длины, типов, диапазонов и независимых от БД ограничений. Для чтения внешних данных API также не обращается к PostgreSQL: явный RabbitMQ request/response к Worker с конечным timeout или согласованный read-model, без скрытых прямых EF-запросов. Смотри `docs/WORKER_DATABASE_BOUNDARY.md`.

## Порядок работы

1. **Сверь факт с кодом.** На текущей ревизии API сохраняет Weather в PostgreSQL и публикует `ForecastCreatedEvent`; Worker только логирует; Realtime не реализован. Уточни, что должна делать новая работа и кто владеет результатом. Не объявляй прототип полнофункциональной распределённой системой.
2. **Выбери семантику:**
   - Одному конкретному Worker поручить действие → command `...Requested`, стабильная очередь, предпочтительно `Send` к endpoint.
   - Сообщить о произошедшем факте нескольким слушателям → event `...Created`/`...Completed`, `Publish`.
   - Если решено `Publish` для задания, документируй, почему один подписчик гарантирован, и последствия появления второго.
3. **Сначала контракт.** Добавь тип только с бизнес-данными в `backend/Contracts/<Feature>/`, укажи `JobId` для дедупликации, избегай полных сущностей и секретов. Учитывай обратную совместимость.
4. **API:** REST/HTTP DTO остаются в `Presentation`, command/query — в `Application`. Controller → MediatR handler; handler проводит FluentValidation и доступную без БД проверку прав, генерирует `JobId` и отправляет `...Requested` через MassTransit. Для долгой работы возвращай `202 Accepted` + `JobId`; не исполняй её в HTTP, **не сохраняй Job/Task/Forecast в БД из API**. Если гарантия постановки требует DB-ack, проектируй подтверждение со стороны Worker и документируй failure semantics.
5. **Worker:** добавь `IConsumer<T>` + DI в Worker и настрой конкретный receive endpoint или `ConfigureEndpoints`. Consumer вызывает код Worker, делает проверку бизнес-правил, обращается к **Worker-owned** EF Core репозиторию/DbContext и сохраняет результат с идемпотентностью; завершает успешно только после успешного commit. Настрой DI, credentials, миграции в Worker. Не ссылайся на проект API или его DbContext.
6. **Обработка сбоев:** настроить ограниченные retry/redelivery и обработку poison messages, обеспечить диагностику `_error` очередей. Исключения не глотать. Учитывать дубли и повторный запуск.
7. **Результат:** для уведомления пользователя публикуй `...Completed`/`...Failed` (или согласованный отдельный event); Realtime потребляет событие и доставляет через авторизованный SignalR hub **только после его реализации**. Не добавляй SignalR внутрь Worker ради короткого пути.
8. **Проверка:** `dotnet build backend/Kaban.sln`, `dotnet test backend/Kaban.sln`; `docker compose config --quiet`; интеграционный проход через реальные RabbitMQ/PostgreSQL, включая дубль/сбой/отсутствие потребителя. Отдельно проверь rollback/commit и фактическое состояние БД. Если проверки не запускались, скажи об этом.
9. **Документация:** обнови `docs/MESSAGING.md`, `docs/ARCHITECTURE.md`, вложенные AGENTS.md и, если нужно, README при изменении кода/топологии.

## Паттерн, который НЕ нужно копировать для задания

```csharp
// Реальный код сейчас — уже выполненное API-действие, не делегирование работы:
await repository.AddAsync(forecast, ct);
await unitOfWork.SaveChangesAsync(ct);
await publishEndpoint.Publish(new ForecastCreatedEvent(...), ct);
```

Для задания Worker сначала создай contract `...Requested`, затем отправь задание и определи получение результата. Не оставляй саму работу в API. Не подменяй надёжность транспорта утверждением exactly-once.

## Готовность задачи

- [ ] Известны владельцы данных, contract, consumer и имя/семантика endpoint.
- [ ] API провёл FluentValidation и **только отправил задание**, не использовал EF/SQL; Worker **действительно исполняет** работу и владеет БД.
- [ ] Бизнес-проверки по состоянию БД выполнены в Worker; защищены атомарность/доставка там, где это критично, и повторная обработка.
- [ ] Сценарий ошибки и наблюдение `_error` предусмотрены.
- [ ] Автотесты и интеграционный проход выполнены либо честно помечены как невыполненные.
- [ ] AGENTS/docs соответствуют реализации, без выдачи будущего за настоящее.
