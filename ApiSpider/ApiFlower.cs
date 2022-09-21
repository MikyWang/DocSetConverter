using System.Threading.Tasks.Dataflow;

namespace ApiSpider;

public class ApiFlower
{
    private CancellationTokenSource _cancellationTokenSource = null!;
    private ITargetBlock<string>? _worker;
    private int _linkTotal;
    private int _linkSucceed;
    private int _linkFailed;
    private int _linkExisted;
    private readonly DirectoryInfo _rootDir;

    public int LinkCompleted => _linkSucceed + _linkFailed + _linkExisted;
    public ActionBlock<LinkHandler>? EndBlock { get; private set; }
    public int MaxDegreeOfParallelism { get; set; } = 1;
    public ApiFlower(DirectoryInfo rootDir) { _rootDir = rootDir; }

    ~ApiFlower() { _cancellationTokenSource.Dispose(); }

    public async Task ParserLinks(params string[] links)
    {
        if (_worker is null || _worker.Completion.IsCompleted)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _worker = CreateFlow(out var endBlock);
            EndBlock = endBlock;
        }

        await Parallel.ForEachAsync(links, _cancellationTokenSource.Token, async (link, token) => { await _worker.SendAsync(link, token); });
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
        _worker?.Complete();
    }

    private ITargetBlock<string> CreateFlow(out ActionBlock<LinkHandler> endBlock)
    {
        var blockOption = new ExecutionDataflowBlockOptions { CancellationToken = _cancellationTokenSource.Token, MaxDegreeOfParallelism = MaxDegreeOfParallelism };
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        var downloadBlock = new TransformBlock<string, LinkHandler>(async link =>
        {
            Interlocked.Increment(ref _linkTotal);
            var handler = new LinkHandler(link);
            if (handler is { Suffix: "html" }) return await handler.OpenDomAsync(_cancellationTokenSource.Token);
            return await handler.GetResourceFileAsync(_cancellationTokenSource.Token);
        }, blockOption);
        var handleLinksBlock = new TransformBlock<LinkHandler, LinkHandler>(handler =>
        {
            if (handler is { Suffix: "html" })
            {
                var subLinks = handler.SubLinks().ToArray();
                _ = ParserLinks(subLinks);
            }
            return handler;
        }, blockOption);
        var saveBlock = new TransformBlock<LinkHandler, LinkHandler>(async handler =>
        {
            handler = await handler.SaveAsync(_rootDir, _cancellationTokenSource.Token);
            switch (handler.Status)
            {
                case LinkStatus.Completed:
                    Interlocked.Increment(ref _linkSucceed);
                    break;
                case LinkStatus.Existed:
                    Interlocked.Increment(ref _linkExisted);
                    break;
                default:
                    Interlocked.Increment(ref _linkFailed);
                    break;
            }

            return handler;
        }, blockOption);

        var failedBlock = new TransformBlock<LinkHandler, LinkHandler>(handler =>
        {
            Interlocked.Increment(ref _linkFailed);
            return handler;
        });

        endBlock = new ActionBlock<LinkHandler>(handler =>
        {
            Console.Clear();
            Console.WriteLine("****** 开始爬取API ******");
            Console.WriteLine(handler);
            Console.WriteLine(this);
            if (_linkTotal == LinkCompleted) _worker?.Complete();
            handler.Dispose();
        }, new ExecutionDataflowBlockOptions { CancellationToken = _cancellationTokenSource.Token });

        downloadBlock.LinkTo(handleLinksBlock, linkOptions, handle => handle is { Status: LinkStatus.InProgress });
        downloadBlock.LinkTo(failedBlock, linkOptions);
        handleLinksBlock.LinkTo(saveBlock, linkOptions, handle => handle is { Status: LinkStatus.InProgress });
        handleLinksBlock.LinkTo(failedBlock, linkOptions);
        saveBlock.LinkTo(endBlock, linkOptions);
        failedBlock.LinkTo(endBlock, linkOptions);
        return downloadBlock;
    }

    public override string ToString() => $"总链接数：{_linkTotal}\t成功链接数：{_linkSucceed + _linkExisted}\t失败链接数：{_linkFailed}";
}
