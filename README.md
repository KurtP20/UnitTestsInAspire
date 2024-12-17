This is a minimal viable project demonstrating a nice (?) way of performing functional unit tests 
of a project that relies on dependencies managed by Aspire, such as Postgres, Qdrant, etc. Thus far 
functional unit tests are not supported, i.e. if a service defined in a project requires resources
managed by Aspire, those resources have to be maintained manually by e.g. `docker-compose`, which is
undesirable, as it should be Aspire's responsibility to manage the resources.

To do functional unit tests, a new custom `WebApplicationFactory` spins up the AppHost, waits for all dependencies to become ready,
and then copies the environmental variables of the project-under-test inside the `DistributedApplicationTestingBuilder`
to a second instance of the project-under-test in a new `WebApplicationFactory` that is used for the actual testing. 
This way, all connection strings and other settings that Aspire manages are available to the test project.

The functional unit tests are demoed in `FunctionalTests`. It uses the `CustomWebApplicationFactory<Program>`
factory to gain access to the services defined in the `UnitTestsInAspire.Web` project. It also 
implements `IClassFixture` so that the AppHost is only spun up once for all tests. With access to the DI mechanism,
a `HistoryService` is obtained from `UnitTestsInAspire.Web`, which stores some dummy weather data in a Postgres database, 
managed by Aspire.

There are some caveat's to this approach:
1) To be able to reference `Program.cs` of the `UnitTestsInAspire.Web` project, `public partial class Program { }` has
to be added to the `Program.cs`.

2) The connection string to the Postgres database can be obtained via `GetConnectionString` (see
`CustomWebApplicationFactory.ConfigureWebHost`), but strangely, the connection string is not picked up by the
`HistoryService`, unless a dummy string is added to the `appsettings.json` of the `UnitTestsInAspire.Web` project.

3) To add the migration and update the database, a workaround described [here](https://github.com/dotnet/aspire/issues/4497) 
and [here](https://stackoverflow.com/questions/79077499/why-cant-i-specify-a-startup-project-when-manually-updating-a-database-in-net)
has to be used:
   - Change to the UnitTestsInAspire.Web directory, then execute ```dotnet ef migrations add Init --output-dir ./Data/Migrations```
   - Run the App and get the connection string from the environmental variables in AppHost via the Aspire Dashboard (check Details page)
   - While the app is still running, update the database with `dotnet ef database update --no-build --connection "..."`

It would be nice if this code somehow finds its way into the Aspire project, maybe as a new
`DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost, List<string> projectNames>()`,
which would do the same as the `CustomWebApplicationFactory<Program>` and return handles to all projects that should be tested.
I might post the solution on Stack OVerflow so others can use it, but I would like to hear your opinion first.

My solution is adapted from [here](https://github.com/dotnet/aspire/discussions/878#discussioncomment-9596424). It uses a
cumbersome way to materialize the environmental variables of the project-under-test, maybe there is a more direct way of accessing
the environmental variables. 

Please let me know what you think.

