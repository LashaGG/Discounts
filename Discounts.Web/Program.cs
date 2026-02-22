using System.Globalization;
using Discounts.Infrastructure;
using Discounts.Web.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Discounts Web");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddWebPresentation();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddWebAuthorization();

    var app = builder.Build();

    await app.SeedDataAsync().ConfigureAwait(false);

    app.UseWebPipeline();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
