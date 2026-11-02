# Правила тестирования backend

Действует на `backend/tests/**` вместе с корневым `AGENTS.md` и skill `.agents/skills/kaban-rabbitmq-workflow/SKILL.md` при задачах обмена.

- Текущий `backend/tests/Tests/Tests.csproj` — xUnit с ProjectReference на API. Проверь реальные тестовые файлы перед выбором фильтра: старые Weather-тесты и фильтр `CreateForecastHandlerTests` **в обновлённом main отсутствуют**.
- После изменения кода запусти из корня `dotnet build backend/Kaban.sln`, `dotnet test backend/Kaban.sln`. Если изначальные ошибки (контрактный namespace, DLL reference) мешают сборке — точно укажи, что не удалось проверить.
- Тест FluentValidation: некорректный request приводит к 400 и **не вызывает `IRequestClient`**. Недостаточно проверить один лишь `ValidationBehavior` без регистрации `IValidator<T>` в DI.
- Тест API handler: ждёт `GetResponse<TSuccess>`, передаёт `CancellationToken`, возвращает DTO **только после ответа Worker**. Проверяй business failure, `RequestFaultException`, `RequestTimeoutException`, отмену клиента, корректную обработку ответа и отсутствие преждевременного сообщения об успехе.
- Тест Worker: после реального PostgreSQL commit отправляет `RespondAsync`, после отката не подтверждает успех, одинаковый `OperationId` не создаёт дубликаты. Проверяй fault, retries, timeout + поздний ответ и ошибки публикации дополнительного event.
- Держи unit-тесты независимыми от Docker; тесты RabbitMQ/PostgreSQL запускай отдельно с изолированными контейнерами и чёткой очисткой своих тестовых данных. EF InMemory/fake не проверяет SQL constraints. Не удаляй пользовательскую базу.
- Для changes только в Markdown не нужно создавать тесты; проверь пути, соответствие коду и Markdown/skill metadata.
