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

    /// <summary>
    /// 单个 html 文件转 Dash 文档
    /// </summary>
    /// <param name="fileName">html 文件名(不含路径)</param>
    /// <param name="htmlContent">html 文件内容</param>
    /// <param name="token"><see cref="CancellationToken"/></param>
    /// <returns>转换后的html文档</returns>
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

    /// <summary>
    /// 添加dash数据库索引
    /// </summary>
    /// <param name="root">dom 根节点</param>
    /// <param name="fileName">文件名（不含路径）</param>
    /// <param name="token"><see cref="CancellationToken"/></param>
    private static async Task AddIndex(IParentNode root, string fileName, CancellationToken token = default)
    {
        var typeElement = root.QuerySelector("div.section > h1");
        if (typeElement is null) return;
        var name = fileName.Split(@".html")[0];
        var path = Path.Join(LinkPath, fileName);
        const string entryType = "Guide";
        await DocSetConverterHelper.AddSearchIndex(name, path, entryType, token);
    }

    /// <summary>
    /// 添加Dash文档内目录
    /// </summary>
    /// <param name="root">dom 根节点</param>
    /// <param name="fileName">html 文件名</param>
    /// <param name="token"><see cref="CancellationToken"/></param>
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
