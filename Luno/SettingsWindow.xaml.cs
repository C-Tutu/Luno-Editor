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
    /// 12色の円形ボタンを作成（二重枠で視認性向上）
    /// </summary>
    private void CreateThemeButtons()
    {
        ThemeButtonsPanel.Children.Clear();
        
        foreach (var theme in LunoPalette.Themes)
        {
            var button = new Button
            {
                Width = 44,
                Height = 44,
                Margin = new Thickness(4),
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = $"{theme.Name} ({theme.JapaneseName})",
                Tag = theme
            };

            // 二重枠のコンテンツ (Grid)
            var grid = new Grid();
            
            // 外側の枠（白）
            var outerRing = new Ellipse
            {
                Width = 36,
                Height = 36,
                Fill = Brushes.Transparent,
                Stroke = Brushes.White,
                StrokeThickness = theme == _selectedTheme ? 3 : 1
            };
            
            // 内側の枠（黒）
            var innerRing = new Ellipse
            {
                Width = 32,
                Height = 32,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
                StrokeThickness = 1
            };
            
            // 円形のテーマカラー
            var colorCircle = new Ellipse
            {
                Width = 28,
                Height = 28,
                Fill = new SolidColorBrush(theme.Background)
            };
            
            grid.Children.Add(outerRing);
            grid.Children.Add(innerRing);
            grid.Children.Add(colorCircle);
            button.Content = grid;

            // クリックイベント
            button.Click += OnThemeButtonClick;
            
            // ホバーエフェクト
            button.MouseEnter += (s, e) =>
            {
                outerRing.StrokeThickness = 2;
            };
            button.MouseLeave += (s, e) =>
            {
                if (button.Tag is LunoPalette.LunoTheme t)
                    outerRing.StrokeThickness = t == _selectedTheme ? 3 : 1;
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
                if (b.Content is Grid g && g.Children.Count > 0 && g.Children[0] is Ellipse outerRing && b.Tag is LunoPalette.LunoTheme t)
                {
                    outerRing.StrokeThickness = t == _selectedTheme ? 3 : 1;
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
