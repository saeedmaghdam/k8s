using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IServiceStatus, ServiceStatus>();
builder.Services.AddHealthChecks()
    .AddCheck<FakeHealthCheck>("fake_health_check");

var app = builder.Build();

app.UseHealthChecks("/health");
app.UseMiddleware<RequestsMetricMiddleware>();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.MapGet("/{id}", async ([FromRoute] string id, ILogger<Program> logger) =>
{
    logger.LogInformation("Received request with id {0}", id);

    var randomDelayInMs = new Random().Next(100, 2500);
    await Task.Delay(randomDelayInMs);

    return Results.Ok(new {
        Id = id
    });
})
.WithName("GetCustomer")
.WithOpenApi();

app.MapPost("/health-status", (bool isHealthy, ILogger<Program> logger, IServiceStatus serviceStatus) =>
{
    serviceStatus.Status = isHealthy;
    logger.LogInformation("Health status is set to {0}", isHealthy);
})
.WithName("HealthStatus")
.WithOpenApi();

app.MapGet("/metrics", (IServiceStatus serviceStatus) => Results.Ok(serviceStatus.GetRequestsInFlight()));

app.Run();

public class FakeHealthCheck : IHealthCheck
{
    private readonly IServiceStatus _serviceStatus;

    public FakeHealthCheck(IServiceStatus serviceStatus) => _serviceStatus = serviceStatus;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_serviceStatus.Status)
            return Task.FromResult(HealthCheckResult.Healthy());
        else
            return Task.FromResult(HealthCheckResult.Unhealthy());
    }
}

public class RequestsMetricMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceStatus _serviceStatus;

    public RequestsMetricMiddleware(RequestDelegate next, IServiceStatus serviceStatus)
    {
        _next = next;
        _serviceStatus = serviceStatus;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        bool shouldIncreaseMetrics = context.Request.Path != "/metrics";
        try
        {
            if (shouldIncreaseMetrics)
                _serviceStatus.IncreaseRequestsInFlight();

            await _next(context);
        }
        finally
        {
            if (shouldIncreaseMetrics)
                _serviceStatus.DecreaseRequestsInFlight();
        }
    }
}


public interface IServiceStatus
{
    bool Status { get; set; }
    void IncreaseRequestsInFlight();
    void DecreaseRequestsInFlight();
    int GetRequestsInFlight();
}

public class ServiceStatus : IServiceStatus
{
    public bool Status { get; set; }
    public int TotalRequestsInFlight { get; set; }

    public ServiceStatus()
    {
        Status = true;
    }

    public void IncreaseRequestsInFlight() {
        Interlocked.Increment(ref TotalRequestsInFlight);
    }

    public void DecreaseRequestsInFlight() {
        Interlocked.Decrement(ref TotalRequestsInFlight);
    }

    public int GetRequestsInFlight() {
        return TotalRequestsInFlight;
    }
}