# Lifestream-zhTW Agent Guide

給操作此 repo 的 AI agent 的操作規範與機制說明。  
**在觸碰任何東西之前，請先讀完這份文件。**

---

## 1. Workflow 模式總覽

GitHub Actions workflow 有三個模式，行為**完全不同**：

| 模式 | Clone 上游 | Commit source | 觸發方式 |
|------|-----------|--------------|---------|
| `sync` | ✓ 重新 clone `mod_ref` | ✓ 寫回 repo | `workflow_dispatch` 選 sync |
| `build` | ✗ 用 committed source | ✗ 不 commit | `workflow_dispatch` 預設、手動觸發 |
| `extract` | ✗ 用 committed source | ✓ 寫回 repo（reverted state） | `workflow_dispatch` 選 extract |

**重要**：`gh workflow run main.yml` 不帶參數 → 預設 `build` 模式。

---

## 2. BUILD 模式的 patch 行為（容易誤判的坑）

### Localizer 會修改 `Lifestream.cs`

在 BUILD 模式，localizer 執行時**會翻譯 `Lifestream.cs` 內的 log 字串**，例如：

```csharp
// committed source 裡的英文
PluginLog.Information($"Same dc/{primary}/{w}");

// localizer 跑完後，source 變成
PluginLog.Information($"同資料中心/{primary}/{w}");
```

這發生在 `Apply Consumer Patches` **之前**。所以 patch 的 context / `-` / `+` 行，都是看 **localizer 跑完後的狀態**，不是 committed source 的狀態。

### `0005` patch 的設計邏輯

`0005-support-tw-cross-world-travel.patch` 的 `Lifestream.cs` hunk 有個特殊設計：

```diff
-                if(S.Data.DataStore.Worlds.TryGetFirst(...))   ← 舊程式碼（上游原始）
+                var targetInput = ...
+                if(Utils.TryResolveTravelWorldInput(...))        ← 新程式碼
                 {
                     PluginLog.Information($"同資料中心/{primary}/{w}");  ← context line（中文）
```

**為什麼 log 字串是中文 context line，而不是 `-`/`+` 替換：**

- Localizer 把 `Same dc` 翻成 `同資料中心`（兩種模式都會）
- BUILD 模式下，committed source 已有新程式碼 + `Same dc`（英文）
- Localizer 跑完 → 變成 `同資料中心`（中文）
- `git apply --reverse --check` 的 `+` 行必須能匹配 source
- 如果 `+` 行有 `Same dc`（英文），但 source 有 `同資料中心`（中文）→ **兩個 check 都失敗**
- 改成 context line（`同資料中心`）→ 中文 context 匹配 ✓，reverse check 偵測到 "already present" ✓

**不要把 log 字串改回 `-`/`+` 替換形式，這樣 BUILD 模式會壞。**

### patch 套用邏輯

```
Forward check 成功 → apply（通常發生在 SYNC 模式 first-time）
Forward 失敗 + Reverse check 成功 → "already present, skipping"（通常發生在 BUILD 模式）
兩個都失敗 → "Patch could not be applied cleanly" → build 失敗
```

---

## 3. 觸發 build 的正確方式

### 一般驗證（不 commit）

```bash
gh workflow run main.yml --repo mcc1/Lifestream-zhTW
```

預設 `build` 模式，不寫回 repo，只做 build + artifact upload。

### 同步上游並更新 committed source

```bash
gh workflow run main.yml --repo mcc1/Lifestream-zhTW \
  -f workflow_mode=sync
```

SYNC 模式：重新 clone upstream、localizer 翻譯、apply patches、commit source。  
**升版 `mod_ref` 後必須跑這個**，否則 committed source 會過期。

### 追蹤 run 到完成

```bash
gh run watch <run_id> --repo mcc1/Lifestream-zhTW
```

或取最新 run id：

```bash
gh run list --repo mcc1/Lifestream-zhTW --limit 1 --json databaseId --jq '.[0].databaseId'
```

---

## 4. Localizer ref 固定（Pin）

`b6204736` 開始，workflow 將 `dalamud-mod-localizer` 固定在特定 SHA：

```yaml
uses: mcc1/dalamud-mod-localizer/.github/workflows/reusable-build-mod.yml@32f2a6ed...
template_ref: 32f2a6ed...
```

**為什麼要 pin：**  
使用 `@main` 時，localizer 更新可能在不知情的情況下改變翻譯行為，導致 patch context 不匹配。  
固定 SHA 確保 build 結果可重現。

**更新 pin 時注意：**  
1. 先在 SYNC 模式跑一次，確認 source + patches 正常
2. 再更新 `template_ref` SHA
3. 再跑一次 BUILD 模式確認

---

## 5. Consumer patches 一覽

| Patch | 性質 | 作用 |
|-------|------|------|
| `0001` | CI 環境 | 將 `Dalamud.home` 指向 CI 的 `DALAMUD_HOME` 環境變數 |
| `0002` | 相容性 | 移除 `AddonAirshipExploration` using（API 12 已不存在） |
| `0003` | 相容性 | 停用 API 12 不相容的 input hook |
| `0005` | 功能+翻譯 | 台服跨伺服器旅行支援（world alias、world list、選單匹配） |
| `0006` | 翻譯 | OtterGui 硬編碼 UI 提示翻譯（Filter/Add New 等） |
| `0007` | 翻譯相容 | 台服住宅區選單文字變體補上 |
| `0008` | 相容性 | 補回 `OtterGui/Log/Logger.cs`（上游 submodule 版本落差） |
| `0009` | 相容性 | 補回 `OtterGui/Log/LazyString.cs`（同上） |

---

## 6. 失敗排查

### `0005` "Patch could not be applied cleanly"

最常見原因：

1. **patch 的 context 行用了英文 log 字串**（如 `Same dc`），但 localizer 跑完後 source 是中文  
   → 確認 `0005` 的 `Lifestream.cs` hunk 中 log 字串是中文 context line，不是 `+` 行

2. **committed source 太舊或被人工改過**  
   → 用 SYNC 模式重建 committed source

3. **上游 `mod_ref` 更新後 patch base 漂移**  
   → 重新對 SYNC base 生成 patch（見 TRANSLATION_PATCH_NOTES.md 中的說明）

### `0001`/`0002`/`0003` 顯示 error 但 "Patch already present, skipping"

這是**正常現象**。這些 patch 的改動在上游 source 已經存在（forward apply 失敗但 reverse check 成功）。不要動它們。

### BUILD 模式成功但 SYNC 模式失敗

通常是 patch 的 `-` 行期待的 context 在 localizer 跑後不存在。  
解法：在 SYNC base 重新生成 patch。

---

## 7. Committed source 的角色

Committed source（`Lifestream/` 目錄）是**上一次 SYNC 模式 workflow 跑完後 commit 的快照**。

- 它的狀態 = upstream `mod_ref` + localizer 翻譯 + consumer patches 套用後
- BUILD 模式把它當起點，localizer 再跑一次（可能再翻一遍 log 字串）
- **不要手動編輯 committed source** 並期待 patch 還能套用

---

## 8. 已知 commit 歷史雜訊（2026-03-17）

以下 commit 是 net-zero 的操作失誤，不影響功能，但供未來 git blame 時參考：

| Commit | 說明 |
|--------|------|
| `18979c95` + `c1af2424` | Scan OtterGui → 立刻 revert，net zero |
| `bb946101` + `f36edfda` | 錯誤 revert `0005` → 修正，net zero |

`a0c66df2`（"Stabilize TW world travel patch for build mode"）是**正確且必要的修法**，不要 revert 它。

---

## 9. 禁止事項

- **不要** 把 `0005` 的 log 字串改成 `+`/`-` 替換（會讓 BUILD 模式的 reverse check 失敗）
- **不要** 在沒有 SYNC 模式重建的情況下手動修改 committed source 後直接套 patch
- **不要** 在不確定影響的情況下更動 `template_ref` / localizer SHA
- **不要** 把 BUILD 模式失敗直接等同於「repo 壞了」—先確認是 sync 模式是否也失敗
