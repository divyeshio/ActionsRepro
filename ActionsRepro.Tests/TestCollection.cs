using Aspire.Hosting;
using Meziantou.Extensions.Logging.Xunit.v3;
using Microsoft.Extensions.Logging;
using Projects;
using System.Diagnostics.CodeAnalysis;

[assembly: CaptureTrace]
namespace ActionsRepro.Tests;

[CollectionDefinition(nameof(TestCollection))]
public class TestCollection : ICollectionFixture<TestFixture>
{
}

public class TestFixture : IAsyncLifetime
{
    private DistributedApplication _app = null!;

    private readonly ITestOutputHelper testOutputHelper;

    public TestFixture(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    public HttpClient ApiClient { get; private set; } = null!;
    public HttpClient FrontendClient { get; private set; } = null!;

    [Experimental("EXTEXP0001")]
    public async ValueTask InitializeAsync()
    {
        try
        {
            var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<ActionsRepro_AppHost>(["DcpPublisher:RandomizePorts=false"], TestContext.Current.CancellationToken);

            builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.RemoveAllResilienceHandlers();
                clientBuilder.ConfigureHttpClient(configure => configure.Timeout = TimeSpan.FromSeconds(60));
            });

            builder.Services.AddLogging(configure =>
            {
                configure.AddProvider(new XUnitLoggerProvider(testOutputHelper, appendScope: false));
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Trace);
                // Override the logging filters from the app's configuration
                configure.AddFilter(builder.Environment.ApplicationName, LogLevel.Trace);
                configure.AddFilter("Aspire.", LogLevel.Trace);
            });

            _app = await builder.BuildAsync(TestContext.Current.CancellationToken);

            await _app.StartAsync(TestContext.Current.CancellationToken);

            var httpClient = _app.CreateHttpClient("apiservice");

            httpClient.BaseAddress = new Uri($"{_app.CreateHttpClient("apiservice").BaseAddress}api/");

            ApiClient = httpClient;
            FrontendClient = _app.CreateHttpClient("webfrontend");

            var apiTask = _app.ResourceNotifications.WaitForResourceAsync("apiservice");
            var frontendTask = _app.ResourceNotifications.WaitForResourceAsync("webfrontend");

            await Task.WhenAll(apiTask, frontendTask);
        }
        catch (Exception ex)
        {
            TestContext.Current.SendDiagnosticMessage(ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
    }
}