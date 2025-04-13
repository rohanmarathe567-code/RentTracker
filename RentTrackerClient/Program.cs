using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using RentTrackerClient;
using RentTrackerClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddProvider(new ClientLoggerProvider(LogLevel.Debug));

// Configure HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:7000") });

// Add Authentication Services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

// Add Authentication Service
builder.Services.AddScoped(sp =>
    new AuthenticationService(
        sp.GetRequiredService<HttpClient>(),
        "http://localhost:7000",
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
    new RentalPaymentService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<RentalPaymentService>>(),
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

await builder.Build().RunAsync();
