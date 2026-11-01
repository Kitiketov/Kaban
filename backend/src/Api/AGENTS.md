# Правила разработки API

Применяй к backend/src/Api/** вместе с корневым AGENTS.md. Рабочий пример — Features/Weather; Users и Tasks пока содержат только заготовки папок.

## Поток запроса и зависимости

HTTP request → Controller → mediator.Send(Command/Query) → ValidationBehavior → Handler → Repository / IUnitOfWork → EF Core → PostgreSQL.

- Presentation: HTTP DTO, маршруты, авторизация, преобразование запроса в command/query и результата в HTTP-ответ. Не размещай здесь бизнес-логику, SQL и SaveChanges.
- Application: use cases, команды/запросы MediatR, handlers, validators, output DTO и интерфейсы репозиториев. Не импортируй MVC, HttpContext, EF Core и конкретную Infrastructure в handlers.
- Domain: сущности и инварианты, без зависимостей от HTTP, MediatR, FluentValidation, EF Core и RabbitMQ. Mapping хранения оставляй в Infrastructure.
- Infrastructure внутри фичи: реализации репозиториев, EF-конфигурации, регистрация зависимостей фичи.
- Api/Infrastructure/Persistence: общий для API KabanDbContext, подключение БД и EfUnitOfWork. Common/Application/IUnitOfWork — прикладной контракт сохранения.
- Это логические слои одной сборки. Соблюдай границы даже там, где компилятор допускает обратную зависимость. Не вызывай controller/handler другой фичи напрямую и не связывай фичи через чужие конкретные репозитории.

## Размещение и стиль

Создавай только необходимые файлы по существующей схеме:

```text
Features/<Feature>/
  Presentation/<Operation>Request.cs
  Presentation/<Feature>Controller.cs
  Application/<Operation>/Command.cs или Query.cs
  Application/<Operation>/Handler.cs
  Application/<Operation>/Validator.cs
  Application/DTO/<Name>Dto.cs
  Application/Abstractions/I<Name>Repository.cs
  Domain/<Entity>.cs
  Infrastructure/<Name>Repository.cs
  Infrastructure/<Entity>Configuration.cs
  Infrastructure/<Feature>Registration.cs
```

- Сохраняй короткие имена файлов Command.cs, Handler.cs, Validator.cs; типы называй явно: CreateForecastCommand, CreateForecastHandler, CreateForecastValidator.
- Namespace отражает путь: Kaban.Api.Features.<Feature>.Application.<Operation>. Используй file-scoped namespaces, четыре пробела, nullable-контекст и существующий стиль constructor injection.
- Используй records для неизменяемых сообщений/DTO, sealed для типов без необходимости наследования. Не добавляй интерфейс для каждого класса.
- HTTP request DTO хранится в Presentation; command/query — в Application. Не возвращай EF/доменные сущности по HTTP. Общий output DTO нескольких операций размещай в Application/DTO; отдельный внешний DTO создавай при реальном различии контрактов.

## MediatR и FluentValidation

- Use case представляет IRequest<TResponse> и IRequestHandler<TRequest, TResponse>. Контроллер вызывает mediator.Send(..., ct); command меняет состояние, query читает без скрытых записей.
- Проверки формы входа и допустимых значений размещай в AbstractValidator<TRequest> рядом с операцией. Проверяй command/query, чтобы валидация не зависела от HTTP.
- Используй Common/Validation/ValidationBehavior; не дублируй ручной вызов validators в контроллерах/handlers. Для асинхронных правил используй ValidateAsync и передавай cancellation token.
- Защищай инварианты также в Domain: создание в обход pipeline не должно нарушать бизнес-правила. Проверки прав, существования и конкурентных изменений выполняй в соответствующем use case/хранилище; предварительная валидация не заменяет ограничения БД.
- Не запускай параллельно операции с одним scoped DbContext, включая validators.
- AddApplication() сканирует сборку API для handlers/validators и регистрирует behavior. При удалении Weather замени marker CreateForecastValidator для сканирования на сохраняемый тип сборки.

## Хранение и DI

- Handler зависит от интерфейсов репозитория и IUnitOfWork; EF-запросы остаются в Infrastructure. Репозиторий изменяет отслеживаемое состояние, handler завершает операцию через SaveChangesAsync(ct). Не сохраняй независимо в каждом методе репозитория.
- Регистрируй репозитории/UnitOfWork как scoped, согласованно с DbContext. Подключай новый модуль через Add<Feature>Feature() в Program.cs.
- IEntityTypeConfiguration<T> размещай рядом с фичей: KabanDbContext уже применяет конфигурации своей сборки. Добавляй ограничения, индексы и правила удаления под сценарий.
- Для read-only запросов используй AsNoTracking, явную сортировку и ограничение выборки/пагинацию. Не возвращай IQueryable из Infrastructure. Для update обеспечь tracking или явное обновление; не копируй read-only загрузку Weather без проверки.
- Передавай CancellationToken по всей цепочке I/O. Не используй .Result, .Wait() и fire-and-forget для обязательных записей/публикаций.
- Сейчас схема Weather создаётся через EnsureCreatedAsync только в Development. Перед реальной доменной схемой подготовь EF migrations, план перехода существующей БД и убери EnsureCreated; не смешивай механизмы и не удаляй данные ради перехода.

## Интеграция новой модели БД

- Статус предложенной библиотеки backend/Database смотри в корневом AGENTS.md. Пока она не интегрирована, KabanDbContext остаётся действующим контекстом API; не описывай новую модель как уже работающую.
- При переносе сущностей сохраняй разделение Domain и EF mapping. Если mapping остаётся в отдельной сборке, явно сканируй её: текущий ApplyConfigurationsFromAssembly сканирует только сборку своего контекста.
- Сохраняй одну границу commit для всего use case. Не сочетай независимые commit в RecordWriter с IUnitOfWork, если несколько записей должны быть атомарны. Явную транзакцию добавляй для нужного сценария, а не для каждого одиночного SaveChanges.
- Update должен изменять найденную сущность и возвращать актуальный результат; AddAsync предназначен для вставки. Разрешай изменение конкретных полей, защищай ключи и поля владения от массового присваивания.
- Не превращай ошибки БД и отмену в null/false, неотличимые от отсутствия записи. Сохраняй причину ошибки, различай not-found, конфликт ограничения и инфраструктурный сбой; отмену передавай вызывающему коду.
- При реализации модели из PR обеспечь принадлежность роли той же команде, что и UsersTeams.TeamId; отдельные FK на TeamId и RoleId этого не гарантируют. Закрепляй инвариант подходящим ограничением БД и проверкой use case.
- Для уникальных позиций (BoardId, BoardOrder) и (BoardColumnId, TaskOrder) проектируй атомарное перемещение/переупорядочивание с учётом временных коллизий и конкурентных запросов. Явно проверяй правила удаления связанных записей.
- При использовании PostgreSQL enum task_priority проверяй согласованность CLR/provider mapping, модели и миграций на настоящем PostgreSQL; одной декларации типа столбца недостаточно для проверки round-trip.
- Не выдавай HashedPassword через DTO, логи или события. Перед уникальностью Email определи единое правило нормализации для регистрации и поиска; не считай обычный уникальный индекс автоматически регистронезависимым.

## HTTP и безопасность

- Сохраняй маршруты/контракты, если задача не требует изменения. Создание возвращает 201 с Location; отсутствие ресурса — 404; FluentValidation — 400 ValidationProblemDetails через ValidationExceptionHandler.
- Для новых ожидаемых ошибок определяй явное отображение, например конфликт состояния в 409. Не маскируй все исключения под 400/200 и не раскрывай stack trace, SQL и секреты клиенту.
- Авторизации сейчас нет. При реализации Users/Tasks добавь проверку личности и прав на конкретный ресурс. Не доверяй UserId/OwnerId из тела; получай текущего пользователя из проверенного контекста через прикладную абстракцию.
- Не отправляй RabbitMQ-сообщения прямо из controller или доменной сущности. При реализации messaging отдельно определи контракты и согласованность публикации с commit БД в docs/ARCHITECTURE.md.

## Проверки из корня репозитория

```powershell
dotnet build backend/Kaban.sln
dotnet test backend/tests/Tests/Tests.csproj
dotnet test backend/tests/Tests/Tests.csproj --filter FullyQualifiedName~CreateForecastHandlerTests
```

- Последняя команда проверяет пример Weather; для других операций выбирай соответствующие тесты. Правила тестов находятся в backend/tests/AGENTS.md.
- При изменении HTTP проверь успешный ответ, невалидный ввод и отсутствие ресурса; для авторизации — запрет доступа. EF-поведение проверяй на изолированном PostgreSQL: unit-тесты не проверяют SQL/constraints.
- Сейчас есть только unit-тесты. Прямой вызов handler не проверяет DI, middleware и validation pipeline; для их изменений нужна отдельная интеграционная проверка.
