# Кабан — MediatR + Repository + DI, короткие имена файлов

Шаблон адаптирован после анализа **Kitiketov/Blueprint** и **Kitiketov/Boardify**. Здесь есть знакомые команде `IMediator`, `IRequest`, `IRequestHandler`, `FluentValidation`, `IWeatherRepository`, `IUnitOfWork`, DI через `AddScoped`, EF Core + PostgreSQL. Но все специфичные для Weather классы теперь находятся в **`Features/Weather`**, а не распределены между `Api`, `Client.Models` и `Infrastructure`.

Откройте в JetBrains Rider **`backend/Kaban.sln`**. Это **один solution** с тремя отдельными запускаемыми .NET-проектами: `Api`, `Worker`, `Realtime`. Есть также `Tests` — он не микросервис. Фронтенд физически отделён от бэкенда в `frontend/`, весь проект — монорепозиторий.

## Главное — где лежит код

```text
backend/src/Api/
├── Program.cs                       # Composition root; вызовы регистрации DI
├── Features/
│   ├── Weather/
│   │   ├── Presentation/
│   │   │   ├── WeatherController.cs
│   │   │   └── CreateForecastRequest.cs  # HTTP input DTO
│   │   ├── Application/
│   │   │   ├── Abstractions/
│   │   │   │   └── IWeatherRepository.cs
│   │   │   ├── DTO/
│   │   │   │   └── WeatherDto.cs      # Output DTO; не EF Entity
│   │   │   ├── CreateForecast/
│   │   │   │   ├── Command.cs
│   │   │   │   ├── Handler.cs
│   │   │   │   └── Validator.cs
│   │   │   ├── GetForecasts/
│   │   │   │   ├── Query.cs
│   │   │   │   └── Handler.cs
│   │   │   └── GetForecastById/
│   │   │       ├── Query.cs
│   │   │       └── Handler.cs
│   │   ├── Domain/
│   │   │   └── WeatherForecast.cs
│   │   └── Infrastructure/
│   │       ├── WeatherRepository.cs
│   │       ├── WeatherForecastConfiguration.cs
│   │       └── WeatherRegistration.cs  # Feature DI
│   ├── Users/                         # Пока пустая фича
│   └── Tasks/                         # Пока пустая фича
├── Common/
│   ├── Application/IUnitOfWork.cs     # общий UoW, не дублировать по фичам
│   ├── ApplicationRegistration.cs     # MediatR + FluentValidation pipeline
│   ├── Validation/ValidationBehavior.cs
│   └── Exceptions/ValidationExceptionHandler.cs
└── Infrastructure/Persistence/
    ├── KabanDbContext.cs              # один общий DbContext для API
    ├── EfUnitOfWork.cs
    └── PersistenceRegistration.cs     # подключения к PostgreSQL, DI UoW
```

`Worker` и `Realtime` имеют собственные `.csproj`, `Program.cs`, Dockerfile и `/health/live`, `/health/ready`. В них **нет** Weather, RabbitMQ и SignalR-сценариев. Сообщения, топики, consumers/publishers и real-time обновления оставлены команде для изучения. Реализован только обычный HTTP + DB сценарий в API.

## Почему короткие имена файлов, но полные имена классов

Поскольку папка операции уже содержит её имя, файлы **внутри этой папки** называются `Command.cs`, `Handler.cs`, `Validator.cs`, `Query.cs`. C#-типы остаются `CreateForecastCommand`, `CreateForecastHandler`, `GetForecastsQuery` и т. д. Это облегчает поиск, регистрацию и диагностику. Компилятор C# не требует совпадения имени файла и типа. В папке `Presentation` файл `CreateForecastRequest.cs` остаётся с полным названием, потому что в ней нет отдельного каталога `CreateForecast`, а в будущем возможны другие `*Request`.

## Чем DTO отличается от сущности

- `Presentation/CreateForecastRequest.cs` — HTTP **request DTO**, входной JSON. `WeatherController` преобразует его в `CreateForecastCommand`.
- `Application/CreateForecast/Command.cs` — команда MediatR, **не HTTP DTO**. Её принимает application handler.
- `Application/DTO/WeatherDto.cs` — **общий DTO результата**, используется `CreateForecast`, `GetForecasts`, `GetForecastById`. Application возвращает DTO, не зависит от HTTP слоя и не отдаёт EF-сущность напрямую. Если DTO понадобится только одной операции, лучше перенести его внутрь папки операции (например `Response.cs`).
- `Domain/WeatherForecast.cs` — **доменная сущность**, сохраняемая через EF Core; у неё есть состояние и правило `Create()`.

`Abstractions/IWeatherRepository.cs` — интерфейс, потребляемый обработчиками. `Infrastructure/WeatherRepository.cs` реализует его на EF Core. `Api/Infrastructure/Persistence/` — общая инфраструктура PostgreSQL; `KabanDbContext` и `EfUnitOfWork` не дублируются по каждой фиче. `EfUnitOfWork` — адаптер `IUnitOfWork` с вызовом `SaveChangesAsync`; называть его `UnitOfWorkRepository` необязательно.

## Как выполняется запрос

**Создание (`POST /api/weather`):** `WeatherController` принимает `CreateForecastRequest` → формирует `CreateForecastCommand` → `mediator.Send(...)` → `ValidationBehavior` вызывает `CreateForecastValidator` → `CreateForecastHandler` создаёт доменную сущность → `IWeatherRepository.AddAsync()` → `IUnitOfWork.SaveChangesAsync()` → `WeatherDto` возвращается как HTTP 201. Все файлы уникальные для Weather — внутри `Features/Weather`.

**Чтение (`GET /api/weather` и `GET /api/weather/{id}`):** контроллер вызывает MediatR query → handler → `IWeatherRepository` → EF Core / PostgreSQL → DTO → 200 (или 404 по ID).

### Почему `KabanDbContext` общий, а конфигурация таблицы — в Weather

Репозиторий `WeatherRepository` и `WeatherForecastConfiguration` принадлежат фиче. Сам `KabanDbContext` один для БД API; для каждой новой сущности в нём появляется `DbSet`, а настройки таблиц размещаются внутри инфраструктуры соответствующей фичи. `ApplyConfigurationsFromAssembly(...)` автоматически их подхватывает. Так не появляется новый DbContext для каждой HTTP-ручки и не возникают дублирующие классы с одинаковыми именами по слоям.

### Где интерфейсы и DTO?

- Интерфейс репозитория: `Weather/Application/Abstractions/IWeatherRepository.cs`. Реализация: `Weather/Infrastructure/WeatherRepository.cs`. Handler знает только об интерфейсе.
- `CreateForecastRequest` — HTTP DTO: `Weather/Presentation/`.
- `WeatherDto` — выходной DTO приложения: `Weather/Application/DTO/`. Контроллер может вернуть его клиенту, а handler не зависит от `Presentation`.
- `IUnitOfWork` — сквозной контракт API в `Common/`, поэтому не дублируется в `Weather`, `Users` и `Tasks`.

Это **feature-first с локальными слоями**, а не строгий DDD и не отдельная сборка под каждую фичу. EF Core Entity и DTO — разные вещи, и доменная модель не зависит от MediatR или MVC.

## Запуск

Нужны .NET 10 SDK и запущенный Docker Desktop (Linux containers).

### Из Rider — приложение локально, PostgreSQL в Docker

1. Из корня репозитория запустите только БД и дождитесь её готовности:

   ```bash
   docker compose up -d --wait postgres
   ```

2. Откройте `backend/Kaban.sln`. Выберите профиль **Api** из `backend/src/Api/Properties/launchSettings.json` и нажмите Run/Debug. Если solution уже был открыт и сохранилась старая конфигурация `.NET Project`, создайте конфигурацию **.NET Launch Settings Profile**, выберите проект Api и профиль Api.
3. Откройте `http://localhost:5080/swagger` или `http://localhost:5080/api/weather`.

Профили задают `Development` и отдельные порты: Api — 5080, Worker — 5081, Realtime — 5082. Worker и Realtime для запросов Weather запускать не нужно. Эквивалент запуска API из терминала:

```bash
dotnet run --project backend/src/Api --launch-profile Api
```

PostgreSQL доступен с компьютера по `localhost:55432`, БД и пользователь — `kaban`, локальный пароль — `local_dev_password`. Эти значения уже согласованы с `appsettings.json`; копировать `.env.example` для стандартного запуска не требуется. Внутри Docker API использует `postgres:5432`.

Файл `.env` читает Docker Compose, но не локальное .NET-приложение. Если меняете `POSTGRES_PASSWORD`, задайте соответствующий пароль и в `ConnectionStrings__DefaultConnection` в конфигурации запуска Rider. Изменение `POSTGRES_PASSWORD` не меняет пароль в уже созданном томе PostgreSQL.

### Всё в Docker

Из корня репозитория:

```bash
dotnet restore backend/Kaban.sln
dotnet build backend/Kaban.sln
dotnet test backend/Kaban.sln
docker compose up -d --build
```

Перед запуском всего стека остановите локальные Api/Worker/Realtime в Rider, чтобы освободить порты. При обратном переходе выполните `docker compose stop frontend api worker realtime`, затем запустите БД и приложение по инструкции выше.

После запуска всего стека:

- `http://localhost:8080` — статическая демонстрационная страница (просто HTTP-клиент).
- `http://localhost:5080/api/weather` — GET/POST Weather; `GET /api/weather/{id}`.
- `http://localhost:5081/health/live` — Worker (пустой host).
- `http://localhost:5082/health/live` — Realtime (пустой host).

```bash
curl -X POST http://localhost:5080/api/weather \
  -H 'Content-Type: application/json' \
  -d '{"date":"2026-10-19","temperatureC":17,"summary":"Облачно"}'
curl http://localhost:5080/api/weather
```

База для учебного Weather создаётся автоматически **только в Development** через `EnsureCreated`. При переносе на реальную доменную модель следует перейти на EF Core migrations, убрать инициализацию схемы из `Program.cs` и раздельно управлять миграциями. Docker Compose запускает PostgreSQL 16 и три host-проекта; связи между ними нет. При подключении к БД из Rider используйте `ConnectionStrings__DefaultConnection` или `appsettings.json` (локальный пример). Секреты для production вне репозитория.

У MediatR 14 могут появляться информационные предупреждения о лицензировании; перед развёртыванием проверьте условия использования выбранной версии.

### Если возникает ошибка БД

- `Failed to connect to 127.0.0.1:55432`: выполните `docker compose up -d --wait postgres`. В `docker compose ps` должен отображаться проброс `127.0.0.1:55432->5432/tcp`.
- `relation ... does not exist`: проверьте, что выбран профиль Api из `launchSettings.json` (окружение `Development`), затем перезапустите API.
- `password authentication failed`: проверьте строку подключения и пароль существующей БД. Не удаляйте том для исправления пароля: он содержит данные.
- Порт 5080 уже занят: остановите API в Docker или другой локальный экземпляр.
