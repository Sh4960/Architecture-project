using MassTransit;
using Serilog;
using SerilogLogContext = Serilog.Context.LogContext;
using NotificationService.BLL.Interfaces;
using NotificationService.BLL;
using NotificationService.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ======== Logging ========
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "NotificationService")
    .WriteTo.Console()
    .WriteTo.File("Logs/notification-service-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ======== Dependency Injection ========
builder.Services.AddScoped<INotificationBLLService, NotificationBLLService>();

// ======== MassTransit with RabbitMQ ========
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderFinalizedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration["RabbitMq:HostName"] ?? "rabbitmq";
        var rabbitMqUser = builder.Configuration["RabbitMq:UserName"] ?? "guest";
        var rabbitMqPass = builder.Configuration["RabbitMq:Password"] ?? "guest";

        cfg.Host(rabbitMqHost, h =>
        {
            h.Username(rabbitMqUser);
            h.Password(rabbitMqPass);
        });

        cfg.UseRawJsonSerializer();
        cfg.ConfigureEndpoints(context);
    });
});

// ======== Controllers & Swagger ========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
    }

    context.Response.Headers["X-Correlation-ID"] = correlationId;
    context.Items["CorrelationId"] = correlationId;

    using (SerilogLogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

app.UseSerilogRequestLogging();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

Log.Information("NotificationService is starting...");
app.Run();
