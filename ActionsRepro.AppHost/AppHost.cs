var builder = DistributedApplication.CreateBuilder(args);

var defaultParam = new GenerateParameterDefault();

var password = builder.AddParameter("db-password", defaultParam, true);

#pragma warning disable ASPIREPROXYENDPOINTS001
var dbServer = builder.AddPostgres("postgres-server", port: 5432, password: password)
    .WithImageTag("18.1")
    .WithEndpointProxySupport(false)
#pragma warning restore ASPIREPROXYENDPOINTS001
    .WithLifetime(ContainerLifetime.Persistent);

dbServer.WithVolume(VolumeNameGenerator.Generate(dbServer, "data"),
            "/var/lib/postgresql/18/docker", false);

var database = dbServer.AddDatabase("db");

var pgConnectionString = builder.AddConnectionString(
    "database",
    ReferenceExpression.Create($"{database};Include Error Detail=true"));


var apiService = builder.AddProject<Projects.ActionsRepro_ApiService>("apiservice")
    .WithReference(pgConnectionString)
    .WaitFor(pgConnectionString)
    .WithHttpHealthCheck("/health");


builder.AddProject<Projects.ActionsRepro_Web>("webfrontend")
    .WithHttpHealthCheck("/health")
    .WaitFor(apiService)
    .WithReference(apiService);

builder.Build().Run();
