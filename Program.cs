using Microsoft.Extensions.Caching.Memory;
using NZBlood.DirectedTransfer.Blazor.Components;
using NZBlood.DirectedTransfer.Blazor.Services;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSyncfusionBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IDirectedTransferReportService, DirectedTransferReportService>();
builder.Services.AddScoped<IDirectedTransferEmailService, DirectedTransferEmailService>();
builder.Services.AddScoped<IDirectedTransferService>(sp =>
    builder.Configuration.GetValue<bool>("DirectedTransfer:UseMockData")
        ? ActivatorUtilities.CreateInstance<MockDirectedTransferService>(sp)
        : ActivatorUtilities.CreateInstance<DirectedTransferService>(sp));

var app = builder.Build();

var syncfusionLicenseKey = builder.Configuration["Syncfusion:LicenseKey"];
if (!string.IsNullOrWhiteSpace(syncfusionLicenseKey))
{
    SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    if (builder.Configuration.GetValue<bool>("DirectedTransfer:RequireHttpsRedirection"))
    {
        app.UseHsts();
    }
}

if (builder.Configuration.GetValue<bool>("DirectedTransfer:RequireHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/reports/directed-transfer/{reportId}", (string reportId, IMemoryCache cache) =>
{
    return cache.TryGetValue<GeneratedReportFile>(reportId, out var report) && report is not null
        ? Results.File(report.Content, "application/pdf", report.FileName)
        : Results.NotFound();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
