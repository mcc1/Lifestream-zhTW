# Lifestream Translation Patch Notes

這份文件只記錄「無法單靠字典 / extract / shared localizer 完成，必須進 consumer patch」的翻譯修改。

## 0006 translate address book controls and filter hints

- Patch file:
  - `.consumer-patches/0006-translate-address-book-controls-and-filter-hints.patch`
- 類型:
  - 翻譯用 source patch
- 原因:
  - 這批文字不是完整由 `zh-TW.json` 驅動
  - `Filter...` 與 `Add New` tooltip 來自 `OtterGui` 共用 UI 元件的硬編碼提示字
  - 這些字串目前不會被 `extract` / `TranslationRewriter` 收進字典
- 修正範圍:
  - 地址簿、選擇器、檔案樹、篩選輸入框的硬編碼提示字
- 實際修正的 UI:
  - `Filter...` -> `篩選...`
  - `Add New` tooltip -> `新增`
- 修改檔案:
  - `Lifestream/OtterGui/Classes/FilterUtility.cs`
  - `Lifestream/OtterGui/Classes/StartTimeTracker.cs`
  - `Lifestream/OtterGui/Filesystem/Selector/FileSystemSelector.State.cs`
  - `Lifestream/OtterGui/Filesystem/Selector/FileSystemSelector.cs`
  - `Lifestream/OtterGui/ItemSelector.cs`
  - `Lifestream/OtterGui/Widgets/ClippedSelectableCombo.cs`
  - `Lifestream/OtterGui/Widgets/FilteredCombo.cs`
- 為什麼不是字典:
  - 這些字串目前不是經過 Lifestream 自己的翻譯入口，而是直接寫在 `OtterGui` source
- 後續處理方向:
  - 若 shared localizer 未來支援這類 ImGui hint / tooltip 文字抽取，應評估把這個 patch 回推成 shared 能力
  - 在那之前，必須維持這個 consumer patch，否則 `sync` 後會被上游 source 覆蓋
