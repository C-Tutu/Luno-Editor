namespace Luno.Core.Persistence;

using System.IO;
using System.Text.Json;

/// <summary>
/// 設定ファイルの読み書きを管理
/// 実行ファイル同階層のsettings.jsonを使用
/// </summary>
public sealed class SettingsManager
{
    private static SettingsManager? _instance;
    private static readonly object _lock = new();

    private readonly string _settingsPath;
    private LunoSettings _settings;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static SettingsManager Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    _instance ??= new SettingsManager();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 現在の設定
    /// </summary>
    public LunoSettings Settings => _settings;

    private SettingsManager()
    {
        // 実行ファイル同階層にsettings.jsonを配置
        var exePath = AppContext.BaseDirectory;
        _settingsPath = Path.Combine(exePath, "settings.json");
        _settings = Load();
    }

    /// <summary>
    /// 設定ファイルを読み込む
    /// ファイルが存在しない場合はデフォルト値を生成
    /// </summary>
    private LunoSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize(json, LunoSettingsContext.Default.LunoSettings);
                return settings ?? new LunoSettings();
            }
        }
        catch
        {
            // 読み込み失敗時はデフォルト値を使用
        }

        return new LunoSettings();
    }

    /// <summary>
    /// 設定を保存する
    /// </summary>
    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, LunoSettingsContext.Default.LunoSettings);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // 保存失敗時は静かに無視（ログは将来実装）
        }
    }

    /// <summary>
    /// 設定を非同期で保存する
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, LunoSettingsContext.Default.LunoSettings);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch
        {
            // 保存失敗時は静かに無視
        }
    }
}
