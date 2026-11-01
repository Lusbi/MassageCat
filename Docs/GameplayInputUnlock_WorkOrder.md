# 工作單：直接進入遊玩與遊玩輸入解鎖

## 狀態

需求已確認，尚未開始實作。

## 目標

取消 Title 點擊進入主遊戲的流程。完成現有資料庫、語系、設定與存檔初始化後，直接顯示 `GameMain`，但遊玩輸入預設鎖定。

玩家須在遊戲視窗內以左鍵長按 1.5 秒才解除鎖定。長按本身不可觸發治療或其他戰鬥操作。

## 範圍

### 啟動流程

- `GameProcess` 的初始 View 改為 `UIEnum.GameMain`。
- `UILoadProcess` 不再預載 `UITitle`，只等待 `UIGameMain` 載入完成。
- 保留 `UITitle`、`View_Title` 與其 Addressables 項目，不刪除資產、不更動 Addressables key。
- 由 `GameMain` 既有啟用流程開始戰鬥與角色資料載入。

### 遊玩輸入解鎖

- 每次啟動遊戲皆為鎖定狀態；成功解鎖後直到結束遊戲前都維持解鎖。
- 鎖定期間只允許長按解鎖與基本視窗移動；所有戰鬥輸入與 UI 控制項均不可操作。
- 解鎖手勢為：游標在遊戲視窗內，以左鍵持續按住 1.5 秒，且位移不得超過 8 個邏輯 UI 像素。
- 游標離開遊戲視窗、視窗失焦、最小化、左鍵放開或位移超過門檻時，取消長按並將進度歸零。
- 解鎖完成後隱藏提示，恢復鍵盤與滑鼠的既有遊玩輸入。

### 提示與互動

- 鎖定期間顯示不遮蔽角色的半透明引導層，文字為「長按 1.5 秒開始遊玩」，文字下方顯示 0% 至 100% 的進度環。
- 提前放開或取消手勢時進度立即歸零；成功時提示淡出並隱藏。
- 解鎖前，提示層攔截所有 UI 操作；但不得阻礙允許的原生視窗移動。

### 視窗拖曳與滑鼠治療

- 桌面寵物模式：將現有拖曳區擴展為整個介面。
- 一般模式：保留作業系統的標準視窗操作，不增加自訂全介面拖曳。
- 解鎖後，UI 控制項優先於視窗拖曳；非 UI 區域的短按仍可治療。
- 左鍵位移超過 8 個邏輯 UI 像素時改為拖曳視窗，且不觸發治療。
- 為區分短按與拖曳，非 UI 區的短按治療改在左鍵放開時觸發一次；長按解鎖與拖曳均不觸發治療。

## 實作設計

### 輸入狀態

`Locked`、`TrackingUnlockHold`、`Unlocked`、`DraggingWindow` 四個狀態應由單一輸入閘門管理，避免讓 `BattleManager`、提示 UI 與視窗控制器各自判斷同一筆原始輸入。

- `Locked`：顯示提示，停用 UI 與戰鬥輸入。
- `TrackingUnlockHold`：更新進度環；符合 1.5 秒、未位移、仍在視窗內時進入 `Unlocked`。
- `DraggingWindow`：取消解鎖進度或短按候選，交由桌面寵物模式的原生拖曳處理。
- `Unlocked`：隱藏提示並啟用 UI；鍵盤維持既有即時處理，滑鼠短按改於放開時送出一次戰鬥輸入。

### 輸入 Hook 擴充

目前 `WindowsInputHookManager` 僅發送滑鼠按下事件，不含放開、移動或螢幕座標，無法判斷長按、位移與是否仍在遊戲視窗內。新增原始指標按下、放開與移動事件，需包含螢幕座標並統一轉回 Unity 主執行緒處理。

保留既有 `OnGlobalKeyDown` 與 `OnGlobalMouseDown` 以避免破壞現有呼叫端；新輸入閘門只使用新增的原始指標事件。不得在輸入熱路徑加入 LINQ、閉包或每次事件配置的暫存物件。

### 受影響檔案

- `Assets/Script/GameProcess.cs`
- `Assets/GameCore/UI/UILoadProcess.cs`
- `Assets/Script/UI/2.GameMain/GameMainView.cs`
- `Assets/Script/Modules/Manager/BattleManager.cs`
- `Assets/Modules/InputListener/Windows/WindowsInputHookManager.cs`
- `Assets/Script/DesktopPet/DesktopPetWindowController.cs`
- 新增的遊玩輸入解鎖閘門與提示 UI 元件（位置依既有 UI 結構決定）

## 不在本工作單範圍

- 不刪除 Title 相關程式、Prefab 或 Addressables 項目。
- 不變更 Addressables keys、資料庫、存檔資料或核心治療公式。
- 不更動一般模式的原生視窗外觀與拖曳行為。
- 不重構桌面寵物模式、圖鑑或設定選單架構。

## 驗收條件

1. 啟動後不顯示 Title，直接顯示 `GameMain`；Title prefab 不被預載。
2. 解鎖前鍵盤、滑鼠戰鬥輸入及所有 UI 控制項皆不產生遊玩效果。
3. 在遊戲視窗內左鍵靜止長按 1.5 秒，進度環正確填滿並只解除一次鎖定，不造成治療。
4. 提前放開、位移超過 8 UI 像素、游標離窗、失焦或最小化，進度均歸零且不解鎖。
5. 解鎖後鍵盤可維持既有遊玩操作；非 UI 區短按在放開時僅治療一次；拖曳不治療。
6. 桌面寵物模式可從非 UI 區拖曳視窗，且控制項在解鎖後仍可正常點擊。
7. 一般模式可長按解鎖，但未出現自訂全介面視窗拖曳。
8. 完成 Unity 編譯、Addressables build、一般模式與桌面寵物模式的 Play Mode smoke test。
