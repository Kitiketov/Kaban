# Правила имён и размещения DTO — Kaban

## 1. Не дублируем название операции в имени файла

`Application/CreateForecast/{Command,Handler,Validator}.cs`; `Application/GetForecasts/{Query,Handler}.cs`; `Application/GetForecastById/{Query,Handler}.cs`.

Сохраняем имена **типов** `CreateForecastCommand` и `CreateForecastHandler`; имя файла не определяет имя типа в C#. При таком подходе `namespace Kaban.Api.Features.Weather.Application.CreateForecast` используется без конфликтов, а классы остаются понятными в DI/логах/IDE.

## 2. Кто за что отвечает

| Что | Пример | Расположение | Обязанность |
|---|---|---|---|
| HTTP request DTO | `CreateForecastRequest` | `Presentation/` | Десериализация входного JSON |
| Command/Query | `CreateForecastCommand`, `GetForecastsQuery` | `Application/CreateForecast/`, `Application/GetForecasts/` | Запрос на выполнение use case в MediatR |
| Общий output DTO | `WeatherDto` | `Application/DTO/` | Возвращаемые данные нескольких use case |
| Доменная сущность | `WeatherForecast` | `Domain/` | Состояние и правила создания прогноза |
| Интерфейс репозитория | `IWeatherRepository` | `Application/Abstractions/` | Контракт чтения/записи для use case |
| Реализация репозитория | `WeatherRepository` | `Infrastructure/` фичи | EF Core запросы |
| Общая Persistence | `KabanDbContext`, `EfUnitOfWork` | `Api/Infrastructure/Persistence/` | Подключение БД, общий SaveChanges |

Общий `WeatherDto` **не** доменная сущность. Он остаётся в `Application/DTO`, поскольку используется тремя обработчиками; поэтому нет смысла дублировать DTO для каждой операции. Не импортируйте MVC-тип в application handler, а контроллер может возвращать application DTO как HTTP-ответ, если отдельный внешний контракт не нужен.

## 3. Куда класть файлы новых фич

- Внутри фичи можно оставить четыре папки `Presentation`, `Application`, `Domain`, `Infrastructure`.
- В `Application` группировать файлы по операциям, с короткими именами файлов.
- Не создавать DTO, репозиторий или интерфейс, если операция реально в них не нуждается.
- Общие классы `KabanDbContext`, `IUnitOfWork` и регистрация MediatR остаются вне фич. Это не отменяет feature-first: специфичная Weather реализация лежит в Weather.
- Worker/Realtime остаются пустыми хостами; обмен сообщениями намеренно не реализован.

## Проверка

Изменены исключительно имена семи `.cs`-файлов и документация. Объявления типов, namespaces, `using`, DI, MediatR, логика БД, `.csproj` оставлены без изменений. В SDK-style `.csproj` файлы `.cs` включаются автоматически (`**/*.cs`). Сборка `dotnet build` и тесты в этой среде не выполнялись.
