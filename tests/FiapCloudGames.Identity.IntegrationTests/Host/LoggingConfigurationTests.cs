using FiapCloudGames.Identity.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.IntegrationTests.Host;

public sealed class LoggingConfigurationTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Configurar logging - deve incluir correlação e console estruturado")]
    public void Logging_ShouldIncludeCorrelationAndStructuredConsole()
    {
        var factoryOptions = factory.Services
            .GetRequiredService<IOptions<LoggerFactoryOptions>>()
            .Value;
        var consoleOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>()
            .CurrentValue;

        Assert.True(factoryOptions.ActivityTrackingOptions.HasFlag(
            ActivityTrackingOptions.TraceId));
        Assert.True(factoryOptions.ActivityTrackingOptions.HasFlag(
            ActivityTrackingOptions.SpanId));
        Assert.True(factoryOptions.ActivityTrackingOptions.HasFlag(
            ActivityTrackingOptions.ParentId));
        Assert.True(consoleOptions.IncludeScopes);
        Assert.True(consoleOptions.SingleLine);
        Assert.Equal("yyyy-MM-dd HH:mm:ss.fff zzz ", consoleOptions.TimestampFormat);
    }
}
