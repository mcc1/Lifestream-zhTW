<h1>Lifestream zh-TW</h1>

這個 repo 是 Lifestream 的繁體中文 consumer repo。

它保留 Lifestream 專屬內容：

- `zh-TW.json`
- Lifestream wrapper workflow
- Lifestream 版本 pin
- Lifestream package 規則
- 已處理的 Lifestream 原始碼快照

共用翻譯與建置框架已移到：

- `mcc1/dalamud-mod-localizer`

<h2>Workflow</h2>

此 repo 的 GitHub Actions wrapper 會呼叫外部 reusable workflow：

- [main.yml](.github/workflows/main.yml)

<h2>Notes</h2>

- 目前先以 API 12 對應的 `2.5.1.13` 作為初始基線。
- 若後續要升級，應先重新確認 `DalamudApiLevel`、目標 framework 與打包內容。
- 本地驗證與 build 注意事項請先看：
  - [docs/LOCAL_BUILD_NOTES.md](docs/LOCAL_BUILD_NOTES.md)
