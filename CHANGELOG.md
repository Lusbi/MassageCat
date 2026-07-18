# 更新紀錄

本文件記錄 Massage Cat 專案的重要版本與結構調整。

## 2026-07-18

### 遊戲修正

- Desktop Pet 模式在擊倒敵人後會自動切換至下一個已解鎖角色，並略過自動下一關按鈕教學。
- 啟動蕃茄鐘時保留目前選定角色，不再重置為預設紙箱。
- 左右角色切換箭頭移至畫面邊緣並放大兩倍；閒置時為 35% 透明度，滑入時完整顯示。

### 專案整理

- 將原本只保存 Assets 內容的 Repository 整理為完整 Unity `6000.3.12f1` 專案結構。
- 將既有遊戲素材與程式統一放回標準 `Assets/` 路徑，並加入 `Packages`、`ProjectSettings` 與專案說明文件。
- 移除來源專案中未使用的範例、Demo 與 Pedometer 內容，保留目前 Runtime 所需的套件與資產。
- 新增 Unity 專用 `.gitignore` 與 `.gitattributes`，排除可重建的快取、IDE 檔案、建置輸出及本機內部文件。
- 保留所有必要的 Unity `.meta` 檔與既有 Addressables key，未修改 Public API 或核心遊戲邏輯。

### 遷移驗證

- Unity `6000.3.12f1` Batch Mode 已完成腳本編譯與 AssetDatabase Refresh，結束碼為 0。
- SceneMap 背景 Sprite 已遷移，ScenemapRepository 中的 GUID 引用有效。
- Missing Script 與 Missing Renderer Material 掃描結果皆為 0。
- Addressables 建置完成，結果為 `Error: None`。
- 已知的 `GameCore.Database.DependencyManager` 資料引用警告未阻擋編譯、場景掃描或 Addressables 建置。

### 已知行為

- Desktop Pet Mode 會停用戰鬥背景 Image，且背景更新流程會提早結束；一般背景測試需停用此模式。
