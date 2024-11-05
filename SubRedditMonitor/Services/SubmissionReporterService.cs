using System.Diagnostics;
using System.Text;
using SubRedditMonitor.Models;

namespace SubRedditMonitor.Services;

public class SubmissionReporterService
{
    public SubmissionReporterService() { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="headerLine"></param>
    /// <param name="authorList"></param>
    public void DisplayAuthorListInTerminal(string headerLine, List<SubmissionByAuthorInternal> authorList)
    {
        // Prepare the formatted table output
        var output = FormatAuthorListAsTable(headerLine, authorList);

        var psi = new ProcessStartInfo
        {
            FileName = "wt.exe", // Opens Windows Terminal
            Arguments = $"powershell -NoExit \"Write-Host '{output}'\"",
            UseShellExecute = true
        };

        Process.Start(psi);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="headerLine"></param>
    /// <param name="submissions"></param>
    public void DisplayAuthorListInConsole(string headerLine, List<SubmissionByAuthorInternal> submissions)
    {
        Console.WriteLine();
        Console.WriteLine(FormatAuthorListAsTable(headerLine, submissions));
        Console.WriteLine();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="submissions"></param>
    /// <param name="headerLine"></param>
    public void DisplaySubmissionsInTerminal(string headerLine, List<SubmissionDetailsInternal> submissions)
    {
        // Prepare the formatted table output
        var output = FormatSubmissionsAsTable(headerLine, submissions);

        var psi = new ProcessStartInfo
        {
            FileName = "wt.exe", // Opens Windows Terminal
            Arguments = $"powershell -NoExit \"Write-Host '{output}'\"",
            UseShellExecute = true
        };

        Process.Start(psi);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="headerLine"></param>
    /// <param name="submissions"></param>
    public void DisplaySubmissionsInConsole(string headerLine, List<SubmissionDetailsInternal> submissions)
    {
        Console.WriteLine();
        Console.WriteLine(FormatSubmissionsAsTable(headerLine, submissions));
        Console.WriteLine();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="authorList"></param>
    /// <param name="headerLine"></param>
    /// <returns></returns>
    private string FormatAuthorListAsTable(string headerLine, List<SubmissionByAuthorInternal> authorList)
    {
        if (authorList == null || !authorList.Any())
            return "No submissions available.";

        // Calculate maximum column widths for alignment
        var authorWidth = Math.Max("Author".Length, authorList.Max(s => s.Author?.Length ?? 0));
        var postCountWidth = 5;


        // Build the table header
        var builder = new StringBuilder();
        builder.AppendLine().AppendLine(headerLine).AppendLine();
        builder.AppendLine($" {"Author".PadRight(authorWidth)} | {"Post Count".PadRight(postCountWidth)}");
        builder.AppendLine(new string('-', authorWidth + postCountWidth + 10));

        // Build each row
        foreach (var author in authorList)
        {
            builder.AppendLine(
                $" {(author.Author ?? "N/A").PadRight(authorWidth)} | " +
                $" {(author.PostCount?.ToString() ?? "N/A").PadRight(postCountWidth)}");
        }

        return builder.ToString().Replace("'", "''"); // Escape single quotes for PowerShell
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="headerLine"></param>
    /// <returns></returns>
    public string FormatSubmissionsAsTable(string headerLine, List<SubmissionDetailsInternal> submissions)
    {
        if (submissions == null || !submissions.Any())
            return "No submissions available.";

        // Calculate maximum column widths for alignment
        var titleWidth = Math.Max("Title".Length, submissions.Max(s => s.Title?.Length ?? 0));
        var authorWidth = Math.Max("Author".Length, submissions.Max(s => s.Author?.Length ?? 0));
        var upvotesWidth = Math.Max("Upvotes".Length, submissions.Max(s => s.Upvotes?.ToString().Length ?? 0));


        // Build the table header
        var builder = new StringBuilder();
        builder.AppendLine().AppendLine(headerLine).AppendLine();
        builder.AppendLine($" {"Upvotes".PadRight(upvotesWidth)} | {"Author".PadRight(authorWidth)} | {"Title".PadRight(titleWidth)} ");
        builder.AppendLine(new string('-', titleWidth + authorWidth + upvotesWidth + 10));

        // Build each row
        foreach (var submission in submissions)
        {
            builder.AppendLine(
                $" {(submission.Upvotes?.ToString() ?? "N/A").PadRight(upvotesWidth)} |" +
                $" {(submission.Author ?? "N/A").PadRight(authorWidth)} | " +
                $" {(submission.Title ?? "N/A").PadRight(titleWidth)}");
        }

        return builder.ToString().Replace("'", "''"); // Escape single quotes for PowerShell
    }
}