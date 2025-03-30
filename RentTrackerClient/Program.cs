using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RentTrackerClient;
using RentTrackerClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:7000") });

// Register services
builder.Services.AddScoped<RentalPropertyService>();
builder.Services.AddScoped<RentalPaymentService>();
builder.Services.AddScoped<AttachmentService>();

await builder.Build().RunAsync();
