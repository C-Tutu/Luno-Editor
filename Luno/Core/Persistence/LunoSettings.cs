namespace Luno.Core.Persistence;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// アプリケーション設定モデル
/// Source Generator対応のためpartial宣言
/// </summary>
public sealed class LunoSettings
{
    /// <summary>
    /// ウィンドウ左端位置
    /// </summary>
    public double WindowLeft { get; set; } = 100;

    /// <summary>
    /// ウィンドウ上端位置
    /// </summary>
    public double WindowTop { get; set; } = 100;

    /// <summary>
    /// ウィンドウ幅
    /// </summary>
    public double WindowWidth { get; set; } = 800;

    /// <summary>
    /// ウィンドウ高さ
    /// </summary>
    public double WindowHeight { get; set; } = 600;

    /// <summary>
    /// ウィンドウが最大化されているか
    /// </summary>
    public bool IsMaximized { get; set; }

    /// <summary>
    /// フォントサイズ
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// フォントファミリー
    /// </summary>
    public string FontFamily { get; set; } = "Consolas, Yu Gothic UI, Meiryo";

    /// <summary>
    /// 最後に編集したテキスト（自動保存用）
    /// </summary>
    public string LastContent { get; set; } = "";

    /// <summary>
    /// 最終保存日時
    /// </summary>
    public DateTime LastSavedAt { get; set; } = DateTime.MinValue;

    /// <summary>
    /// テーマ名（Kinari, Sumi, Sakura, など）
    /// </summary>
    public string ThemeName { get; set; } = "";

    /// <summary>
    /// ズームレベル（50-200%）
    /// </summary>
    public int ZoomLevel { get; set; } = 100;
}

/// <summary>
/// JSON Source Generator コンテキスト
/// AOT互換のためリフレクション不使用
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LunoSettings))]
public partial class LunoSettingsContext : JsonSerializerContext
{
}
