### Project Luno: Development Directive (for Antigravity)

このドキュメントは、軽量テキストエディタ「Luno」の開発指針です。
開発AIは、以下の**「絶対的な制約（Constraints）」**と**「達成すべき体験（Experience Goals）」**を遵守し、具体的なコード実装やクラス設計については、最新のベストプラクティスに基づき自律的に判断してください。

#### 1. Tech Stack & Environment

```yaml
Target Framework: .NET 10 (LTS)
Language: C# 14
UI Framework: WPF (Windows Presentation Foundation)
Build Target: Windows 11 (Primary), Windows 10 (Fallback support required)
Deployment: Native AOT (PublishAot=true)
Architecture: Single Executable (No external DLLs)

```

#### 2. Core Philosophy & Constraints

開発AIへの指示: 以下の制約は厳守すること。

```text
[Performance]
- コールドスタート目標: 200ms以内。
- リフレクションや動的コード生成（Emit）はAOT互換性のため禁止。
- 巨大な外部ライブラリ（MVVMフレームワーク等）の導入禁止。標準機能または軽量な独自実装のみを使用。

[Compatibility & Resilience]
- OSバージョン依存機能（Mica, DWM等）は必ずバージョンチェックを行い、非対応環境（Win10等）では「単色背景」などにGraceful Degradation（優雅な退行）すること。
- 非管理リソース（GDIオブジェクト等）のリークは厳禁。Disposeパターンを徹底。

[UX/UI]
- 「設定画面」を作らず、JSONファイル編集または直感的なUI操作（D&D等）で完結させる。
- エラー発生時、ユーザーにスタックトレースを見せず、静かにログを残して復帰すること。

```

#### 3. Development Phases (Directives)

開発AIは各フェーズごとに実装を行い、コード品質を自己評価してください。

**Phase 1: The Canvas (Windowing & Rendering)**

> **目的:** ウィンドウシステムの構築と、Lunoのアイデンティティである「紙の質感」の実現。
> **AIへの裁量:** Win32 APIのP/Invoke設計、ウィンドウメッセージ処理（WndProc）のフック方法は任せる。

```markdown
1. ウィンドウ生成:
   - タイトルバーなし（Chromeless）。
   - リサイズ、ドラッグ移動、スナップレイアウト（Win11）が正常に動作すること。
   
2. 視覚効果 (Visuals):
   - Win11: DWMによるMica Alt効果の適用。
   - 全環境: ノイズテクスチャ（Paper Texture）のオーバーレイ描画。
   - ダーク/ライトモードのOS設定検知と、動的なテーマ切り替え実装。

3. 互換性チェック:
   - Windows 10環境での動作確認ロジック（Mica非適用時の代替色 #F9F9F7/#1E1E1E の適用）。

```

**Phase 2: The Ink (Editor Engine)**

> **目的:** 思考を止めない入力体験。
> **AIへの裁量:** `TextBox` 派生か `RichTextBox` 派生か、あるいは低レイヤー描画を使うかは「日本語入力(IME)の安定性」と「軽量さ」のバランスで判断せよ。

```markdown
1. 基本編集機能:
   - アンドゥ/リドゥ、標準的なショートカットキーの実装。
   - フォントレンダリングの最適化（ClearType設定など）。

2. Luno Markdown (LMD):
   - 以下の記法を正規表現等で高速に検知し、スタイル（色・太字）のみを適用する。
     - Header: `# `
     - List: `- `, `* `
     - Quote: `> `
   - *注意:* 構造解析（AST構築）は行わず、あくまで「ハイライト」に留めること。

3. Smart Behaviors:
   - オートインデント実装。
   - URLの自動リンク化とクリック処理。

```

**Phase 3: The Memory (Persistence & Lifecycle)**

> **目的:** 「保存」という概念の消失。
> **AIへの裁量:** JSONシリアライザーの実装詳細（System.Text.JsonのSource Generator利用推奨）や、非同期書き込みのタイミング制御。

```markdown
1. ライフサイクル管理:
   - アプリ終了、サスペンド、非アクティブ化のタイミングをフック。
   
2. データの永続化:
   - 編集中のテキストを自動保存（Auto-Save）。
   - ウィンドウ位置・サイズの記憶と復元。
   
3. 設定管理:
   - 実行ファイル同階層の `settings.json` の読み書き。
   - 起動時にファイルがない場合はデフォルト値を生成。

```

#### 4. Color Palette Constants

AIがUI構築時に参照すべき定数定義です。

```csharp
public static class LunoColors
{
    // Base Colors
    public static readonly Color PaperLight = Color.FromRgb(0xF9, 0xF9, 0xF7);
    public static readonly Color PaperDark  = Color.FromRgb(0x1E, 0x1E, 0x1E);

    // Accents (For Markdown & UI Elements)
    public static readonly Color AccentRed    = Color.FromRgb(0xD3, 0x2F, 0x2F); // Important
    public static readonly Color AccentBlue   = Color.FromRgb(0x19, 0x76, 0xD2); // Links
    public static readonly Color AccentGreen  = Color.FromRgb(0x38, 0x8E, 0x3C); // Done/Safe
    public static readonly Color AccentYellow = Color.FromRgb(0xFB, 0xC0, 0x2D); // Highlight
    
    // Add other colors from spec as needed...
}

```