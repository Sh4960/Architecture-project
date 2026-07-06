using System.Net.Http.Json;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// --- תוספת: רישום שירותי ה-Swagger בשרת ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// רישום שירותי ה-YARP Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// רישום HttpClient עבור ה-BFF
builder.Services.AddHttpClient();

var app = builder.Build();

// --- תוספת: הפעלת ה-Swagger (חובה לפני ה-YARP!) ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // 1. הסוואגר המקומי של ה-Gateway עצמו (כולל ה-BFF)
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway (BFF) v1");

    // 2. משיכת הסוואגר של שירות ההזמנות דרך הצינור של YARP
    c.SwaggerEndpoint("/api/orders/swagger/v1/swagger.json", "Orders Service v1");

    // 3. משיכת הסוואגר של שירות הקטלוג דרך הצינור של YARP
    c.SwaggerEndpoint("/api/products/swagger/v1/swagger.json", "Product Catalog v1");
    
    c.SwaggerEndpoint("/api/inventory/swagger/v1/swagger.json", "Inventory Service");
    c.SwaggerEndpoint("/api/notification/swagger/v1/swagger.json", "Notification Service");
    
    c.RoutePrefix = "swagger";
});

// נקודת קצה של ה-BFF
app.MapGet("/api/bff/order-details/{id}", async (int id, HttpClient http) =>
{
    try
    {
        // תיקון פורט: שינוי מ-8080 ל-80 (בהתאם ל-docker-compose)
        var order = await http.GetFromJsonAsync<OrderDto>($"http://order-service:80/api/orders/{id}");
        if (order == null) return Results.NotFound($"Order {id} not found");

        // תיקון פורט: שינוי מ-8080 ל-80
        var product = await http.GetFromJsonAsync<ProductDto>($"http://product-catalog-service-1:80/api/products/{order.ProductId}");

        var bffResponse = new
        {
            OrderId = order.Id,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            ProductName = product?.Name ?? "Unknown Product",
            ProductDescription = product?.Description ?? "No description available"
        };

        return Results.Ok(bffResponse);
    }
    catch (Exception ex)
    {
        return Results.Problem($"BFF Error: {ex.Message}");
    }
});

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