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

The Blazor migration is functional in mock mode and has been tested on the dev IIS server against live SQL. The workflow now begins with an active order-form selection; the form supplies the POU site, Pick From site, and item list.

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

- Active order-form selector, displayed as `OrderFormID : Description`.
- Read-only Point of Use and Pick From site fields supplied by `nzbDirectedTransferOrderForms` and shown beside one another after selection.
- Order reference entry.
- Refresh, Print, and Process actions. Refresh reloads the selected order form and rebuilds its item list.
- Directed transfer item list with Zone, Zone/Priority ordering, paging, filtering, ordered-only view, and inline quantity entry.
- Inline editing for `QTY to Order` only.
- UOM is read-only in the Blazor migration.
- Validation prevents negative quantities and displays line-level warnings when quantity ordered is greater than available quantity. The entered value remains visible for user correction.
- Print is enabled once a site has loaded items. Process is enabled only when at least one line has a quantity to order.
- Process confirmation dialog.
- SQL-backed service for live mode.
- Mock service for local UI review.
- Report generation from HTML to PDF.
- Print generates and downloads a portrait pick-list PDF directly. It includes all item rows, leaves `Qty To Order` as a write-in underline, and does not require entered quantities.
- Print and processed reports identify the selected order form as `OrderFormID : Description`, separately from the user-entered order reference.
- Process creates the transfer, generates and downloads the processed PDF, and optionally emails it depending on `Smtp:SendEmail`. The process report includes only ordered rows and shows the entered `Qty To Order` values.
- Email service uses the selected site's `SiteTransferEmailAddress` when enabled.
- Configurable IIS path base and default page size.
- Permanent `DirectedTransfer:MainMessage` notice displayed in the selection panel.
- NZ Blood favicon and header branding.

## Important Files

- `Scripts/Publish Web Application.ps1`  
  Assigns a `yy.MM.dd.HHmm` deployment version, updates `BuildInfo.cs`, and publishes the Blazor app to `.\publish`. The output also contains `version.txt`.

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

- `Sql Objects`  
  Local copy of the directed transfer SQL object scripts from the Wisej project.

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

- `nzbDirectedTransferOrderForms`
- `nzbDirectedTransferOrderFormItems`
- `nzbSiteOptions`
- `IV40700`
- `IV00103`
- `nzbCalculateDirectedTransferWI`
- `nzbCalculateDirectedTransferItemWI`
- `nzbDirectedTransferHdr`
- `nzbDirectedTransferLne`
- `nzbDirectedTransferEmailLne`
- `nzbCreateDirectedTransfer`

SQL scripts are included in this project under:

```text
Sql Objects
```

The source Wisej project also has the original scripts under:

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
  "RequireHttpsRedirection": false,
  "PathBase": "/NZBlood.DirectedTransfer.Blazor",
  "DefaultPageSize": 50,
  "DataProtectionKeysPath": "C:\\ProgramData\\NZBlood\\DirectedTransfer\\DataProtection-Keys"
}
```

`PathBase` must match the IIS application alias. For production, use a cleaner alias such as `/DirectedTransfer` and update `PathBase` accordingly. Leave it blank if the app is hosted at a dedicated site root.

`DefaultPageSize` controls the initial item-table page size. The UI caps selectable page size at 250.

Connection string:

```json
"ConnectionStrings": {
  "DirectedTransfer": "Server=NZBLOOD;Database=NZBS;User ID=<sql user>;Password=<sql password>;TrustServerCertificate=True;"
}
```

SMTP:

```json
"Smtp": {
  "SendEmail": 0,
  "Host": "smtp.nzblood.co.nz",
  "Port": 25,
  "From": "directedtransfer@nzblood.co.nz",
  "EnableSsl": false,
  "UserName": "",
  "Password": ""
}
```

`Smtp:SendEmail=0` disables SMTP. This is recommended for dev process testing. `Smtp:SendEmail=1` enables SMTP on Process. Print never sends email.

Use user secrets, IIS environment variables, or server-local configuration for real credentials and the Syncfusion license key. Avoid committing live credentials.

Examples:

```powershell
dotnet user-secrets set "ConnectionStrings:DirectedTransfer" "<connection string>"
dotnet user-secrets set "Syncfusion:LicenseKey" "<license key>"
dotnet user-secrets set "Smtp:SendEmail" "0"
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

The version defaults to the current local time in `yy.MM.dd.HHmm` format. A specific version can be supplied when reproducing a deployment:

```powershell
.\Scripts\Publish Web Application.ps1 -Version "26.07.20.1430"
```

The publish script clears `.\publish` before publishing to avoid nested publish output. If a dev IIS deployment has had several partial file copies and starts showing runtime/dependency errors, perform a full clean deploy from `.\publish`.

For a narrow hotfix where dependencies and configuration have not changed, deploying the new `publish\NZBlood.DirectedTransfer.Blazor.dll` and recycling the IIS app pool is sufficient for UI/report-code-only changes. Use a full clean deploy when package dependencies, runtime files, configuration files, or static assets change.

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

- Compare the generated process PDF against the sample Crystal PDFs in `..\NZBlood.DirectedTransferWI\SamplePDF`.
- Tune report page breaks further if production item volumes expose wrapping issues.
- Test Process against a controlled POU site and confirm Panatracker rows are created correctly.
- Confirm whether the SQL-side `nzbCreateDirectedTransfer` procedure should raise errors on failure instead of returning a result set from its catch block.
- Confirm SMTP settings and whether relay allows the IIS app server before enabling `Smtp:SendEmail=1`.
- Add IIS Windows Authentication if required for production identity.
- Consider adding a small SQL wrapper procedure for transfer creation if transaction boundaries need to include header/line inserts plus `nzbCreateDirectedTransfer`.
