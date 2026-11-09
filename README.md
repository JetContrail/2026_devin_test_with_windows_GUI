# DrawingApp

下絵画像の上に描画できる Windows 向けお絵描きアプリケーション (C# / .NET 10 / WPF)。

> 開発中 (Phase8: ペン・点・消しゴム・レイヤー管理・下絵読込・PNG 保存・Undo/Redo・ズーム/パン・オートセーブまで実装済み)。進捗は [Roadmap](docs/Roadmap.md) と [Milestones](https://github.com/JetContrail/2026_devin_test_with_windows_GUI/milestones) を参照。

## 概要

- 下絵 (PNG / JPG / JPEG) を背景レイヤーとして読込み、その上の描画レイヤーに描く
- ツール: ペン (1/3/5/10/20px)、点 (1/3/5/10/20px)、消しゴム (10/20/50px、描画レイヤーのみ消去)
- 保存: 描画のみ (透過 PNG) / 下絵との合成 (PNG)
- Undo (Ctrl+Z) / Redo (Ctrl+Y)、ズーム (10%〜800%)、パン (中ボタン / Space+ドラッグ)
- オートセーブ: 60 秒ごと、`%LOCALAPPDATA%\DrawingApp\AutoSave`、10 世代保持

## 開発環境

| 項目 | バージョン |
|----|----|
| OS | Windows 10/11 (実行・UI テスト) |
| SDK | .NET 10 SDK (`global.json` で固定) |
| IDE | Visual Studio 2026 (または 2022 17.14 以降、`.slnx` 対応版) |
| 言語 | C# 14 |
| 主要ライブラリ | CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, xUnit, coverlet |

## ビルド方法

```powershell
dotnet restore DrawingApp.slnx
dotnet build DrawingApp.slnx -c Release
```

Visual Studio の場合は `DrawingApp.slnx` を開いてビルドする。
Compiler Warning はエラー扱い (`TreatWarningsAsErrors`)。

## 実行方法

```powershell
dotnet run --project src/DrawingApp.UI
```

### 配布用 exe (.NET のインストール不要)

```powershell
dotnet publish src/DrawingApp.UI -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none `
  -o publish/DrawingApp
```

`publish/DrawingApp/DrawingApp.exe` (約 140MB, Windows 10/11 x64) を起動する。CI でも毎回同じ exe を作り、Actions の実行結果から Artifact `DrawingApp-win-x64` としてダウンロードできる (保持 14 日)。署名していないため、初回は SmartScreen の警告が出ることがある。

## テスト方法

```powershell
dotnet test DrawingApp.slnx -c Release
```

カバレッジ (HTML レポート):

```powershell
dotnet test DrawingApp.slnx -c Release --collect "XPlat Code Coverage" --results-directory TestResults
dotnet tool restore
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;MarkdownSummaryGithub"
```

Windows 以外の OS では WPF を実行できないため、`DrawingApp.Core` のテストのみが実行される。

## プロジェクト構成

```
src/DrawingApp.Core            ドメイン + アプリケーション層 (OS 非依存)
src/DrawingApp.Infrastructure  画像入出力・ファイル保存 (Windows)
src/DrawingApp.UI              WPF View / ViewModel / DI コンポジションルート
tests/DrawingApp.Tests         Unit Test
docs/                          設計・計画ドキュメント
```

## ドキュメント

- [docs/README.md](docs/README.md) — ドキュメント索引
- [Architecture.md](docs/Architecture.md) — アーキテクチャ設計
- [ProductBacklog.md](docs/ProductBacklog.md) / [Roadmap.md](docs/Roadmap.md) / [Risks.md](docs/Risks.md) — 計画
- [Performance.md](docs/Performance.md) — 起動時間・メモリの計測手順と結果
- [UserManual.md](docs/UserManual.md) — 利用者マニュアル

## CI

GitHub Actions (`.github/workflows/ci.yml`) が Pull Request ごとに `windows-latest` で restore / build / test とカバレッジ集計を実行する。行カバレッジが 80% 未満の場合は失敗する。
