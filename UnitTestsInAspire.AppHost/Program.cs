var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.UnitTestsInAspire_ApiService>("apiservice");

//var dbPassword = builder.AddParameter("PostgresPassword", secret: true);
var postgresServer = builder
    .AddPostgres("PostgresServer")
    .WithPgWeb()
    .WithDataVolume("PgVolume");

var applicationDb = postgresServer.AddDatabase("applicationDb");

builder.AddProject<Projects.UnitTestsInAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(applicationDb)
    .WaitFor(apiService);

builder.Build().Run();
