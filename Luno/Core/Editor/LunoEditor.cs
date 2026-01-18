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

        // スペースまたはエンターでMarkdown適用をチェック
        if (e.Key == Key.Space || e.Key == Key.Enter)
        {
            if (TryApplyMarkdownFormat(e.Key))
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
    /// 現在行のMarkdown記法を検出し、書式を適用
    /// 記号を削除し、書式のみを残す
    /// </summary>
    private bool TryApplyMarkdownFormat(Key triggerKey)
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

            // 引用継続
            if (tag == "Quote")
            {
                e.Handled = true;
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

    /// <summary>
    /// URLクリック時のブラウザ起動
    /// </summary>
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        try
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var position = e.GetPosition(this);
                var textPointer = GetPositionFromPoint(position, true);

                if (textPointer != null)
                {
                    var start = textPointer.GetPositionAtOffset(-50) ?? textPointer.DocumentStart;
                    var end = textPointer.GetPositionAtOffset(50) ?? textPointer.DocumentEnd;
                    var text = new TextRange(start, end).Text;

                    var urlMatch = Regex.Match(text, @"https?://[^\s<>""]+");
                    if (urlMatch.Success)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = urlMatch.Value,
                            UseShellExecute = true
                        });
                    }
                }
            }
        }
        catch
        {
            // エラーは無視
        }
    }

    #endregion

    /// <summary>
    /// プレーンテキストを取得
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
                    var text = new TextRange(para.ContentStart, para.ContentEnd).Text;

                    // 保存時はMarkdown記号を復元
                    if (tag?.StartsWith("H") == true && int.TryParse(tag[1..], out var level))
                    {
                        result.AppendLine(new string('#', level) + " " + text);
                    }
                    else if (tag == "List")
                    {
                        // •を-に戻す
                        result.AppendLine(text.Replace("• ", "- "));
                    }
                    else if (tag == "Quote")
                    {
                        result.AppendLine("> " + text);
                    }
                    else
                    {
                        result.AppendLine(text);
                    }
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
}
