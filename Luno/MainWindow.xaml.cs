namespace Luno;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Luno.Core.Interop;
using Luno.Core.Persistence;
using Luno.Core.Theming;

/// <summary>
/// Lunoメインウィンドウ
/// Chromelessウィンドウ + Mica効果 + テーマ対応 + 自動保存
/// </summary>
public partial class MainWindow : Window
{
    private readonly ThemeManager _themeManager;
    private readonly SettingsManager _settingsManager;
    private readonly DispatcherTimer _autoSaveTimer;
    private readonly DispatcherTimer _statusUpdateTimer;
    private IntPtr _hwnd;
    private int _zoomLevel = 100;

    public MainWindow()
    {
        InitializeComponent();

        _themeManager = ThemeManager.Instance;
        _themeManager.ThemeChanged += OnThemeChanged;

        _settingsManager = SettingsManager.Instance;

        // 自動保存タイマー（3秒間隔）
        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _autoSaveTimer.Tick += OnAutoSaveTick;

        // ステータスバー更新タイマー（500ms間隔）
        _statusUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _statusUpdateTimer.Tick += OnStatusUpdateTick;

        // ウィンドウハンドル取得後にMica効果を適用
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closing += OnClosing;
        PreviewMouseWheel += OnPreviewMouseWheel;
    }

    /// <summary>
    /// ウィンドウハンドル初期化時の処理
    /// </summary>
    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        ApplySystemBackdrop();
        ApplyTheme(_themeManager.IsDarkMode);

        // 保存されたウィンドウ位置を復元
        RestoreWindowState();
    }

    /// <summary>
    /// ウィンドウ読み込み完了時の処理
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // ノイズテクスチャを適用
        NoiseOverlay.Fill = NoiseTextureGenerator.GetNoiseBrush();

        // エディタにテーマを適用
        Editor.ApplyTheme(_themeManager.IsDarkMode);

        // 保存されたテキストを復元
        if (!string.IsNullOrEmpty(_settingsManager.Settings.LastContent))
        {
            Editor.SetPlainText(_settingsManager.Settings.LastContent);
        }

        // タイマー開始
        _autoSaveTimer.Start();
        _statusUpdateTimer.Start();

        // 初回ステータス更新
        UpdateStatusBar();

        // エディタにフォーカスを設定
        Editor.Focus();
    }

    /// <summary>
    /// ステータスバー更新タイマーハンドラ
    /// </summary>
    private void OnStatusUpdateTick(object? sender, EventArgs e)
    {
        UpdateStatusBar();
    }

    /// <summary>
    /// ステータスバーの情報を更新
    /// </summary>
    private void UpdateStatusBar()
    {
        try
        {
            var text = Editor.GetPlainText();
            var charCount = text.Length - text.Count(c => c == '\r' || c == '\n');
            var lineCount = text.Split('\n').Length;

            CharCountText.Text = $"{charCount:N0} 文字";
            LineCountText.Text = $"{lineCount:N0} 行";
            ZoomLevelText.Text = $"{_zoomLevel}%";
        }
        catch
        {
            // 更新失敗は無視
        }
    }

    /// <summary>
    /// Ctrl+ホイールでズーム
    /// </summary>
    private void OnPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
        {
            e.Handled = true;

            if (e.Delta > 0 && _zoomLevel < 200)
            {
                _zoomLevel += 10;
            }
            else if (e.Delta < 0 && _zoomLevel > 50)
            {
                _zoomLevel -= 10;
            }

            // エディタのフォントサイズを更新
            Editor.FontSize = 14.0 * (_zoomLevel / 100.0);
            UpdateStatusBar();
        }
    }

    /// <summary>
    /// ウィンドウクローズ時の処理
    /// </summary>
    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _autoSaveTimer.Stop();
        SaveState();
    }

    /// <summary>
    /// 自動保存タイマーハンドラ
    /// </summary>
    private void OnAutoSaveTick(object? sender, EventArgs e)
    {
        SaveContentOnly();
    }

    /// <summary>
    /// テキストのみ保存（高頻度用）
    /// </summary>
    private void SaveContentOnly()
    {
        try
        {
            var content = Editor.GetPlainText();
            if (_settingsManager.Settings.LastContent != content)
            {
                _settingsManager.Settings.LastContent = content;
                _settingsManager.Settings.LastSavedAt = DateTime.Now;
                _ = _settingsManager.SaveAsync();
            }
        }
        catch
        {
            // 保存失敗は静かに無視
        }
    }

    /// <summary>
    /// 全状態を保存（終了時用）
    /// </summary>
    private void SaveState()
    {
        var settings = _settingsManager.Settings;

        // ウィンドウ状態を保存
        settings.IsMaximized = WindowState == WindowState.Maximized;

        if (WindowState == WindowState.Normal)
        {
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
        }

        // テキストを保存
        settings.LastContent = Editor.GetPlainText();
        settings.LastSavedAt = DateTime.Now;

        _settingsManager.Save();
    }

    /// <summary>
    /// ウィンドウ状態を復元
    /// </summary>
    private void RestoreWindowState()
    {
        var settings = _settingsManager.Settings;

        // 位置とサイズを復元（画面内に収まるように調整）
        var screenWidth = SystemParameters.VirtualScreenWidth;
        var screenHeight = SystemParameters.VirtualScreenHeight;

        if (settings.WindowLeft >= 0 && settings.WindowLeft < screenWidth - 100)
            Left = settings.WindowLeft;
        if (settings.WindowTop >= 0 && settings.WindowTop < screenHeight - 100)
            Top = settings.WindowTop;
        if (settings.WindowWidth > MinWidth && settings.WindowWidth <= screenWidth)
            Width = settings.WindowWidth;
        if (settings.WindowHeight > MinHeight && settings.WindowHeight <= screenHeight)
            Height = settings.WindowHeight;

        if (settings.IsMaximized)
            WindowState = WindowState.Maximized;
    }

    /// <summary>
    /// システム背景効果（Mica Alt）の適用
    /// </summary>
    private void ApplySystemBackdrop()
    {
        if (NativeMethods.IsWindows11_22H2OrGreater())
        {
            int backdropType = NativeMethods.DWMSBT_TABBEDWINDOW;
            NativeMethods.DwmSetWindowAttribute(
                _hwnd,
                NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdropType,
                Marshal.SizeOf<int>());
        }
        else if (NativeMethods.IsWindows11OrGreater())
        {
            int backdropType = NativeMethods.DWMSBT_MAINWINDOW;
            NativeMethods.DwmSetWindowAttribute(
                _hwnd,
                NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdropType,
                Marshal.SizeOf<int>());
        }
    }

    /// <summary>
    /// テーマ変更時のハンドラ
    /// </summary>
    private void OnThemeChanged(bool isDark)
    {
        Dispatcher.Invoke(() => ApplyTheme(isDark));
    }

    /// <summary>
    /// テーマの適用（Light/Dark）
    /// </summary>
    private void ApplyTheme(bool isDark)
    {
        if (NativeMethods.IsWindows11OrGreater())
        {
            int darkMode = isDark ? 1 : 0;
            NativeMethods.DwmSetWindowAttribute(
                _hwnd,
                NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref darkMode,
                Marshal.SizeOf<int>());
        }

        var bgColor = LunoColors.GetBackgroundColor(isDark);
        var textColor = LunoColors.GetTextColor(isDark);

        Resources["BackgroundBrush"] = new SolidColorBrush(bgColor);
        Resources["TextBrush"] = new SolidColorBrush(textColor);
        Resources["EditorBackgroundBrush"] = new SolidColorBrush(
            isDark ? Color.FromArgb(0x40, 0x00, 0x00, 0x00) : Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));

        if (!NativeMethods.IsWindows11OrGreater())
        {
            RootGrid.Background = new SolidColorBrush(bgColor);
        }

        Editor.ApplyTheme(isDark);
    }

    #region Window Control Buttons

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    /// <summary>
    /// クリーンアップ
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _autoSaveTimer.Stop();
        _themeManager.ThemeChanged -= OnThemeChanged;
        base.OnClosed(e);
    }
}