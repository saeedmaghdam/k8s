using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IServiceStatus, ServiceStatus>();
builder.Services.AddHealthChecks()
    .AddCheck<FakeHealthCheck>("fake_health_check");

var configuration = builder.Configuration;
builder.Services.AddHttpClient("customer-management", config =>
{
    config.BaseAddress = new Uri(configuration["CUSTOMER_MANAGEMENT_API_URL"]);
});
builder.Services.AddHttpClient("notification-system", config =>
{
    config.BaseAddress = new Uri(configuration["NOTIFICATION_SYSTEM_API_URL"]);
});

var app = builder.Build();

app.UseHealthChecks("/health");
app.UseMiddleware<RequestsMetricMiddleware>();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.MapPost("/", async (Order order, ILogger<Program> logger) =>
{
    logger.LogInformation("Received order with id {0}", order.Id);

    var randomDelayInMs = new Random().Next(100, 2500);
    await Task.Delay(randomDelayInMs);

    var customerManagementClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient("customer-management");
    var notificationSystemClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient("notification-system");

    var response = await customerManagementClient.GetAsync($"/{order.CustomerId}");
    if (!response.IsSuccessStatusCode)
        return Results.BadRequest("Customer not found");

    var customer = await response.Content.ReadAsStringAsync();
    logger.LogInformation("Customer: {0}", customer);

    var notificationResponse = await notificationSystemClient.PostAsJsonAsync("/notify", new
    {
        Channel = "email",
        Message = $"Order {order.Id} has been created for customer {order.CustomerId}"
    });
    if (!notificationResponse.IsSuccessStatusCode)
        return Results.BadRequest("Notification failed");

    return Results.Ok(new
    {
        OrderId = order.Id,
        Customer = customer
    });
})
.WithName("CreateOrder")
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

public record Order(string Id, string CustomerId, string ProductId, int Quantity);

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