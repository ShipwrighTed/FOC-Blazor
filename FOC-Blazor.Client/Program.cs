using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection; // (top of file if missing)
using System.Net.Http; // Add this using directive
using Microsoft.Extensions.Http; // Add this using directive


var builder = WebAssemblyHostBuilder.CreateDefault(args);


// ... rest of your code remains unchanged
// Named HttpClients (API is same-origin, camera fetches go direct to devices)
builder.Services.AddHttpClient("api", c =>
{
    c.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});
builder.Services.AddHttpClient("camera");

// Client-side polling service
builder.Services.AddScoped<ICameraPollingService, CameraPollingService>();


await builder.Build().RunAsync();

