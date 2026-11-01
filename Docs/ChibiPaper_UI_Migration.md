# ChibiPaper UI 素材統一

日期：2026-09-06。Unity：6000.3.12f1。

## 已確認範圍

- 只替換舊 UI 美術；保留角色、場景、動畫、材質與現有玩法。
- 將 Resources 中的獨立貼圖移到 Assets/2DArt，保留 SteamConfig、TMP 與其他非美術設定。
- 以既有 ChibiPaper 的奶油紙色、鼠尾草綠、陶土橘與深棕輪廓為視覺基準。

## 素材與載入

- 原 Assets/Resources/DesktopPet 共 16 張貼圖連同 .meta 移至 Assets/2DArt/DesktopPet；ChibiPaper 原始資產 GUID 與切片 ID 保留。
- MPUIKit 的預設貼圖移至 Assets/2DArt/Support/MPUIKit。Resources 只保留 MPImageResources.asset 設定引用；執行時仍快取同一張 Sprite。
- DesktopPetChibiPaperArt 保留原內部存取介面，改讀 MassageCatUiArt 在 InBattle Prefab 上的序列化 Sprite。取消字串路徑載入與 Sprite.Create，無每幀載入或切圖。
- 24 張 MassageCatUI 舊貼圖及 4 張已被使用的 Kenney 圖示由 ChibiPaper 取代。未被替換的 Kenney 套件內容保留。
- 新增 controls_atlas_v1.png（16 個功能圖示）與 support_atlas_v1.png（進度條底、填充與資訊圖示）。Sprite 切片在 Unity 匯入器設定，未以影像程式重繪或修改生成素材。
- Prefab 內原有非白色染色移除時保留透明度；狀態切換仍由原功能程式控制。圖鑑的背景、文字與按鈕同步調整成紙繪色系。
- 既有 Addressables 載入鍵不變。

## 生成紀錄

使用內建 image_gen 工具；風格參考：Assets/2DArt/DesktopPet/ChibiPaper/settings_icon_v1.png。生成候選經透明通道與切片範圍檢查後選用；未採用的候選未放入 Assets。

### controls_atlas_v1.png 最終提示詞

Create ONE production Unity sprite atlas PNG, 4 columns by 4 rows equal cells, square 2048x2048, truly transparent alpha background, no grid lines, no text. The attached gear is STYLE REFERENCE ONLY: match its ChibiPaper handmade cut-paper style EXACTLY: thick dark cocoa outline, flat warm cream / sage green / terracotta color blocks, subtle paper fiber, round chunky forms, tiny cream highlights, no realistic metal, no purple, readable at 32 pixels. Each symbol centered in its exact cell, generous transparent padding 20% each side, do not cross cell boundaries. Sixteen symbols in strict reading order: row 1: music note with sound waves (BgmOn), music note with terracotta diagonal mute slash (BgmOff), speaker with waves (SfxOn), speaker with diagonal mute slash (SfxOff). row 2: two sage forward triangles with a small check mark (AutoNextOn), same two triangles muted cream with a terracotta slash (AutoNextOff), simple thick horizontal minus (Minimize), doorway with arrow pointing out to right (Quit). row 3: open eye (SceneVisible), eye with diagonal slash (SceneHidden), chunky X (Close), cream collectible card with small paw print (CardStatus). row 4: chunky left arrow, chunky right arrow, warm golden five-point star, sage floppy disk save icon. Standalone silhouettes with authentic transparent background, without any surrounding button frames. Output image file for game project use.

### support_atlas_v1.png 最終提示詞

Generate a clean flat 2D game sprite atlas with REAL TRANSPARENT BACKGROUND, exact alpha zero everywhere outside sprites, no glow no cast shadow no backdrop or floor, NO CHECKERBOARD. Use the attached style reference gear's warm paper cutout colors and chunky dark brown outlines. Exactly THREE isolated shapes on a square sheet: top third an empty horizontal cream capsule with sage rim and brown outline; middle third a solid sage green horizontal capsule with brown outline; bottom third a small round cream information badge with a terracotta lowercase i and brown outline. Both horizontal capsules have same width, height, and rounded radius; capsule width 80% of canvas, height 12%. Badge diameter 20% canvas. All isolated with generous true transparent space. Very simple flat paper illustration, no decoration outside the shapes. This is a transparent UI sprite sheet, not a mockup.

## 驗證與回復

實際執行結果與畫面位於 Artifacts/ChibiPaperMigration。replacement-map.md 記錄每個舊圖對應的新 Sprite，filesystem-validation.json 記錄 Resources 與 GUID 檢查。backup 保留搬移前的素材及 Prefab；不加入正式遊戲資產。

Unity 工具選單：Tools / Massage Cat / Art / Validate ChibiPaper UI。

### 後續由使用者手動驗證

已移除本次新增的 ChibiPaperVerificationRunner 與 ChibiPaperPlayModeSmoke 自動執行腳本。既有結果只代表先前版本；最後的面板修正與新增 log 尚未完成驗證。

1. 手動執行 Tools / Massage Cat / Art / Repair ChibiPaper Presentation，套用上次尚未執行的設定背景與九宮格修正。
2. 自行進入 Play Mode，在 Console 搜尋 `[Codex驗證]`，檢查主畫面、設定開關、BGM/SFX 圖示及圖鑑開關。
3. 手動執行 Validate ChibiPaper UI；Addressables 建置也由使用者自行執行。

暫時 log 均為獨立單行，刪除包含 `[Codex驗證]` 的 log 呼叫即可；不需刪除相鄰功能程式碼。log 只存在於 Editor 或 Development Build。

## 官方來源

- [Unity 6.3 資產搬移](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetDatabase.MoveAsset.html)
- [Resources 載入規則](https://docs.unity3d.com/6000.3/Documentation/Manual/LoadingResourcesatRuntime.html)
- [Sprite 九宮格](https://docs.unity3d.com/6000.3/Documentation/Manual/9SliceSprites.html)
