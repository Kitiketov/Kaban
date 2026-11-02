# Realtime — правила для кодового агента

Действует для `backend/src/Realtime/**` вместе с корневым `AGENTS.md`, `docs/MESSAGING.md` и skill `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`.

- На текущем `main` в `Program.cs` имеются только health endpoints; **SignalR, JWT authorization, MassTransit consumers и уведомления пока не реализованы**. Не утверждай обратного.
- После реализации Realtime слушает **публичные события** `...Completed`/`...Created` от Worker и уведомляет разрешённых пользователей через защищённый SignalR Hub.
- **Realtime не отвечает на `IRequestClient.GetResponse` API.** Для ожидающего API Worker вызывает `ConsumeContext.RespondAsync`; `Publish` используется отдельно для независимых подписчиков (Realtime, аудит, аналитика).
- Не обрабатывай бизнес-команды и не используй PostgreSQL/EF сущности Worker. Контракты сообщений — в `backend/src/Contracts`.
- Проверяй access JWT и права на группы/каналы; не используй refresh JWT, не доверяй userId от клиента и не логируй секреты.
- Если уведомления должны переживать отключение клиента, явно проектируй durable storage/доставку; сам SignalR не гарантирует получение офлайн-клиентом. Тестируй запрет чужих уведомлений и повторы.
