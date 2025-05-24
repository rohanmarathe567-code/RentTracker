using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using RentTrackerClient;
using RentTrackerClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddProvider(new ClientLoggerProvider(LogLevel.Debug));

// Configure HttpClient with JSON serialization options
builder.Services.AddScoped(sp =>
{
    var client = new HttpClient { BaseAddress = new Uri(builder.Configuration["BaseApiUrl"] ?? "http://localhost:7000") };
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    return client;
});

// Add JSON options
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNameCaseInsensitive = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.Converters.Add(new JsonStringEnumConverter());
});

// Add Authentication Services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

// Add Authentication Service
builder.Services.AddScoped(sp =>
    new AuthenticationService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<HttpClient>().BaseAddress!.ToString(),
        sp.GetRequiredService<AuthenticationStateProvider>(),
        sp.GetRequiredService<ILogger<AuthenticationService>>(),
        sp.GetRequiredService<ILocalStorageService>()
    )
);
builder.Services.AddScoped<IAuthenticationService>(sp => sp.GetRequiredService<AuthenticationService>());

// Register services with logging and authentication
builder.Services.AddScoped(sp =>
    new RentalPropertyService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<RentalPropertyService>>(),
        sp.GetRequiredService<IAuthenticationService>()
    )
);
builder.Services.AddScoped(sp =>
    new AttachmentService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<AttachmentService>>(),
        sp.GetRequiredService<IAuthenticationService>()
    )
);

// Register PaymentMethodService with logging
builder.Services.AddScoped(sp =>
    new PaymentMethodService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<PaymentMethodService>>(),
        sp.GetRequiredService<IAuthenticationService>()
    )
);

// Register PropertyTransactionService with logging
builder.Services.AddScoped(sp =>
    new PropertyTransactionService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<PropertyTransactionService>>(),
        sp.GetRequiredService<IAuthenticationService>()
    )
);

// Register TransactionCategoryService with logging and authentication
builder.Services.AddScoped(sp =>
    new TransactionCategoryService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<TransactionCategoryService>>(),
        sp.GetRequiredService<IAuthenticationService>()
    )
);

await builder.Build().RunAsync();
