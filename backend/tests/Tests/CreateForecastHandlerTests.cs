using Kaban.Api.Common.Application;
using Kaban.Api.Features.Weather.Application.Abstractions;
using Kaban.Api.Features.Weather.Application.CreateForecast;
using Kaban.Api.Features.Weather.Domain;
using MassTransit;
using MassTransit.Util;
using Xunit;

namespace Kaban.Tests;

public sealed class CreateForecastHandlerTests
{
    [Fact]
    public async Task Handle_SavesThroughRepositoryAndUnitOfWork()
    {
        var repo = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var publisher = new FakePublishEndpoint();
        var handler = new CreateForecastHandler(repo, publisher, unitOfWork);

        var result = await handler.Handle(
            new CreateForecastCommand(new DateOnly(2026, 10, 19), 15, "Облачно"),
            CancellationToken.None);

        Assert.Equal("Облачно", result.Summary);
        Assert.Equal(result.Id, repo.Added?.Id);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public void CreateValidator_RejectsOutOfRangeTemperature()
    {
        var validator = new CreateForecastValidator();
        var result = validator.Validate(
            new CreateForecastCommand(new DateOnly(2026, 10, 19), 200, "Тепло"));
        Assert.False(result.IsValid);
    }

    private sealed class FakeRepository : IWeatherRepository
    {
        public WeatherForecast? Added { get; private set; }

        public Task<IReadOnlyList<WeatherForecast>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<WeatherForecast>>([]);

        public Task<WeatherForecast?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<WeatherForecast?>(null);

        public Task AddAsync(WeatherForecast forecast, CancellationToken ct)
        {
            Added = forecast;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakePublishEndpoint : IPublishEndpoint
    {
        public object? PublishedMessage { get; private set; }

        public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            PublishedMessage = message;
            return Task.CompletedTask;
        }

        public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe,
            CancellationToken cancellationToken = new()) where T : class => Task.CompletedTask;
       
        public Task Publish<T>(T message, IPipe<PublishContext> publishPipe,
            CancellationToken cancellationToken = new()) where T : class => Task.CompletedTask;

        public Task Publish(object message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish(object message, IPipe<PublishContext> publishPipe,
            CancellationToken cancellationToken = new()) => Task.CompletedTask;

        public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe,
            CancellationToken cancellationToken = new()) => Task.CompletedTask;

        public Task Publish<T>(object anonymousArguments, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe,
            CancellationToken cancellationToken = new()) where T : class => Task.CompletedTask;

        public Task Publish<T>(object values, IPipe<PublishContext> publishPipe,
            CancellationToken cancellationToken = new()) where T : class => Task.CompletedTask;

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new EmptyConnectHandle();
    }
}