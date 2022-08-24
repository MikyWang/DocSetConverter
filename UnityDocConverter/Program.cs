// See https://aka.ms/new-console-template for more information

using DocSetLibrary.Helpers;
using UnityDocConverter;

var tokenSource = new CancellationTokenSource();
var config = DocSetConverterHelper.Configure;

var scriptReferencePath = $"{config.DocSetDocumentsPath}/ScriptReference";
var manualReferencePath = $"{config.DocSetDocumentsPath}/Manual";

var manualDir = new DirectoryInfo(manualReferencePath);
var scriptDir = new DirectoryInfo(scriptReferencePath);
var scriptFiles = scriptDir.GetFiles();
var manualFiles = manualDir.GetFiles();

var count = 0;
var progress = PrintProgress(scriptFiles.Length + manualFiles.Length);

var savePlistTask = File.WriteAllTextAsync(config.DocSetPlistFile, config.DocSetPlist, tokenSource.Token);
var convertScriptReferenceTask = Parallel.ForEachAsync(scriptFiles, tokenSource.Token, async (file, token) =>
{
    var content = await File.ReadAllTextAsync(file.FullName, token);
    var result = await UnityScriptReferenceConverter.ConvertAsync(file.Name, content, token);
    await File.WriteAllTextAsync(file.FullName, result, token);
    Interlocked.Increment(ref count);
    progress.Report(count);
});
var convertManualTask = Parallel.ForEachAsync(manualFiles, tokenSource.Token, async (file, token) =>
{
    var content = await File.ReadAllTextAsync(file.FullName, token);
    var result = await UnityManualConverter.Convert(file.Name, content, token);
    await File.WriteAllTextAsync(file.FullName, result, token);
    Interlocked.Increment(ref count);
    progress.Report(count);
});

await Task.WhenAll(savePlistTask, convertScriptReferenceTask, convertManualTask);
Console.WriteLine();
Console.WriteLine("****** 手册制作完成! ******");

IProgress<int> PrintProgress(int totalSize)
{
    Console.WriteLine("****** 开始生成手册 ******");
    var pos = Console.GetCursorPosition();
    return new Progress<int>(cnt =>
    {
        Console.SetCursorPosition(pos.Left, pos.Top);
        Console.Write(new string(' ', Console.BufferWidth));
        Console.SetCursorPosition(pos.Left, pos.Top);
        Console.Write($"{cnt}/{totalSize}");
    });
}
