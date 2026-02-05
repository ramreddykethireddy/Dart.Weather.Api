using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Dart.Weather.Api.Application.Interfaces;
using Dart.Weather.Api.Infrastructure;
using Dart.Weather.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Ensure a local folder on C: exists for logs
var logFolder = @"C:\weather-logs";
try
{
    Directory.CreateDirectory(logFolder);
}
catch (Exception)
{
    // If creating the folder fails (permissions/etc.), continue without throwing here.
}

// Configure logging: keep Console and add a simple file logger that writes to C:\weather-logs\app.log
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(logFolder, "app.log")));

// Add services to the container.
builder.Services.AddControllers();

// Add CORS policy to allow Angular front-end during development
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAngularDev",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});


// Register Open-Meteo HTTP client (typed client)
builder.Services.AddHttpClient<OpenMeteoClient>();

// Repository that persists JSON files under <ContentRoot>/weather-data
builder.Services.AddSingleton(provider =>
    new WeatherRepository(builder.Environment.ContentRootPath));

// Application service wiring (clean architecture)
builder.Services.AddScoped<IWeatherService, WeatherService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply CORS globally
app.UseCors("AllowAngularDev");

app.UseAuthorization();

app.MapControllers();

app.Run();


// --- Simple file logger implementation ---
// Minimal ILoggerProvider that appends log lines to a single file.
// Suitable for small projects and local development. For production prefer Serilog/NLog/etc.
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;

    public FileLoggerProvider(string filePath) => _filePath = filePath;

    public ILogger CreateLogger(string categoryName) => new FileLogger(_filePath);

    public void Dispose() { /* no-op */ }
}

internal sealed class FileLogger : ILogger
{
    private readonly string _filePath;
    private static readonly object _lock = new();

    public FileLogger(string filePath) => _filePath = filePath;

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter == null) return;

        var message = formatter(state, exception);
        var line = $"{DateTime.UtcNow:O} [{logLevel}] {message}";
        if (exception is not null)
            line += $" Exception: {exception}";

        try
        {
            lock (_lock)
            {
                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Swallow IO exceptions to avoid crashing the app due to logging issues.
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}