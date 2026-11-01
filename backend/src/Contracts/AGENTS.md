# Contracts — правила для межсервисных сообщений

Действует на `backend/src/Contracts/**` вместе с корневым `AGENTS.md` и `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`.

- `backend/src/Contracts/Contracts.csproj` — общая библиотека DTO для API, Worker и будущего Realtime, **не микросервис**. Namespace должен совпадать в publisher/requester и consumer; не копируй namespace по старым путям `backend/Contracts`.
- Для короткого синхронного HTTP-сценария определяй **request + success response + (при необходимости) типизированные бизнес-отказы**. API использует `IRequestClient<TRequest>.GetResponse<TSuccess, TFailure>`; Worker — `RespondAsync<TSuccess/TFailure>`. Ответная схема должна содержать данные, необходимые клиенту, а не только факт публикации.
- Пример сейчас: `Features/Tasks/TaskCreateEvent.cs` (`Contracts.Features.Tasks.TaskCreateEvent`) фактически **request/command**, несмотря на суффикс `Event`; `TaskCreateSuccess` — ответ с `TaskId`. Не смешивай этот request с публичным доменным событием. При расширении/переименовании учитывай совместимость и обе стороны обмена.
- Когда результат нужен Realtime/другим слушателям, введи **отдельный event** `TaskCreated`/`...Completed` и публикуй его после успешного commit Worker. `Publish<TEvent>` не является заменой `RespondAsync<TResponse>` для ожидающего `GetResponse`.
- Для операций записи, которые клиент может повторить после тайм-аута, добавляй стабильный `OperationId`/idempotency key в request и соответствие в ответе, если нужно. Отделяй бизнес-идентификатор от `MessageId` и транспортного `RequestId`, который обеспечивает корреляцию ответа MassTransit.
- Используй переносимые, сериализуемые `record`/POCO с примитивными безопасными полями; не передавай EF сущности, `DbContext`, HTTP DTO, MediatR handler, токены, пароли, stack traces, `IQueryable`, SQL expressions. Согласовывай версии DTO и тестируй serialization round-trip.
- Не допускай обратных ссылок Contracts на Api/Worker/Realtime. Для будущих Query/Response следуй тому же request/response протоколу и единой политике timeout.
