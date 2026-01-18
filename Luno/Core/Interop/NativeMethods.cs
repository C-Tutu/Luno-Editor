namespace Luno.Core.Interop;

using System.Runtime.InteropServices;

/// <summary>
/// Win32 API P/Invoke 定義
/// DWMによるMica効果およびダークモード制御に使用
/// </summary>
internal static partial class NativeMethods
{
    // DWM Window Attribute Types
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    // Backdrop Types
    public const int DWMSBT_AUTO = 0;
    public const int DWMSBT_NONE = 1;
    public const int DWMSBT_MAINWINDOW = 2;  // Mica
    public const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic
    public const int DWMSBT_TABBEDWINDOW = 4;  // Mica Alt

    /// <summary>
    /// ウィンドウ属性を設定するDWM API
    /// </summary>
    [LibraryImport("dwmapi.dll")]
    public static partial int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    /// <summary>
    /// Windows 11 Build 22000以上かどうかを判定
    /// Mica効果はWin11以降でのみサポート
    /// </summary>
    public static bool IsWindows11OrGreater()
    {
        return Environment.OSVersion.Version.Build >= 22000;
    }

    /// <summary>
    /// Windows 11 22H2 (Build 22621) 以上かどうかを判定
    /// DWMSBT_TABBEDWINDOW (Mica Alt) はこのバージョン以降でサポート
    /// </summary>
    public static bool IsWindows11_22H2OrGreater()
    {
        return Environment.OSVersion.Version.Build >= 22621;
    }
}
