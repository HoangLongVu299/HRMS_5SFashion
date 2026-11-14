namespace HRMS.Web.ViewModels;

public sealed record PrintListColumn(string Heading, bool SingleLine = false);

public sealed record PrintListVm(
    string Title, PrintListColumn[] Columns, IReadOnlyList<string?[]> Rows, bool ShowRowNumbers = true)
{
    public string? Subtitle { get; init; }
}
