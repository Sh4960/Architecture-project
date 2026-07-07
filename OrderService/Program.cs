using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SerilogLogContext = Serilog.Context.LogContext;
using OrderService.BLL.Interfaces;
using OrderService.BLL;
using OrderService.Consumers;
using OrderService.DAL.Interfaces;
using OrderService.DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ======== Logging ========
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "OrderService")
    .WriteTo.Console()
    .WriteTo.File("Logs/order-service-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ======== Database ========
var orderConnection = builder.Configuration.GetConnectionString("OrderDbConnection") 
    ?? "Server=order-db;Database=OrderDB;User Id=sa;Password=YourStrong!Password;Encrypt=false;";

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(orderConnection)
);

// ======== Dependency Injection ========
builder.Services.AddScoped<IOrderDAL, OrderDAL>();
builder.Services.AddScoped<IOrderBLLService, OrderBLLService>();

// ======== MassTransit with RabbitMQ ========
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InventoryReservedConsumer>();
    x.AddConsumer<InventoryRejectedConsumer>();

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

// ======== Database Migration ========
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.Migrate();
}

app.UseSerilogRequestLogging();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", async (OrderDbContext dbContext) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync();
    return canConnect ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503);
});

Log.Information("OrderService is starting...");
app.Run();
