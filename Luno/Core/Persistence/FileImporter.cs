namespace Luno.Core.Persistence;

using System.IO;
using System.Text;
using Microsoft.Win32;

/// <summary>
/// ファイルインポート機能
/// </summary>
public static class FileImporter
{
    /// <summary>
    /// 対応ファイルフィルター
    /// </summary>
    public static string FileFilter =>
        "テキストファイル (*.txt)|*.txt|" +
        "Markdown (*.md)|*.md|" +
        "すべてのテキスト|*.txt;*.md;*.log;*.cs;*.js;*.py;*.json;*.xml;*.html;*.css;*.yaml;*.yml|" +
        "すべてのファイル|*.*";

    /// <summary>
    /// ファイル選択ダイアログを表示
    /// </summary>
    public static string? ShowOpenDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = FileFilter,
            Title = "ファイルを開く"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>
    /// ファイルを非同期で読み込み
    /// </summary>
    public static async Task<(string content, Encoding encoding)> ReadFileAsync(string path)
    {
        // エンコーディング自動検出
        var bytes = await File.ReadAllBytesAsync(path);
        var encoding = DetectEncoding(bytes);
        var content = encoding.GetString(bytes);
        return (content, encoding);
    }

    /// <summary>
    /// エンコーディング自動検出
    /// </summary>
    private static Encoding DetectEncoding(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode;
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode;
        
        // デフォルトはUTF-8
        return Encoding.UTF8;
    }

    /// <summary>
    /// ファイルを非同期で保存
    /// </summary>
    public static async Task SaveFileAsync(string path, string content)
    {
        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
    }

    /// <summary>
    /// 保存ダイアログを表示
    /// </summary>
    public static string? ShowSaveDialog(string defaultFileName = "無題.txt")
    {
        var dialog = new SaveFileDialog
        {
            Filter = FileFilter,
            Title = "名前を付けて保存",
            FileName = defaultFileName
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
