using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Api.Data;

namespace Api.IntegrationTests.Setup;

public class CalcioWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public CalcioWebApplicationFactory()
    {
        _databaseName = Guid.NewGuid().ToString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            services.RemoveAll(typeof(DbContextOptions<CalcioDbContext>));
            services.RemoveAll(typeof(CalcioDbContext));

            // Add in-memory database for testing with unique name
            services.AddDbContext<CalcioDbContext>(options =>
            {
                // InMemory database doesn't support NetTopologySuite, so we'll work with Latitude/Longitude in tests
                options.UseInMemoryDatabase(_databaseName);
            });

            // Build the service provider and create the database
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

            // Ensure the database is created
            context.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}
