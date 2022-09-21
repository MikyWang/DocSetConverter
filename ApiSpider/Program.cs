// See https://aka.ms/new-console-template for more information

using ApiSpider;

const string source = @"https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.csharp.runtimebinder";
var rootDir = new DirectoryInfo("/Users/wangqiyuan/Documents/mydoc");
var apiFlower = new ApiFlower(rootDir) { MaxDegreeOfParallelism = 4 };
await apiFlower.ParserLinks(source);

if (apiFlower.EndBlock is null) return;
await apiFlower.EndBlock.Completion;

Console.WriteLine("****** API爬取完成! ******");
