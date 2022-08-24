using System.Text.Encodings.Web;
using AngleSharp;
using AngleSharp.Dom;
using DocSetLibrary.Helpers;

namespace UnityDocConverter;

public static class UnityManualConverter
{
    private static readonly string[] FilterSelector =
    {
        @"link[rel=""stylesheet""]", "meta", "title", @"div.section"
    };

    private const string LinkPath = @"Manual";

    public static async Task<string> Convert(string fileName, string htmlContent, CancellationToken token = default)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(htmlContent), token);

        DocSetConverterHelper.RemoveUselessElements(document.DocumentElement, FilterSelector);
        await DocSetConverterHelper.ReplaceCssStyle(document, "div#content-wrap", "content-wrap", token);
        await AddIndex(document, fileName, token);
        await AddToc(document, fileName, token);

        return document.DocumentElement.OuterHtml;
    }

    private static async Task AddIndex(IParentNode root, string fileName, CancellationToken token = default)
    {
        var typeElement = root.QuerySelector("div.section > h1");
        if (typeElement is null) return;
        var name = fileName.Split(@".html")[0];
        var path = Path.Join(LinkPath, fileName);
        const string entryType = "Guide";
        await DocSetConverterHelper.AddSearchIndex(name, path, entryType, token);
    }

    private static async Task AddToc(IDocument root, string fileName, CancellationToken token = default)
    {
        var subSections = root.QuerySelectorAll("h2");
        foreach (var subSection in subSections)
        {
            var name = subSection.TextContent.Trim();
            var encodeName = subSection.Id = UrlEncoder.Default.Encode(name);
            var path = Path.Join(LinkPath, $"{fileName}#{encodeName}");
            const string type = "Guide";
            await DocSetConverterHelper.AddSearchIndex(name, path, type, token);
            DocSetConverterHelper.AddDashAnchor(root, subSection, encodeName, type);
        }
    }
}
