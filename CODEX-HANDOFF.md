# Codex Handoff - Directed Transfer Blazor

Use this file when opening this folder in a new Codex session.

## Current Goal

Continue hardening and polishing the Blazor directed transfer app. The project lives in this folder:

```text
C:\Users\RobertHyndman\OneDrive - Altara Limited\Customers\NZ Blood\Projects\Directed Transfer Blazor
```

The source Wisej project remains here:

```text
C:\Users\RobertHyndman\OneDrive - Altara Limited\Dev\General\NZBlood.DirectedTransferWI
```

The styling/config reference app is:

```text
C:\Users\RobertHyndman\OneDrive - Altara Limited\Dev\General\NZBlood.ApprovalWorkflowsWI\NZBlood.ApprovalWorkflows.Blazor
```

## User Decisions Already Made

- Create a new Blazor project beside the Wisej project.
- Preserve the Wisej workflow pattern but modernise styling.
- Match the Approval Workflows Blazor look and feel.
- Use Syncfusion components.
- Inline grid editing should only be `QtyToOrder`.
- UOM must no longer be editable.
- Wrapper SQL procedures are acceptable if needed.
- Print report should include all rows.
- Print report should be available even when no quantities have been entered.
- Print report should be portrait, exclude `Qty Pending` and `Qty Available`, and render `Qty To Order` as a bottom-aligned write-in underline.
- Process report should include only rows where `QtyToOrder != 0`.
- Process report should show the actual entered `QtyToOrder` values.
- Print should generate and download the PDF directly.
- Process should generate and download the processed PDF directly.
- Process should be disabled until at least one line has a quantity to order.
- SMTP should only happen on Process, and only when `Smtp:SendEmail=1`.
- Email recipients are the selected site's `SiteTransferEmailAddress`.
- PDF generation can use Syncfusion.
- Dev server processing is tested with `Smtp:SendEmail=0`.

## Current Implementation Summary

- `Home.razor` implements the main workflow.
- `DirectedTransferService` implements live SQL mode.
- `MockDirectedTransferService` supports local mock mode.
- `DirectedTransferReportService` builds an HTML report and converts it to PDF with Syncfusion.
- `DirectedTransferEmailService` sends the PDF through SMTP only when `Smtp:SendEmail` is enabled.
- `Program.cs` switches mock/live service based on `DirectedTransfer:UseMockData`.
- `appsettings.Development.json` sets mock mode to true.
- `appsettings.json` is intended for live/dev-server deployment.
- Current visible build marker is in `BuildInfo.cs`.
- Item list supports filtering, ordered-only view, paging, configurable default page size, row-level validation banners, and direct quantity entry.
- Quantity values are preserved across paging/filtering.
- The POU dropdown receives initial focus.
- Refresh re-resolves the selected POU site and reloads the item list. There is no separate Load button.
- `DirectedTransfer:MainMessage` is a permanent notice inside the selection panel, separate from transient status banners.
- Print and Process auto-download generated PDFs through `/reports/directed-transfer/{reportId}`.
- Print is enabled when item rows are loaded; Process is still quantity-gated.
- Process confirmation uses site names and email address.
- `wwwroot/favicon.png` is NZ Blood branded.

## Build Verification

Known good:

```powershell
dotnet build .\NZBlood.DirectedTransfer.Blazor.csproj
```

If the app is running and locks output:

```powershell
dotnet build .\NZBlood.DirectedTransfer.Blazor.csproj -o .\.verify-build
```

## Publish And Git Scripts

The project has helper scripts matching the Approval Workflows app:

```powershell
.\Scripts\Publish Web Application.ps1
.\Scripts\Update-GitHub.ps1 -Message "_2026.5.12"
```

`Publish Web Application.ps1` clears `.\publish` before publishing so old publish output does not get copied into itself. It currently enables stdout logging in generated `web.config`; turn this off once IIS diagnostics are no longer needed.

`Update-GitHub.ps1` defaults to:

```text
https://github.com/rhyndman17/NZBlood.DirectedTransfer.Blazor.git
```

Override the remote if the repo name differs.

## Local Run

```powershell
dotnet run --urls http://localhost:5222
```

Development mode runs with mock data unless overridden.

## Key Configuration

Important `appsettings.json` keys:

```json
"DirectedTransfer": {
  "UseMockData": false,
  "RequireHttpsRedirection": false,
  "PathBase": "/NZBlood.DirectedTransfer.Blazor",
  "DefaultPageSize": 50,
  "DataProtectionKeysPath": "C:\\ProgramData\\NZBlood\\DirectedTransfer\\DataProtection-Keys"
},
"Smtp": {
  "SendEmail": 0
}
```

`PathBase` must match the IIS application alias exactly. For production, prefer a clean alias such as `/DirectedTransfer` and update `PathBase`.

`Smtp:SendEmail=0` disables SMTP for dev process testing. Use `1`, `true`, or `yes` to enable.

The Syncfusion key is read from `Syncfusion:LicenseKey` and registered in `Program.cs`. Prefer server-local configuration or environment variables for secrets in production.

## Live SQL Objects

Review these local SQL scripts before changing live SQL:

```text
Sql Objects
```

They were copied from the Wisej folder:

```text
..\NZBlood.DirectedTransferWI\ExtObjects
```

Important objects:

- `nzbCalculateDirectedTransferWI`
- `nzbCalculateDirectedTransferItemWI`
- `nzbCreateDirectedTransfer`
- `nzbDirectedTransferHdr`
- `nzbDirectedTransferLne`
- `nzbDirectedTransferEmailLne`
- `nzbDirectedTransferReport`
- `nzbDirectedTransferItems`
- `nzbSiteOptions`

## Important Caution

`DirectedTransferService.CreateTransferAsync` currently inserts into:

- `nzbDirectedTransferHdr`
- `nzbDirectedTransferLne`
- `nzbDirectedTransferEmailLne`

It commits those inserts, then calls `nzbCreateDirectedTransfer`. This matches the migration shape but does not wrap the stored procedure call in the same app transaction. If full all-or-nothing behaviour is required, create a wrapper stored procedure or refactor the SQL boundary.

Also review `nzbCreateDirectedTransfer` error handling. The supplied procedure catches errors and selects error details, but may not throw an exception back to the app. That could make app-side success detection too optimistic.

The app currently uses SQL username/password authentication for initial testing. Revisit integrated security or a least-privileged SQL login before production.

## Report Notes

Sample PDFs exist in:

```text
..\NZBlood.DirectedTransferWI\SamplePDF
```

Current generated report is a clean HTML approximation, not a pixel-perfect Crystal clone. Print report is an A4 portrait pick list that includes all rows, omits `Qty Pending` and `Qty Available`, and uses a bottom-aligned underline for manual `Qty To Order` entry. Process report remains A4 landscape, includes only non-zero ordered rows, and shows the actual entered quantities. Next iteration should compare:

- header content
- transfer code/reference placement
- row columns
- page breaks
- font sizes
- NZ Blood branding
- whether recipient-facing copy should match the old emailed Crystal PDF

## Next Useful Tasks

1. Test Process in a controlled scenario on the dev server with `Smtp:SendEmail=0`.
2. Verify Panatracker header/detail rows are created.
3. Verify `nzbDirectedTransferEmailLne` rows are correct.
4. Compare generated PDF with the sample PDFs and tune the HTML.
5. Turn off stdout logging in the generated `web.config` once deployment is stable.
6. Decide production IIS alias and update `DirectedTransfer:PathBase`.
7. Confirm production Syncfusion key/SQL credential handling outside source control.
8. Configure SMTP and confirm email delivery to `SiteTransferEmailAddress` before enabling `Smtp:SendEmail=1`.
