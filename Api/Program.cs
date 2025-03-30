using Microsoft.EntityFrameworkCore;
using RentTracker.Data;
using RentTracker.Models;
using RentTracker.Services;
using RentTracker.Api;
using RentTracker.Api.Endpoints;

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

// Map API endpoints from controllers
app.MapHealthEndpoints();
app.MapPropertyEndpoints();
app.MapPaymentEndpoints();
app.MapAttachmentEndpoints();

// Create uploads directory if it doesn't exist
var uploadsDir = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}

app.Run();