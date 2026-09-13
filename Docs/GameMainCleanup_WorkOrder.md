# GameMain 無用物件清理

日期：2026-09-07。Unity：6000.3.12f1。

需求來源：[Notion：清理 GameMain 無用物件與素材](https://app.notion.com/p/3d37be674992815b878ecc87a51518e5)。

狀態：檔案修改完成，等待使用者手動驗收。未啟動 Unity、編譯、測試或建置。

## 處理範圍與結果

`Assets/AssetResources/UI/GameMain.prefab` 引用 `Assets/2DArt/UI/InBattle.prefab`，所以從後者移除已無用途的內容：

| 物件 | 移除依據 | 結果 |
| --- | --- | --- |
| TalkController | 只有 RectTransform，沒有子物件或行為元件；Assets、Packages、ProjectSettings 文字搜尋未發現外部 fileID 引用、名稱查找或動畫路徑。 | 移除 GameObject、RectTransform 與父節點的子節點引用。 |
| LegacyRoleTalkText_Disabled | 已停用且文字為空；六個序列化區塊的 fileID 只用於物件自身與父子關係。現行 BattlePanel 初始化會將 TalkManager 綁定到 EnemySpawn.RoleTalkText。 | 移除 GameObject、RectTransform、CanvasRenderer、TMP 文字與兩個 Febucci 元件，以及父節點的子節點引用。 |

此次共移除 2 個 GameObject、8 個序列化區塊。保留區塊只改動 2 處父節點的子節點清單。靜態文字比對未發現移除後仍引用這 8 個 fileID 的內容。

修改前的現有 InBattle.prefab 已完整備份至 `Artifacts/GameMainCleanup/20260907_133512/InBattle.prefab`，其中包含本次任務開始前的未提交修改；不應以 Git HEAD 覆寫還原。

## 保留內容與素材判定

- GameMain 的 TalkManager：角色說話、拼字提示仍會呼叫，不是閒置元件。
- GameMain 的 None-/img-bg：是啟用中的底色 Image，仍參與繪製與射線判定；沒有程式引用不代表沒有用途。
- RecyclePanel 與 ForegroundRoot：雖然 Prefab 中沒有子物件，但分別供執行期間療癒特效及場景前景使用。
- 場景圖片、教學、按鈕、拼字與番茄鐘介面：仍有序列化或執行流程依賴。
- 舊對話的字型／材質 GUID `d5eb740e69fe73b49913475dc1b38df9`：InBattle 與 EnemySpawn 的現行文字仍引用，因此保留。
- 舊對話使用的 TMP 與 Febucci 腳本仍是現行文字功能的依賴，因此保留。本次未找到可隨這兩個物件安全刪除的獨占素材。

本次沒有更動公共 API、玩法計算、Addressables key 或素材 GUID，也沒有新增執行時清理掃描。靜態分析不等於 Unity 執行驗證，未量測記憶體、GC 或效能差異。

## 使用者手動驗收

1. 在 Unity 開啟 GameMain Prefab，展開 InBattle，確認兩個舊物件已不存在，且 Inspector 沒有新增 Missing 引用。
2. 手動進入主遊戲，預期出現 `[Codex驗證] GameMain 對話已綁定角色文字元件，請確認角色說話與拼字提示正常。`。此 log 只代表綁定欄位存在，畫面仍須人工確認。
3. 讓角色說話，切換角色並進入已解鎖的拼字模式，確認角色對話、拼字提示與結束後清除仍正常。若出現角色文字元件未設定的警告，檢查 EnemySpawn 的對話引用並回報。
4. 操作番茄鐘、圖鑑與場景顯示開關，確認介面及背景／前景符合原本行為；如需發佈，請自行執行編譯與 Addressables 建置。

暫時 log 位於 `Assets/Script/UI/2.GameMain/Battle/BattlePanel.cs`，限制於 `UNITY_EDITOR || DEVELOPMENT_BUILD`，只在初始化綁定時輸出；驗收後可移除該段診斷條件區塊。

## 官方技術來源

- [Unity 6.3 Prefab 覆寫規則](https://docs.unity3d.com/6000.3/Documentation/Manual/PrefabInstanceOverrides.html)
- [Unity 素材與 GUID](https://docs.unity3d.com/Manual/AssetMetadata.html)
