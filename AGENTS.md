# Инструкции для агентов — Kaban

## Область действия

- Применяй этот файл ко всему репозиторию; вложенные AGENTS.md уточняют правила своей папки. Прямые инструкции пользователя имеют приоритет.
- Перед изменениями читай README.md, docs/ARCHITECTURE.md и docs/FILE_NAMING_AND_DTO.md. Сверяй документацию с кодом и обновляй её при изменении архитектуры.
- Стек: .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core/Npgsql, PostgreSQL. Версии проверяй в .csproj и backend/Directory.Build.props; не обновляй пакеты попутно.

## Структура и границы

- backend/src/Api — HTTP API, прикладные операции и принадлежащая API БД. Следуй backend/src/Api/AGENTS.md.
- backend/src/Worker — самостоятельный host с MassTransit consumer: пока получает и логирует ForecastCreatedEvent; см. backend/src/Worker/AGENTS.md.
- backend/src/Realtime — отдельный host только с health endpoints, пока без SignalR и RabbitMQ; см. backend/src/Realtime/AGENTS.md.
- backend/tests/Tests — xUnit-тесты API. Следуй backend/tests/AGENTS.md.
- frontend — статический демонстрационный HTTP-клиент, без npm toolchain и Playwright.
- deploy/docker, deploy/nginx.conf и docker-compose.yml — контейнеры и локальный запуск.
- Сохраняй feature-first и локальные слои. Не добавляй ссылки Worker/Realtime на сборку API, его EF-сущности и DbContext. Сервис владеет своими данными; не читай и не изменяй чужие таблицы напрямую.
- Не превращай каждый handler в микросервис. Новые границы обосновывай ответственностью и владением данными. Не создавай пустые слои и универсальные абстракции без конкретной задачи.

## Команды из корня репозитория

Требуются .NET 10 SDK; для контейнеров — Docker с Compose.

```powershell
dotnet restore backend/Kaban.sln
dotnet build backend/Kaban.sln --no-restore
dotnet test backend/Kaban.sln --no-build
```

Локальный API с PostgreSQL:

```powershell
docker compose up -d --wait postgres
dotnet run --project backend/src/Api --launch-profile Api
```

- Swagger: http://localhost:5080/swagger; API: http://localhost:5080/api/weather.
- PostgreSQL с хоста: localhost:55432; из контейнеров: postgres:5432. .env читает Compose, не локальный .NET host; строку подключения переопределяй через ConnectionStrings__DefaultConnection.
- Полный стек: `docker compose up -d --build`. Не занимай одновременно порты локальными и контейнерными процессами: API 5080, Worker 5081, Realtime 5082, frontend 8080.
- При изменении Compose выполняй `docker compose config --quiet`. Отдельные lint/format-команды и CI workflow пока не настроены; не сообщай о прохождении отсутствующих проверок.


## Безопасность и завершение работы

- Сохраняй пользовательские изменения и ограничивай diff задачей. Не коммить секреты, токены и реальные строки подключения; не логируй auth-заголовки и чувствительные данные.
- Не используй демонстрационные настройки для production. Не удаляй PostgreSQL volume и не запускай `docker compose down -v` ради исправления подключения.
- Не применяй миграции к production в рамках локальной проверки.
- Для кода выполняй сборку и подходящие тесты. Для документации проверяй пути, команды и соответствие коду; новые тесты только ради Markdown не нужны.
- В результате указывай изменения, фактически выполненные проверки и ограничения. Обновляй инструкции при изменении структуры и команд.

## Правила агента для RabbitMQ (актуальное состояние)

- При задачах **«отправь из API в Worker»**, «добавь очередь», «создай consumer», «доставь результат через Realtime» **сначала** прочитай `docs/MESSAGING.md` и `.agents/skills/kaban-rabbitmq-workflow/SKILL.md`; также применяй вложенные `AGENTS.md` соответствующих проектов и `backend/Contracts/AGENTS.md`.
- **Сейчас:** API `CreateForecastHandler` сохраняет Weather в PostgreSQL и после коммита публикует `ForecastCreatedEvent` через MassTransit. Worker `ForecastCreatedConsumer` только логирует событие. Это **не** передача создания прогноза на Worker. Realtime пока не подписывается на брокер и не содержит SignalR Hub.
- **Для новой фоновой работы:** контроллер API принимает HTTP-запрос → MediatR handler валидирует и ставит работу в очередь через согласованный контракт `...Requested` → Worker consumer действительно выполняет её → сохраняет эффект и, при необходимости, публикует `...Completed`/`...Failed`. При длительной работе API отвечает 202 Accepted/JobId, а не ждёт выполнения.
- Для задания одному исполнителю предпочитай **адресное Send** к настроенному endpoint, для широковещательного факта — **Publish**. Не придумывай существующие `topic`/routing keys: текущий `ConfigureEndpoints` использует соглашения MassTransit и fanout exchange по типам, не ручной RabbitMQ topic exchange.
- Межсервисные message DTO размещай **только в `backend/Contracts`**. Не передавай EF сущности, JWT или секреты. Не добавляй ссылок Worker/Realtime на API и его DbContext.
- При доступе к данным реши границы владения, учти **outbox**, повторные доставки, идемпотентность, bounded retry и `_error` очереди. В действующем Weather-примере **пока нет** outbox и гарантий восстановления после отказа между DB commit и Publish.
- Меняя код обмена, одновременно обновляй `docs/MESSAGING.md` и нужные `AGENTS.md`. Не называй несуществующие функции готовыми.
