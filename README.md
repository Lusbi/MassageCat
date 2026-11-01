# Massage Cat

Massage Cat 是由 WalkBrave 專案精簡、遷移而成的 Unity 遊戲。專案保留目前遊戲執行所需的戰鬥、番茄鐘、桌面寵物、角色對話、在地化、音訊、Steam 與 Addressables 資料，並移除未使用的範例與 Demo 內容。

## 開發環境

- Unity：`6000.3.12f1`
- Render Pipeline：Built-in Render Pipeline
- 主要平台：Windows
- 主場景：`Assets/Scenes/Main.unity`
- Runtime 資料根目錄：`Assets/AssetResources`

主要套件包含 Addressables `2.9.1`、Localization `1.5.8`、UGUI `2.0.0`、Cinemachine `2.10.7`、Steamworks.NET、PrimeTween、TextMesh Pro、Febucci Text Animator、SuperScrollView、MPUIKit 與 Odin。

## 取得與開啟專案

1. Clone 此 Repository。
2. 使用 Unity Hub 加入專案根目錄。
3. 確認 Unity Editor 版本為 `6000.3.12f1`。
4. 開啟 `Assets/Scenes/Main.unity`。
5. 等待 Package Manager、腳本編譯與 Addressables 資料載入完成後再進入 Play Mode。

Unity 會自動重建 `Library`、`Temp`、`Logs`、`obj` 與 IDE 專案檔，這些內容不納入版本控制。

## 專案結構

| 路徑 | 用途 |
| --- | --- |
| `Assets/Script` | 遊戲專用程式：啟動、UI、戰鬥、儲存、法術、番茄鐘與桌面寵物 |
| `Assets/GameCore` | 共用框架：UI、Addressables、Database、事件、音訊、Pool 與 Steam |
| `Assets/AssetResources` | Addressables Runtime 資料、UI Prefab、資料庫與設定資產 |
| `Assets/AddressableAssetsData` | Addressables 設定、Group、Schema 與 Profile |
| `Assets/2DArt` | 角色、場景、UI 與其他 2D 素材 |
| `Assets/SFX` | BGM 與音效素材 |
| `Packages` | Unity Package Manager 套件宣告與鎖定版本 |
| `ProjectSettings` | Unity 專案設定 |

## Runtime 注意事項

- UI、設定與資料庫資產使用穩定的 Addressables key；修改 key 時必須同步更新所有載入端。
- `Assets/AssetResources/Database/Manager/DatabaseManager.cs` 為產生檔，除非無法重新產生，否則不要手動修改。
- 戰鬥場景背景在 Desktop Pet Mode 下會刻意隱藏。測試一般遊戲背景時，請先確認 Desktop Pet Mode 未啟用。
- Unity `.meta` 檔保存素材 GUID 與引用關係，新增、搬移或替換素材時必須一併保留。
- 高頻路徑以低／零 GC 為原則，避免在 `Update`、`Tick`、戰鬥輸入、UI 綁定與動畫 callback 中使用 LINQ 或建立不必要的物件。

## 驗證狀態

- SceneMap 背景素材與資料庫 GUID 引用已確認有效。
- 遷移掃描結果：0 個 Missing Script、0 個 Missing Renderer Material。
- Addressables 建置紀錄：`Error: None`。
- 已知的 Database DependencyManager 資料引用警告不會阻擋編譯、場景掃描或 Addressables 建置。

## 官方參考

- [Unity Asset Metadata 與 GUID](https://docs.unity3d.com/Manual/AssetMetadata.html)
- [Unity Addressables 概觀](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/AddressableAssetsOverview.html)
- [Unity Package Manifest](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-manifestPrj.html)
