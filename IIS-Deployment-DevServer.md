# IIS Deployment - Dev Server

This guide covers testing and publishing `NZBlood.DirectedTransfer.Blazor` to the dev IIS server.

## 1. Prerequisites On Dev Server

Confirm these before publishing:

- .NET 8 Hosting Bundle is installed.
- IIS is installed with ASP.NET Core Module V2.
- The server can reach the GP SQL Server/database.
- The app pool identity or SQL user has required SQL permissions.
- SMTP relay is reachable from the server if `Smtp:SendEmail` is enabled.
- Syncfusion license key is configured in server-local configuration or environment configuration.
- The IIS site/app can run as 64-bit. Syncfusion HTML-to-PDF ships Chromium native files under `runtimes\win-x64`.

Recommended app pool:

- .NET CLR version: `No Managed Code`
- Managed pipeline mode: `Integrated`
- Enable 32-bit applications: `False`
- Identity: confirm with infrastructure/DB permissions

Recommended IIS authentication for this app:

- Anonymous Authentication: `Enabled`
- Windows Authentication: `Disabled`

Users do not need to log in. With Anonymous Authentication, IIS will not prompt for credentials and the app will use `DirectedTransfer:DefaultUserId` for reports and transfer records.

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

The helper script clears `.\publish` before publishing to avoid nested `publish\publish` folders. Use a full folder copy if IIS starts showing runtime/dependency errors after several partial deploys.

The publish folder should include:

- `NZBlood.DirectedTransfer.Blazor.dll`
- `web.config`
- `appsettings.json`
- `wwwroot`
- Syncfusion assemblies
- `runtimes\win-x64\native\chrome.exe` and related files for HTML-to-PDF

The publish helper currently enables ASP.NET Core stdout logging in generated `web.config` and creates `publish\logs`. This is useful while stabilising IIS deployment. Turn `stdoutLogEnabled` back to `false` once diagnostics are no longer needed.

For UI/report-code-only hotfixes where dependencies, settings, and static assets have not changed, the minimum deployment is the new `publish\NZBlood.DirectedTransfer.Blazor.dll` followed by an IIS app-pool recycle. Use the full folder copy above for dependency, runtime, configuration, or asset changes.

## 4. Configure App Settings

For the dev server, set:

```json
"DirectedTransfer": {
  "UseMockData": false,
  "DefaultUserId": "DirectedTransfer",
  "RequireHttpsRedirection": false,
  "DomainName": "NZBLOOD",
  "PathBase": "/NZBlood.DirectedTransfer.Blazor",
  "DefaultPageSize": 50,
  "DataProtectionKeysPath": "C:\\ProgramData\\NZBlood\\DirectedTransfer\\DataProtection-Keys"
}
```

`PathBase` must exactly match the IIS application alias, including spelling and casing. Blazor Server validates the browser URL against the rendered base URL case-sensitively, even though IIS usually treats paths case-insensitively. The app redirects casing-only mismatches back to the configured `PathBase`, but the published URL should still use the canonical casing. For production, a cleaner alias such as `/DirectedTransfer` can be used; update `PathBase` to match. If the app is hosted at a dedicated site root, leave `PathBase` blank.

`DefaultPageSize` controls the initial rows shown in the item list. Users can change page size in the UI up to 250.

Create the Data Protection folder and grant the app pool identity read/write access:

```text
C:\ProgramData\NZBlood\DirectedTransfer\DataProtection-Keys
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
setx Smtp__SendEmail "0" /M
setx Smtp__Host "<smtp relay host>" /M
setx Smtp__From "directedtransfer@nzblood.co.nz" /M
```

After changing machine-level environment variables, restart IIS or the app pool.

SMTP sending is controlled by:

```json
"Smtp": {
  "SendEmail": 0
}
```

- `0` disables SMTP and is recommended for dev processing tests.
- `1` enables SMTP on Process.
- Print never sends email.

## 5. Create IIS App

Example target path:

```text
C:\inetpub\wwwroot\NZBlood.DirectedTransfer.Blazor
```

Recommended process:

1. Stop the IIS app pool.
2. Copy `.\publish\*` to the target folder.
3. Ensure the app pool identity has read/execute permissions on the folder.
4. Start the app pool.
5. Browse to the configured IIS URL.

If the browser prompts for credentials before the app opens, IIS is challenging the browser with Windows Authentication. For this app, turn that challenge off.

Confirm these settings:

- Open IIS Manager.
- Select the Directed Transfer application, not just the parent website.
- Open `Authentication`.
- Set Anonymous Authentication to `Enabled`.
- Set Windows Authentication to `Disabled`.
- Restart the app pool or recycle the site.
- Browse using the production URL.

If deploying under an existing website as an application, confirm the base path works with the Blazor app. The current app uses:

```json
"PathBase": "/NZBlood.DirectedTransfer.Blazor"
```

The IIS alias is not editable after creation in IIS Manager. To rename it, remove the IIS application mapping and add a new application with the desired alias, pointing at the same physical folder.

If 500.31 appears after partial deploys, do a full clean deploy:

1. Stop the IIS app pool.
2. Back up server-local `appsettings.json` if needed.
3. Clear the IIS app folder.
4. Copy all `.\publish\*`.
5. Confirm `appsettings.json`.
6. Start the app pool.

## 6. Dev Server Smoke Test

Start with read-only behaviour:

1. Open the IIS URL.
2. Confirm the header and logo render.
3. Confirm the current Windows user is shown correctly.
4. Select a POU site.
5. Confirm Pick From site is populated.
6. Confirm item rows load.
7. Confirm `QTY Pending`, `QTY Available`, and `Order Up To` match the Wisej app for the same site.
8. Confirm Print is enabled after items load, even when no quantities are entered.
9. Confirm Process remains disabled until at least one line has a `QTY to Order`.
10. Enter a `QTY to Order` greater than available and confirm the line-level warning appears below that row. The entered value should remain visible.
11. Click Print.
12. Confirm the banner says `Building report...` and the PDF downloads automatically.
13. Confirm the print PDF is portrait, includes all item rows, excludes `Qty Pending` and `Qty Available`, and shows a bottom-aligned underline instead of entered `Qty To Order` values.
14. Confirm the PDF has no Syncfusion trial watermark.

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
6. Confirm the processed report contains only ordered lines and shows the actual entered `Qty To Order` values.
7. Confirm the processed PDF downloads automatically.
8. If `Smtp:SendEmail=1`, confirm email delivery and PDF attachment.
9. If `Smtp:SendEmail=0`, confirm Process completes without attempting SMTP.

## 7. Troubleshooting

Build or publish locked:

```powershell
dotnet build .\NZBlood.DirectedTransfer.Blazor.csproj -o .\.verify-build
```

ASP.NET Core stdout logs:

- Published `web.config` currently writes stdout logs to `.\logs\stdout*.log`.
- Ensure the app pool identity can write to the deployed `logs` folder.
- Disable stdout logging after troubleshooting to avoid log growth.

IIS 500.30:

- Check Windows Event Viewer.
- Check app pool identity.
- Confirm .NET 8 Hosting Bundle is installed.
- Confirm published folder contains `web.config`.

IIS 500.31:

- Confirm .NET 8 Hosting Bundle is installed on the server.
- Confirm these files were deployed together:
  - `NZBlood.DirectedTransfer.Blazor.dll`
  - `NZBlood.DirectedTransfer.Blazor.deps.json`
  - `NZBlood.DirectedTransfer.Blazor.runtimeconfig.json`
  - `web.config`
- If this appears after partial copies, perform a full clean deploy.

Blazor stays on `Connecting`:

- Confirm `DirectedTransfer:PathBase` exactly matches the IIS application alias.
- Check browser Network tab for `_blazor/negotiate`.
- If the page loads but controls do not respond, treat it as a Blazor Server circuit connection failure.
- From the client workstation, browse directly to `<app-url>/_blazor/negotiate?negotiateVersion=1`; a 404, login prompt, or wrong host indicates the SignalR endpoint is not reachable at the same base URL as the page.
- Confirm IIS has the WebSocket Protocol feature installed. SignalR can fall back to other transports, but WebSockets are the expected production path for Blazor Server.
- Path/casing mismatches can render the first page but prevent the interactive circuit from starting. If logs show `The URI ... is not contained by the base URI ...`, browse to the exact `PathBase` casing from `appsettings.json`.

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

- Confirm `Smtp:SendEmail=1`.
- Confirm `Smtp:Host`.
- Confirm relay permissions for the IIS server.
- Confirm port and SSL requirements.
- Confirm `SiteTransferEmailAddress` is populated for the selected POU site.

Browser login prompt before app opens:

- This is an IIS authentication challenge, not a Blazor login screen.
- For this app, enable Anonymous Authentication and disable Windows Authentication.
- If the prompt shows `http://localhost`, test using the real production URL from a client workstation. `localhost` means the browser is talking to the local machine from its own point of view.

## 8. Go/No-Go Checklist

- [ ] App opens in IIS.
- [ ] Windows/current user resolves as expected.
- [ ] Live SQL mode is enabled.
- [ ] POU site list loads.
- [ ] Item grid matches Wisej for at least one site.
- [ ] Quantity validation works.
- [ ] Print is enabled once item rows load, even with no item quantities entered.
- [ ] Process is disabled until at least one item quantity is entered.
- [ ] Filtering, paging, and ordered-only view work.
- [ ] Print PDF generates and downloads automatically in portrait format.
- [ ] Print PDF includes all item rows, excludes `Qty Pending` and `Qty Available`, and shows a bottom-aligned underline for `Qty To Order`.
- [ ] Process creates expected SQL/Panatracker rows.
- [ ] Process report includes only non-zero quantities and shows the actual entered `Qty To Order` values.
- [ ] Browser download works after Print and Process.
- [ ] SMTP sends to site email when `Smtp:SendEmail=1`.
- [ ] Generated PDF has been compared against the sample Crystal PDFs.
