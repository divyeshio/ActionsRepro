var builder = DistributedApplication.CreateBuilder(args);


#pragma warning disable ASPIREPROXYENDPOINTS001
var dbServer = builder.AddPostgres("postgres-server", port: 5432)
    .WithImageTag("18.0")
    .WithEndpointProxySupport(false)
#pragma warning restore ASPIREPROXYENDPOINTS001
    .WithLifetime(ContainerLifetime.Persistent)
    ;

dbServer.WithVolume(VolumeNameGenerator.Generate(dbServer, "data"),
            "/var/lib/postgresql/18/docker", false);

var database = dbServer.AddDatabase("db");

var pgConnectionString = builder.AddConnectionString(
    "database",
    ReferenceExpression.Create($"{database};Include Error Detail=true"));

var apiService = builder.AddProject<Projects.ActionsRepro_ApiService>("apiservice")
    .WaitFor(pgConnectionString)
    .WithReference(pgConnectionString)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.ActionsRepro_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
