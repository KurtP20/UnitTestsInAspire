using Microsoft.Extensions.Configuration;
using UnitTestsInAspire.Web.Data;
using UnitTestsInAspire.Web.Models;
using Xunit.Abstractions;

namespace UnitTestsInAspire.Tests;

/// <summary>
/// Perform functional unit tests on the HistoryService defined in UnitTestsInAspire.Web.
/// The custom WebApplicationFactory spins up the AppHost and injects the environmental variables from the project (webfrontend).
/// </summary>
/// <remarks>
/// In order for the compiler to find the project to test, you need to add <code>public partial class Program { }</code> to
/// <code>Program.cs</code> in the project you want to test.
/// </remarks>
public sealed class FunctionalTests(CustomWebApplicationFactory<Program> _factory,
                                    ITestOutputHelper _output) : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
    // grants access to the Services defined in UnitTestsInAspire.Web
    // InitializeAsync throws an exception if the scope could not be created, so it is never null when used
    private IServiceScope _scope = null!;

    /// <summary>
    /// Initializes the test environment. 
    /// Creates the scope for the UnitTestsInAspire.Web services.
    /// </summary>
    public async Task InitializeAsync()
    {
        // === Spinning up AppHost and copying the environmantal variables to UnitTestsInAspire.Web is 
        // done in the custon WebApplicationFactory


        // get ServiceProvider from UnitTestsInAspire.Web (for obtain dependencies from DI)
        // I store the scope as class variable, so that each test can derive it's own service. I think that if all tests were
        // to use the same service and run in parallel, they might interfere with each other (e.g. concurrent DBcontext access)
        _scope = _factory.Services.CreateScope();

        if (_scope == null)
        {
            throw new InvalidOperationException("ServiceScope could not be created for UnitTestsInAspire.Web.");
        }


        // development: Verify the configuration within the application
        // Note: The connection string is found in this way, even if the ConnectionStrings:applicationDb is not present in the appsettings.json
        // Surprisingly, the HistoryService does not pick up the connection string, unless at least a dummy string is present in the appsettings.json
        var projectConfig = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var dbConnectionString = projectConfig.GetConnectionString("applicationDb");
        if (string.IsNullOrEmpty(dbConnectionString))
        {
            throw new InvalidOperationException("ConnectionString for applicationDb is missing.");
        }

        // avoid CS1998 Async method lacks 'await' operators and will run synchronously
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddAndRetreiveDataPoint()
    {
        // === Arrange
        // is done in InitializeAsync


        // === Act

        // get Services via dependency injection
        var serviceProvider = _scope.ServiceProvider;
        var HistoryService = serviceProvider.GetRequiredService<HistoryService>();


        // create a new data point
        var weatherData = new WeatherData
        {
            Date = DateOnly.FromDateTime(DateTime.Now),
            Temperature = Random.Shared.Next(-20, 55),
            Summary = "functional unit tests in Aspire"
        };

        // store in DB
        await HistoryService.AddDataPointAsync(weatherData);

        // optup to test window
        _output.WriteLine("Added a data point.");



        // === Assert

        // retrieve the last data point from the DB
        WeatherData lastDataPoint = await HistoryService.GetLastDataPointAsync();


        Assert.NotNull(lastDataPoint);
        Assert.Equal(weatherData.Date, lastDataPoint.Date);

        _output.WriteLine("Sucessfully retriebed the last data point");
    }


    /// <summary>
    /// Disposes the scope for the UniTestsInAspire.Web services.
    /// </summary>
    /// <returns></returns>
    public async Task DisposeAsync()
    {
        _scope?.Dispose();

        // avoid CS1998 Async method lacks 'await' operators and will run synchronously
        await Task.CompletedTask;
    }
}
