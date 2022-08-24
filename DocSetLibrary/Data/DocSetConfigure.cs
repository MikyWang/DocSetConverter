#pragma warning disable CS8618
namespace DocSetLibrary.Data;

[Serializable]
public record DocSetConfigure
{
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DocSetName { get; init; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string DocSetShortName { get; init; }

    /// <summary>
    /// DocSet存放路径
    /// </summary>
    public string DocSetPath { get; init; }

    /// <summary>
    /// 主界面路径
    /// </summary>
    public string DashIndexFilePath { get; init; }

    /// <summary>
    /// 包路径
    /// </summary>
    private string DocSetContentPath => $"{DocSetPath}/{DocSetName}.docset/Contents";

    /// <summary>
    /// 资源路径
    /// </summary>
    private string DocSetResourcePath => $"{DocSetContentPath}/Resources";

    /// <summary>
    /// 文档路径
    /// </summary>
    public string DocSetDocumentsPath => $"{DocSetResourcePath}/Documents";

    /// <summary>
    /// plist文件路径
    /// </summary>
    public string DocSetPlistFile => $"{DocSetContentPath}/info.plist";

    /// <summary>
    /// SQLite数据库路径
    /// </summary>
    public string DbPath => $"{DocSetResourcePath}/docSet.dsidx";

    /// <summary>
    /// plist内容
    /// </summary>
    public string DocSetPlist =>
        $@"<?xml version=""1.0"" encoding=""UTF-8""?>
    <!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
    <plist version=""1.0"">
    <dict>
    <key>CFBundleIdentifier</key>
    <string>{DocSetShortName}</string>
    <key>CFBundleName</key>
    <string>{DocSetName}</string>
    <key>DocSetPlatformFamily</key>
    <string>{DocSetShortName}</string>
    <key>isDashDocset</key>
    <true/>
    <key>dashIndexFilePath</key>
    <string>{DashIndexFilePath}</string>
    <key>DashDocSetFamily</key>
    <string>dashtoc</string>
    </dict>
    </plist>";
}
