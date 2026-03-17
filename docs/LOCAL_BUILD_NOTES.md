# Lifestream zh-TW Local Build Notes

這份文件是給本地協作者或其他 agent 參考，目的是避免重複踩同一個坑。

重點先講：

- 不要直接在 consumer repo root 對目前 snapshot 執行 `dotnet build` 當成第一步。
- `Lifestream-zhTW` 的正確本地驗證順序是：
  1. 先重現 shared workflow 的 source state
  2. 先確認 consumer patches 能套用
  3. 最後才嘗試本地編譯
- 目前 GitHub Actions 仍是最可靠的最終 build 基準。

## 1. 目前已知狀態

- 本機已安裝 `.NET SDK 9`
- `Lifestream-zhTW` 的 GitHub Actions workflow 目前是健康的
- 本地 `sync` 模式的 consumer patch 驗證可成功
- 本地 `build` 模式的 consumer patch 驗證目前仍失敗
- 就算先在本地重現 `sync` 後的 source state，再直接 `dotnet build`，目前仍可能因本機 Dalamud 參考環境不完整而失敗

## 2. 為什麼不能直接 `dotnet build`

`Lifestream-zhTW` 不是單純 clone 下來就能直接編的 repo。

GitHub Actions 會先做這些事：

1. 抓 upstream source
2. 跑 `dalamud-mod-localizer`
3. 套用 `.consumer-patches/`
4. 設定 `DALAMUD_HOME`
5. 最後才 build

如果直接對目前 repo snapshot 跑：

```powershell
dotnet build Lifestream\Lifestream\Lifestream.csproj -c Release
```

你測到的是「繞過 shared workflow 前置步驟的裸 source」，不是實際 CI 會 build 的狀態。

## 3. 正確的本地驗證入口

請從 `dalamud-mod-localizer` 開始，不要直接在 `Lifestream-zhTW` 開編。

工作目錄：

```powershell
H:\project\ffxiv\DalamudPlugins\dalamud-mod-localizer
```

### 3.1 驗證 `sync` 模式

這是目前最可靠、最接近 CI 的本地檢查。

```powershell
python scripts\validate_consumer_patches.py `
  --consumer-repo H:\project\ffxiv\DalamudPlugins\Lifestream-zhTW `
  --workflow-mode sync `
  --mod-repo-url https://github.com/NightmareXIV/Lifestream.git `
  --mod-ref e91124d8f7fb0477b46d2c12c9db0fd59e66f3ad `
  --mod-repo-dir Lifestream `
  --localizer-source-subpaths Lifestream `
  --localizer-dict-path zh-TW.json
```

目前這條在本機已驗證成功。

意義是：

- shared localizer 可跑
- `sync` 後的 post-localizer source state 正常
- `Lifestream-zhTW` 的 consumer patches 在這個 base 上可套用

### 3.2 驗證 `build` 模式

```powershell
python scripts\validate_consumer_patches.py `
  --consumer-repo H:\project\ffxiv\DalamudPlugins\Lifestream-zhTW `
  --workflow-mode build `
  --mod-repo-url https://github.com/NightmareXIV/Lifestream.git `
  --mod-ref e91124d8f7fb0477b46d2c12c9db0fd59e66f3ad `
  --mod-repo-dir Lifestream `
  --localizer-source-subpaths Lifestream `
  --localizer-dict-path zh-TW.json
```

目前這條在本機已知會失敗，原因是：

- `0005-support-tw-cross-world-travel.patch` 在 `build` mode 的 source state 上無法乾淨套用

這代表：

- 目前 consumer repo 的 committed snapshot 不是穩定的本地 `build` patch base
- 如果只是想確認 CI 契約有沒有壞，請優先看 `sync` mode，不要拿 `build` mode 失敗就誤判 workflow 壞掉

## 4. 如果要保留本地驗證暫存目錄

可以加上 `--keep-temp`：

```powershell
python scripts\validate_consumer_patches.py `
  --consumer-repo H:\project\ffxiv\DalamudPlugins\Lifestream-zhTW `
  --workflow-mode sync `
  --mod-repo-url https://github.com/NightmareXIV/Lifestream.git `
  --mod-ref e91124d8f7fb0477b46d2c12c9db0fd59e66f3ad `
  --mod-repo-dir Lifestream `
  --localizer-source-subpaths Lifestream `
  --localizer-dict-path zh-TW.json `
  --keep-temp
```

這會留下 CI 重現後的工作目錄，方便繼續檢查實際 build 會看到的 source state。

## 5. 本地編譯目前卡在哪裡

就算先走完 `sync` 驗證，再對保留下來的 temp workspace 跑：

```powershell
$env:CI='true'
$env:DALAMUD_HOME="$env:APPDATA\XIVLauncher\addon\Hooks\dev"
dotnet build <temp>\consumer\Lifestream\Lifestream\Lifestream.csproj -c Release
```

目前仍可能失敗，已知會撞到這類問題：

- `Dalamud.Bindings.ImGui` 找不到
- `Dalamud.Bindings.ImPlot` 找不到
- `IDalamudTextureWrap.ImGuiHandle` 不存在
- `ECommons` 把 `IGameNetwork` obsolete 視為錯誤

這代表本機的 `DALAMUD_HOME` / Dalamud 開發參考環境，和 GitHub Actions 使用的資產還沒有完全對齊。

所以目前最準確的說法是：

- 本機已經可以做 patch base 驗證
- 但還不能保證能完整重現最終 binary build

## 6. 遇到問題時怎麼判斷

### 情況 A: `sync` validator 成功

代表：

- shared workflow 基本正常
- localizer 沒有明顯壞掉
- consumer patches 沒有在 CI 契約層面失配

### 情況 B: `build` validator 失敗

先不要急著怪 workflow。

優先判斷：

- 是不是 consumer snapshot 漂移了
- patch 是不是只對 `sync` base 生成，沒有對 `build` base 重建

### 情況 C: 裸 `dotnet build` 失敗

這通常不能直接當成 workflow 壞掉。

先確認：

1. 你是不是繞過了 localizer / consumer patch
2. `DALAMUD_HOME` 是否正確
3. 本機 Dalamud dev 資產是否真的和 CI 一致

## 7. 給其他 agent 的硬規則

- 不要把本地裸 `dotnet build` 當成唯一真相。
- 先跑 `validate_consumer_patches.py`。
- `sync` mode 成功比 `build` mode 更能代表 shared workflow 契約沒壞。
- 如果要做翻譯或功能修正，不要只改 snapshot source。
- 所有需要 source patch 的修改，都必須回收到 `.consumer-patches/`，並記錄到 `docs/TRANSLATION_PATCH_NOTES.md`。

## 8. 目前建議

如果目標是「本地確認 repo 沒壞」：

- 先跑 `sync` validator

如果目標是「本地真的要出 DLL」：

- 先承認目前仍以 GitHub Actions 為準
- 本機環境要再補齊與 Actions 對齊的 Dalamud dev 資產後，才值得繼續追完整編譯
