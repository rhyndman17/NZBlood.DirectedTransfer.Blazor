using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Caching.Memory;
using NZBlood.DirectedTransfer.Blazor.Components;
using NZBlood.DirectedTransfer.Blazor.Services;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = builder.Configuration.GetValue<bool>("DirectedTransfer:DetailedCircuitErrors");
});
var dataProtectionKeysPath = builder.Configuration["DirectedTransfer:DataProtectionKeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName("NZBlood.DirectedTransfer.Blazor");
}

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
    try
    {
        SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Syncfusion license registration failed.");
    }
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

var pathBase = builder.Configuration["DirectedTransfer:PathBase"]?.TrimEnd('/');
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.Use(async (context, next) =>
    {
        var requestPath = context.Request.Path.Value ?? string.Empty;
        var isPathBaseRequest = requestPath.Equals(pathBase, StringComparison.OrdinalIgnoreCase)
            || requestPath.StartsWith(pathBase + "/", StringComparison.OrdinalIgnoreCase);

        if (isPathBaseRequest
            && (!requestPath.StartsWith(pathBase, StringComparison.Ordinal)
                || requestPath.Length == pathBase.Length))
        {
            var remainingPath = requestPath.Length > pathBase.Length
                ? requestPath[pathBase.Length..]
                : "/";
            context.Response.Redirect(pathBase + remainingPath + context.Request.QueryString);
            return;
        }

        await next();
    });

    app.UsePathBase(pathBase);
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
