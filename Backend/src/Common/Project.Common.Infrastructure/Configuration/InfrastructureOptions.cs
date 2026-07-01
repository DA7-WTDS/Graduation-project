using MassTransit;
using Microsoft.Extensions.Logging;

namespace Project.Common.Infrastructure.Configuration;

public sealed class InfrastructureOptions
{
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public RabbitMqConfiguration RabbitMq { get; set; } = new();
    public OpenTelemetryOptions OpenTelemetryOptions { get; set; } = new();
    public ILoggingBuilder LoggingBuilder { get; set; }
    public Action<IRegistrationConfigurator>[] ModuleConfigureConsumers { get; set; } = [];

    /// <summary>
    /// When true, runs without external Redis/RabbitMQ: caching falls back to an
    /// in-memory distributed cache and MassTransit uses the in-memory transport.
    /// Intended for single-instance demo/showcase hosting on free tiers.
    /// </summary>
    public bool DemoMode { get; set; }
}
