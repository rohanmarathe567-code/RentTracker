using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using RentTrackerBackend.Data;
using RentTrackerBackend.Services;
using RentTrackerBackend.Endpoints;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.IIS;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

try 
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:5112", "http://localhost:5113", "http://localhost:7000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .WithExposedHeaders("Content-Disposition", "Content-Length");  // Required for file downloads
        });
    });

    // Configure maximum request body size for file uploads (50MB)
    builder.Services.Configure<IISServerOptions>(options =>
    {
        options.MaxRequestBodySize = 52428800; // 50MB in bytes
    });

    builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = 52428800; // 50MB in bytes
    });

    // Add antiforgery services
    builder.Services.AddAntiforgery();

    // Configure JSON serialization
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    // Configure Swagger/OpenAPI
    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    });

    // Register Storage and Attachment Services
    builder.Services.AddScoped<IStorageService, FileService>();
    builder.Services.AddScoped<IAttachmentService, AttachmentService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<DatabaseSeeder>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAntiforgery();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        
        // Apply migrations
        await dbContext.Database.MigrateAsync();
        
        // Seed initial data
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
// Map controllers and endpoints
app.MapControllers();
app.MapHealthEndpoints();
app.MapPropertyEndpoints();
app.MapPaymentEndpoints();
app.MapAttachmentEndpoints(); 
app.MapPaymentMethodEndpoints();

    // Create uploads directory if it doesn't exist
    var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
    Directory.CreateDirectory(uploadsDir);
    
    // Create subdirectories for different attachment types
    Directory.CreateDirectory(Path.Combine(uploadsDir, "property"));
    Directory.CreateDirectory(Path.Combine(uploadsDir, "payment"));

    app.Run();
}
catch (HostAbortedException)
{
    // Ignore HostAbortedException as it's an expected part of the shutdown process
    Log.Information("Host aborted - shutting down gracefully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}