using MongoDB.Driver;
using Serilog;
using ProductCatalogService.BLL.Interfaces;
using ProductCatalogService.BLL;
using ProductCatalogService.DAL.Interfaces;
using ProductCatalogService.DAL;

var builder = WebApplication.CreateBuilder(args);

// ======== Logging ========
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/product-catalog-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ======== MongoDB ========
var mongoConnection = builder.Configuration.GetConnectionString("MongoDbConnection") 
    ?? "mongodb://mongo:27017";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnection));

// ======== Dependency Injection ========
builder.Services.AddScoped<IProductDAL, ProductDAL>();
builder.Services.AddScoped<IProductBLLService, ProductBLLService>();

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

Log.Information("ProductCatalogService is starting...");
app.Run();
