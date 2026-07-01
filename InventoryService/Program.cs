using Serilog;
using StackExchange.Redis;
using InventoryService.BLL.Interfaces;
using InventoryService.BLL;
using InventoryService.DAL.Interfaces;
using InventoryService.DAL;

var builder = WebApplication.CreateBuilder(args);

// ======== Logging ========
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/inventory-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ======== Database (Redis) ========
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection") 
    ?? "redis:6379";

var redis = ConnectionMultiplexer.Connect(redisConnection);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// ======== Dependency Injection ========
builder.Services.AddScoped<IInventoryDAL, InventoryDAL>();
builder.Services.AddScoped<IInventoryBLLService, InventoryBLLService>();

// ======== Controllers & Swagger ========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Log.Information("InventoryService is starting...");
app.Run();
