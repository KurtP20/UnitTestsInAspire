using Aspire.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
namespace UnitTestsInAspire.Tests;


/// <summary>
/// Custom WebApplicationFactory for the UnitTestsInAspire.Web project. It spins up the AppHost and injects the environmental 
/// variables from the project (webfrontend). To adapt it for other projects, you need to change the name of the
/// AppHost project, of the required dependencies to spin up and the project's designation defined in the AppHost; see (§§).
/// </summary>
/// <typeparam name="TEntryPoint">See remark above</typeparam>
public class CustomWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    // holds the AppHost instance
    // I guess it should be a class variable to keep AppHost running as long as the factory is alive, i.e. the tests are running
    private DistributedApplication? _app;

    // used by the Dispose pattern
    private bool disposedValue;


    /// <summary>
    /// Configure the WebHostBuilder for the test server. Spins up AppHost and waits for required resources to start.
    /// Obtains the environmental variables from the project (webfrontend) and injects them into the test server.
    /// </summary>
    /// <param name="builder"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // local variables
        var environmentVariables = new Dictionary<string, string?>();

        // === Arrange

        // build and configure AppHost (§§)
        var appHost = DistributedApplicationTestingBuilder.CreateAsync<Projects.UnitTestsInAspire_AppHost>().GetAwaiter().GetResult();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging

        // disable port randomization for tests
        // https://github.com/dotnet/aspire/discussions/6843#discussioncomment-11444891
        //appHost.Configuration["DcpPublisher:RandomizePorts"] = "false";


        // does not quite work, but it might be close
        //// remove pgWeb for tests
        //// https://github.com/dotnet/aspire/discussions/878#discussioncomment-9596424
        //var pg = appHost.Resources.OfType<PgWebContainerResource>().First(r => r.Name == "PostgresServer-pgweb");
        //if (pg.TryGetLastAnnotation<ContainerMountAnnotation>(out var mountAnnotation))
        //{
        //    pg.Annotations.Remove(mountAnnotation);
        //}

        // build the AppHost synchronously
        _app = appHost.BuildAsync().GetAwaiter().GetResult();



        // start AppHost
        // Note: As far as I understand, the containers are not run in DockerDesktop but in some in-memory thingy
        var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
        _app.StartAsync().GetAwaiter().GetResult();

        // wait until the required resources are running (§§)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(20));
        resourceNotificationService.WaitForResourceAsync("PostgresServer", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(20));
        stopwatch.Stop();
        Console.WriteLine($"Waited {stopwatch.ElapsedMilliseconds} ms for required resources to start");



        // = get env variables from mentormate

        // find UnitTestsInAspire.Web project in AppHost (§§) (adapted from https://github.com/dotnet/aspire/discussions/878#discussioncomment-9596424)
        var testProject = appHost.Resources.First(r => r.Name == "webfrontend");

        // make sure it is a ProjectResource with an environment and
        // get the annotations marking the methods/fields that contain the environmental variables
        if (testProject is IResourceWithEnvironment &&
            testProject.TryGetEnvironmentVariables(out var annotations))
        {
            // To invoke the callback functions to obtain the environmental variables, an EnvironmentCallbackContext
            // is required, which can be created from a DistributedApplicationExecutionContext, but this requires
            // certain services, that can be obtained from the AppHost's service registry. Thus, hijack the AppHosts services
            // to create the DistributedApplicationExecutionContext.
            var options = new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Run)
            {
                ServiceProvider = _app.Services
            };

            var executionContext = new DistributedApplicationExecutionContext(options);
            var environmentCallbackContext = new EnvironmentCallbackContext(executionContext);


            // materialize the environmental variables by calling the callbacks in the environmentCallbackContext
            foreach (var annotation in annotations)
            {
                annotation.Callback(environmentCallbackContext).GetAwaiter().GetResult();
            }

            // Translate environment variable __ syntax to :
            foreach (var (key, value) in environmentCallbackContext.EnvironmentVariables)
            {
                if (testProject is ProjectResource && key == "ASPNETCORE_URLS") continue;

                var configValue = value switch
                {
                    string val => val,
                    IValueProvider v => v.GetValueAsync().AsTask().Result,
                    null => null,
                    _ => throw new InvalidOperationException($"Unsupported value, {value.GetType()}")
                };

                if (configValue is not null)
                {
                    environmentVariables[key.Replace("__", ":")] = configValue;
                }
            }
        }


        // inject the environmental variables into the test server
        builder.ConfigureAppConfiguration((_, cb) =>
        {
            cb.AddInMemoryCollection(environmentVariables);
        });


        //// Force the factory to recreate the host and services (not required, keep to remember that switch)
        //factory.Server.PreserveExecutionContext = false;

        //// iterate over environment variables and log them
        //foreach (var (key, value) in environmentVariables)
        //{
        //    Console.WriteLine($"Injecting env varialbes {key} = {value}");
        //}
    }


    /// <summary>
    /// This method is part of the recommended dispose pattern. I assume that WebApplicationFactory already implements
    /// <code>public void Dispose()</code>, which calls <code>Dispose(disposing: true)</code>.
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // dispose the AppHost
                _app?.Dispose();
            }

            // free unmanaged resources and override finalizer
            // set large fields to null

            // Call the base class's Dispose method to ensure base class resources are disposed of
            base.Dispose(disposing);

            disposedValue = true;
        }
    }
}
