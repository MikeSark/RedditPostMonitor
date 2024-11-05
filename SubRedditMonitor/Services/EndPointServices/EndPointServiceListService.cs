using System.Text;
using Microsoft.Extensions.Options;
using SubRedditMonitor.Configuration;
using SubRedditMonitor.Models;
using SubRedditMonitor.Services.Cache;

namespace SubRedditMonitor.Services.EndPointServices;

internal class EndPointServiceListService
{
    private readonly CacheRepository<List<SubmissionDetailsInternal>> _cacheRepository;
    private readonly RedditMonitor _redditOptions;

    public EndPointServiceListService(CacheRepository<List<SubmissionDetailsInternal>> cacheRepository,
                                      IOptions<RedditMonitor> redditOptions)
    {
        _cacheRepository = cacheRepository;
        _redditOptions = redditOptions.Value;
    }

    public string CreateHtmlStringForCacheItem(string subredditName, Guid reference) // DateTime issueDateTime)
    {
        var output = new StringBuilder(HtmlStart).AppendLine(HtmlBodyStart);
        if (_cacheRepository.TryGetValue(subredditName, reference, out var cacheItem))
        {
            var filteredByUpVote = cacheItem
                .OrderByDescending(x => x.Upvotes)
                .Take(_redditOptions.ShowTopPosts)
                .ToList();

            GetHtmlContentForSubmissionList(ref output, subredditName, filteredByUpVote);

            var authorList = cacheItem
                .GroupBy(s => s.Author)
                .Select(g => new SubmissionByAuthorInternal(g.Count(), g.Key))
                .Take(_redditOptions.ShowTopPosts)
                .OrderByDescending(x => x.PostCount)
                .ToList();

            GetHtmlContentForAuthorList(ref output, subredditName, authorList);
        }
        else
        {
            output.AppendLine(GetErrorHtmlString(subredditName, reference.ToString()));
        }

        output.AppendLine(HtmlBodyEnd).Append(HtmlEnd);

        return output.ToString();
    }

    private void GetHtmlContentForAuthorList(ref StringBuilder output, string subredditName, List<SubmissionByAuthorInternal> authorList)
    {
        output.AppendLine(GetHeaderHtmlString($"Top {_redditOptions.ShowTopPosts} Posts with highest submission by user."))
            .Append(TableStart)
            .Append(TableHeadStart)
            .Append(TableColumnTempalte("Author", width: 100))
            .Append(TableColumnTempalte("Post Count", width: 100))
            .Append(TableHeadEnd)
            .Append(TableBodyStart);


        foreach (var detailsInternal in authorList)
        {
            output.Append(TableRowStart)
                .Append(TableColumnTempalte(detailsInternal.Author!, "left"))
                .Append(TableColumnTempalte(detailsInternal.PostCount.ToString()!))
                .AppendLine(TableRowEnd);
        }

        output.AppendLine(TableBodyEnd)
            .AppendLine(TableEnd)
            .AppendLine(HtmlLineBreak)
            .AppendLine(HtmlLineBreak)
            .AppendLine(DivTemplate("Created by Mike Sarkezians"))
            .AppendLine(HtmlLineBreak)
            .AppendLine(HtmlLineBreak);
    }


    private void GetHtmlContentForSubmissionList(ref StringBuilder output, string subredditName, List<SubmissionDetailsInternal> filterCacheItems)
    {
        output.AppendLine(GetHeaderHtmlString(subredditName, $"Top {_redditOptions.ShowTopPosts} Posts with highest Upvote."))
            .Append(TableStart)
            .Append(TableHeadStart)
            .Append(TableColumnTempalte("Upvotes", width: 70))
            .Append(TableColumnTempalte("Author", width: 200))
            .Append(TableColumnTempalte("Title", width: 700))
            .Append(TableHeadEnd)
            .Append(TableBodyStart);


        foreach (var detailsInternal in filterCacheItems)
        {
            output.Append(TableRowStart)
                .Append(TableColumnTempalte(detailsInternal.Upvotes.ToString()!))
                .Append(TableColumnTempalte(detailsInternal.Author!, "left"))
                .Append(TableColumnTempalte(detailsInternal.Title!, "left"))
                .AppendLine(TableRowEnd);
        }

        output.Append(TableRowStart)
            .AppendLine(TableBodyEnd)
            .AppendLine(TableEnd)
            .AppendLine(HtmlLineBreak)
            .AppendLine(HtmlLineBreak);
    }

    public string CreateHtmlStringForListOfCacheEnteries()
    {
        var outPut = new StringBuilder("<div>Here is a list of all items in the cache</div>")
            .AppendLine(new string('*', 50)).AppendLine();

        outPut.AppendLine(TableStart)
            .AppendLine(TableHeadStart)
            .AppendLine(TableColumnTempalte("SubReddit Name", width: 250))
            .Append(TableColumnTempalte("Reference", width: 350))
            .Append(TableColumnTempalte("Link to stats", width: 250))
            .AppendLine(TableHeadEnd)
            .AppendLine(TableBodyStart);


        var cacheItems = _cacheRepository.GetAllItems().ToList(); // .OrderByDescending(c => c.Key.Date).ToList();
        foreach (var cacheItem in cacheItems)
        {
            outPut.Append(TableRowStart)
                .Append(TableColumnTempalte(cacheItem.Key.RedditName, "left"))
                .Append(TableColumnTempalte(cacheItem.Key.Reference.ToString().ToUpper(), "left"))
                .Append(TableColumnTempalte(BuildLink(cacheItem.Key.RedditName, cacheItem.Key.Reference.ToString())))
                .AppendLine(TableRowEnd);
        }

        outPut.AppendLine(TableBodyEnd)
            .AppendLine(TableEnd)
            .AppendLine(HtmlLineBreak)
            .AppendLine(HtmlLineBreak)
            .AppendLine(DivTemplate("Created by Mike Sarkezians"));

        return outPut.ToString();
    }


    private string DivTemplate(string value) =>
        $"<div >{value}</div>";

    private string TableTempalte(string value) =>
        $"<table>{value}</table>";

    private string TableRowTempalte(string value) =>
        $"<tr>{value}</tr>";

    private string TableColumnTempalte(string value, string textAlign = "center", int width = 150) =>
        $"<td style='width: {width}px; text-align:  {textAlign} ;'>{value}</td>";

    private string TableHeaderRowTempalte(string value) =>
        $"<thead style='background-color: navy; color: antiquewhite; margin:0px. padding:0px; '><tr>{value}</tr></thead>";

    private string TableHeaderColumnTempalte(string value, string textAlign = "center", int width = 100) =>
        $"<th style='width: {width}px;text-align: {textAlign} ;'>{value}</th>";


    private string GetErrorHtmlString(string subredditName, string reference) =>
        $$"""
          <div style="border-color: red; border: 5px solid darkred; padding: 0px; border-radius: 5px; position: relative">
          <div style="background-color: brown; color: antiquewhite; font-weight: 900;font-size: 1.3rem;;">Error Message:</div>
          <div style="padding: 5px; font-size: 1.1rem; ">$Unable to find the cache entry for </br> 
             Issued Reference: {{reference}}</br> SubReddit Name: {{subredditName}}</br></div>
          """;


    private string GetHeaderHtmlString(string subredditName, string subjectLine) =>
        $"""
         <div style="font-size: 1.2rem;" >Report for SubReddit: {subredditName}</div>
         <div style="font-size: 1.1rem; margin-bottom:10px;"> {subjectLine}</div>
         """;

    private string GetHeaderHtmlString(string subjectLine) =>
        $"""
         <div style="font-size: 1.1rem; margin-bottom:10px;"> {subjectLine}</div>
         """;

    private string BuildLink(string subRedditName, string cacheEntryReference) =>
        $"<a href='http://localhost:6001/stats/{subRedditName}/{cacheEntryReference}'> Click to see stats</a>";

    private string HtmlLineBreak => "<br/>";

    private string HtmlStart => "<html>";
    private string HtmlEnd => "</html>";
    private string HtmlBodyStart => "<body>";
    private string HtmlBodyEnd => "</body>";
    private string TableStart => "<table>";
    private string TableEnd => "</table>";
    private string TableRowStart => "<tr>";
    private string TableRowEnd => "</tr>";
    private string TableHeadStart => "<thead style='background-color: navy; color: antiquewhite;'>";
    private string TableHeadEnd => "</thead>";
    private string TableBodyStart => "<tbody>";
    private string TableBodyEnd => "</tbody>";
}