using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Data;
using RentTrackerBackend.Services;
using RentTrackerBackend.Endpoints;
using Serilog;
using Serilog.Events;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
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
            policy.WithOrigins("http://localhost:5112", "http://localhost:7000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

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

    // Register FileService and Services
    builder.Services.AddScoped<FileService>();
    builder.Services.AddScoped<IAttachmentService, AttachmentService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapOpenApi();
    }

    app.UseCors();

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
    var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
    Directory.CreateDirectory(uploadsDir);
    
    // Create subdirectories for different attachment types
    Directory.CreateDirectory(Path.Combine(uploadsDir, "property"));
    Directory.CreateDirectory(Path.Combine(uploadsDir, "payment"));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}