using NuGetPackageManagerApp.AppCode;
using NuGetPackageManagerApp.AppCode.NuGet;
using NuGetPackageManagerWebApp.AppCode;
using NuGetPackageManagerWebApp.Components;
using NuGetPackageManagerWebApp.AppCode.CustomNuGetV2;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register AppDataService
builder.Services.AddSingleton<AppDataService>();

// Register NuGetFileService
builder.Services.AddSingleton<NuGetFileServiceV2>();

// Register OutsideCalls
builder.Services.AddSingleton<OutsideCalls>();

// Register VisualizeChartBackEnd
builder.Services.AddSingleton<VisualizeChartBackEnd>();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Register HttpClient
builder.Services.AddScoped(sp =>
{
    var baseAddress = builder.Configuration["BaseAddress"] ?? "https://localhost:5001"; // Default to HTTPS
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var nugetService = scope.ServiceProvider.GetRequiredService<NuGetFileServiceV2>();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
