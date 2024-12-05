var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.UnitTestsInAspire_ApiService>("apiservice");

builder.AddProject<Projects.UnitTestsInAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
