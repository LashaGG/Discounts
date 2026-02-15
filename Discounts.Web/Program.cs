using Discounts.Infrastructure;
using Discounts.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebPresentation();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebAuthorization();

var app = builder.Build();

await app.SeedDataAsync().ConfigureAwait(false);

app.UseWebPipeline();

app.Run();
