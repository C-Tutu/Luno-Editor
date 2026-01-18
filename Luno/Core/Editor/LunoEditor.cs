namespace Luno.Core.Editor;

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Luno.Core.Theming;

/// <summary>
/// Luno Markdown (LMD) 対応のエディタコントロール
/// 軽量なハイライト処理に特化（AST構築なし）
/// </summary>
public partial class LunoEditor : RichTextBox
{
    // Markdownパターン（正規表現）
    private static readonly Regex HeaderPattern = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex ListPattern = new(@"^(\s*)([-*+]|\d+\.)\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex QuotePattern = new(@"^>\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex UrlPattern = new(@"https?://[^\s<>""]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*|__(.+?)__", RegexOptions.Compiled);
    private static readonly Regex ItalicPattern = new(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)|(?<!_)_(?!_)(.+?)(?<!_)_(?!_)", RegexOptions.Compiled);

    private bool _isUpdating;
    private readonly DispatcherTimer _highlightTimer;

    public LunoEditor()
    {
        // 基本設定
        AcceptsReturn = true;
        AcceptsTab = true;
        IsDocumentEnabled = true;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(16);
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        // フォント設定
        FontFamily = new FontFamily("Consolas, Yu Gothic UI, Meiryo");
        FontSize = 14;

        // ハイライト用タイマー（入力後遅延実行）
        _highlightTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _highlightTimer.Tick += OnHighlightTimerTick;

        // イベント登録
        TextChanged += OnTextChanged;
        PreviewKeyDown += OnPreviewKeyDown;

        // 初期ドキュメント設定
        Document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            LineHeight = 1.5
        };
    }

    /// <summary>
    /// テーマに応じた色を設定
    /// </summary>
    public void ApplyTheme(bool isDark)
    {
        Background = Brushes.Transparent;
        Foreground = new SolidColorBrush(LunoColors.GetTextColor(isDark));
        CaretBrush = Foreground;
    }

    #region Text Changed & Highlighting

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;

        // タイマーリセット（連続入力時は遅延）
        _highlightTimer.Stop();
        _highlightTimer.Start();
    }

    private void OnHighlightTimerTick(object? sender, EventArgs e)
    {
        _highlightTimer.Stop();
        ApplyHighlighting();
    }

    /// <summary>
    /// Markdown構文のハイライト適用
    /// パフォーマンスのため、可視範囲のみ処理
    /// </summary>
    private void ApplyHighlighting()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            var text = new TextRange(Document.ContentStart, Document.ContentEnd).Text;
            // ハイライト処理は将来的に実装
            // 現時点では安定性優先でスキップ
        }
        finally
        {
            _isUpdating = false;
        }
    }

    #endregion

    #region Smart Behaviors

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            HandleEnterKey(e);
        }
        else if (e.Key == Key.Tab)
        {
            HandleTabKey(e);
        }
    }

    /// <summary>
    /// Enterキー処理：オートインデントとリスト継続
    /// </summary>
    private void HandleEnterKey(KeyEventArgs e)
    {
        var caretPosition = CaretPosition;
        var lineStart = caretPosition.GetLineStartPosition(0);
        
        if (lineStart == null) return;

        var lineText = new TextRange(lineStart, caretPosition).Text;

        // リスト継続パターンの検出
        var listMatch = ListPattern.Match(lineText);
        if (listMatch.Success)
        {
            var indent = listMatch.Groups[1].Value;
            var marker = listMatch.Groups[2].Value;
            var content = listMatch.Groups[3].Value;

            // 空のリスト項目なら終了
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            // 次のリスト項目を自動挿入
            e.Handled = true;
            
            string nextMarker = marker;
            if (int.TryParse(marker.TrimEnd('.'), out int num))
            {
                nextMarker = $"{num + 1}.";
            }

            var newLine = $"\n{indent}{nextMarker} ";
            caretPosition.InsertTextInRun(newLine);
            CaretPosition = caretPosition.GetPositionAtOffset(newLine.Length);
        }
        else
        {
            // 通常のインデント継続
            var leadingWhitespace = GetLeadingWhitespace(lineText);
            if (!string.IsNullOrEmpty(leadingWhitespace))
            {
                e.Handled = true;
                var newLine = $"\n{leadingWhitespace}";
                caretPosition.InsertTextInRun(newLine);
                CaretPosition = caretPosition.GetPositionAtOffset(newLine.Length);
            }
        }
    }

    /// <summary>
    /// Tabキー処理：インデント挿入
    /// </summary>
    private void HandleTabKey(KeyEventArgs e)
    {
        e.Handled = true;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            // Shift+Tab: アンインデント（将来実装）
        }
        else
        {
            // Tab: スペース挿入（2スペース）
            CaretPosition.InsertTextInRun("  ");
            CaretPosition = CaretPosition.GetPositionAtOffset(2);
        }
    }

    /// <summary>
    /// 行頭の空白を取得
    /// </summary>
    private static string GetLeadingWhitespace(string text)
    {
        int i = 0;
        while (i < text.Length && char.IsWhiteSpace(text[i]))
        {
            i++;
        }
        return text[..i];
    }

    #endregion

    #region URL Handling

    /// <summary>
    /// URLクリック時のブラウザ起動
    /// </summary>
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            var position = e.GetPosition(this);
            var textPointer = GetPositionFromPoint(position, true);

            if (textPointer != null)
            {
                var wordRange = GetWordRange(textPointer);
                var word = wordRange?.Text ?? "";

                if (UrlPattern.IsMatch(word))
                {
                    OpenUrl(word);
                }
            }
        }
    }

    /// <summary>
    /// 単語範囲を取得
    /// </summary>
    private static TextRange? GetWordRange(TextPointer position)
    {
        var start = position.GetPositionAtOffset(-50) ?? position.DocumentStart;
        var end = position.GetPositionAtOffset(50) ?? position.DocumentEnd;
        return new TextRange(start, end);
    }

    /// <summary>
    /// URLを既定のブラウザで開く
    /// </summary>
    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // URL起動失敗時は静かに無視
        }
    }

    #endregion

    /// <summary>
    /// プレーンテキストを取得
    /// </summary>
    public string GetPlainText()
    {
        return new TextRange(Document.ContentStart, Document.ContentEnd).Text;
    }

    /// <summary>
    /// プレーンテキストを設定
    /// </summary>
    public void SetPlainText(string text)
    {
        Document.Blocks.Clear();
        Document.Blocks.Add(new Paragraph(new Run(text)));
    }
}
