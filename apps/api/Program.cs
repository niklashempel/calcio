using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Business.Interfaces;
using Api.Business.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CalcioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseNetTopologySuite()));

builder.Services.AddControllers();

builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IAgeGroupService, AgeGroupService>();
builder.Services.AddScoped<ICompetitionService, CompetitionService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Calcio API",
        Version = "v1",
        Description = "API for managing football clubs, teams, matches, and venues"
    });
});

// Configure JSON serialization to handle circular references
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Automatic database migration on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking for pending database migrations...");
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw; // Re-throw to prevent the application from starting with an invalid database state
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calcio API v1");
    c.RoutePrefix = "swagger"; // Access via /swagger
});

// Use CORS
app.UseCors("AllowAll");

app.MapControllers();

// Root endpoint
app.MapGet("/", () => "Calcio API is running!")
.WithName("Root")
.WithSummary("API status")
.WithDescription("Returns a simple status message")
.WithTags("Status");

app.Run();
