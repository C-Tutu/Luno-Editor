namespace Luno;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Luno.Core.Theming;

/// <summary>
/// 設定ウィンドウ
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly MainWindow _mainWindow;
    private bool _isInitialized;

    public class ThemeDisplayItem
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public SolidColorBrush BackgroundBrush { get; set; } = Brushes.Transparent;
        public LunoPalette.LunoTheme Theme { get; set; }
    }

    public SettingsWindow(MainWindow owner)
    {
        InitializeComponent();
        Owner = owner;
        _mainWindow = owner;
        
        InitializeThemes();
        InitializeControls();
        
        _isInitialized = true;
    }

    private void InitializeThemes()
    {
        var items = new List<ThemeDisplayItem>();
        foreach (var theme in LunoPalette.Themes)
        {
            items.Add(new ThemeDisplayItem
            {
                Name = theme.Name,
                DisplayName = $"{theme.Name} ({theme.JapaneseName})",
                BackgroundBrush = new SolidColorBrush(theme.Background),
                Theme = theme
            });
        }
        ThemeComboBox.ItemsSource = items;

        // 現在のテーマを選択
        var currentThemeName = Core.Persistence.SettingsManager.Instance.Settings.ThemeName;
        if (string.IsNullOrEmpty(currentThemeName))
        {
            // デフォルト
            ThemeComboBox.SelectedIndex = 0;
        }
        else
        {
            var selected = items.FirstOrDefault(x => x.Name == currentThemeName);
            if (selected != null) ThemeComboBox.SelectedItem = selected;
        }
    }

    private void InitializeControls()
    {
        // ズーム
        ZoomSlider.Value = Core.Persistence.SettingsManager.Instance.Settings.ZoomLevel;
        UpdateZoomText();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        
        if (ThemeComboBox.SelectedItem is ThemeDisplayItem item)
        {
            _mainWindow.ApplyLunoThemePublic(item.Theme);
        }
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
