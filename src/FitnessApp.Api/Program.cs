using FitnessApp.Api.Common.Extensions;
using FitnessApp.Api.Infrastructure.Auth;
using FitnessApp.Api.Infrastructure.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

// Each call registers one area; the details live in the extension methods.
builder.Services
    .AddPersistence()
    .AddAuth()
    .AddRateLimiting()
    .AddApiDefaults()
    .AddFrontendCors(builder.Configuration)
    .AddSwagger();

var app = builder.Build();

app.UseApiPipeline();
app.MapApiEndpoints();

await app.InitializeDatabaseAsync();
await app.RunAsync();
