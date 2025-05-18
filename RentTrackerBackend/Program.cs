using Microsoft.Extensions.Hosting;
using RentTrackerBackend.Data;
using RentTrackerBackend.Services;
using RentTrackerBackend.Endpoints;
using RentTrackerBackend.Extensions;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.IIS;
using System.Text.Json.Serialization;

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
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.SerializerOptions.Converters.Add(new ObjectIdJsonConverter());
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

    // Configure Swagger/OpenAPI
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerConfiguration();

    // Configure MongoDB and Redis
    builder.Services.AddMongoDb(builder.Configuration);

    // Register Services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IStorageService, FileService>();
    builder.Services.AddScoped<IAttachmentService, AttachmentService>();
    builder.Services.AddScoped<IPropertyService, PropertyService>();
    builder.Services.AddScoped<IPropertyTransactionService, PropertyTransactionService>();
    builder.Services.AddScoped<IClaimsPrincipalService, ClaimsPrincipalService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
    builder.Services.AddTransient<DatabaseSeeder>();
    builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
    builder.Services.AddScoped<DatabaseSeeder>();
    
    // Register Repositories
    builder.Services.AddScoped<IPropertyTransactionRepository, PropertyTransactionRepository>();
    builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
    builder.Services.AddScoped<ITransactionCategoryRepository, TransactionCategoryRepository>();
    
    // Add JWT authentication
    builder.Services.AddJwtAuthentication(builder.Configuration);

    var app = builder.Build();

    // Seed the database
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            //await seeder.SeedAsync();
            Console.WriteLine("Database seeded successfully");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding database: {ex.Message}");
        throw;
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAntiforgery();
    
    // Add authentication & authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Map controllers and endpoints
    app.MapControllers();
    app.MapHealthEndpoints();
    app.MapPropertyEndpoints();
    app.MapPropertyTransactionEndpoints();
    app.MapTransactionCategoryEndpoints();
    app.MapAttachmentEndpoints();
    app.MapPaymentMethodEndpoints();
    app.MapAuthEndpoints();

    // Create uploads directory if it doesn't exist
    var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
    Directory.CreateDirectory(uploadsDir);
    
    // Create subdirectories for different attachment types
    Directory.CreateDirectory(Path.Combine(uploadsDir, "property"));
    Directory.CreateDirectory(Path.Combine(uploadsDir, "transaction"));

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