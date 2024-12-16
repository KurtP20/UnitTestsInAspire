using Microsoft.EntityFrameworkCore;
using UnitTestsInAspire.Web.Models;

namespace UnitTestsInAspire.Web.Data;

public class HistoryService(IDbContextFactory<ApplicationDbContext> DbFactory)
{
    // create the Db context from the factory
    private readonly ApplicationDbContext _context = DbFactory.CreateDbContext();


    public async Task AddDataPointAsync(WeatherData dataPoint )
    {
        // add data point to the context
        _context.HistoricData.Add(dataPoint);

        // save data
        await _context.SaveChangesAsync();
    }


    public async Task<WeatherData> GetLastDataPointAsync()
    {
        return await _context.HistoricData.OrderBy(data => data.Id).LastAsync();
    }
}
