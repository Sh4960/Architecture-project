using System.Net.Http.Json;
using Serilog;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "ApiGateway")
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// --- תוספת: רישום שירותי ה-Swagger בשרת ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// רישום שירותי ה-YARP Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// רישום HttpClient עבור ה-BFF
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
    }

    context.Request.Headers["X-Correlation-ID"] = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;

    await next();
});

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");

// --- תוספת: הפעלת ה-Swagger (חובה לפני ה-YARP!) ---
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        if (!httpReq.Host.Host.Contains("localhost"))
            return;
    });
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
    c.SwaggerEndpoint("/api/orders/swagger/v1/swagger.json", "Orders Service v1");
    c.SwaggerEndpoint("/api/products/swagger/v1/swagger.json", "Product Catalog v1");
    c.SwaggerEndpoint("/api/inventory/swagger/v1/swagger.json", "Inventory Service v1");
    c.SwaggerEndpoint("/api/notification/swagger/v1/swagger.json", "Notification Service v1");
    c.RoutePrefix = "";
});

// נקודת קצה של ה-BFF
app.MapGet("/api/bff/order-details/{id}", async (int id, HttpClient http, HttpContext httpContext) =>
{
    try
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        var orderRequest = new HttpRequestMessage(HttpMethod.Get, $"http://order-service:80/api/orders/{id}");
        orderRequest.Headers.Add("X-Correlation-ID", correlationId);
        var orderResponse = await http.SendAsync(orderRequest);
        if (!orderResponse.IsSuccessStatusCode)
            return Results.StatusCode((int)orderResponse.StatusCode);

        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        if (order == null) return Results.NotFound($"Order {id} not found");

        var productRequest = new HttpRequestMessage(HttpMethod.Get, $"http://product-catalog-service-1:80/api/products/{order.ProductId}");
        productRequest.Headers.Add("X-Correlation-ID", correlationId);
        var productResponse = await http.SendAsync(productRequest);
        ProductDto? product = null;
        if (productResponse.IsSuccessStatusCode)
        {
            product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
        }

        var bffResponse = new
        {
            OrderId = order.Id,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            ProductName = product?.Name ?? "Unknown Product",
            ProductDescription = product?.Description ?? "No description available",
            CorrelationId = correlationId
        };

        return Results.Ok(bffResponse);
    }
    catch (Exception ex)
    {
        return Results.Problem($"BFF Error: {ex.Message}");
    }
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// הפעלת ה-Gateway של YARP (בסוף הקוד, כדי שלא יבלע את ה-Swagger)
app.MapReverseProxy();

app.Run();

// ה-DTOs עבור ה-BFF
public class OrderDto
{
    public int Id { get; set; }
    public int ProductId { get; set; } 
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}