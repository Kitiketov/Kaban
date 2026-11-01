# Contracts — межсервисные сообщения

Применяй на `backend/Contracts/**` вместе с корневым `AGENTS.md` и при изменениях прочитай `docs/MESSAGING.md` / `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`.

- Это **библиотека типов сообщений** для MassTransit, не четвёртый сервис, не слой с бизнес-логикой и не общий `Infrastructure`.
- Сейчас реально используется только `Weather/ForecastCreatedEvent.cs`: `Date`, `TemperatureC`, `Summary`. У него нет `ForecastId`, `JobId` или status/result; он означает «прогноз уже создан», а не «создай прогноз».
- Для задания Worker объявляй явно названное `...Requested` с бизнес-ключом `JobId` и только необходимыми входными параметрами; для результата — `...Completed` / `...Failed` с тем же `JobId` и безопасными полями.
- Контракты должны быть transport DTO (например record); не включай ASP.NET types, EF entities, DbContext, DI, MediatR handlers, credentials, JWT, stack traces или `IQueryable`.
- Сообщение принадлежит пересечению интересов сервисов; schema change согласовывай с publisher и consumer. При breaking change добавляй версионированный тип, тесты сериализации/совместимости и миграционный план.
- Не делай обратных project references на Api, Worker, Realtime.
