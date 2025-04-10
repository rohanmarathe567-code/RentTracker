using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
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

// Register services with logging
builder.Services.AddScoped(sp =>
    new RentalPropertyService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<RentalPropertyService>>()
    )
);
builder.Services.AddScoped(sp =>
    new RentalPaymentService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<RentalPaymentService>>()
    )
);
builder.Services.AddScoped(sp =>
    new AttachmentService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<AttachmentService>>()
    )
);

// Register PaymentMethodService with logging
builder.Services.AddScoped(sp =>
    new PaymentMethodService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<PaymentMethodService>>()
    )
);

await builder.Build().RunAsync();
