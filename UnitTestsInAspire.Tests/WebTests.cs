using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using UnitTestsInAspire.Web;

namespace UnitTestsInAspire.Tests;

// requires NuGet package Microsoft.AspNetCore.Mvc.Testing
public class WebTests(WebApplicationFactory<Program> _factory,
                      ITestOutputHelper _output) : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    // holds the AppHost instance
    private DistributedApplication? _appHost;

    // grants access to the Services defined in UnitTestsInAspire.Web
    // InitializeAsync throws an exception if the scope could not be created, so it is never null when used
    private IServiceScope _scope = null!;


    /**
     * Initializes the test environment. 
     * Spins up AppHost to setup all dependencies such as Postgres, Qdrant, ...
     * Creates a separate instance of MentorMate to access the services defined there.
     */
    public async Task InitializeAsync()
    {

        // === Arrange

        // = build and start AppHost

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.UnitTestsInAspire_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging

        _appHost = await appHost.BuildAsync();

        // start host synchronously (requires Microsoft.Extensions.Hosting)
        _appHost.Start();


        // TODO: I am not sure, if using the synchronous version indeed waits until all resources are running. If not, try using
        //var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        //await app.StartAsync();
        //var httpClient = app.CreateHttpClient("webfrontend");
        //await resourceNotificationService.WaitForResourceAsync("webfrontend", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));

        
        // = UnitTestsInAspire.Web project in a separate instance

        // get ServiceProvider for dependency injection
        // I store the scope as class variable, so that each test can derive it's own service. I think that if all tests were
        // to use the same service and run in parallel, they might interfere with each other (e.g. concurrent DBcontext access)
        _scope = _factory.Services.CreateScope();


        if (_scope == null)
        {
            throw new InvalidOperationException("ServiceScope could not be created for UnitTestsInAspire.Web");
        }
    }


    [Fact]
    public void GetRandomNumberFromService()
    {
        // Arrange
        // is done in InitializeAsync

        // Act

        // get Services defined in UnitTestsInAspire.Web
        var serviceProvider = _scope.ServiceProvider;
        var SKService = serviceProvider.GetRequiredService<myService>();

        // get random number
        var randomNumber = SKService.GetRandomNumber();

        // show in test window
        _output.WriteLine($"The number is {randomNumber}");


        // Assert
        Assert.InRange(randomNumber, 0, 100);
    }


    public async Task DisposeAsync()
    {
        if (_appHost != null)
        {
            await _appHost.DisposeAsync();
        }
    }
}
