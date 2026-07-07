using MassTransit;
using Serilog;
using SerilogLogContext = Serilog.Context.LogContext;
using StackExchange.Redis;
using InventoryService.BLL.Interfaces;
using InventoryService.BLL;
using InventoryService.Consumers;
using InventoryService.DAL.Interfaces;
using InventoryService.DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ======== Logging ========
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "InventoryService")
    .WriteTo.Console()
    .WriteTo.File("Logs/inventory-service-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ======== Database (Redis) - Connect with retry logic ========
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection") 
    ?? "redis:6379";

IConnectionMultiplexer redis = null;
int retries = 0;
const int maxRetries = 5;
const int delayMs = 2000;

while (retries < maxRetries && redis == null)
{
    try
    {
        redis = ConnectionMultiplexer.Connect(redisConnection);
        Log.Information("Successfully connected to Redis after {Retries} attempt(s)", retries + 1);
        break;
    }
    catch (Exception ex)
    {
        retries++;
        if (retries >= maxRetries)
        {
            Log.Error(ex, "Failed to connect to Redis after {MaxRetries} attempts", maxRetries);
            throw;
        }
        Log.Warning("Redis connection attempt {Attempt} failed, retrying in {DelayMs}ms: {Error}", retries, delayMs, ex.Message);
        System.Threading.Thread.Sleep(delayMs);
    }
}

builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// ======== Dependency Injection ========
builder.Services.AddScoped<IInventoryDAL, InventoryDAL>();
builder.Services.AddScoped<IInventoryBLLService, InventoryBLLService>();

// ======== MassTransit with RabbitMQ ========
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

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

        cfg.UseRawJsonSerializer(); // <--- תוסיף את זה כאן!
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

Log.Information("InventoryService is starting...");
app.Run();
