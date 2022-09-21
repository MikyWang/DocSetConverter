using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

namespace ApiSpider;

public sealed class LinkHandler : IDisposable
{
    public static readonly string[] SupportedType = { "html", "js", "css", "json", "jpg", "png", "gif", "svg", "bmp" };

    private const string UrlPattern = @"(http|https):\/\/([\w\-_]+(?:\.[\w\-_]+)+(?:\/[\w-.,@^=%/&?:~+#]*))*";
    private readonly Regex _regex = new(UrlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private bool _disposed;
    private HttpClient? _httpClient;
    private readonly IBrowsingContext _context;
    public IDocument? Document { get; private set; }
    public Stream? Resource { get; private set; }
    public string Source { get; }
    public string Path { get; private set; } = string.Empty;
    public string LinkPrefix { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public LinkStatus Status { get; private set; } = LinkStatus.None;
    public string Suffix { get; private set; } = string.Empty;

    public LinkHandler(string source)
    {
        Source = source;
        _context = BrowsingContext.New(Configuration.Default.WithDefaultLoader().WithJs().WithCss());
        DealSource();
    }

    public async Task<LinkHandler> OpenDomAsync(CancellationToken token = default)
    {
        if (Status == LinkStatus.Failed) return this;
        Document = await _context.OpenAsync(Source, token);
        Status = LinkStatus.InProgress;
        return this;
    }

    public async Task<LinkHandler> GetResourceFileAsync(CancellationToken token = default)
    {
        if (Status == LinkStatus.Failed) return this;
        _httpClient = new HttpClient();
        Resource = await _httpClient.GetStreamAsync(Source, token);
        Status = LinkStatus.InProgress;
        return this;
    }

    public static string GetLinkType(string suffix) => SupportedType.Any(type => type == suffix.Trim().ToLower()) ? suffix : "html";

    public IEnumerable<string> SubLinks()
    {
        if (Document is null)
        {
            Status = LinkStatus.Failed;
            yield break;
        }

        var links = Document.QuerySelectorAll("[href]").Select(l => (elem: l, attrName: "href"));
        var srcLinks = Document.QuerySelectorAll("[src]").Select(l => (elem: l, attrName: "src"));
        foreach (var link in links.Concat(srcLinks))
        {
            var href = link.elem.GetAttribute(link.attrName);
            if (href is null) continue;
            var split = href.Split("?");
            var localHref = split[0];
            var param = split.Length > 1 ? split[1] : string.Empty;
            if (href.StartsWith("http:") || href.StartsWith("https:")) continue;
            if (href.StartsWith('#')) continue;
            var splitPaths = Path.Split("/");
            var host = $@"{LinkPrefix}://{Path}";
            var abPath = string.Empty;
            if (href.StartsWith('/'))
            {
                for (var i = 1; i < splitPaths.Length; i++)
                {
                    abPath += "../";
                }

                href = href[1..];
                localHref = localHref[1..];
                host = $@"{LinkPrefix}://{splitPaths[0]}";
            }

            if (GetLinkType(localHref.Split('/')[^1].Split(".")[^1]) != "html")
            {
                link.elem.SetAttribute(link.attrName, $"{abPath}{href}");
                yield return $"{host}/{href}";
            }
            else
            {
                link.elem.SetAttribute(link.attrName, $"{abPath}{localHref}.html?{param}");
                yield return $"{host}/{href}";
            }
        }
    }

    public async Task<LinkHandler> SaveAsync(DirectoryInfo rootDir, CancellationToken token = default)
    {
        if (Document is null && Resource is null)
        {
            Status = LinkStatus.Failed;
            return this;
        }

        var subDir = rootDir.CreateSubdirectory(Path);
        var fileInfo = new FileInfo($"{subDir}/{FileName}.{Suffix}");
        if (fileInfo.Exists)
        {
            Status = LinkStatus.Existed;
            return this;
        }

        if (Suffix is "html")
        {
            await File.WriteAllTextAsync($"{subDir}/{FileName}.{Suffix}", Document.DocumentElement.OuterHtml, token);
        }
        else
        {
            if (Resource is null)
            {
                Status = LinkStatus.Failed;
                return this;
            }

            await using var fileStream = File.Create($"{subDir}/{FileName}.{Suffix}");
            await Resource.CopyToAsync(fileStream, token);
        }

        Status = LinkStatus.Completed;
        return this;
    }

    private void DealSource()
    {
        var match = _regex.Match(Source);
        if (!match.Success)
        {
            Status = LinkStatus.Failed;
            return;
        }

        LinkPrefix = match.Groups[1].Value;
        var path = match.Groups[2].Value;
        var subDirs = path.Split("/");
        Path = string.Join("/", subDirs[..^1]);
        FileName = subDirs[^1].Split("?")[0];
        var fileSpilt = FileName.Split(".");
        var suffix = fileSpilt[^1];
        Suffix = GetLinkType(suffix.ToLower());
        if (Suffix != "html")
        {
            FileName = string.Join(".", fileSpilt[..^1]);
        }

        Status = LinkStatus.Started;
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            Document?.Dispose();
            Resource?.Dispose();
            _httpClient?.Dispose();
            _context.Dispose();
        }
        Document = null;
        Resource = null;
        _disposed = true;
    }

    public override string ToString() => $"当前链接：{Source}\n状态：{Status}\n保存文件名：{Path}/{FileName}.{Suffix}";
}

[Flags]
public enum LinkStatus : short
{
    None = 0,
    Started = 1,
    InProgress = 2,
    Existed = 4,
    Failed = 8,
    Completed = 16
}
