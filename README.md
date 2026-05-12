# NZBlood Directed Transfer - Blazor Migration

This project is a side-by-side Blazor Server replacement for the Wisej directed transfer workflow in:

```text
C:\Users\RobertHyndman\OneDrive - Altara Limited\Dev\General\NZBlood.DirectedTransferWI
```

The visual direction and configuration pattern follow:

```text
C:\Users\RobertHyndman\OneDrive - Altara Limited\Dev\General\NZBlood.ApprovalWorkflowsWI\NZBlood.ApprovalWorkflows.Blazor
```

## Current State

The first Blazor migration pass is functional in mock mode and builds cleanly.

Verified command:

```powershell
dotnet build .\NZBlood.DirectedTransfer.Blazor.csproj
```

Last known result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

Local mock-mode URL used during development:

```text
http://localhost:5222
```

## Technology Choices

- Framework: Blazor Server on `net8.0-windows`.
- UI library: Syncfusion Blazor.
- PDF: Syncfusion HTML-to-PDF converter.
- Data access: `System.Data.SqlClient`.
- Identity: mirrors the Wisej approach by resolving the Windows/domain user through `HttpContext.User.Identity.Name` with an environment username fallback.
- Styling: Office 365-like internal app styling copied/adapted from the Approval Workflows Blazor app.

## Migrated Functionality

- Point of Use site selector.
- Pick From site display, resolved from `nzbSiteOptions`.
- Order reference entry.
- Refresh, Print, and Process actions.
- Directed transfer item grid using Syncfusion.
- Inline editing for `QTY to Order` only.
- UOM is read-only in the Blazor migration.
- Validation prevents negative quantities and caps quantity at available quantity.
- Process confirmation dialog.
- SQL-backed service for live mode.
- Mock service for local UI review.
- Report generation from HTML to PDF.
- Generated PDF browser download endpoint.
- Email service using the selected site's `SiteTransferEmailAddress`.

## Important Files

- `Scripts/Publish Web Application.ps1`  
  Publishes the Blazor app to `.\publish`.

- `Scripts/Update-GitHub.ps1`  
  Initializes/updates a Git repository, builds the project, commits changes, and pushes to GitHub.

- `Program.cs`  
  Registers Razor components, Syncfusion, services, mock/live mode, and the report download endpoint.

- `Components/Pages/Home.razor`  
  Main directed transfer workflow page.

- `Services/DirectedTransferService.cs`  
  Live SQL implementation for sites, items, and transfer creation.

- `Services/MockDirectedTransferService.cs`  
  Mock implementation for local development without SQL access.

- `Services/DirectedTransferReportService.cs`  
  Builds the HTML report and converts it to PDF using Syncfusion.

- `Services/DirectedTransferEmailService.cs`  
  Sends the PDF to `SiteTransferEmailAddress`.

- `Services/UserContextService.cs`  
  Resolves the current user and email address.

- `wwwroot/app.css`  
  App styling aligned to the Approval Workflows Blazor project.

- `wwwroot/brand_logo.png`  
  Shared NZ Blood logo.

- `appsettings.json`  
  Base live-mode configuration.

- `appsettings.Development.json`  
  Development override. Currently sets `DirectedTransfer:UseMockData=true`.

## Expected SQL Objects

The live service expects these existing SQL objects in the configured database:

- `nzbDirectedTransferItems`
- `nzbSiteOptions`
- `IV40700`
- `IV00103`
- `nzbCalculateDirectedTransferWI`
- `nzbDirectedTransferHdr`
- `nzbDirectedTransferLne`
- `nzbDirectedTransferEmailLne`
- `nzbCreateDirectedTransfer`

The source Wisej project has SQL scripts under:

```text
..\NZBlood.DirectedTransferWI\ExtObjects
```

## Configuration

Base config:

```json
"DirectedTransfer": {
  "DomainName": "NZBLOOD",
  "AppName": "Directed Transfer",
  "MainMessage": "IMPORTANT: For next day delivery please order by 12pm",
  "UseMockData": false,
  "RequireHttpsRedirection": false
}
```

Connection string:

```json
"ConnectionStrings": {
  "DirectedTransfer": "Server=NZBLOOD;Database=NZBS;User ID=<sql user>;Password=<sql password>;TrustServerCertificate=True;"
}
```

SMTP:

```json
"Smtp": {
  "Host": "",
  "Port": 25,
  "From": "directedtransfer@nzblood.co.nz",
  "EnableSsl": false
}
```

Use user secrets or environment configuration for real credentials and the Syncfusion license key. Avoid committing live credentials.

Examples:

```powershell
dotnet user-secrets set "ConnectionStrings:DirectedTransfer" "<connection string>"
dotnet user-secrets set "Syncfusion:LicenseKey" "<license key>"
dotnet user-secrets set "Smtp:Host" "<smtp host>"
```

## Local Development

Run in mock mode:

```powershell
dotnet run --urls http://localhost:5222
```

Run against SQL locally by overriding:

```powershell
dotnet user-secrets set "DirectedTransfer:UseMockData" "false"
dotnet user-secrets set "ConnectionStrings:DirectedTransfer" "<connection string>"
dotnet run --urls http://localhost:5222
```

If a running app locks the build output, either stop the app or verify with:

```powershell
dotnet build .\NZBlood.DirectedTransfer.Blazor.csproj -o .\.verify-build
```

## Publish And Git Scripts

Publish:

```powershell
.\Scripts\Publish Web Application.ps1
```

Update GitHub:

```powershell
.\Scripts\Update-GitHub.ps1 -Message "_2026.5.12"
```

The Git script defaults to:

```text
https://github.com/rhyndman17/NZBlood.DirectedTransfer.Blazor.git
```

Override if needed:

```powershell
.\Scripts\Update-GitHub.ps1 -RemoteUrl "https://github.com/<owner>/<repo>.git" -RemoteName "origin"
```

## Known Follow-Up Work

- Compare the generated PDF against the sample Crystal PDFs in `..\NZBlood.DirectedTransferWI\SamplePDF`.
- Tune report spacing, columns, and page breaks to better match the Crystal report.
- Test live SQL reads against the dev database.
- Test Process against a controlled POU site and confirm Panatracker rows are created correctly.
- Confirm whether the SQL-side `nzbCreateDirectedTransfer` procedure should raise errors on failure instead of returning a result set from its catch block.
- Confirm SMTP settings and whether relay allows the IIS app server.
- Add IIS Windows Authentication if required for production identity.
- Consider adding a small SQL wrapper procedure for transfer creation if transaction boundaries need to include header/line inserts plus `nzbCreateDirectedTransfer`.
