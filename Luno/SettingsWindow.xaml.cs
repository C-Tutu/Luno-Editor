namespace Luno;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Luno.Core.Theming;

/// <summary>
/// 設定ウィンドウ（12色テーマの視覚的選択）
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly MainWindow _mainWindow;
    private bool _isInitialized;
    private LunoPalette.LunoTheme _selectedTheme;

    public SettingsWindow(MainWindow owner)
    {
        InitializeComponent();
        Owner = owner;
        _mainWindow = owner;

        // 現在のテーマを取得
        var currentThemeName = Core.Persistence.SettingsManager.Instance.Settings.ThemeName;
        _selectedTheme = string.IsNullOrEmpty(currentThemeName)
            ? LunoPalette.GetDefaultTheme(ThemeManager.Instance.IsDarkMode)
            : LunoPalette.GetTheme(currentThemeName);

        // テーマを適用
        ApplyWindowTheme(_selectedTheme);
        
        // テーマボタンを生成
        CreateThemeButtons();
        
        // ズーム設定
        ZoomSlider.Value = Core.Persistence.SettingsManager.Instance.Settings.ZoomLevel;
        UpdateZoomText();
        
        _isInitialized = true;
    }

    /// <summary>
    /// ウィンドウ自体にテーマを適用
    /// </summary>
    private void ApplyWindowTheme(LunoPalette.LunoTheme theme)
    {
        RootBorder.Background = new SolidColorBrush(theme.Background);
        RootBorder.Tag = new SolidColorBrush(theme.Text); // TextBrush用
        
        // 全TextBlockにテーマを適用
        foreach (var tb in FindVisualChildren<TextBlock>(this))
        {
            tb.Foreground = new SolidColorBrush(theme.Text);
        }
    }

    /// <summary>
    /// 12色の円形ボタンを作成
    /// </summary>
    private void CreateThemeButtons()
    {
        ThemeButtonsPanel.Children.Clear();
        
        foreach (var theme in LunoPalette.Themes)
        {
            var button = new Button
            {
                Width = 36,
                Height = 36,
                Margin = new Thickness(4),
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = $"{theme.Name} ({theme.JapaneseName})",
                Tag = theme
            };

            // 円形のコンテンツ
            var ellipse = new Ellipse
            {
                Width = 28,
                Height = 28,
                Fill = new SolidColorBrush(theme.Background),
                Stroke = theme == _selectedTheme 
                    ? new SolidColorBrush(theme.Text) 
                    : new SolidColorBrush(Color.FromArgb(80, 128, 128, 128)),
                StrokeThickness = theme == _selectedTheme ? 3 : 1
            };
            button.Content = ellipse;

            // クリックイベント
            button.Click += OnThemeButtonClick;
            
            // ホバーエフェクト
            button.MouseEnter += (s, e) =>
            {
                if (button.Content is Ellipse el) el.StrokeThickness = 2;
            };
            button.MouseLeave += (s, e) =>
            {
                if (button.Content is Ellipse el && button.Tag is LunoPalette.LunoTheme t)
                    el.StrokeThickness = t == _selectedTheme ? 3 : 1;
            };

            ThemeButtonsPanel.Children.Add(button);
        }
    }

    private void OnThemeButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is LunoPalette.LunoTheme theme)
        {
            _selectedTheme = theme;
            _mainWindow.ApplyLunoThemePublic(theme);
            ApplyWindowTheme(theme);
            
            // ボタンの選択状態を更新
            foreach (Button b in ThemeButtonsPanel.Children)
            {
                if (b.Content is Ellipse el && b.Tag is LunoPalette.LunoTheme t)
                {
                    el.Stroke = t == _selectedTheme 
                        ? new SolidColorBrush(t.Text) 
                        : new SolidColorBrush(Color.FromArgb(80, 128, 128, 128));
                    el.StrokeThickness = t == _selectedTheme ? 3 : 1;
                }
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isInitialized) return;
        UpdateZoomText();
        _mainWindow.SetZoomLevel((int)e.NewValue);
    }

    private void UpdateZoomText()
    {
        ZoomValueText.Text = $"{(int)ZoomSlider.Value}%";
    }

    /// <summary>
    /// ビジュアルツリーから特定の型の子要素を検索
    /// </summary>
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) yield return t;
            foreach (var c in FindVisualChildren<T>(child)) yield return c;
        }
    }
}
