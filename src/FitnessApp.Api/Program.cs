using FitnessApp.Api.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Each call registers one area; the details live in Common/Extensions/ServiceCollectionExtensions.cs.
builder.Services
    .AddPersistence()
    .AddApiDefaults()
    .AddFrontendCors(builder.Configuration)
    .AddSwagger();

var app = builder.Build();

app.UseApiPipeline();
app.MapApiEndpoints();

await app.RunAsync();
