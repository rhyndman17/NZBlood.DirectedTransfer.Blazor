namespace NZBlood.DirectedTransfer.Blazor.Services;

public sealed class GeneratedReportFile
{
    public GeneratedReportFile(string reportId, string fileName, byte[] content)
    {
        ReportId = reportId;
        FileName = fileName;
        Content = content;
    }

    public string ReportId { get; }
    public string FileName { get; }
    public byte[] Content { get; }
}
