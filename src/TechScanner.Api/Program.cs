using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using TechScanner.Core.Interfaces;
using TechScanner.Infrastructure.Data;
using TechScanner.Infrastructure.Llm;
using TechScanner.Infrastructure.Repositories;
using TechScanner.Scanner;
using TechScanner.Scanner.Analysis;
using TechScanner.Scanner.Background;
using TechScanner.Scanner.Orchestrator;
using TechScanner.Scanner.Parsers;
using TechScanner.Scanner.Sources;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connStr = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddDbContext<TechScannerDbContext>(options =>
{
    if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(connStr);
    else
        options.UseSqlite(connStr);
});

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IScanRepository, ScanRepository>();

// ── Manifest Parsers (Strategy pattern) ──────────────────────────────────────
builder.Services.AddTransient<IManifestParser, CsprojParser>();
builder.Services.AddTransient<IManifestParser, PackageJsonParser>();
builder.Services.AddTransient<IManifestParser, RequirementsTxtParser>();
builder.Services.AddTransient<IManifestParser, PyprojectParser>();
builder.Services.AddTransient<IManifestParser, MavenParser>();
builder.Services.AddTransient<IManifestParser, GradleParser>();
builder.Services.AddTransient<IManifestParser, DockerfileParser>();
builder.Services.AddTransient<IManifestParser, GoModParser>();
builder.Services.AddTransient<IManifestParser, CargoTomlParser>();

// ── Scanner services ──────────────────────────────────────────────────────────
builder.Services.AddTransient<FileCollector>();
builder.Services.AddTransient<UsageAnalyzer>();
builder.Services.AddTransient<LocalFolderProvider>();
builder.Services.AddTransient<ZipArchiveProvider>();
builder.Services.AddTransient<GitRepoProvider>();
builder.Services.AddTransient<SourceProviderFactory>();
builder.Services.AddTransient<ScanOrchestrator>();

// ── LLM Enricher ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<LlmEnricherFactory>();
builder.Services.AddTransient<ILlmEnricher>(sp =>
    sp.GetRequiredService<LlmEnricherFactory>().Create());

// ── Background queue ──────────────────────────────────────────────────────────
var scanChannel = Channel.CreateUnbounded<ScanJob>(new UnboundedChannelOptions
{
    SingleReader = true
});
builder.Services.AddSingleton(scanChannel);
builder.Services.AddSingleton<ScanBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ScanBackgroundService>());

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ── CORS (Vite dev server) ────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// ── Init DB on startup ───────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TechScannerDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
