# Auth и Users

Реализованы пять ручек со скриншота. PUT /api/users/me остаётся вне текущего объёма.

| Метод и путь | Вход | Успех |
| --- | --- | --- |
| POST /api/users | JSON email, name, password | 201, без тела |
| POST /api/auth/sessions | JSON email, password | 200: accessToken, refreshToken |
| POST /api/auth/tokens/refresh | Authorization: Bearer <refreshToken>, без тела | 200: accessToken, refreshToken |
| GET /api/users/me | Authorization: Bearer <accessToken> | 200: id (int), name, email |
| GET /api/users?query=name&limit=10 | access token, query (1–320 символов), limit (1–100, default 10) | 200: массив id, name, email |

Email нормализуется Trim + ToLowerInvariant; имя обрезается по краям, максимум 64 символа.
Пароль регистрации — 8–1024 символа. Поиск — регистронезависимая подстрока имени/email, с устойчивой сортировкой name/id.
Авторизованный пользователь может искать других пользователей; Worker проверяет существование actor.
Id берётся из проверенного claim user_id, никогда из тела HTTP.

## Разделение ответственности

Controller → MediatR → зарегистрированный FluentValidation → IRequestClient → RabbitMQ →
Worker/EF Core/PostgreSQL → RespondAsync → API/HTTP. Новые Auth/Users не используют API DbContext.
После merge origin/main (7d9c04e) старый Weather удалён; сохранены Boards/Tasks и OpenTelemetry.

По прямому решению пользователя пароль хешируется и проверяется в API; Worker хранит хеш.
Для этого внутренние RegisterUserRequested/CredentialsFound передают только солёный хеш:
это согласованное исключение к общей формулировке «без секретов» в инструкциях.
Исходный пароль и JWT в MQ не передаются. Хеши не возвращаются в HTTP, не логируются;
доступ к очередям, replies, _error и резервным копиям брокера должен быть ограничен внутренними сервисами.

За основу HTTP auth взят [UnitHack](https://github.com/prlzrakk/UnitHack/tree/5b5d96499edc41ca25e2de57b122db70b537569a/Infrastructure/Security):
Bearer refresh без тела, claim типа токена, JWT в API. Здесь используется стандартный ASP.NET Identity
PasswordHasher (PBKDF2, 210000 итераций), а не BCrypt из примера.
JWT HS256 имеет issuer/audience, обязательный срок, user_id, token_type и jti.
Обычные ручки принимают только access, refresh — только refresh.
Сроки по умолчанию: access 15 минут, refresh 7 дней. Non-expiring tokens отсутствуют.
Проверка соответствует [параметрам JWT Bearer ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

Refresh, как в примере, stateless: выдача новой пары НЕ отзывает старый refresh; он действует до истечения срока.
Сессии, logout, отзыв/одноразовая ротация refresh и смена пароля в текущий объём не входят.
На вход/регистрацию/refresh установлен локальный лимит 20 запросов в минуту на IP.
При нескольких API-репликах нужен общий limiter на входном proxy.

## Надёжность

API ждёт ответ до Messaging:RequestTimeoutSeconds (default 30, допустимо 1–120) и передаёт CancellationToken.
HTTP-клиент и reverse proxy должны иметь больший бюджет, например 40/60 секунд для default 30.
Default MassTransit request publish обслуживается одним логическим consumer для каждого типа;
ConfigureEndpoints создаёт очереди Worker. Дополнительных публичных событий нет.

По решению пользователя регистрация не использует ключи повторов, отпечатки или таблицу операций.
Worker сохраняет только users и отвечает после commit. Уникальный индекс email и транзакционная
блокировка по нормализованному email защищают от конкурентных регистраций. Занятый email возвращает 409.
Timeout не гарантирует отмену Worker: после 503 можно проверить результат входом.
Повтор сообщения или HTTP-запроса после успешного сохранения также возвращает 409, а не прежний успех.
Worker делает максимум два технических retry с интервалом 200 мс; далее fault и _error.
HTTP: 400 validation, 401 неверные credentials, 403 неверный тип JWT, 404 отсутствующий пользователь,
409 конфликт, 429 rate limit, 502 Worker fault, 503 timeout. Внутренние исключения не входят в ProblemDetails.

## Запуск и миграция

В Development доступен `/swagger`. Получите пару токенов через `POST /api/auth/sessions`,
нажмите **Authorize** и вставьте `accessToken` без префикса `Bearer`.
Swagger сам добавляет заголовок Authorization к защищённым методам.
Для `POST /api/auth/tokens/refresh` замените значение в Authorize на `refreshToken`,
а после обновления верните новый `accessToken`. Вход и регистрация доступны без токена.

Auth:SigningKey обязателен: случайный секрет минимум 32 байта, из env/secret store.
В Compose это KABAN_AUTH_SIGNING_KEY; для локального API — Auth__SigningKey.
Секрет не хранится в appsettings или git. Не менять его при каждом рестарте.

Worker получает ConnectionStrings__DefaultConnection; Compose уже передаёт postgres:5432.
Для запуска Worker на хосте задайте адрес локальной dev-БД и credentials через окружение.
Схема не применяется автоматически при запуске сервисов.

Остаётся базовая миграция Initail из main. Не вошедшие в main миграции таблицы операций удалены; модель регистрации использует существующую таблицу users.
На НОВОЙ пустой development-БД, после задания ConnectionStrings__DefaultConnection:

```powershell
dotnet ef database update --project backend/src/Worker
```

Для существующей БД с таблицами Worker, но без истории миграций, сначала требуется сверка схемы и migration baseline;
не выполнять начальную миграцию Initail вслепую и не удалять данные. Production-миграции здесь не запускались.

## Проверки

```powershell
dotnet restore backend/Kaban.sln
dotnet build backend/Kaban.sln
dotnet test backend/Kaban.sln
docker compose config --quiet
```

AuthEndpointTests запускают настоящий HTTP pipeline с MassTransit in-memory и тестовым consumer:
проверяют JWT policies, подпись, validation до MQ, ожидание ответа и mapping ошибок.
Это не замена проверки настоящего Worker и PostgreSQL.

WorkerIntegrationTests требуют KABAN_TEST_POSTGRES (подключение к изолированному тестовому PostgreSQL,
роль с CREATEDB) и KABAN_TEST_RABBITMQ (amqp URI отдельного тестового брокера/vhost).
Тест создаёт отдельную случайно названную БД kaban_test_* и временную очередь;
удаляет только созданную им БД. Проверяет миграцию, commit, конкурентные дубли, конфликт,
тайм-аут под DB lock с поздним успехом и повтором, настоящий DB fault.
Без обеих переменных он явно пропускается. Полный запуск отдельных процессов API + Worker +
RabbitMQ + PostgreSQL дополнительно обязателен перед выпуском.

В текущей среде Docker Desktop падает при инициализации dockerInference, поэтому внешняя
интеграция пока не подтверждена.
