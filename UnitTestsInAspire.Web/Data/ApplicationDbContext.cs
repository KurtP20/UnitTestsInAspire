using Microsoft.EntityFrameworkCore;
using UnitTestsInAspire.Web.Models;

namespace UnitTestsInAspire.Web.Data;


public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<WeatherData> HistoricData { get; set; }
}
