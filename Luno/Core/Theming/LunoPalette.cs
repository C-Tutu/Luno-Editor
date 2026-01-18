namespace Luno.Core.Theming;

using System.Windows.Media;

/// <summary>
/// 12色のLunoテーマパレット
/// 「紙のような質感」と「目の負担軽減」を両立
/// 完全な黒(#000)や白(#FFF)は使用しない
/// </summary>
public static class LunoPalette
{
    /// <summary>
    /// テーマ定義（和名、背景色、文字色）
    /// </summary>
    public record LunoTheme(string Name, string JapaneseName, Color Background, Color Text);

    /// <summary>
    /// 利用可能な12色のテーマ
    /// </summary>
    public static readonly LunoTheme[] Themes =
    {
        // ライト系テーマ
        new("Kinari", "生成", Color.FromRgb(0xFA, 0xF8, 0xF5), Color.FromRgb(0x2D, 0x2D, 0x2D)),  // オフホワイト
        new("Sakura", "桜", Color.FromRgb(0xFD, 0xF5, 0xF5), Color.FromRgb(0x3D, 0x2D, 0x2D)),    // 淡いピンク
        new("Sora", "空", Color.FromRgb(0xF3, 0xF7, 0xFA), Color.FromRgb(0x2D, 0x35, 0x3D)),      // 薄青
        new("Matcha", "抹茶", Color.FromRgb(0xF5, 0xF8, 0xF3), Color.FromRgb(0x2D, 0x3D, 0x2D)),  // 薄緑
        new("Anzu", "杏", Color.FromRgb(0xFD, 0xF8, 0xF3), Color.FromRgb(0x3D, 0x35, 0x2D)),      // ウォームオレンジ
        new("Kogane", "黄金", Color.FromRgb(0xFC, 0xFA, 0xF0), Color.FromRgb(0x3D, 0x3D, 0x2D)), // アンティークイエロー
        
        // ダーク系テーマ
        new("Sumi", "墨", Color.FromRgb(0x1F, 0x1F, 0x23), Color.FromRgb(0xE8, 0xE8, 0xE5)),      // ダークグレー
        new("Yoru", "夜", Color.FromRgb(0x1A, 0x1D, 0x23), Color.FromRgb(0xD5, 0xDD, 0xE8)),      // ミッドナイトブルー
        new("Fuji", "藤", Color.FromRgb(0x21, 0x1F, 0x25), Color.FromRgb(0xE0, 0xDD, 0xE8)),      // 薄紫
        new("Nezumi", "鼠", Color.FromRgb(0x24, 0x24, 0x24), Color.FromRgb(0xD8, 0xD8, 0xD8)),    // ニュートラルグレー
        new("Koubai", "紅梅", Color.FromRgb(0x25, 0x1F, 0x21), Color.FromRgb(0xE8, 0xDD, 0xE0)), // 赤紫
        new("Aizome", "藍染", Color.FromRgb(0x1A, 0x20, 0x28), Color.FromRgb(0xD5, 0xE0, 0xE8)), // 濃藍
    };

    /// <summary>
    /// テーマ名からテーマを取得（見つからない場合はデフォルト）
    /// </summary>
    public static LunoTheme GetTheme(string name)
    {
        foreach (var theme in Themes)
        {
            if (theme.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return theme;
        }
        return Themes[0]; // Kinari (default light)
    }

    /// <summary>
    /// ダークテーマかどうかを判定（背景の明度で判断）
    /// </summary>
    public static bool IsDarkTheme(LunoTheme theme)
    {
        var brightness = (theme.Background.R * 0.299 + theme.Background.G * 0.587 + theme.Background.B * 0.114);
        return brightness < 128;
    }

    /// <summary>
    /// OS設定に応じたデフォルトテーマを取得
    /// </summary>
    public static LunoTheme GetDefaultTheme(bool systemIsDark)
    {
        return systemIsDark ? GetTheme("Sumi") : GetTheme("Kinari");
    }
}
