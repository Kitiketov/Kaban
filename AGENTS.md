# Инструкции для агентов — Kaban

## Область действия

- Применяй этот файл ко всему репозиторию; вложенные `AGENTS.md` уточняют правила своей папки. Прямые инструкции пользователя имеют приоритет.
- Перед изменениями читай `README.md`, `docs/ARCHITECTURE.md`, `docs/MESSAGING.md` и `docs/FILE_NAMING_AND_DTO.md`. Сверяй документацию с кодом и обновляй её при изменении архитектуры.
- Стек: .NET 10, ASP.NET Core, MediatR, FluentValidation, MassTransit/RabbitMQ, EF Core/Npgsql, PostgreSQL. Версии сверяй с `.csproj` и `backend/Directory.Build.props`; не обновляй пакеты попутно.

## ОБЯЗАТЕЛЬНО: архитектура новых фич

**API → RabbitMQ → Worker → PostgreSQL.** Это принятое правило для **новых фич**, а не утверждение о текущем коде Weather.

- **API** принимает HTTP-запросы, аутентифицирует/авторизует клиента без прямых SQL-запросов, вызывает MediatR и проверяет вход через **FluentValidation pipeline**. После успешной проверки создаёт `JobId` и передаёт `...Requested` в RabbitMQ посредством MassTransit. API **не выполняет бизнес-операцию над БД** и **не использует EF Core/DbContext/SQL/Repository/UnitOfWork для новых фич**.
- **Worker** получает `...Requested` через `IConsumer<T>`, проверяет бизнес-права и инварианты по фактическим данным, обеспечивает идемпотентность и выполняет **все операции чтения/записи PostgreSQL**. Репозитории, `DbContext`, миграции, UnitOfWork и сохранение статуса задания принадлежат Worker или библиотеке данных, подключённой только к Worker.
- **GET через API**, требующий базы: request/response через MassTransit к Worker с конечным тайм-аутом и DTO-ответом. Никакого прямого `DbContext` из API. Для тяжёлых запросов используй асинхронный JobId/status. Другие read-модели возможны только после отдельного архитектурного решения.
- **Realtime** после реализации SignalR получает уже произошедшие события и доставляет уведомления авторизованным клиентам; не выполняет фоновые задания и не подключается к прикладной БД.
- **Contracts** содержит только межсервисные DTO (`...Requested`, `...Completed`, `...Failed`, query/response); не передавай EF entity, JWT, пароли или чувствительные данные. Не добавляй ссылку Worker/Realtime на сборку API.
- Для команды конкретному Worker предпочитай `Send` к **явно согласованному стабильному endpoint**; для факта/события — `Publish`. В коде нет ручного RabbitMQ `topic` exchange. Новые задачи должны учитывать outbox там, где требуется согласованность DB commit и публикации результата, дубли, идемпотентность, ограниченные retry, `_error` очереди и корреляцию по JobId.
- API возвращает `202 Accepted` + `JobId`, когда успешно поставил длительную задачу, **не** обещая, что Worker её уже завершил. Статус и гарантию durable acceptance проектируй отдельно. При проблемах RabbitMQ не отвечай ложным успехом. Перед изменениями обмена прочитай `docs/MESSAGING.md`, `docs/WORKER_DATABASE_BOUNDARY.md`, `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`.

### Старый Weather — не архитектурный образец

**Фактический код сегодня:** `Api/Features/Weather/Application/CreateForecast/Handler.cs` сначала вызывает API `IWeatherRepository`/`IUnitOfWork.SaveChangesAsync()` и сохраняет PostgreSQL **из API**, затем публикует `ForecastCreatedEvent`; `Worker/Features/Weather/ForecastCreatedConsumer.cs` только логирует событие. `Realtime` пока без SignalR. **Не копируй эту схему для новых фич.** Перенос Weather на Worker, запросы чтения через Worker и удаление API DB DI — отдельный рефакторинг с тестами и миграционным планом. `backend/Database` сейчас не включён в solution и не подключён к Worker.

## Структура и границы

- `backend/src/Api` — HTTP, MediatR, FluentValidation, постановка заданий и формирование ответов. См. `backend/src/Api/AGENTS.md`.
- `backend/src/Worker` — MassTransit consumers и **владелец БД для новых фич**. Пока реально только логирует Weather; см. `backend/src/Worker/AGENTS.md`.
- `backend/src/Realtime` — отдельный host с health endpoints, SignalR и RabbitMQ ещё не реализованы; см. `backend/src/Realtime/AGENTS.md`.
- `backend/Contracts` — общие message DTO, библиотека, **не** четвёртый микросервис; см. `backend/Contracts/AGENTS.md`.
- `backend/tests/Tests` — xUnit-тесты, см. `backend/tests/AGENTS.md`.
- `frontend` — статический демонстрационный клиент; `deploy/docker` и `docker-compose.yml` — контейнеры и инфраструктура.
- Сохраняй feature-first, локальные слои и DI. Не превращай каждый handler в микросервис, не создавай пустые слои и универсальные абстракции без необходимости. Не используй чужую БД напрямую.

## Команды из корня репозитория

Требуются .NET 10 SDK и Docker с Compose.

```powershell
dotnet restore backend/Kaban.sln
dotnet build backend/Kaban.sln --no-restore
dotnet test backend/Kaban.sln --no-build
```

Для **текущего учебного Weather** (legacy API DB):

```powershell
docker compose up -d --wait postgres rabbitmq
dotnet run --project backend/src/Api --launch-profile Api
```

- Текущие локальные адреса: API `http://localhost:5080/swagger`, PostgreSQL `localhost:55432`, RabbitMQ `localhost:5672`, management `localhost:15672`. В Compose-контейнерах: `postgres:5432`, `rabbitmq:5672`. Смотри реальные `ports` в `docker-compose.yml` (фронтенд сейчас опубликован на `8081`).
- `docker compose up -d --build` поднимает стек; не запускай одновременно локальные и Docker-хосты на одинаковых портах. `.env` читает Compose, а не локальный .NET host; переменные .NET имеют вид `ConnectionStrings__DefaultConnection`, `RabbitMq__Host` и т. д.
- Если меняешь Compose, выполняй `docker compose config --quiet`. Не утверждай, что отсутствующие CI/lint/health проверки настроены.

## Безопасность и завершение работы

- Не коммить секреты, токены и реальные строки подключения. Не логируй auth-заголовки или чувствительные данные. Для actor identity используй проверенные claims API, а Worker проводит собственную бизнес-проверку и должен доверять только согласованному источнику сообщений.
- Не удаляй PostgreSQL volume и не используй `docker compose down -v` для исправления подключения. Не применяй миграции к production в рамках локальной проверки. Не используйте demo credentials в production.
- Сохраняй пользовательские изменения; меняй только нужные файлы. Для кода выполняй сборку и тесты, для документации проверяй ссылки и соответствие реализации. В итоговом ответе указывай сделанное, выполненные проверки и ограничения.
- Если старая документация/README говорит «API владеет PostgreSQL», это описание **legacy Weather**, не разрешение добавлять новые операции с БД в API. При противоречии для новых фич приоритет имеет этот документ, `docs/WORKER_DATABASE_BOUNDARY.md` и сервисные `AGENTS.md`.
