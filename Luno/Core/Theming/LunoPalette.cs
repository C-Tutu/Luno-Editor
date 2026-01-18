namespace Luno.Core.Theming;

using System.Windows.Media;

/// <summary>
/// 12色のLunoテーマパレット
/// 「紙のような質感」と「目の負担軽減」を両立
/// メモ帳風ダークを基準に、視覚的に明確な差を持たせた配色
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
        // ライト系テーマ（より明確な色味）
        new("Kinari", "生成", Color.FromRgb(0xF5, 0xF3, 0xEB), Color.FromRgb(0x2A, 0x2A, 0x2A)),  // クリーム
        new("Sakura", "桜", Color.FromRgb(0xFD, 0xE8, 0xEB), Color.FromRgb(0x3D, 0x2A, 0x2A)),    // ピンク
        new("Sora", "空", Color.FromRgb(0xE8, 0xF0, 0xFA), Color.FromRgb(0x2A, 0x30, 0x40)),      // スカイブルー
        new("Matcha", "抹茶", Color.FromRgb(0xE8, 0xF5, 0xE0), Color.FromRgb(0x2A, 0x3A, 0x28)),  // 明るい緑
        new("Anzu", "杏", Color.FromRgb(0xFD, 0xF0, 0xE0), Color.FromRgb(0x40, 0x30, 0x20)),      // オレンジクリーム
        new("Kogane", "黄金", Color.FromRgb(0xFC, 0xF5, 0xD8), Color.FromRgb(0x40, 0x38, 0x20)), // ゴールド
        
        // ダーク系テーマ（メモ帳風 #272727 を基準）
        new("Sumi", "墨", Color.FromRgb(0x27, 0x27, 0x27), Color.FromRgb(0xE8, 0xE8, 0xE5)),      // メモ帳ダーク
        new("Yoru", "夜", Color.FromRgb(0x1A, 0x22, 0x35), Color.FromRgb(0xC0, 0xD0, 0xF0)),      // ネイビーブルー
        new("Fuji", "藤", Color.FromRgb(0x28, 0x20, 0x35), Color.FromRgb(0xD8, 0xD0, 0xF0)),      // パープル
        new("Nezumi", "鼠", Color.FromRgb(0x30, 0x30, 0x30), Color.FromRgb(0xD8, 0xD8, 0xD8)),    // ダークグレー
        new("Koubai", "紅梅", Color.FromRgb(0x30, 0x20, 0x28), Color.FromRgb(0xF0, 0xD0, 0xE0)), // ワインレッド
        new("Aizome", "藍染", Color.FromRgb(0x18, 0x25, 0x38), Color.FromRgb(0xC0, 0xE0, 0xF8)), // ディープブルー
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
