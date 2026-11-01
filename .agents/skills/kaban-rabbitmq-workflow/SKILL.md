---
name: kaban-rabbitmq-workflow
description: "Use when changing or adding Kaban API-to-Worker asynchronous work, MassTransit/RabbitMQ contracts, consumers, publishing, retries, idempotency, or Realtime event delivery."
---

# Kaban RabbitMQ workflow — skill для кодового агента

## Область применения

Вызывай этот skill при запросах: «обработай через Worker», «отправь из API через RabbitMQ», «добавь consumer/event», «передай результат в Realtime», «проверь потери/дубли сообщений», а также при изменении MassTransit config.

**Перед началом:** прочитай `AGENTS.md`, вложенный `AGENTS.md` целевого сервиса, `docs/MESSAGING.md`, `docs/ARCHITECTURE.md` и существующие `backend/Contracts`/`Program.cs`. Не считай старые README источником истины, если он противоречит коду.

## Порядок работы

1. **Сверь факт с кодом.** На текущей ревизии API сохраняет Weather в PostgreSQL и публикует `ForecastCreatedEvent`; Worker только логирует; Realtime не реализован. Уточни, что должна делать новая работа и кто владеет результатом. Не объявляй прототип полнофункциональной распределённой системой.
2. **Выбери семантику:**
   - Одному конкретному Worker поручить действие → command `...Requested`, стабильная очередь, предпочтительно `Send` к endpoint.
   - Сообщить о произошедшем факте нескольким слушателям → event `...Created`/`...Completed`, `Publish`.
   - Если решено `Publish` для задания, документируй, почему один подписчик гарантирован, и последствия появления второго.
3. **Сначала контракт.** Добавь тип только с бизнес-данными в `backend/Contracts/<Feature>/`, укажи `JobId` для дедупликации, избегай полных сущностей и секретов. Учитывай обратную совместимость.
4. **API:** REST/HTTP DTO остаются в `Presentation`, command/query — в `Application`. Controller → MediatR handler; handler авторизует и валидирует, создаёт `JobId` и принимает задание. Для долгой работы возвращай `202 Accepted` и статус; не исполняй длительную операцию в HTTP. Организуй атомарность записи задания и публикации через outbox, если она требуется.
5. **Worker:** добавь `IConsumer<T>` + DI в Worker и настрой конкретный receive endpoint или `ConfigureEndpoints`. Consumer вызывает код Worker, записывает результат и обеспечивает идемпотентность; завершает успешно только после успешного эффекта. Не ссылайся на проект API или его DbContext.
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
- [ ] API создаёт задание; Worker **действительно исполняет** работу.
- [ ] Защищены атомарность/доставка там, где это критично, и повторная обработка.
- [ ] Сценарий ошибки и наблюдение `_error` предусмотрены.
- [ ] Автотесты и интеграционный проход выполнены либо честно помечены как невыполненные.
- [ ] AGENTS/docs соответствуют реализации, без выдачи будущего за настоящее.
