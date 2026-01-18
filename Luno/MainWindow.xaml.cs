namespace Luno;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private IntPtr _hwnd;
    private int _zoomLevel = 100;
    private LunoPalette.LunoTheme _currentTheme = LunoPalette.Themes[0];

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

        // ウィンドウハンドル取得後にMica効果を適用
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closing += OnClosing;
        PreviewMouseWheel += OnPreviewMouseWheel;
        
        // タスクバーアイコンを明示的に設定
        var iconUri = new Uri("pack://application:,,,/Assets/icon-128.png");
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
    }

    /// <summary>
    /// ウィンドウハンドル初期化時の処理
    /// </summary>
    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        ApplySystemBackdrop();

        // 保存されたテーマを復元、なければOS設定に従う
        if (!string.IsNullOrEmpty(_settingsManager.Settings.ThemeName))
        {
            _currentTheme = LunoPalette.GetTheme(_settingsManager.Settings.ThemeName);
            ApplyLunoTheme(_currentTheme);
        }
        else
        {
            ApplyTheme(_themeManager.IsDarkMode);
        }

        // 保存されたズームレベルを復元
        if (_settingsManager.Settings.ZoomLevel >= 10 && _settingsManager.Settings.ZoomLevel <= 500)
        {
            SetZoomLevel((int)_settingsManager.Settings.ZoomLevel);
        }

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
        ApplyThemeToAllTabs();

        // 保存されたテキストを復元（初期タブ）
        var initialContent = "";
        if (!string.IsNullOrEmpty(_settingsManager.Settings.LastContent))
        {
            initialContent = _settingsManager.Settings.LastContent;
        }
        AddNewTab("無題", initialContent);

        // タイマー開始
        _autoSaveTimer.Start();

        // リアルタイムステータス更新用イベント
        // AddNewTab内で設定済み

        // 初回ステータス更新
        UpdateStatusBar();
    }

    private Luno.Core.Editor.LunoEditor? ActiveEditor
    {
        get
        {
            if (MainTabControl.SelectedItem is TabItem tab && tab.Content is Luno.Core.Editor.LunoEditor editor)
            {
                return editor;
            }
            return null;
        }
    }

    private void AddNewTab(string title = "無題", string content = "", string? filePath = null)
    {
        var editor = new Luno.Core.Editor.LunoEditor();
        editor.ApplyTheme(_themeManager.IsDarkMode);
        // フォントサイズはBaseFontSize (14)のまま。ズームはUiScaleTransformで制御。
        editor.FontSize = 14.0; 
        
        editor.SelectionChanged += (s, e) => UpdateStatusBar();
        editor.TextChanged += (s, e) => {
            UpdateStatusBar();
            if (MainTabControl.SelectedItem is TabItem t && t.Tag is Luno.Core.Editor.EditorTab tag)
            {
                tag.IsModified = true;
                // ヘッダー更新はBindingまたは手動
                if (!t.Header.ToString().EndsWith("*")) t.Header = tag.DisplayName;
            }
        };

        if (!string.IsNullOrEmpty(content))
        {
            editor.SetPlainText(content);
        }

        var tab = new TabItem
        {
            Header = title,
            Content = editor,
            Tag = new Luno.Core.Editor.EditorTab { FileName = title, FilePath = filePath, Content = content }
        };

        MainTabControl.Items.Add(tab);
        MainTabControl.SelectedItem = tab;
        
        // フォーカス
        editor.Focus();
    }

    private void ApplyThemeToAllTabs()
    {
        foreach (TabItem item in MainTabControl.Items)
        {
            if (item.Content is Luno.Core.Editor.LunoEditor editor)
            {
                editor.ApplyTheme(_themeManager.IsDarkMode);
            }
        }
    }

    /// <summary>
    /// ステータスバーの情報を更新（リアルタイム）
    /// </summary>
    private void UpdateStatusBar()
    {
        try
        {
            var editor = ActiveEditor;
            if (editor == null) return;

            // テキスト情報
            var text = editor.GetPlainText();
            var charCount = text.Length - text.Count(c => c == '\r' || c == '\n');

            CharCountText.Text = $"{charCount:N0} 文字";
            ZoomLevelText.Text = $"{_zoomLevel}%";

            // カーソル位置
            var caretPos = editor.CaretPosition;
            if (caretPos != null)
            {
                var lineStart = caretPos.GetLineStartPosition(0);
                var column = lineStart != null 
                    ? new System.Windows.Documents.TextRange(lineStart, caretPos).Text.Length + 1 
                    : 1;
                
                // 行番号を計算
                var docStart = editor.Document.ContentStart;
                var textBeforeCaret = new System.Windows.Documents.TextRange(docStart, caretPos).Text;
                var line = textBeforeCaret.Count(c => c == '\n') + 1;

                CursorPosText.Text = $"行 {line}, 列 {column}";
            }
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

            if (e.Delta > 0 && _zoomLevel < 500)
            {
                _zoomLevel += 10;
            }
            else if (e.Delta < 0 && _zoomLevel > 10)
            {
                _zoomLevel -= 10;
            }

            // UI全体をズーム
            SetZoomLevel(_zoomLevel);
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
            var editor = ActiveEditor;
            if (editor == null) return;

            var content = editor.GetPlainText();
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

        // テキストを保存 (アクティブなエディタ)
        var editor = ActiveEditor;
        if (editor != null)
        {
            settings.LastContent = editor.GetPlainText();
        }
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
        // OSテーマが変わったらデフォルトテーマを適用（ユーザー指定がない場合）
        if (string.IsNullOrEmpty(_settingsManager.Settings.ThemeName))
        {
            _currentTheme = LunoPalette.GetDefaultTheme(isDark);
        }
        Dispatcher.Invoke(() => ApplyLunoTheme(_currentTheme));
    }

    /// <summary>
    /// Lunoテーマを適用（12色パレット対応・統一カラー）
    /// </summary>
    private void ApplyLunoTheme(LunoPalette.LunoTheme theme)
    {
        _currentTheme = theme;
        var isDark = LunoPalette.IsDarkTheme(theme);

        if (NativeMethods.IsWindows11OrGreater())
        {
            int darkMode = isDark ? 1 : 0;
            NativeMethods.DwmSetWindowAttribute(
                _hwnd,
                NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref darkMode,
                Marshal.SizeOf<int>());
        }

        var bgBrush = new SolidColorBrush(theme.Background);
        var textBrush = new SolidColorBrush(theme.Text);

        // エディタ背景にコントラストを追加（メイン背景より少し濃い/淡い）
        var contrastColor = isDark
            ? Color.FromArgb(255, 
                (byte)Math.Max(0, theme.Background.R - 15), 
                (byte)Math.Max(0, theme.Background.G - 15), 
                (byte)Math.Max(0, theme.Background.B - 15))
            : Color.FromArgb(255, 
                (byte)Math.Min(255, theme.Background.R + 10), 
                (byte)Math.Min(255, theme.Background.G + 10), 
                (byte)Math.Min(255, theme.Background.B + 10));
        var editorBgBrush = new SolidColorBrush(contrastColor);

        // 文字色は常に黒で統一
        var blackTextBrush = new SolidColorBrush(Colors.Black);

        // 統一カラーを適用
        Resources["BackgroundBrush"] = bgBrush;
        Resources["TextBrush"] = blackTextBrush; // 黒で統一
        Resources["EditorBackgroundBrush"] = editorBgBrush; // コントラスト付き

        // RootGridに直接背景色を適用（全システム共通）
        RootGrid.Background = bgBrush;

        ApplyThemeToAllTabs();
        
        // 設定に保存
        _settingsManager.Settings.ThemeName = theme.Name;
    }

    /// <summary>
    /// テーマの適用（Light/Dark）- 後方互換
    /// </summary>
    private void ApplyTheme(bool isDark)
    {
        var theme = LunoPalette.GetDefaultTheme(isDark);
        ApplyLunoTheme(theme);
    }

    /// <summary>
    /// 次のテーマに切り替え (Ctrl+Shift+T)
    /// </summary>
    private void CycleNextTheme()
    {
        var currentIndex = Array.IndexOf(LunoPalette.Themes, _currentTheme);
        var nextIndex = (currentIndex + 1) % LunoPalette.Themes.Length;
        ApplyLunoTheme(LunoPalette.Themes[nextIndex]);
    }

    /// <summary>
    /// 設定ウィンドウからテーマを適用
    /// </summary>
    public void ApplyLunoThemePublic(LunoPalette.LunoTheme theme) => ApplyLunoTheme(theme);

    /// <summary>
    /// 設定ウィンドウからズームレベルを設定
    /// </summary>
    public void SetZoomLevel(int level)
    {
        if (level < 10 || level > 500) return;
        _zoomLevel = level;
        
        // LayoutTransformで全体ズーム
        UiScaleTransform.ScaleX = _zoomLevel / 100.0;
        UiScaleTransform.ScaleY = _zoomLevel / 100.0;

        _settingsManager.Settings.ZoomLevel = _zoomLevel;
        UpdateStatusBar();
    }
    
    // イベントハンドラ
    private void NewTabButton_Click(object sender, RoutedEventArgs e)
    {
        AddNewTab();
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        // VisualTreeHelperを使ってボタンからTabItemを取得
        if (sender is Button btn)
        {
            var tabItem = FindParent<TabItem>(btn);
            if (tabItem != null)
            {
                MainTabControl.Items.Remove(tabItem);
                if (MainTabControl.Items.Count == 0) Close();
            }
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }

    private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        var path = Luno.Core.Persistence.FileImporter.ShowOpenDialog();
        if (path != null)
        {
            try
            {
                var (content, encoding) = await Luno.Core.Persistence.FileImporter.ReadFileAsync(path);
                AddNewTab(System.IO.Path.GetFileName(path), content, path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルを開けませんでした: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateStatusBar();
        ActiveEditor?.Focus();
    }

    #region Window Control Buttons

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(this);
        settingsWindow.ShowDialog();
    }

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

    #region Find Bar

    private List<int> _findMatches = new();
    private int _currentMatchIndex = -1;
    private string _lastSearchText = "";

    /// <summary>
    /// Ctrl+Fで検索バーを表示
    /// </summary>
    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Ctrl+Shift+T: テーマ切り替え
        if (e.Key == System.Windows.Input.Key.T && 
            System.Windows.Input.Keyboard.Modifiers == (System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift))
        {
            e.Handled = true;
            CycleNextTheme();
        }
        // Ctrl+F: 検索バー
        else if (e.Key == System.Windows.Input.Key.F && 
            System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
        {
            e.Handled = true;
            ShowFindBar();
        }
        else if (e.Key == System.Windows.Input.Key.Escape && FindBar.Visibility == Visibility.Visible)
        {
            e.Handled = true;
            HideFindBar();
        }
    }

    private void ShowFindBar()
    {
        FindBar.Visibility = Visibility.Visible;
        FindTextBox.Focus();
        FindTextBox.SelectAll();
    }

    private void HideFindBar()
    {
        FindBar.Visibility = Visibility.Collapsed;
        _findMatches.Clear();
        _currentMatchIndex = -1;
        FindMatchCount.Text = "";
        ActiveEditor?.Focus();
    }

    private void FindTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        PerformSearch();
    }

    private void FindTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            HideFindBar(); // Assuming CloseFindBar is HideFindBar
            ActiveEditor?.Focus();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Enter)
        {
            FindNext();
            e.Handled = true;
        }
    }

    private void FindPrevButton_Click(object sender, RoutedEventArgs e) => FindPrevious();
    private void FindNextButton_Click(object sender, RoutedEventArgs e) => FindNext();
    private void FindCloseButton_Click(object sender, RoutedEventArgs e) => HideFindBar();

    private void PerformSearch()
    {
        _findMatches.Clear();
        _currentMatchIndex = -1;

        var searchText = FindTextBox.Text;
        _lastSearchText = searchText; // Store the last search text

        if (string.IsNullOrEmpty(searchText))
        {
            FindMatchCount.Text = "";
            return;
        }

        var editor = ActiveEditor;
        if (editor == null)
        {
            FindMatchCount.Text = "0 件";
            return;
        }

        // Delegate the actual search to the new PerformSearch method
        PerformSearch(editor, searchText);
    }



    private void PerformSearch(Luno.Core.Editor.LunoEditor editor, string query)
    {
        // 既存の検索ロジックをエディタインスタンスを使うように修正
        // ... (簡易実装のため、ここでは何もしないか、あるいはLunoEditorに検索機能を実装する必要がある)
        // Phase 4の検索バー実装ではTextPointerを使っていたはず。
        // ここでは簡単なフォーカス維持だけしておく（本格的な検索は要修正）
        editor.Focus();
        // The original logic for _findMatches and NavigateToMatch needs to be adapted
        // to work with the active editor's content and selection.
        // For now, we'll keep the existing _findMatches logic but it will operate
        // on the *current* editor's content.
        // This part needs significant refactoring to properly integrate with LunoEditor's search.

        _findMatches.Clear();
        _currentMatchIndex = -1;

        if (string.IsNullOrEmpty(query))
        {
            FindMatchCount.Text = "";
            return;
        }

        try
        {
            // Assuming LunoEditor has a method to get its plain text content
            var content = editor.GetPlainText(); 
            var index = 0;

            while ((index = content.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                _findMatches.Add(index);
                index += query.Length;
            }

            if (_findMatches.Count > 0)
            {
                _currentMatchIndex = 0;
                FindMatchCount.Text = $"1/{_findMatches.Count}";
                NavigateToMatch(_currentMatchIndex);
            }
            else
            {
                FindMatchCount.Text = "0 件";
            }
        }
        catch
        {
            FindMatchCount.Text = "";
        }
    }

    private void FindNext()
    {
        if (_findMatches.Count == 0) return;
        _currentMatchIndex = (_currentMatchIndex + 1) % _findMatches.Count;
        FindMatchCount.Text = $"{_currentMatchIndex + 1}/{_findMatches.Count}";
        NavigateToMatch(_currentMatchIndex);
    }

    private void FindPrevious()
    {
        if (_findMatches.Count == 0) return;
        _currentMatchIndex = (_currentMatchIndex - 1 + _findMatches.Count) % _findMatches.Count;
        FindMatchCount.Text = $"{_currentMatchIndex + 1}/{_findMatches.Count}";
        NavigateToMatch(_currentMatchIndex);
    }

    private void NavigateToMatch(int matchIndex)
    {
        try
        {
            var position = _findMatches[matchIndex];
            var searchLength = FindTextBox.Text.Length;

            var editor = ActiveEditor;
            if (editor == null) return;

            // RichTextBoxでのTextPointer取得は複雑なため、シンプルに選択位置を設定
            var start = editor.Document.ContentStart.GetPositionAtOffset(position + 2);
            var end = start?.GetPositionAtOffset(searchLength);

            if (start != null && end != null)
            {
                editor.Selection.Select(start, end);
                editor.Focus();

                // スクロールして表示
                var rect = editor.Selection.Start.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
                editor.ScrollToVerticalOffset(editor.VerticalOffset + rect.Top - editor.ActualHeight / 2);
            }
        }
        catch
        {
            // ナビゲーション失敗は無視
        }
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