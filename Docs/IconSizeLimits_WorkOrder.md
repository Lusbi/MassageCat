# 圖示素材尺寸規範

日期：2026-09-07。Unity：6000.3.12f1。

來源：[Notion：圖示素材尺寸規範](https://app.notion.com/p/3d37be6749928166aec6d981e0eb23bd)。

採用匯入尺寸限制方案，保留原始 PNG。這不表示來源檔的像素尺寸已縮小。狀態為檔案修改完成、等待使用者手動匯入與驗收；尚未執行 Unity、編譯、測試、建置或畫面驗證。

## 設定

路徑：`Assets/2DArt/DesktopPet/ChibiPaper`。

| 貼圖 | Max Size | 說明 |
| --- | ---: | --- |
| settings_icon_v1.png | 256 | 設定圖示 |
| gallery_icon_v1.png | 256 | 圖鑑圖示 |
| spell_icon_v1.png | 256 | 拼字圖示 |
| tomato_icon_v1.png | 256 | 番茄鐘圖示 |
| icon_button_frame_v1.png | 256 | 小型圖示按鈕框 |
| controls_atlas_v1.png | 1024 | 16 個圖示共用的圖集；原圖 1254×1254，最大切片邊長 276，按比例匯入後預估約 226 像素。 |
| message_icon_v1.png | 512 | support_atlas_v1.png 的原始 PNG 副本，只保留 Message 切片。原切片 328×331，按比例匯入後預估約 134×136 像素。 |

同步更新預設與現有平台區段的 maxTextureSize；保留既有平台 override 狀態、壓縮、GUID、切片 ID、切片座標與 pivot。既有檔案未更換 GUID；新資訊圖示副本使用新 GUID，InBattle 的資訊圖示引用已改指向該副本。

來源副本未重新繪製或縮放 PNG，因此增加一份來源檔與一張低解析度匯入貼圖；換取資訊圖示與進度條可使用不同解析度。support_atlas_v1.png 仍保留原有切片，現行資訊圖示引用已遷移；原有進度條引用與解析度不變。若日後新增資訊圖示，應使用 message_icon_v1.png 的 Message 切片。

背景、場景框、大面板、拼字／番茄鐘底板及進度條保留原設定。Kenney 的獨立 PNG 圖示來源尺寸均未超過 256，套件預覽圖不列為圖示。

既有手動 ChibiPaper 搬移工具同步保留控制圖集 1024 限制，且將舊 Message 素材對應到新副本；未新增自動匯入回呼，也未執行該工具。不要為驗收這項改動重跑整個搬移流程。

## 手動驗收

1. 自行讓 Unity 匯入這次變更，確認以上貼圖的 Default 與 Standalone Max Size 設定。
2. 在匯入後的預覽確認獨立圖示不超過 256×256；圖集需確認個別切片在匯入貼圖中的尺寸。Sprite Editor 顯示的來源切片座標仍保持原值。
3. 自行進入 Play Mode，確認設定、圖鑑、音效、BGM、自動切換、左右箭頭、儲存、資訊、拼字及番茄鐘圖示沒有遺失、裁切或不可接受的模糊；進度條與大型面板應維持原外觀。
4. Console 的 `[Codex驗證]` 初始化提示只提醒目視檢查，不代表尺寸或畫面測試已通過。完成驗收後可刪除新增的單行 log。
5. 若需要發佈，請自行重新建置 Addressables 與 Player，避免沿用舊貼圖內容。

本次只靜態檢查文字設定及引用。沒有量測執行時貼圖尺寸、記憶體或 GC。

修改前的 6 份 meta 與 InBattle 已備份到 `Artifacts/IconSizeLimits/20260907_161441`。還原時需連同資訊圖示引用一起還原，避免先移除新資產而留下 Missing 引用。

## 官方來源

- [Unity 6.3 Sprite 匯入設定](https://docs.unity3d.com/6000.3/Documentation/Manual/texture-type-sprite.html)
- [Unity 素材 metadata 與 GUID](https://docs.unity3d.com/Manual/AssetMetadata.html)
