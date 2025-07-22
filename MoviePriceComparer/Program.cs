using MoviePriceComparer.Middleware;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Prometheus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MoviePriceComparer.Models;
using Microsoft.OpenApi.Models;
using Polly;
using MoviePriceComparer.Services;



Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/movie-comparison-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.Extensions.Http.Resilience", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.Extensions.Resilience", LogLevel.Debug);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Debug);
builder.Logging.AddFilter("Polly", LogLevel.Debug);

var movieProviders = builder.Configuration
.GetSection("MovieProviders")
.Get<List<MovieProviderConfig>>() ?? [];
builder.Services.AddSingleton<IReadOnlyList<MovieProviderConfig>>(movieProviders);


builder.Host.UseSerilog();


builder.Services.AddControllers();


if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "Movie Comparison API",
            Version = "v1",
            Description = "API for comparing movie prices from different providers"
        });
        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = "API Token needed to access the endpoints. Example: `your-token-here`",
            Name = "x-access-token",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "ApiKeyScheme"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
    });

}




builder.Services.AddHttpClient<IMovieApiClient, MovieApiClient>(client =>
{
    client.BaseAddress = new Uri("https://webjetapitest.azurewebsites.net/");
    client.Timeout = TimeSpan.FromSeconds(30);

})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(35);
});

builder.Services.AddScoped<IMovieService, MovieService>();


builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddUrlGroup(new Uri("https://webjetapitest.azurewebsites.net/"),
        name: "webjet-api-base",
        timeout: TimeSpan.FromSeconds(10));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});



var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Movie Comparison API v1");
        c.RoutePrefix = string.Empty;
    });

    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseHttpMetrics();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();


app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                exception = x.Value.Exception?.Message,
                duration = x.Value.Duration.ToString()
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");


app.MapMetrics();

app.MapControllers();


app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();