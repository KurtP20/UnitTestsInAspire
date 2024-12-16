using Microsoft.EntityFrameworkCore;
using UnitTestsInAspire.Web;
using UnitTestsInAspire.Web.Components;
using UnitTestsInAspire.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    });


// Add DB context to DI (requires Aspire.Npgsql.EntityFrameworkCore.PostgreSQL NuGet package)
// the context is required by e.g. MS Identity
builder.AddNpgsqlDbContext<ApplicationDbContext>("applicationDb");

// Add DB context factory
// required by e.g. HistoryService
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("applicationDb")));


// add the historic temperature data service
builder.Services.AddSingleton<HistoryService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();


// Define partial Program class so that xUnit can discover this class.
public partial class Program { }