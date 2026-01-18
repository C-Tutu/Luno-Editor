namespace Luno.Core.Theming;

using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

/// <summary>
/// OSテーマ設定の検知とアプリケーションテーマの動的切り替えを管理
/// </summary>
public sealed class ThemeManager
{
    private static ThemeManager? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static ThemeManager Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    _instance ??= new ThemeManager();
                }
            }
            return _instance;
        }
    }

    private ThemeManager()
    {
        // OSテーマ変更の監視を開始
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    /// <summary>
    /// 現在のテーマがダークモードかどうか
    /// </summary>
    public bool IsDarkMode => GetSystemTheme();

    /// <summary>
    /// テーマ変更時に発火するイベント
    /// </summary>
    public event Action<bool>? ThemeChanged;

    /// <summary>
    /// システムのテーマ設定を取得
    /// </summary>
    private static bool GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            
            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0; // 0 = Dark, 1 = Light
            }
        }
        catch
        {
            // レジストリアクセス失敗時はライトモードをデフォルトとする
        }
        return false;
    }

    /// <summary>
    /// OSのテーマ変更を検知するハンドラ
    /// </summary>
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            ThemeChanged?.Invoke(IsDarkMode);
        }
    }

    /// <summary>
    /// リソースの解放（アプリ終了時に呼び出す）
    /// </summary>
    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }
}

/// <summary>
/// Lunoアプリケーションの色定義
/// README.md Section 4 に基づく
/// </summary>
public static class LunoColors
{
    // Base Colors (背景色)
    public static readonly Color PaperLight = Color.FromRgb(0xF9, 0xF9, 0xF7);
    public static readonly Color PaperDark = Color.FromRgb(0x1E, 0x1E, 0x1E);

    // Accents (Markdown & UI要素用)
    public static readonly Color AccentRed = Color.FromRgb(0xD3, 0x2F, 0x2F);    // 重要項目
    public static readonly Color AccentBlue = Color.FromRgb(0x19, 0x76, 0xD2);   // リンク
    public static readonly Color AccentGreen = Color.FromRgb(0x38, 0x8E, 0x3C);  // 完了/安全
    public static readonly Color AccentYellow = Color.FromRgb(0xFB, 0xC0, 0x2D); // ハイライト

    // Text Colors
    public static readonly Color TextLight = Color.FromRgb(0x21, 0x21, 0x21);
    public static readonly Color TextDark = Color.FromRgb(0xE0, 0xE0, 0xE0);
    public static readonly Color TextMuted = Color.FromRgb(0x75, 0x75, 0x75);

    /// <summary>
    /// 現在のテーマに応じた背景色を取得
    /// </summary>
    public static Color GetBackgroundColor(bool isDark) => isDark ? PaperDark : PaperLight;

    /// <summary>
    /// 現在のテーマに応じたテキスト色を取得
    /// </summary>
    public static Color GetTextColor(bool isDark) => isDark ? TextDark : TextLight;
}
