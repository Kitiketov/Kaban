# Weather как пример привычного команде подхода

1. Presentation: `WeatherController` принимает HTTP DTO `CreateForecastRequest`, строит команду и вызывает `IMediator.Send`.
2. Application: `CreateForecastCommand + Handler + Validator`, `GetForecastsQuery`, `GetForecastByIdQuery`, интерфейс `IWeatherRepository`, `WeatherDto`.
3. Domain: `WeatherForecast` — сущность и минимальные инварианты.
4. Infrastructure: EF Core реализация `WeatherRepository`, конфигурация таблицы и регистрация `IWeatherRepository` в DI.

Два общих куска инфраструктуры не дублируем между фичами: `Api/Infrastructure/Persistence/KabanDbContext` и `Api/Common/Application/IUnitOfWork`. Это **одна БД и один DbContext на API-сервис**, не отдельный DbContext на endpoint.

MediatR и FluentValidation зарегистрированы единожды в `Common/ApplicationRegistration.cs`. Создание сохраняется через `IUnitOfWork`, чтение через `IWeatherRepository`. В Worker и Realtime пример Weather не копируем.
