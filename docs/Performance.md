# 性能計測

非機能要求「起動 3 秒以内」「通常利用時メモリ 300MB 以下」の計測手順と結果。

## 計測方法

`scripts/Measure-Performance.ps1` が配布用 exe (`dotnet publish` の単一 exe) を起動・終了する処理を 5 回繰り返し、中央値を判定する。CI (`windows-latest`) では publish の後に毎回実行し、基準を超えたら失敗する。

| 項目 | 定義 |
|----|----|
| 起動時間 | プロセス起動 → メインウィンドウが作られ、入力待ち (`WaitForInputIdle`) になるまで |
| メモリ | 起動 2 秒後のワーキングセット (`WorkingSet64`)。参考としてプライベートメモリも記録 |
| 判定 | 中央値が 3 秒 / 300MB を超えたら exit 1 |

ローカル (Windows) で計測する場合:

```powershell
dotnet publish src/DrawingApp.UI -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none `
  -o publish/DrawingApp
./scripts/Measure-Performance.ps1            # -Runs / -MaxStartupSeconds / -MaxMemoryMB で変更可
```

## 結果 (GitHub Actions `windows-latest`, 2026-11-09)

| Run | 起動 (秒) | ワーキングセット (MB) | プライベート (MB) |
|---|---|---|---|
| 1 | 1.16 | 134.5 | 68.2 |
| 2 | 0.85 | 133.0 | 67.8 |
| 3 | 0.81 | 133.1 | 67.2 |
| 4 | 0.69 | 132.8 | 66.0 |
| 5 | 0.71 | 132.7 | 66.8 |
| **中央値** | **0.81** | **133.0** | |

1 回目だけ遅いのは、単一 exe に同梱したネイティブライブラリを初回に展開するため。

## 判断

- **起動:** 中央値 0.81 秒で基準 (3 秒) の 1/3 以下。起動時の遅延初期化や ReadyToRun は現時点では不要と判断した
- **メモリ:** 起動直後は 133MB。描画中に増える分は、キャンバス 1920x1080 の場合の見積もりで以下のとおり

| 用途 | 見積もり |
|----|----|
| 背景・描画レイヤー・Undo 用の描画レイヤーのコピー (各 W×H×4 byte) | 約 24MB |
| WPF 表示用ビットマップ (背景・描画の 2 枚) | 約 16MB |
| Undo 履歴 (圧縮パッチ, 上限) | 最大 64MB |
| **合計 (起動直後の 133MB を含む)** | **約 237MB** |

- **仮定:** 「通常利用時」はキャンバス 1920x1080 程度までとする。下絵の上限 4096x4096 では各バッファが 64MB になり、Undo 履歴を上限まで使うと 300MB を超えうる (`docs/Risks.md` のメモリリスク)
- 実際に描画・下絵読込をした状態のメモリは、UI 自動操作が必要なため CI では計測していない。100 回繰り返しで保持メモリが増え続けないことは耐久テスト (`EnduranceScenario`) で確認している
