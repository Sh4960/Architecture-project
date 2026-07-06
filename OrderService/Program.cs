using Microsoft.EntityFrameworkCore;
using Serilog;
using OrderService.BLL.Interfaces;
using OrderService.BLL;
using OrderService.DAL.Interfaces;
using OrderService.DAL;

var builder = WebApplication.CreateBuilder(args);

// ======== Logging ========
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/order-service-.txt", rollingInterval: RollingInterval.Day)
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
builder.Services.AddHttpClient<OrderBLLService>();

// ======== Controllers & Swagger ========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ======== Database Migration ========
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.Migrate();
}

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Log.Information("OrderService is starting...");
app.Run();
