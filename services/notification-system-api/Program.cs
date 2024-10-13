using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

var configurations = app.Services.GetRequiredService<IConfiguration>();
var rabbitMqConnectionString = configurations["ConnectionStrings:RabbitMQ"];
var queueName = "notification-management-api-queue";

var factory = new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
var connection = factory.CreateConnection();
var channel = connection.CreateModel();

channel.QueueDeclare(queue: queueName,
    durable: false,
    exclusive: false,
    autoDelete: true,
    arguments: null);

app.MapPost("/notify", async ([FromBody] Notification notification, ILogger<Program> logger) =>
{
    var message = JsonSerializer.Serialize(notification);
    var body = Encoding.UTF8.GetBytes(message);

    channel.BasicPublish(exchange: string.Empty,
                        routingKey: queueName,
                        basicProperties: null,
                        body: body);

    var randomDelayInMs = new Random().Next(100, 2500);
    await Task.Delay(randomDelayInMs);

    logger.LogInformation("[x] Sent {0}", message);
})
.WithName("Notify")
.WithOpenApi();

app.MapPost("/health-status", (bool isHealthy, ILogger<Program> logger, IServiceStatus serviceStatus) =>
{
    serviceStatus.Status = isHealthy;
    logger.LogInformation("Health status is set to {0}", isHealthy);
})
.WithName("HealthStatus")
.WithOpenApi();

app.MapGet("/metrics", (IServiceStatus serviceStatus) => Results.Ok(serviceStatus.GetRequestsInFlight()));

InitializeConsumer(app, queueName, channel);

app.Run();

static void InitializeConsumer(WebApplication app, string queueName, IModel channel)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(" [*] Waiting for messages.");

    var consumer = new EventingBasicConsumer(channel);
    consumer.Received += (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        logger.LogInformation($" [x] Received {message}");
    };
    channel.BasicConsume(queue: queueName,
                         autoAck: true,
                         consumer: consumer);
}

record class Notification(string Channel, string Message);

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