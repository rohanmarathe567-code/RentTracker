
using Microsoft.EntityFrameworkCore;
using RentTracker.Data;
using RentTracker.Models;
using RentTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

// Register FileService
builder.Services.AddScoped<FileService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// API Endpoints

// Health Check
app.MapGet("/api/health", async (ApplicationDbContext db) =>
{
    try
    {
        // Test database connection by attempting a simple query
        await db.Database.CanConnectAsync();
        return Results.Ok(new { Status = "Healthy", Database = "Connected" });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Database Connection Failed",
            extensions: new Dictionary<string, object>
            {
                { "Status", "Unhealthy" },
                { "Database", "Disconnected" }
            }
        );
    }
});

// Rental Properties
app.MapGet("/api/properties", async (ApplicationDbContext db) =>
    await db.RentalProperties.ToListAsync());

app.MapGet("/api/properties/{id}", async (int id, ApplicationDbContext db) =>
    await db.RentalProperties.FindAsync(id) is RentalProperty property
        ? Results.Ok(property)
        : Results.NotFound());

app.MapPost("/api/properties", async (RentalProperty property, ApplicationDbContext db) =>
{
    property.CreatedAt = DateTime.UtcNow;
    property.UpdatedAt = DateTime.UtcNow;
    
    db.RentalProperties.Add(property);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/properties/{property.Id}", property);
});

app.MapPut("/api/properties/{id}", async (int id, RentalProperty updatedProperty, ApplicationDbContext db) =>
{
    var property = await db.RentalProperties.FindAsync(id);
    
    if (property == null)
        return Results.NotFound();
    
    property.Address = updatedProperty.Address;
    property.Suburb = updatedProperty.Suburb;
    property.State = updatedProperty.State;
    property.PostCode = updatedProperty.PostCode;
    property.Description = updatedProperty.Description;
    property.WeeklyRentAmount = updatedProperty.WeeklyRentAmount;
    property.LeaseStartDate = updatedProperty.LeaseStartDate;
    property.LeaseEndDate = updatedProperty.LeaseEndDate;
    property.PropertyManager = updatedProperty.PropertyManager;
    property.PropertyManagerContact = updatedProperty.PropertyManagerContact;
    property.UpdatedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    
    return Results.NoContent();
});

app.MapDelete("/api/properties/{id}", async (int id, ApplicationDbContext db) =>
{
    var property = await db.RentalProperties.FindAsync(id);
    
    if (property == null)
        return Results.NotFound();
    
    db.RentalProperties.Remove(property);
    await db.SaveChangesAsync();
    
    return Results.NoContent();
});

// Rental Payments
app.MapGet("/api/properties/{propertyId}/payments", async (int propertyId, ApplicationDbContext db) =>
    await db.RentalPayments
        .Where(p => p.RentalPropertyId == propertyId)
        .OrderByDescending(p => p.PaymentDate)
        .ToListAsync());

app.MapGet("/api/payments/{id}", async (int id, ApplicationDbContext db) =>
    await db.RentalPayments.FindAsync(id) is { } payment
        ? Results.Ok(payment)
        : Results.NotFound());

app.MapPost("/api/payments", async (RentalPayment payment, ApplicationDbContext db) =>
{
    payment.CreatedAt = DateTime.UtcNow;
    payment.UpdatedAt = DateTime.UtcNow;
    
    db.RentalPayments.Add(payment);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/payments/{payment.Id}", payment);
});

app.MapPut("/api/payments/{id}", async (int id, RentalPayment updatedPayment, ApplicationDbContext db) =>
{
    var payment = await db.RentalPayments.FindAsync(id);
    
    if (payment == null)
        return Results.NotFound();
    
    payment.Amount = updatedPayment.Amount;
    payment.PaymentDate = updatedPayment.PaymentDate;
    payment.PaymentMethod = updatedPayment.PaymentMethod;
    payment.PaymentReference = updatedPayment.PaymentReference;
    payment.Notes = updatedPayment.Notes;
    payment.UpdatedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    
    return Results.NoContent();
});

app.MapDelete("/api/payments/{id}", async (int id, ApplicationDbContext db) =>
{
    var payment = await db.RentalPayments.FindAsync(id);
    
    if (payment == null)
        return Results.NotFound();
    
    db.RentalPayments.Remove(payment);
    await db.SaveChangesAsync();
    
    return Results.NoContent();
});

// Attachments
app.MapGet("/api/attachments/{id}", async (int id, ApplicationDbContext db) =>
    await db.Attachments.FindAsync(id) is { } attachment
        ? Results.Ok(attachment)
        : Results.NotFound());

app.MapGet("/api/properties/{propertyId}/attachments", async (int propertyId, ApplicationDbContext db) =>
    await db.Attachments
        .Where(a => a.RentalPropertyId == propertyId)
        .OrderByDescending(a => a.UploadDate)
        .ToListAsync());

app.MapGet("/api/payments/{paymentId}/attachments", async (int paymentId, ApplicationDbContext db) =>
    await db.Attachments
        .Where(a => a.RentalPaymentId == paymentId)
        .OrderByDescending(a => a.UploadDate)
        .ToListAsync());

// File Upload and Download endpoints
app.MapPost("/api/properties/{propertyId}/attachments", async (int propertyId, HttpRequest request, FileService fileService, ApplicationDbContext db) =>
{
    // Check if property exists
    var property = await db.RentalProperties.FindAsync(propertyId);
    if (property == null)
        return Results.NotFound("Property not found");

    // Check if the request contains files
    if (!request.HasFormContentType || request.Form.Files.Count == 0)
        return Results.BadRequest("No files were uploaded");

    var file = request.Form.Files[0];
    var description = request.Form["description"].ToString();

    // Save the file
    var attachment = await fileService.SaveFileAsync(file, description, propertyId, null);
    
    return Results.Created($"/api/attachments/{attachment.Id}", attachment);
});

app.MapPost("/api/payments/{paymentId}/attachments", async (int paymentId, HttpRequest request, FileService fileService, ApplicationDbContext db) =>
{
    // Check if payment exists
    var payment = await db.RentalPayments.FindAsync(paymentId);
    if (payment == null)
        return Results.NotFound("Payment not found");

    // Check if the request contains files
    if (!request.HasFormContentType || request.Form.Files.Count == 0)
        return Results.BadRequest("No files were uploaded");

    var file = request.Form.Files[0];
    var description = request.Form["description"].ToString();

    // Save the file
    var attachment = await fileService.SaveFileAsync(file, description, null, paymentId);
    
    return Results.Created($"/api/attachments/{attachment.Id}", attachment);
});

app.MapGet("/api/attachments/{id}/download", async (int id, FileService fileService) =>
{
    try
    {
        var (fileStream, contentType, fileName) = await fileService.GetFileAsync(id);
        return Results.File(fileStream, contentType, fileName);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound("File not found");
    }
});

app.MapDelete("/api/attachments/{id}", async (int id, FileService fileService) =>
{
    try
    {
        await fileService.DeleteFileAsync(id);
        return Results.NoContent();
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound("Attachment not found");
    }
});

// Create uploads directory if it doesn't exist
var uploadsDir = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}

app.Run();