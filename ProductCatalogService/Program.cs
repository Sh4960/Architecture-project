using MongoDB.Driver;
using Serilog;
using Serilog.Context;
using StackExchange.Redis;
using ProductCatalogService.BLL.Interfaces;
using ProductCatalogService.BLL;
using ProductCatalogService.DAL.Interfaces;
using ProductCatalogService.DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// ======== Logging ========
var containerName = builder.Configuration["CONTAINER_NAME"] ?? "ProductCatalogService";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "ProductCatalogService")
    .Enrich.WithProperty("ContainerName", containerName)
    .WriteTo.Console()
    .WriteTo.File("Logs/product-catalog-service-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ======== Redis ========
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection") 
    ?? "redis:6379";

IConnectionMultiplexer redis = null;
int maxRetries = 10;
int retries = 0;
int delayMs = 2000;

while (retries < maxRetries && redis == null)
{
    try
    {
        redis = ConnectionMultiplexer.Connect(redisConnection);
        Log.Information("Successfully connected to Redis after {Retries} attempt(s)", retries + 1);
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
        Thread.Sleep(delayMs);
    }
}
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// ======== MongoDB ========
var mongoConnection = builder.Configuration.GetConnectionString("MongoDbConnection") 
    ?? "mongodb://mongo:27017";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnection));

// ======== Dependency Injection ========
builder.Services.AddScoped<IProductDAL, ProductDAL>();
builder.Services.AddScoped<IProductBLLService, ProductBLLService>();
builder.Services.AddScoped<ICacheService, RedisCache>();

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

    using (LogContext.PushProperty("CorrelationId", correlationId))
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

Log.Information("ProductCatalogService is starting...");
app.Run();
