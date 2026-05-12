# Codex Handoff - Directed Transfer Blazor

Use this file when opening this folder in a new Codex session.

## Current Goal

Continue migrating the Wisej directed transfer app to Blazor. The Blazor project is newly scaffolded and lives in this folder:

```text
C:\Users\RobertHyndman\OneDrive - Altara Limited\Dev\General\NZBlood.DirectedTransfer.Blazor
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
- Process report should include only rows where `QtyToOrder != 0`.
- Report should be emailed and also available for browser download.
- Email recipients are the selected site's `SiteTransferEmailAddress`.
- PDF generation can use Syncfusion.

## Current Implementation Summary

- `Home.razor` implements the main workflow.
- `DirectedTransferService` implements live SQL mode.
- `MockDirectedTransferService` supports local mock mode.
- `DirectedTransferReportService` builds an HTML report and converts it to PDF with Syncfusion.
- `DirectedTransferEmailService` sends the PDF through SMTP.
- `Program.cs` switches mock/live service based on `DirectedTransfer:UseMockData`.
- `appsettings.Development.json` sets mock mode to true.
- `appsettings.json` is intended for live/dev-server deployment.

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

## Live SQL Objects

Review these source scripts in the Wisej folder before changing live SQL:

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

## Report Notes

Sample PDFs exist in:

```text
..\NZBlood.DirectedTransferWI\SamplePDF
```

Current generated report is a clean HTML approximation, not yet visually matched to Crystal. Next iteration should compare:

- header content
- transfer code/reference placement
- row columns
- page orientation
- page breaks
- font sizes
- NZ Blood branding
- whether recipient-facing copy should match the old emailed Crystal PDF

## Next Useful Tasks

1. Run live SQL read-only mode on the dev server or from a machine with DB access.
2. Validate site list and item columns match expected Wisej output.
3. Test Print with a selected POU site.
4. Configure SMTP and confirm email delivery to `SiteTransferEmailAddress`.
5. Test Process in a controlled scenario.
6. Verify Panatracker header/detail rows are created.
7. Verify `nzbDirectedTransferEmailLne` rows are correct.
8. Compare generated PDF with the sample PDFs and tune the HTML.
9. Publish to IIS dev server following `IIS-Deployment-DevServer.md`.
