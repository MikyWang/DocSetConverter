namespace ApiSpider;

public class Utils
{
    public static string MakeConsoleString(string source) => Console.BufferWidth <= source.Length ? source : source.PadRight(Console.BufferWidth - source.Length);
    
}
