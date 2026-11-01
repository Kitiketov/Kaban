# Инструкции для агентов — Kaban

## Область действия

- Применяй этот файл ко всему репозиторию; вложенные AGENTS.md уточняют правила своей папки. Прямые инструкции пользователя имеют приоритет.
- Перед изменениями читай README.md, docs/ARCHITECTURE.md и docs/FILE_NAMING_AND_DTO.md. Сверяй документацию с кодом и обновляй её при изменении архитектуры.
- Стек: .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core/Npgsql, PostgreSQL. Версии проверяй в .csproj и backend/Directory.Build.props; не обновляй пакеты попутно.

## Структура и границы

- backend/src/Api — HTTP API, прикладные операции и принадлежащая API БД. Следуй backend/src/Api/AGENTS.md.
- backend/src/Worker и backend/src/Realtime — самостоятельные хосты, пока только health endpoints. RabbitMQ, consumers, SignalR и авторизация ещё не реализованы.
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
