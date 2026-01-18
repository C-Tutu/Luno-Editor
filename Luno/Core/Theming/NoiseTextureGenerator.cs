namespace Luno.Core.Theming;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
/// 紙の質感を表現するノイズテクスチャを動的生成
/// 外部ファイル不要でAOT互換
/// </summary>
public static class NoiseTextureGenerator
{
    private const int TextureSize = 128;
    private static WriteableBitmap? _cachedTexture;
    private static readonly object _lock = new();

    /// <summary>
    /// ノイズテクスチャブラシを取得
    /// タイル状に繰り返し描画可能
    /// </summary>
    public static ImageBrush GetNoiseBrush()
    {
        var texture = GetOrCreateTexture();
        return new ImageBrush(texture)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, TextureSize, TextureSize),
            ViewportUnits = BrushMappingMode.Absolute,
            Opacity = 0.04
        };
    }

    /// <summary>
    /// キャッシュされたテクスチャを取得または生成
    /// </summary>
    private static WriteableBitmap GetOrCreateTexture()
    {
        if (_cachedTexture is not null)
            return _cachedTexture;

        lock (_lock)
        {
            if (_cachedTexture is not null)
                return _cachedTexture;

            _cachedTexture = GenerateNoiseTexture();
            return _cachedTexture;
        }
    }

    /// <summary>
    /// モノクロノイズテクスチャを生成
    /// 軽量な紙の質感を表現
    /// </summary>
    private static WriteableBitmap GenerateNoiseTexture()
    {
        var bitmap = new WriteableBitmap(
            TextureSize,
            TextureSize,
            96, 96,
            PixelFormats.Bgra32,
            null);

        var pixels = new byte[TextureSize * TextureSize * 4];
        var random = new Random(42); // 固定シードで再現性確保

        for (int i = 0; i < pixels.Length; i += 4)
        {
            // ランダムなグレースケール値（透明度で調整）
            byte noise = (byte)random.Next(0, 256);
            byte alpha = (byte)random.Next(20, 80); // 微細な透明度変化

            pixels[i] = noise;     // B
            pixels[i + 1] = noise; // G
            pixels[i + 2] = noise; // R
            pixels[i + 3] = alpha; // A
        }

        bitmap.WritePixels(
            new Int32Rect(0, 0, TextureSize, TextureSize),
            pixels,
            TextureSize * 4,
            0);

        bitmap.Freeze(); // 不変化してパフォーマンス向上
        return bitmap;
    }
}
