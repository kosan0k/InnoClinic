using Services.Profiles.Application;
using Services.Profiles.Infrastructure;
using Services.Profiles.Infrastructure.Persistence;

namespace Services.Profiles.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Initialize databases (apply migrations)
        if (app.Environment.IsDevelopment())
        {
            await DatabaseInitializer.InitializeDatabasesAsync(app.Services);
        }

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapDefaultOpenApi(developmentOnly: true);
            app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        await app.RunAsync();
    }
}
