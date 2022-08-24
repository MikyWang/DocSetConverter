using AngleSharp;
using AngleSharp.Dom;
using DocSetLibrary.Helpers;

namespace UnityDocConverter;

public static class UnityScriptReferenceConverter
{
    private static readonly Dictionary<string, string> EntryType = new()
    {
        { "变量", "Property" }, { "受保护的函数", "Method" }, { "公共函数", "Method" }, { "静态函数", "Method" }, { "静态变量", "Property" }, { "参数", "Parameter" }, { "运算符", "Operator" }, { "消息", "Function" }, { "struct", "Struct" }, { "class", "Class" }, { "enumeration", "Enum" }, { "interface", "Interface" },
        { "构造函数", "Constructor" }, { "委托", "Delegate" }, { "Events", "Event" }
    };

    private static readonly string[] PageType =
    {
        "class", "struct", "interface", "enumeration"
    };

    private static readonly string[] FilterSelector =
    {
        @"link[rel=""stylesheet""]", "meta", "title", @"div.section"
    };

    private const string LinkPath = @"ScriptReference";

    /// <summary>
    /// 单个 html 文件转 Dash 文档
    /// </summary>
    /// <param name="fileName">html 文件名(不含路径)</param>
    /// <param name="htmlContent">html 文件内容</param>
    /// <param name="token"><see cref="CancellationToken"/></param>
    /// <returns>转换后的html文档</returns>
    public static async Task<string> ConvertAsync(string fileName, string htmlContent, CancellationToken token = default)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(htmlContent), token);

        DocSetConverterHelper.RemoveUselessElements(document.DocumentElement, FilterSelector);
        await DocSetConverterHelper.ReplaceCssStyle(document, "div#content-wrap", "content-wrap", token);
        await AddIndexAsync(document, fileName, token);
        await AddTocAsync(document, token);

        return document.DocumentElement.OuterHtml;
    }

    /// <summary>
    /// 添加dash数据库索引
    /// </summary>
    /// <param name="root">dom 根节点</param>
    /// <param name="fileName">文件名（不含路径）</param>
    /// <param name="token"><see cref="CancellationToken"/></param>
    private static async Task AddIndexAsync(IParentNode root, string fileName, CancellationToken token = default)
    {
        var typeElement = root.QuerySelector("div.section > div > p.cl.mb0.left.mr10");
        if (typeElement is null) return;
        var typeHtml = typeElement.InnerHtml;
        var type = PageType.FirstOrDefault(pt => typeHtml?.IndexOf(pt) == 0);
        if (type is null) return;
        var name = fileName.Split(@".html")[0];
        var path = Path.Join(LinkPath, fileName);
        var entryType = EntryType[type];
        await DocSetConverterHelper.AddSearchIndex(name, path, entryType, token);
    }

    /// <summary>
    /// 添加Dash文档内目录
    /// </summary>
    /// <param name="root">dom 根节点</param>
    /// <param name="token"><see cref="CancellationToken"/></param>
    private static async Task AddTocAsync(IDocument root, CancellationToken token = default)
    {
        var subSections = root.QuerySelectorAll("div.subsection").Where(sec => sec.Children.Any(ch => ch.Matches("table.list")));
        foreach (var subSection in subSections)
        {
            var table = subSection.QuerySelector("table.list");
            if (table is null) continue;
            var h2 = subSection.QuerySelector("h2")?.InnerHtml.Trim();
            if (h2 is null) continue;
            var type = EntryType[h2];
            var links = table.QuerySelectorAll("a[href]");
            if (links is null) continue;
            foreach (var link in links)
            {
                var name = link.InnerHtml.Trim();
                var path = Path.Join(LinkPath, link.GetAttribute("href"));
                await DocSetConverterHelper.AddSearchIndex(name, path, type, token);
                DocSetConverterHelper.AddDashAnchor(root, link.ParentElement, name, type);
            }
        }
    }
}
