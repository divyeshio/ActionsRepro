using Aspire.Hosting;
using Meziantou.Extensions.Logging.Xunit.v3;
using Microsoft.Extensions.Logging;
using Npgsql;
using Projects;
using Respawn;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace ActionsRepro.Tests;

public class TestFixture : IAsyncLifetime
{
    private DistributedApplication _app = null!;

    private DbConnection? _dbConnection;
    private Respawner? _respawner;

    public HttpClient ApiClient { get; private set; } = null!;

    [Experimental("EXTEXP0001")]
    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<ActionsRepro_AppHost>(["DcpPublisher:RandomizePorts=false"], TestContext.Current.CancellationToken);

        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.RemoveAllResilienceHandlers();
            clientBuilder.ConfigureHttpClient(configure => configure.Timeout = TimeSpan.FromSeconds(30));
        });

        builder.Services.AddLogging(configure =>
        {
            configure.ClearProviders();
            configure.AddProvider(new XUnitLoggerProvider(TestContext.Current.TestOutputHelper));
            configure.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            configure.AddFilter(builder.Environment.ApplicationName, LogLevel.Debug);
            configure.AddFilter("Aspire.", LogLevel.Debug);
        });

        _app = await builder.BuildAsync(TestContext.Current.CancellationToken);

        await _app.StartAsync(TestContext.Current.CancellationToken);

        var httpClient = _app.CreateHttpClient("apiservice");

        httpClient.BaseAddress = new Uri($"{_app.CreateHttpClient("apiservice").BaseAddress}api/");

        ApiClient = httpClient;

        await _app.ResourceNotifications.WaitForResourceAsync("database");
        await InitializeRespawner();

        var apiTask = _app.ResourceNotifications.WaitForResourceAsync("apiservice");

        await apiTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
    }

    public async ValueTask ResetDatabaseAsync()
    {
        if (_respawner == null)
            return;

        var connection = new NpgsqlConnection(await _app.GetConnectionStringAsync("database"));
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await _respawner.ResetAsync(connection);
    }

    private async Task InitializeRespawner()
    {
        _dbConnection = new NpgsqlConnection(await _app.GetConnectionStringAsync("database"));
        await _dbConnection.OpenAsync(TestContext.Current.CancellationToken);
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = ["__EFMigrationsHistory"],
            WithReseed = true
        });
    }
}