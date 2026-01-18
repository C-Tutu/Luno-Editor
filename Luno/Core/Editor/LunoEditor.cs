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
/// 装飾は表示時のみ、保存はプレーンテキスト
/// </summary>
public partial class LunoEditor : RichTextBox
{
    // Markdownパターン（正規表現）
    private static readonly Regex HeaderPattern = new(@"^(#{1,6})\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex ListPattern = new(@"^(\s*)([-*+]|\d+\.)\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex QuotePattern = new(@"^>\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex UrlPattern = new(@"https?://[^\s<>""]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicPattern = new(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", RegexOptions.Compiled);

    private bool _isUpdating;
    private bool _isDarkMode;
    private readonly DispatcherTimer _highlightTimer;
    private const double BaseFontSize = 14.0;

    // テーマカラーブラシ（キャッシュ）
    private SolidColorBrush _defaultBrush = new(LunoColors.TextLight);
    private static readonly SolidColorBrush ListMarkerBrush = new(LunoColors.AccentGreen);
    private static readonly SolidColorBrush QuoteBrush = new(LunoColors.TextMuted);
    private static readonly SolidColorBrush UrlBrush = new(LunoColors.AccentBlue);

    // ヘッダーサイズ倍率（H1=2.0倍、H2=1.75倍 ... H6=1.1倍）
    private static readonly double[] HeaderSizeMultipliers = { 2.0, 1.75, 1.5, 1.35, 1.2, 1.1 };

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
        FontSize = BaseFontSize;

        // ハイライト用タイマー（入力後遅延実行）
        _highlightTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
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

        // ブラシをFreeze（パフォーマンス向上）
        ListMarkerBrush.Freeze();
        QuoteBrush.Freeze();
        UrlBrush.Freeze();
    }

    /// <summary>
    /// テーマに応じた色を設定
    /// </summary>
    public void ApplyTheme(bool isDark)
    {
        _isDarkMode = isDark;
        Background = Brushes.Transparent;
        _defaultBrush = new SolidColorBrush(LunoColors.GetTextColor(isDark));
        _defaultBrush.Freeze();
        Foreground = _defaultBrush;
        CaretBrush = Foreground;

        // テーマ変更時は全体を再ハイライト
        HighlightAllParagraphs();
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
        HighlightCurrentParagraph();
    }

    /// <summary>
    /// カーソル位置の段落のみハイライト（高速）
    /// </summary>
    private void HighlightCurrentParagraph()
    {
        if (_isUpdating) return;

        try
        {
            var para = CaretPosition?.Paragraph;
            if (para != null)
            {
                HighlightParagraph(para);
            }
        }
        catch
        {
            // ハイライト失敗は静かに無視
        }
    }

    /// <summary>
    /// 全段落をハイライト（起動時・テーマ変更時）
    /// </summary>
    private void HighlightAllParagraphs()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            foreach (var block in Document.Blocks.ToList())
            {
                if (block is Paragraph para)
                {
                    HighlightParagraphInternal(para);
                }
            }
        }
        catch
        {
            // ハイライト失敗は静かに無視
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// 指定段落にハイライトを適用
    /// </summary>
    private void HighlightParagraph(Paragraph para)
    {
        _isUpdating = true;
        try
        {
            HighlightParagraphInternal(para);
        }
        catch
        {
            // ハイライト失敗は静かに無視
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// 段落ハイライトの内部実装
    /// </summary>
    private void HighlightParagraphInternal(Paragraph para)
    {
        // 段落のテキストを取得
        var range = new TextRange(para.ContentStart, para.ContentEnd);
        var text = range.Text;

        if (string.IsNullOrEmpty(text)) return;

        // まずデフォルトスタイルにリセット
        range.ApplyPropertyValue(TextElement.ForegroundProperty, _defaultBrush);
        range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
        range.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
        range.ApplyPropertyValue(TextElement.FontSizeProperty, BaseFontSize);
        range.ApplyPropertyValue(Inline.TextDecorationsProperty, null);

        // ヘッダーパターン（フォントサイズ変更）
        var headerMatch = HeaderPattern.Match(text);
        if (headerMatch.Success)
        {
            var level = headerMatch.Groups[1].Length; // #の数 (1-6)
            var sizeMultiplier = HeaderSizeMultipliers[Math.Min(level - 1, 5)];
            var headerSize = BaseFontSize * sizeMultiplier;

            range.ApplyPropertyValue(TextElement.FontSizeProperty, headerSize);
            range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            return; // ヘッダー行は他のパターンを無視
        }

        // 引用パターン
        var quoteMatch = QuotePattern.Match(text);
        if (quoteMatch.Success)
        {
            range.ApplyPropertyValue(TextElement.ForegroundProperty, QuoteBrush);
            range.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
            return;
        }

        // リストパターン（マーカー部分のみ色変更）
        var listMatch = ListPattern.Match(text);
        if (listMatch.Success && listMatch.Groups[2].Length > 0)
        {
            var markerStart = listMatch.Groups[1].Length;
            var markerLength = listMatch.Groups[2].Length;
            ApplyStyleSafe(para, markerStart, markerLength + 1, ListMarkerBrush);
        }

        // URLパターン
        foreach (Match urlMatch in UrlPattern.Matches(text))
        {
            ApplyStyleSafe(para, urlMatch.Index, urlMatch.Length, UrlBrush, textDecorations: TextDecorations.Underline);
        }

        // 太字パターン
        foreach (Match boldMatch in BoldPattern.Matches(text))
        {
            ApplyStyleSafe(para, boldMatch.Index, boldMatch.Length, fontWeight: FontWeights.Bold);
        }

        // 斜体パターン
        foreach (Match italicMatch in ItalicPattern.Matches(text))
        {
            ApplyStyleSafe(para, italicMatch.Index, italicMatch.Length, fontStyle: FontStyles.Italic);
        }
    }

    /// <summary>
    /// 安全にスタイルを適用（例外は無視）
    /// </summary>
    private void ApplyStyleSafe(
        Paragraph para,
        int startIndex,
        int length,
        SolidColorBrush? foreground = null,
        FontWeight? fontWeight = null,
        TextDecorationCollection? textDecorations = null,
        FontStyle? fontStyle = null)
    {
        try
        {
            if (length <= 0) return;

            var start = GetTextPointerAtOffset(para.ContentStart, startIndex);
            var end = GetTextPointerAtOffset(para.ContentStart, startIndex + length);

            if (start == null || end == null) return;
            if (start.CompareTo(end) >= 0) return;

            var range = new TextRange(start, end);

            if (foreground != null)
                range.ApplyPropertyValue(TextElement.ForegroundProperty, foreground);
            if (fontWeight.HasValue)
                range.ApplyPropertyValue(TextElement.FontWeightProperty, fontWeight.Value);
            if (textDecorations != null)
                range.ApplyPropertyValue(Inline.TextDecorationsProperty, textDecorations);
            if (fontStyle.HasValue)
                range.ApplyPropertyValue(TextElement.FontStyleProperty, fontStyle.Value);
        }
        catch
        {
            // スタイル適用失敗は静かに無視
        }
    }

    /// <summary>
    /// 文字オフセットからTextPointerを取得
    /// </summary>
    private static TextPointer? GetTextPointerAtOffset(TextPointer start, int offset)
    {
        if (offset <= 0) return start;

        var current = start;
        int count = 0;

        while (current != null)
        {
            var context = current.GetPointerContext(LogicalDirection.Forward);

            if (context == TextPointerContext.Text)
            {
                var runLength = current.GetTextRunLength(LogicalDirection.Forward);
                var remaining = offset - count;

                if (remaining <= runLength)
                {
                    return current.GetPositionAtOffset(remaining);
                }

                count += runLength;
            }

            var next = current.GetNextContextPosition(LogicalDirection.Forward);
            if (next == null) break;
            current = next;
        }

        return current;
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
        var lineStart = caretPosition?.GetLineStartPosition(0);
        
        if (lineStart == null || caretPosition == null) return;

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
            CaretPosition?.InsertTextInRun("  ");
            CaretPosition = CaretPosition?.GetPositionAtOffset(2);
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
    /// プレーンテキストを設定（ハイライト適用）
    /// </summary>
    public void SetPlainText(string text)
    {
        _isUpdating = true;
        try
        {
            Document.Blocks.Clear();
            
            // 行ごとに段落を作成
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var para = new Paragraph(new Run(line.TrimEnd('\r')))
                {
                    Margin = new Thickness(0)
                };
                Document.Blocks.Add(para);
            }
        }
        finally
        {
            _isUpdating = false;
        }

        // 全体をハイライト
        HighlightAllParagraphs();
    }
}
