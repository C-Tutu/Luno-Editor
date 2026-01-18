# Luno Editor

Luno（ルノ）は、思考を妨げない「紙のような書き心地」を目指した、Windows用軽量テキストエディタです。
余計な装飾を削ぎ落としたChromelessデザインと、環境に溶け込む美しいテクスチャが特徴です。

## 特徴 (Features)

- 📝 **The Canvas:** 没入感を高めるChromelessウィンドウと、紙の質感（Noise Texture）。
- ✨ **Mica Alt Effect:** Windows 11のシステム背景透過（Mica Alt）に対応。OSのテーマに合わせて美しく変化します。
- 🌗 **Light / Dark Mode:** OSの輝度設定を自動検知し、最適な配力に切り替わります。
- ⚡ **Lightweight:** .NET 10 + Native AOT (SingleFile) 技術により、軽量かつ高速に動作します。
- 💾 **The Memory:** 「保存」操作は不要。入力したテキストは即座に自動保存され、ウィンドウ位置も記憶されます。
- 🖋 **Luno Markdown (LMD):** 構造を意識させない、直感的なMarkdownハイライト（`- ` リストや `> ` 引用など）。

## インストール (Installation)

1. [Releases](https://github.com/C-Tutu/Luno-Editor/releases) から最新の `Luno-alpha-v*.zip` をダウンロードします。
2. ZIPを解凍し、中にある `Luno.exe` を実行してください。
    - ※ インストーラーはありません。ポータブルに動作します。

## 動作環境 (Requirements)

- **Windows 11 (推奨):** Mica効果が有効になり、最高の体験が得られます。
- **Windows 10:** 動作可能（Mica効果の代わりに単色背景へのフォールバック機能搭載）。
- .NET ランタイムは不要です（Self-Contained）。

## 開発者向け (For Developers)

このプロジェクトは .NET 10 WPF で構築されています。
開発指針やアーキテクチャについては [docs/DEVELOPMENT_DIRECTIVES.md](docs/DEVELOPMENT_DIRECTIVES.md) を参照してください。

### Tech Stack

- .NET 10 (Preview)
- WPF (Windows Presentation Foundation)
- Native AOT / SingleFile Deployment

## License

MIT License
