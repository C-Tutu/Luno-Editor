namespace Luno.Core.Editor;

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Luno.Core.Theming;

/// <summary>
/// Luno Markdown (LMD) 対応のエディタコントロール
/// マークダウン記号入力後スペース/エンターで書式適用、記号は非表示
/// Inline styles supported: **Bold**, *Italic*, ~~Strike~~, `Code`, ||Spoiler||
/// </summary>
public partial class LunoEditor : RichTextBox
{
    private bool _isUpdating;
    private const double BaseFontSize = 14.0;

    // テーマカラーブラシ
    private SolidColorBrush _defaultBrush = new(LunoColors.TextLight);
    private SolidColorBrush _mutedBrush = new(LunoColors.TextMuted);

    // ヘッダーサイズ倍率
    private static readonly double[] HeaderSizeMultipliers = { 1.8, 1.5, 1.3, 1.2, 1.1, 1.05 };

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

        // イベント登録
        PreviewKeyDown += OnPreviewKeyDown;
        PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
        MouseMove += OnMouseMove;
        TextChanged += OnTextChanged;

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
        _defaultBrush = new SolidColorBrush(LunoColors.GetTextColor(isDark));
        _defaultBrush.Freeze();
        _mutedBrush = new SolidColorBrush(LunoColors.TextMuted);
        _mutedBrush.Freeze();
        Foreground = _defaultBrush;
        CaretBrush = Foreground;
    }

    #region Markdown Processing

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_isUpdating) return;

        // スペースまたはエンターで行頭Markdown適用をチェック
        if (e.Key == Key.Space || e.Key == Key.Enter)
        {
            if (TryApplyBlockMarkdownFormat())
            {
                if (e.Key == Key.Space)
                {
                    e.Handled = true; // スペースは消費
                }
                return;
            }
        }

        // Enterキーでの特殊処理（リスト継続など）
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
    /// テキスト変更時にインラインMarkdownを検出・適用
    /// </summary>
    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        TryApplyInlineMarkdown();
    }

    /// <summary>
    /// 行頭Markdown記法を検出し、書式を適用（ヘッダー、リスト、引用）
    /// </summary>
    private bool TryApplyBlockMarkdownFormat()
    {
        try
        {
            var caretPos = CaretPosition;
            if (caretPos == null) return false;

            var para = caretPos.Paragraph;
            if (para == null) return false;

            var lineStart = caretPos.GetLineStartPosition(0);
            if (lineStart == null) return false;

            var lineText = new TextRange(lineStart, caretPos).Text;
            if (string.IsNullOrEmpty(lineText)) return false;

            // ヘッダー: # ~ ######
            var headerMatch = Regex.Match(lineText, @"^(#{1,6})$");
            if (headerMatch.Success)
            {
                var level = headerMatch.Groups[1].Length;
                ApplyHeaderFormat(para, lineStart, caretPos, level);
                return true;
            }

            // リスト: -, *, +
            var listMatch = Regex.Match(lineText, @"^(\s*)([-*+])$");
            if (listMatch.Success)
            {
                ApplyListFormat(para, lineStart, caretPos, listMatch.Groups[1].Value);
                return true;
            }

            // 番号付きリスト: 1., 2., etc.
            var numListMatch = Regex.Match(lineText, @"^(\s*)(\d+)\.$");
            if (numListMatch.Success)
            {
                ApplyNumberedListFormat(para, lineStart, caretPos, 
                    listMatch.Groups[1].Value, int.Parse(numListMatch.Groups[2].Value));
                return true;
            }

            // 引用: >
            if (lineText.Trim() == ">")
            {
                ApplyQuoteFormat(para, lineStart, caretPos);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// テキスト変更時にインラインMarkdownを検出・適用
    /// </summary>
    private void TryApplyInlineMarkdown()
    {
        try
        {
            var caretPos = CaretPosition;
            if (caretPos == null) return;

            var para = caretPos.Paragraph;
            if (para == null) return;

            var text = new TextRange(para.ContentStart, para.ContentEnd).Text;
            if (string.IsNullOrEmpty(text)) return;

            TryApplyInlineFormat(para, text);
        }
        catch
        {
            // エラーは無視
        }
    }

    /// <summary>
    /// インライン書式を適用
    /// </summary>
    private bool TryApplyInlineFormat(Paragraph para, string text)
    {
        bool changed = false;

        // Bold: **text**
        changed |= ApplyRegexStyle(para, text, @"\*\*(.+?)\*\*", (run) => {
            run.FontWeight = FontWeights.Bold;
            run.Tag = "**";
        });

        // Italic: *text* or _text_
        changed |= ApplyRegexStyle(para, text, @"(?<!\*)\*([^\*]+?)\*(?!\*)", (run) => {
            run.FontStyle = FontStyles.Italic;
            run.Tag = "*";
        });

        // Strike: ~~text~~
        changed |= ApplyRegexStyle(para, text, @"~~(.+?)~~", (run) => {
            run.TextDecorations = TextDecorations.Strikethrough;
            run.Tag = "~~";
        });

        // Underline: __text__
        changed |= ApplyRegexStyle(para, text, @"__(.+?)__", (run) => {
            run.TextDecorations = TextDecorations.Underline;
            run.Tag = "__";
        });

        // Code: `text` (緑色テキスト)
        changed |= ApplyRegexStyle(para, text, @"`(.+?)`", (run) => {
            run.FontFamily = new FontFamily("Consolas");
            run.Foreground = new SolidColorBrush(Color.FromRgb(0x38, 0x8E, 0x3C)); // 緑色
            run.Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));
            run.Tag = "`";
        });

        // Spoiler: ||text||
        changed |= ApplyRegexStyle(para, text, @"\|\|(.+?)\|\|", (run) => {
            run.Background = Brushes.Black;
            run.Foreground = Brushes.Black; // Hidden text
            run.Tag = "||";
            run.ToolTip = "クリックして表示"; 
        });

        // URL: http(s)://...
        changed |= ApplyRegexStyle(para, text, @"https?://[^\s<>""]+", (run) => {
            run.Foreground = new SolidColorBrush(Luno.Core.Theming.LunoColors.AccentBlue);
            run.TextDecorations = TextDecorations.Underline;
            run.Tag = "URL"; 
            run.Cursor = Cursors.Hand;
        });

        return changed;
    }

    private Run? _lastHoveredUrlRun;

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var point = e.GetPosition(this);
        var pointer = GetPositionFromPoint(point, true);
        
        if (pointer != null && pointer.Parent is Run run && run.Tag as string == "URL")
        {
            if (_lastHoveredUrlRun != run)
            {
                // リセット
                if (_lastHoveredUrlRun != null)
                {
                    _lastHoveredUrlRun.Foreground = new SolidColorBrush(Luno.Core.Theming.LunoColors.AccentBlue);
                }
                
                // ホバー適用 (紫)
                run.Foreground = Brushes.Purple; 
                _lastHoveredUrlRun = run;
            }
        }
        else
        {
            if (_lastHoveredUrlRun != null)
            {
                _lastHoveredUrlRun.Foreground = new SolidColorBrush(Luno.Core.Theming.LunoColors.AccentBlue);
                _lastHoveredUrlRun = null;
            }
        }
    }

    private bool ApplyRegexStyle(Paragraph para, string text, string pattern, Action<Run> styleAction)
    {
        var match = Regex.Match(text, pattern);
        if (match.Success)
        {
            _isUpdating = true;
            try
            {
                var fullMatch = match.Value;
                var content = match.Groups[1].Value;

                // Simple replacement using TextRange index
                var range = new TextRange(para.ContentStart, para.ContentEnd);
                var offset = range.Text.IndexOf(fullMatch);
                
                if (offset >= 0)
                {
                    var pointerStart = range.Start.GetPositionAtOffset(offset);
                    var pointerEnd = range.Start.GetPositionAtOffset(offset + fullMatch.Length);

                    if (pointerStart != null && pointerEnd != null)
                    {
                        var targetRange = new TextRange(pointerStart, pointerEnd);
                        targetRange.Text = ""; 
                        
                        var newRun = new Run(content);
                        styleAction(newRun);
                        
                        // Insert the new run
                        // Note: To insert strictly at TextPointer is tricky with Runs.
                        // We use a safe approximation: if the para was just text, we replace.
                        // But since we cleared text, we can insert at pointerStart? 
                        // No, pointerStart might be "dead" after deletion.
                        
                        // Re-calculate position after deletion?
                        // range.Start is still good.
                        var insertionPos = range.Start.GetPositionAtOffset(offset);
                        if (insertionPos != null)
                        {
                            // We need to insert an Inline. 
                            // Paragraph.Inlines.Insert... requires an existing inline.
                            
                            // Simplest way for RichTextBox: Use TextRange to insert text, 
                            // then apply properties, then set Tag via Property (if possible)
                            // But Tag isn't a DependencyProperty on TextElement exposed via ApplyPropertyValue.
                            
                            // Alternative: Modify the Run directly if we can access it.
                            // Inserting `newRun` via `new Span(newRun, insertionPos)`?
                            
                            new Span(newRun, insertionPos); // Inserts at position
                        }
                        
                        return true; 
                    }
                }
                return false;
            }
            finally
            {
                _isUpdating = false;
            }
        }
        return false;
    }

    /// <summary>
    /// ヘッダー書式を適用（記号削除、フォントサイズ変更）
    /// </summary>
    private void ApplyHeaderFormat(Paragraph para, TextPointer start, TextPointer end, int level)
    {
        _isUpdating = true;
        try
        {
            // #記号を削除
            var range = new TextRange(start, end);
            range.Text = "";

            // 段落全体にヘッダースタイルを適用
            var sizeMultiplier = HeaderSizeMultipliers[Math.Min(level - 1, 5)];
            para.FontSize = BaseFontSize * sizeMultiplier;
            para.FontWeight = FontWeights.Bold;
            para.Tag = $"H{level}"; // マーカーとして保存
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// リスト書式を適用（記号を•に置換）
    /// </summary>
    private void ApplyListFormat(Paragraph para, TextPointer start, TextPointer end, string indent)
    {
        _isUpdating = true;
        try
        {
            var range = new TextRange(start, end);
            range.Text = indent + "• ";
            
            para.Tag = "List";
            para.Margin = new Thickness(16, 0, 0, 0);
            
            CaretPosition = para.ContentEnd;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// 番号付きリスト書式を適用
    /// </summary>
    private void ApplyNumberedListFormat(Paragraph para, TextPointer start, TextPointer end, string indent, int number)
    {
        _isUpdating = true;
        try
        {
            var range = new TextRange(start, end);
            range.Text = $"{indent}{number}. ";
            
            para.Tag = "NumList";
            para.Margin = new Thickness(16, 0, 0, 0);
            
            CaretPosition = para.ContentEnd;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// 引用書式を適用（記号削除、斜体・インデント）
    /// </summary>
    private void ApplyQuoteFormat(Paragraph para, TextPointer start, TextPointer end)
    {
        _isUpdating = true;
        try
        {
            var range = new TextRange(start, end);
            range.Text = "";

            para.FontStyle = FontStyles.Italic;
            para.Foreground = _mutedBrush;
            para.Margin = new Thickness(24, 0, 0, 0);
            para.Tag = "Quote";
        }
        finally
        {
            _isUpdating = false;
        }
    }

    #endregion

    #region Smart Behaviors

    /// <summary>
    /// Enterキー処理：リスト継続
    /// </summary>
    private void HandleEnterKey(KeyEventArgs e)
    {
        try
        {
            var caretPos = CaretPosition;
            var para = caretPos?.Paragraph;
            if (para == null) return;

            var tag = para.Tag as string;

            // リスト継続
            if (tag == "List")
            {
                e.Handled = true;
                var newPara = new Paragraph(new Run("• "))
                {
                    Tag = "List",
                    Margin = new Thickness(16, 0, 0, 0)
                };
                Document.Blocks.InsertAfter(para, newPara);
                CaretPosition = newPara.ContentEnd;
                return;
            }

            // 番号付きリスト継続
            if (tag == "NumList")
            {
                e.Handled = true;
                var text = new TextRange(para.ContentStart, para.ContentEnd).Text;
                var match = Regex.Match(text, @"^(\s*)(\d+)\.");
                var nextNum = match.Success ? int.Parse(match.Groups[2].Value) + 1 : 1;
                
                var newPara = new Paragraph(new Run($"{nextNum}. "))
                {
                    Tag = "NumList",
                    Margin = new Thickness(16, 0, 0, 0)
                };
                Document.Blocks.InsertAfter(para, newPara);
                CaretPosition = newPara.ContentEnd;
                return;
            }

            // ヘッダー後は通常段落に
            if (tag?.StartsWith("H") == true)
            {
                e.Handled = true;
                var newPara = new Paragraph()
                {
                    FontSize = BaseFontSize,
                    FontWeight = FontWeights.Normal
                };
                Document.Blocks.InsertAfter(para, newPara);
                CaretPosition = newPara.ContentStart;
                return;
            }

            // 引用継続（空なら解除）
            if (tag == "Quote")
            {
                e.Handled = true;
                var currentText = new TextRange(para.ContentStart, para.ContentEnd).Text;
                
                // 空行なら引用を解除して通常段落に
                if (string.IsNullOrWhiteSpace(currentText))
                {
                    // 現在の空の引用段落を通常に変更
                    para.FontStyle = FontStyles.Normal;
                    para.Foreground = _defaultBrush;
                    para.Margin = new Thickness(0);
                    para.Tag = null;
                    return;
                }
                
                // 内容がある場合は引用を継続
                var newPara = new Paragraph()
                {
                    FontStyle = FontStyles.Italic,
                    Foreground = _mutedBrush,
                    Margin = new Thickness(24, 0, 0, 0),
                    Tag = "Quote"
                };
                Document.Blocks.InsertAfter(para, newPara);
                CaretPosition = newPara.ContentStart;
                return;
            }
        }
        catch
        {
            // エラーは無視
        }
    }

    /// <summary>
    /// Tabキー処理
    /// </summary>
    private void HandleTabKey(KeyEventArgs e)
    {
        try
        {
            e.Handled = true;
            CaretPosition?.InsertTextInRun("  ");
            CaretPosition = CaretPosition?.GetPositionAtOffset(2);
        }
        catch
        {
            // エラーは無視
        }
    }

    #endregion

    #region URL Handling

    #region URL Handling

    /// <summary>
    /// URLクリック時のブラウザ起動 (Robust)
    /// </summary>
    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            var pos = e.GetPosition(this);
            var pointer = GetPositionFromPoint(pos, true);
            if (pointer != null)
            {
                // カーソル位置の単語を取得
                var start = pointer;
                var end = pointer;
                
                // 前に探索
                while (start.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text)
                {
                    var text = start.GetTextInRun(LogicalDirection.Backward);
                    var spaceIndex = text.LastIndexOfAny(new[] { ' ', '\t', '\n', '>', '<' });
                    if (spaceIndex != -1)
                    {
                        start = start.GetPositionAtOffset(spaceIndex - text.Length); // 近似
                        break; 
                    }
                    start = start.GetPositionAtOffset(-text.Length);
                }
                
                // 後ろに探索
                // (簡易実装: 行全体を取得してRegexで判定)
                var lineStart = pointer.GetLineStartPosition(0) ?? pointer.DocumentStart;
                var lineEnd = pointer.GetLineStartPosition(1) ?? pointer.DocumentEnd;
                var range = new TextRange(lineStart, lineEnd);
                var textFull = range.Text;
                
                // クリック位置の文字インデックス
                var offset = lineStart.GetOffsetToPosition(pointer); 
                // Note: GetOffsetToPosition returns symbol count, not char count. Unreliable.
                
                // Simple approach: Match all URLs in line, check if point is inside geometry
                foreach (Match match in Regex.Matches(textFull, @"https?://[^\s<>""]+"))
                {
                    var urlStart = lineStart.GetPositionAtOffset(textFull.IndexOf(match.Value)); // Very rough
                    if (urlStart != null)
                    {
                        var urlEnd = urlStart.GetPositionAtOffset(match.Value.Length); // Rough
                        
                        // 行内検索してRangeを作る
                        var foundStart = range.Start;
                        while (foundStart != null && foundStart.CompareTo(range.End) < 0)
                        {
                            var runText = foundStart.GetTextInRun(LogicalDirection.Forward);
                            int index = runText.IndexOf(match.Value);
                            if (index >= 0)
                            {
                                var s = foundStart.GetPositionAtOffset(index);
                                var en = s.GetPositionAtOffset(match.Value.Length);
                                
                                // クリック位置がURL範囲内か？
                                // TextPointerの比較は信頼性が高い
                                if (pointer.CompareTo(s) >= 0 && pointer.CompareTo(en) <= 0)
                                {
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = match.Value,
                                        UseShellExecute = true
                                    });
                                    e.Handled = true;
                                    return;
                                }
                            }
                            foundStart = foundStart.GetNextContextPosition(LogicalDirection.Forward);
                        }
                    }
                }
            }
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        // Deprecated simple logic removed
    }

    #endregion

    /// <summary>
    /// プレーンテキストを取得 (Markdown記号復元)
    /// </summary>
    public string GetPlainText()
    {
        try
        {
            var result = new System.Text.StringBuilder();
            foreach (var block in Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    var tag = para.Tag as string;
                    
                    // 行頭マーカー
                    string prefix = "";
                    if (tag?.StartsWith("H") == true && int.TryParse(tag[1..], out var level))
                    {
                        prefix = new string('#', level) + " ";
                    }
                    else if (tag == "List")
                    {
                        prefix = "- ";
                    }
                    else if (tag == "NumList")
                    {
                        // 番号は再計算せず簡易的に1.とするか、パラグラフから取得する
                        // ここでは正規表現で取得したテキストに既に番号が含まれているか確認が必要だが
                        // Inlines再構築では prefix は自分で不可する必要がある
                        // リストの実装: Inlinesの最初は "1. " というRunになっているはず
                        // prefixなしでInlinesから取得する
                        prefix = ""; // Inlines will contain the number
                    }
                    else if (tag == "Quote")
                    {
                        prefix = "> ";
                    }

                    result.Append(prefix);

                    // Inline要素を走査してMarkdownを復元
                    foreach (var inline in para.Inlines)
                    {
                        if (inline is Run run)
                        {
                            var rText = run.Text;
                            if (tag == "List" && rText.StartsWith("• ")) rText = rText.Substring(2); // Remove bullet

                            var iTag = run.Tag as string;
                            if (!string.IsNullOrEmpty(iTag))
                            {
                                if (iTag == "**") result.Append($"**{rText}**");
                                else if (iTag == "*") result.Append($"*{rText}*");
                                else if (iTag == "~~") result.Append($"~~{rText}~~");
                                else if (iTag == "__") result.Append($"__{rText}__");
                                else if (iTag == "`") result.Append($"`{rText}`");
                                else if (iTag == "||") result.Append($"||{rText}||");
                                else result.Append(rText);
                            }
                            else
                            {
                                result.Append(rText);
                            }
                        }
                        else if (inline is Span span) // Spanもチェック
                        {
                            // 簡易再帰
                            foreach (var si in span.Inlines)
                            {
                                if (si is Run sr) result.Append(sr.Text);
                            }
                        }
                    }
                    result.AppendLine();
                }
            }
            return result.ToString().TrimEnd();
        }
        catch
        {
            return new TextRange(Document.ContentStart, Document.ContentEnd).Text;
        }
    }

    /// <summary>
    /// プレーンテキストを設定
    /// </summary>
    public void SetPlainText(string text)
    {
        _isUpdating = true;
        try
        {
            Document.Blocks.Clear();
            
            if (string.IsNullOrEmpty(text))
            {
                Document.Blocks.Add(new Paragraph() { Margin = new Thickness(0) });
                return;
            }

            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.TrimEnd('\r');
                var para = CreateParagraphFromMarkdown(trimmedLine);
                Document.Blocks.Add(para);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// Markdown行から書式付き段落を作成
    /// </summary>
    private Paragraph CreateParagraphFromMarkdown(string line)
    {
        var para = new Paragraph { Margin = new Thickness(0) };

        // ヘッダー
        var headerMatch = Regex.Match(line, @"^(#{1,6})\s+(.*)$");
        if (headerMatch.Success)
        {
            var level = headerMatch.Groups[1].Length;
            para.Inlines.Add(new Run(headerMatch.Groups[2].Value));
            para.FontSize = BaseFontSize * HeaderSizeMultipliers[Math.Min(level - 1, 5)];
            para.FontWeight = FontWeights.Bold;
            para.Tag = $"H{level}";
            return para;
        }

        // リスト
        var listMatch = Regex.Match(line, @"^(\s*)[-*+]\s(.*)$");
        if (listMatch.Success)
        {
            para.Inlines.Add(new Run("• " + listMatch.Groups[2].Value));
            para.Margin = new Thickness(16, 0, 0, 0);
            para.Tag = "List";
            return para;
        }

        // 番号付きリスト
        var numListMatch = Regex.Match(line, @"^(\s*)(\d+)\.\s(.*)$");
        if (numListMatch.Success)
        {
            para.Inlines.Add(new Run($"{numListMatch.Groups[2].Value}. {numListMatch.Groups[3].Value}"));
            para.Margin = new Thickness(16, 0, 0, 0);
            para.Tag = "NumList";
            return para;
        }

        // 引用
        var quoteMatch = Regex.Match(line, @"^>\s?(.*)$");
        if (quoteMatch.Success)
        {
            para.Inlines.Add(new Run(quoteMatch.Groups[1].Value));
            para.FontStyle = FontStyles.Italic;
            para.Foreground = _mutedBrush;
            para.Margin = new Thickness(24, 0, 0, 0);
            para.Tag = "Quote";
            return para;
        }

        // 通常テキスト
        para.Inlines.Add(new Run(line));
        return para;
    }
    #endregion
}
