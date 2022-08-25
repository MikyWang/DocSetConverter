# DocSetConverter

[![GitHub issues](https://img.shields.io/github/issues/MikyWang/DocSetConverter)](https://github.com/MikyWang/DocSetConverter/issues)
[![GitHub forks](https://img.shields.io/github/forks/MikyWang/DocSetConverter)](https://github.com/MikyWang/DocSetConverter/network)
[![GitHub stars](https://img.shields.io/github/stars/MikyWang/DocSetConverter)](https://github.com/MikyWang/DocSetConverter/stargazers)
[![GitHub license](https://img.shields.io/github/license/MikyWang/DocSetConverter)](https://github.com/MikyWang/DocSetConverter/blob/main/LICENSE)
[![QQ群](https://img.shields.io/badge/QQ%E7%BE%A4-485860756-blue)](https://qm.qq.com/cgi-bin/qm/qr?k=H0Y3-K_amuWI8dngP_3T63CB1LHgqJKe&authKey=FJqZ7cANv+KRFXRSfJPUIq/TNiOvRqA0TsUKe5aOGxUO0wlNOy0RVEnnFitJ8s58&noverify=0)

## 手册预览图

![手册预览图](https://github.com/MikyWang/DocSetConverter/blob/main/images/preview.png)

## 用途


由于Dash中几乎没有中文手册，所以本工程根据中文网页版API文档镜像成本地文档，再转换成DocSet文档，支持Dash Zeal导入使用。

## 已生成的手册


可 [单击加入QQ群](https://qm.qq.com/cgi-bin/qm/qr?k=QMjmDrFL8TS16lyNWp7A4ti83BT-TNIJ&authKey=vBHc5brxt11RDcibiR8bkDZ5Ukcw7B0w9cNtUsdCp9ZTevoTMEhyFw0zOKLIwXeI&noverify=0) 后在群文件内下载以下手册：

- [x]  Unity Manual 中文手册
- [x]  Unity ScriptReference 中文手册
- [ ]  .Net6.0 API 中文手册
- [ ]  C# Manual 中文手册

## 使用方法


1. 安装 `httrack` ,macos 下可使用 homebrew 进行安装：
    
    ```bash
    brew install httrack
    ```
    
2. 下载最新的 Release 压缩包：
    
    ```bash
    https://github.com/MikyWang/DocSetConverter/releases
    ```
    
3. 解压。
4. 在net6.0同级目录新建目录及DocSet文件：
    
    ```bash
    cd net6.0
    mkdir unity
    mkdir -p unity/Unity3D-Chinese.docset/Contents/Resources/Documents/
    ```
    
5. 执行脚本将网站镜像至本地：
    
    ```bash
    ./unity-toc.sh unity/unity-docs
    mv unity/unity-docs/docs.unity.cn/cn/current/Manual unity/Unity3D-Chinese.docset/Contents/Resources/Documents/
    mv unity/unity-docs/docs.unity.cn/cn/current/ScriptReference unity/Unity3D-Chinese.docset/Contents/Resources/Documents/
    ```
    
6. 打开 net6.0目录中 `appsettings.json` ，将DocSetPath的值设置为docset文件的绝对路径：
    
    ```json
    {
      "DocSetConfig": {
        "DocSetName": "Unity3D-Chinese",
        "DocSetShortName": "UnityChinese",
        "DocSetPath": "unity",
        "DashIndexFilePath": "Manual/index.html"
      }
    }
    ```
    
7. 执行文档转换程序：
    
    ```bash
    ./UnityDocConverter
    ```
    
8. 等待手册制作完成后，关闭终端，将unity目录下的 `Unity3D-Chinese.docset` 导入至 `Dash` 或 `Zeal` 中。
9. 打开 `Unity3D-Chinese` 文档。
    
    
    
