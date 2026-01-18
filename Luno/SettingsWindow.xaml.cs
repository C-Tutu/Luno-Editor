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
        var savedZoom = Core.Persistence.SettingsManager.Instance.Settings.ZoomLevel;
        ZoomSlider.Value = savedZoom >= 10 && savedZoom <= 500 ? savedZoom : 100;
        UpdateZoomText();
        
        _isInitialized = true;
    }

    /// <summary>
    /// ウィンドウ自体にテーマを適用
    /// </summary>
    private void ApplyWindowTheme(LunoPalette.LunoTheme theme)
    {
        var bgBrush = new SolidColorBrush(theme.Background);
        var textBrush = new SolidColorBrush(theme.Text);
        
        RootBorder.Background = bgBrush;
        TitleBarBorder.Background = bgBrush;
        
        TitleText.Foreground = textBrush;
        ThemeLabel.Foreground = textBrush;
        ZoomLabel.Foreground = textBrush;
        ZoomValueText.Foreground = textBrush;
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
                Width = 40,
                Height = 40,
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
                Width = 32,
                Height = 32,
                Fill = new SolidColorBrush(theme.Background),
                Stroke = theme == _selectedTheme 
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromArgb(100, 128, 128, 128)),
                StrokeThickness = theme == _selectedTheme ? 3 : 1
            };
            button.Content = ellipse;

            // クリックイベント
            button.Click += OnThemeButtonClick;
            
            // ホバーエフェクト
            button.MouseEnter += (s, e) =>
            {
                if (button.Content is Ellipse el) 
                {
                    el.StrokeThickness = 2;
                    el.Stroke = Brushes.White;
                }
            };
            button.MouseLeave += (s, e) =>
            {
                if (button.Content is Ellipse el && button.Tag is LunoPalette.LunoTheme t)
                {
                    el.Stroke = t == _selectedTheme 
                        ? Brushes.White
                        : new SolidColorBrush(Color.FromArgb(100, 128, 128, 128));
                    el.StrokeThickness = t == _selectedTheme ? 3 : 1;
                }
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
                        ? Brushes.White
                        : new SolidColorBrush(Color.FromArgb(100, 128, 128, 128));
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
}
