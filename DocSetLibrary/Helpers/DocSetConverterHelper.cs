using System.Diagnostics;
using AngleSharp.Dom;
using DocSetLibrary.Data;
using Microsoft.Extensions.Configuration;

namespace DocSetLibrary.Helpers;

public static class DocSetConverterHelper
{
    private static DocSetConfigure? _configure;

    /// <summary>
    /// docset 文档配置
    /// </summary>
    public static DocSetConfigure Configure
    {
        get
        {
            if (_configure is not null) return _configure;

            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            _configure = config.GetRequiredSection("DocSetConfig").Get<DocSetConfigure>();
            Debug.Assert(_configure != null, nameof(_configure) + " != null");
            return _configure;
        }
    }

    /// <summary>
    /// 在 parent 节点下添加一个文档目录节点
    /// </summary>
    /// <param name="root">Dom 文档根节点</param>
    /// <param name="parent">要添加目录节点的父节点</param>
    /// <param name="name">目录节点名称</param>
    /// <param name="entryType">docset 显示类型</param>
    /// <returns>重复添加目录节点返回 false,添加成功返回 true</returns>
    public static bool AddDashAnchor(IDocument root, IElement parent, string name, string entryType)
    {
        if (parent.QuerySelector("a.dashAnchor") is not null) return false;
        var element = root.CreateElement("a");
        element.SetAttribute("name", $"//apple_ref/cpp/{entryType}/{name}");
        element.ClassName = "dashAnchor";
        parent.AppendElement(element);
        return true;
    }

    /// <summary>
    /// 添加一个数据库索引
    /// </summary>
    /// <param name="name">显示名称</param>
    /// <param name="path">文档路径</param>
    /// <param name="entryType">docset 显示类型</param>
    /// <param name="token"><see cref="CancellationToken"/></param>
    /// <returns>重复插入返回 false,添加成功返回 true</returns>
    public static async Task<bool> AddSearchIndex(string name, string path, string entryType, CancellationToken token = default)
    {
        var searchIndex = new SearchIndex { Name = name, Path = path, Type = entryType };
        await using var dbCtx = new DocSetContext();
        var found = dbCtx.SearchIndices.Any(si => si.Name == searchIndex.Name);
        if (found) return false;
        await dbCtx.SearchIndices.AddAsync(searchIndex, token);
        await dbCtx.SaveChangesAsync(token);
        return true;
    }

    /// <summary>
    /// 替换节点样式
    /// </summary>
    /// <param name="root">dom 根节点</param>
    /// <param name="selectors">节点 CSS 选择器名称</param>
    /// <param name="replaceCLassNames">要替换样式的 class 名称</param>
    /// <param name="token">任务取消token</param>
    public static async Task ReplaceCssStyle(IDocument root, string selectors, string replaceCLassNames, CancellationToken token = default)
    {
        var elements = root.QuerySelectorAll(selectors);
        await Parallel.ForEachAsync(elements, token, (element, tok) =>
        {
            if (tok.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(tok);
            }

            element.ClassName = replaceCLassNames;
            return ValueTask.CompletedTask;
        });
    }

    /// <summary>
    /// 删除无用DOM节点
    /// </summary>
    /// <param name="root">dom 根节点</param>
    /// <param name="filterSelector">要保留的节点 CSS 选择器名称,多个选择器用,或者数组传递</param>
    public static void RemoveUselessElements(IElement root, params string[] filterSelector)
    {
        var removeElems = new List<IElement>();
        FilterUselessElements(root, removeElems, filterSelector);
        for (var i = removeElems.Count - 1; i >= 0; i--)
        {
            removeElems[i].Remove();
        }
    }

    private static void FilterUselessElements(IElement root, in IList<IElement> elements, params string[] filterSelector)
    {
        if (filterSelector.Length == 0)
        {
            Console.WriteLine("未传入节点选择器!");
            return;
        }

        if (filterSelector.Any(root.Matches))
        {
            var node = root;
            while (node is not null)
            {
                elements.Remove(node);
                node = node.ParentElement;
            }

            return;
        }

        foreach (var child in root.Children)
        {
            elements.Add(child);
            FilterUselessElements(child, elements, filterSelector);
        }
    }
}
