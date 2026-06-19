using RateLimitVisualizer.Api.Endpoints;
using RateLimitVisualizer.Api.Services;
using RateLimitVisualizer.Api.Storage;
using RateLimitVisualizer.Core.Alerts;
using RateLimitVisualizer.Core.BurnRate;
using RateLimitVisualizer.Core.EndpointNormalization;
using RateLimitVisualizer.Core.HeaderParsing;
using RateLimitVisualizer.Core.Time;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<RateLimitHeaderParser>();
builder.Services.AddSingleton<EndpointNormalizer>();
builder.Services.AddSingleton<BurnRateCalculator>();
builder.Services.AddSingleton<QuotaProjectionCalculator>();
builder.Services.AddSingleton<QuotaAlertEvaluator>();
builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddScoped<IObservationRepository, ObservationRepository>();
builder.Services.AddScoped<ObservationIngestionService>();
builder.Services.AddScoped<DashboardQueryService>();

var app = builder.Build();

await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealthEndpoints();
app.MapObservationEndpoints();
app.MapDashboardEndpoints();

app.Run();

public partial class Program;
