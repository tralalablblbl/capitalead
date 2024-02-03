using System.Net.Http.Headers;
using Capitalead.Data;
using Capitalead.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(nameof(LobstrService), (services, client) =>
    {
        client.BaseAddress = new Uri(LobstrService.LOBSTR_BASE_URL);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token",
            services.GetRequiredService<IConfiguration>()["lobstr_auth_token"] ??
            throw new KeyNotFoundException("lobstr_auth_token"));
        client.DefaultRequestHeaders.Connection.Add("keep-alive");
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
    .AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient(nameof(NoCrmService), (services, client) =>
    {
        client.BaseAddress = new Uri(NoCrmService.NOCRM_API_URL);
        client.DefaultRequestHeaders.Add("X-API-KEY",
            services.GetRequiredService<IConfiguration>()["nocrm_auth_token"] ??
            throw new KeyNotFoundException("nocrm_auth_token"));
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
    .AddPolicyHandler(GetRetryPolicy());

var connectionString = Environment.GetEnvironmentVariable("ConnectionString__Default");
builder.Services.AddDbContext<AppDatabase>((provider, options) =>
{
    options.UseNpgsql(connectionString ?? throw new KeyNotFoundException("ConnectionString__Default can not be empty."),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(300);
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 10);
        });
});

builder.Services
    .AddTransient<LobstrService>()
    .AddTransient<CrmDataProcessingService>()
    .AddTransient<NoCrmService>()
    .AddTransient<MainService>()
    .AddHostedService<Scheduler>();

builder.Services.AddHangfire(config =>
{
    config.UseMemoryStorage();
});
builder.Services.AddHangfireServer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDatabase>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/v1/run", async ([FromServices]MainService mainService, [FromServices]ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("api/v1/run");
        logger.LogInformation("Started uploading script...");
        await mainService.StartMigration();
        logger.LogInformation("Successfully uploaded all data to new prospecting lists!");
        return Results.Ok();
    })
    .WithName("run")
    .WithOpenApi();

app.MapGet("/api/v1/find-duplicates", async ([FromServices]MainService mainService, [FromServices]ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("api/v1/remove-duplicates");
        logger.LogInformation("Started find duplicates script...");
        await mainService.FindDuplicates();
        logger.LogInformation("Successfully found duplicates!");
        return Results.Ok();
    })
    .WithName("find-duplicates")
    .WithOpenApi();

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));