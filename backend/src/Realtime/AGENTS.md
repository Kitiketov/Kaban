# Realtime — инструкции агенту

Действует на `backend/src/Realtime/**` вместе с корневым `AGENTS.md`. Перед задачами взаимодействия читай `docs/MESSAGING.md` и `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`.

## Текущее состояние

- `Realtime.csproj` и `Program.cs` содержат только самостоятельный ASP.NET Core host и `/health/live`, `/health/ready`.
- **SignalR Hub, RabbitMQ/MassTransit consumer, JWT auth и выдача уведомлений отсутствуют**. Переменные RabbitMq в Docker Compose сами по себе не подключают сервис к брокеру.
- У Realtime нет ссылки на `Api.csproj`, `Contracts.csproj` или API DB context.

## Когда понадобится подключить события

- Realtime принимает *события об уже завершённых изменениях*, например `...Completed`/`NotificationAvailable`, а не исполняет тяжёлую задачу Worker.
- Контракты держи в `backend/Contracts`. Реализация подписчика и Hub — только внутри Realtime.
- Hub должен **валидировать access JWT** перед подключением и маршрутизировать события только их законным получателям. Не доверяй `userId` из клиентских параметров. Refresh token не должен открывать WebSocket.
- Если сообщение требуется отправить пользователю, проверяй привязку события к авторизованному пользователю/группе. Не выкладывай токены в query-string logs.
- Realtime не должен знать EF-сущности API и напрямую изменять его таблицы. Для истории уведомлений владелец хранения определяется отдельно; не объявляй это уже работающим.
- Добавь интеграционные тесты авторизации, доставки и запрета чужих событий; обнови `docs/MESSAGING.md`.
