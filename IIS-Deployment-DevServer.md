# IIS Deployment - Dev Server

This guide covers testing and publishing `NZBlood.DirectedTransfer.Blazor` to the dev IIS server.

## 1. Prerequisites On Dev Server

Confirm these before publishing:

- .NET 8 Hosting Bundle is installed.
- IIS is installed with ASP.NET Core Module V2.
- The server can reach the GP SQL Server/database.
- The app pool identity or SQL user has required SQL permissions.
- SMTP relay is reachable from the server.
- Syncfusion license key is available outside source control.
- The IIS site/app can run as 64-bit. Syncfusion HTML-to-PDF ships Chromium native files under `runtimes\win-x64`.

Recommended app pool:

- .NET CLR version: `No Managed Code`
- Managed pipeline mode: `Integrated`
- Enable 32-bit applications: `False`
- Identity: confirm with infrastructure/DB permissions

## 2. SQL Permissions

SQL object scripts are included with the project under:

```text
Sql Objects
```

The configured SQL login needs read access to:

- `nzbDirectedTransferItems`
- `nzbSiteOptions`
- `IV40700`
- `IV00103`

It also needs execute access to:

- `nzbCalculateDirectedTransferWI`
- `nzbCreateDirectedTransfer`

It needs insert/read permissions for:

- `nzbDirectedTransferHdr`
- `nzbDirectedTransferLne`
- `nzbDirectedTransferEmailLne`

It may also need permissions on downstream objects used by `nzbCreateDirectedTransfer`, including Panatracker tables and `SY90000`, depending on ownership chaining and SQL security setup.

## 3. Configure Local Publish Settings

From this project folder:

```powershell
dotnet restore
dotnet build .\NZBlood.DirectedTransfer.Blazor.csproj
dotnet publish .\NZBlood.DirectedTransfer.Blazor.csproj -c Release -o .\publish
```

Or use the helper script:

```powershell
.\Scripts\Publish Web Application.ps1
```

The publish folder should include:

- `NZBlood.DirectedTransfer.Blazor.dll`
- `web.config`
- `appsettings.json`
- `wwwroot`
- Syncfusion assemblies
- `runtimes\win-x64\native\chrome.exe` and related files for HTML-to-PDF

## 4. Configure App Settings

For the dev server, set:

```json
"DirectedTransfer": {
  "UseMockData": false,
  "RequireHttpsRedirection": false,
  "DomainName": "NZBLOOD"
}
```

Set the real connection string using one of these preferred methods:

- IIS environment variable
- deployment-time transform
- protected server-local `appsettings.Production.json`
- app pool environment configuration

Avoid committing the real SQL password.

Environment variable examples on the server:

```powershell
setx ConnectionStrings__DirectedTransfer "<connection string>" /M
setx Syncfusion__LicenseKey "<syncfusion license key>" /M
setx Smtp__Host "<smtp relay host>" /M
setx Smtp__From "directedtransfer@nzblood.co.nz" /M
```

After changing machine-level environment variables, restart IIS or the app pool.

## 5. Create IIS App

Example target path:

```text
C:\inetpub\NZBlood.DirectedTransfer.Blazor
```

Recommended process:

1. Stop the IIS app pool.
2. Copy `.\publish\*` to the target folder.
3. Ensure the app pool identity has read/execute permissions on the folder.
4. Start the app pool.
5. Browse to the configured IIS URL.

If deploying under an existing website as an application, confirm the base path works with the Blazor app. The current app uses:

```html
<base href="/" />
```

If hosted below a virtual directory, this may need adjustment.

## 6. Dev Server Smoke Test

Start with read-only behaviour:

1. Open the IIS URL.
2. Confirm the header and logo render.
3. Confirm the current Windows user is shown correctly.
4. Select a POU site.
5. Confirm Pick From site is populated.
6. Confirm item rows load.
7. Confirm `QTY Pending`, `QTY Available`, and `Order Up To` match the Wisej app for the same site.
8. Enter a `QTY to Order` greater than available and confirm the UI caps or rejects it.
9. Click Print.
10. Confirm PDF is generated and the download link appears.
11. Confirm email behaviour:
    - If SMTP is configured, email should go to `SiteTransferEmailAddress`.
    - If SMTP is not configured, the app should say the PDF was generated but not emailed.

Then test processing with a controlled test case:

1. Pick a known low-risk POU site.
2. Enter a small quantity on one or two lines.
3. Click Process and confirm.
4. Confirm a transfer code is created.
5. Confirm rows in:
   - `nzbDirectedTransferHdr`
   - `nzbDirectedTransferLne`
   - `nzbDirectedTransferEmailLne`
   - `PanatrackerGP7_DirectedTransOrder`
   - `PanatrackerGP7_DirectedTransOrderUnit`
6. Confirm the processed report contains only ordered lines.
7. Confirm email delivery and PDF attachment.

## 7. Troubleshooting

Build or publish locked:

```powershell
dotnet build .\NZBlood.DirectedTransfer.Blazor.csproj -o .\.verify-build
```

IIS 500.30:

- Check Windows Event Viewer.
- Check app pool identity.
- Confirm .NET 8 Hosting Bundle is installed.
- Confirm published folder contains `web.config`.

SQL connection errors:

- Confirm `ConnectionStrings:DirectedTransfer` is loaded by the app.
- Confirm firewall/network path to SQL Server.
- Confirm SQL login permissions.
- Confirm `TrustServerCertificate=True` if the SQL certificate chain is not trusted.

PDF generation errors:

- Confirm the app is running 64-bit.
- Confirm `runtimes\win-x64\native\chrome.exe` exists in the published folder.
- Confirm the app pool identity can execute files from the published folder.
- Review application logs. The report service currently creates a simple fallback PDF if Syncfusion conversion fails.

Email errors:

- Confirm `Smtp:Host`.
- Confirm relay permissions for the IIS server.
- Confirm port and SSL requirements.
- Confirm `SiteTransferEmailAddress` is populated for the selected POU site.

## 8. Go/No-Go Checklist

- [ ] App opens in IIS.
- [ ] Windows/current user resolves as expected.
- [ ] Live SQL mode is enabled.
- [ ] POU site list loads.
- [ ] Item grid matches Wisej for at least one site.
- [ ] Quantity validation works.
- [ ] Print PDF generates.
- [ ] SMTP sends to site email.
- [ ] Process creates expected SQL/Panatracker rows.
- [ ] Process report includes only non-zero quantities.
- [ ] Browser download works after Print and Process.
- [ ] Generated PDF has been compared against the sample Crystal PDFs.
