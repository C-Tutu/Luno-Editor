namespace Luno.Core.Editor;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// タブデータモデル（ファイル状態管理）
/// </summary>
public class EditorTab : INotifyPropertyChanged
{
    private string _fileName = "無題";
    private string? _filePath;
    private bool _isModified;
    private string _content = "";

    /// <summary>
    /// 表示用ファイル名
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// ファイルパス（新規の場合はnull）
    /// </summary>
    public string? FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 変更フラグ
    /// </summary>
    public bool IsModified
    {
        get => _isModified;
        set { _isModified = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    /// <summary>
    /// タブ表示名（変更時は*付き）
    /// </summary>
    public string DisplayName => IsModified ? $"{FileName}*" : FileName;

    /// <summary>
    /// エディタ内容
    /// </summary>
    public string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
