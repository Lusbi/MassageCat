using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GameCore.Database;
using GameCore.UI;
using SuperScrollView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MassageCat.Gallery
{
    public sealed class GalleryBootstrap : MonoBehaviour
    {
        private const int k_defaultWindowWidth = 960;
        private const int k_defaultWindowHeight = 640;
        private const float k_connectionTimeout = 30f;
        private const string k_roleItemPrefabName = "GalleryRoleItem";

        [SerializeField] private TMP_FontAsset m_fontAsset;

        internal static GalleryBootstrap IntegratedInstance { get; private set; }

        private readonly GalleryCatalogModel m_catalogModel = new GalleryCatalogModel();
        private readonly List<Button> m_sceneButtons = new List<Button>(12);
        private readonly List<string> m_sceneKeys = new List<string>(12);
        private readonly HashSet<string> m_unlockedSceneKeys = new HashSet<string>();

        private GalleryPipeEndpoint m_endpoint;
        private GalleryStateSnapshot m_snapshot;
        private GallerySortMode m_sortMode;
        private string m_selectedSceneKey = string.Empty;
        private string m_selectedRoleKey = string.Empty;
        private bool m_missingCardOnly;
        private bool m_tomatoIncompleteOnly;
        private int m_nextRequestId = 1;
        private GallerySelectRoleRequest m_pendingRequest;
        private float m_pendingRequestDeadline;
        private bool m_pendingRequestSent;

        private Canvas m_canvas;
        private TextMeshProUGUI m_connectionText;
        private TextMeshProUGUI m_sortButtonText;
        private TextMeshProUGUI m_missingFilterText;
        private TextMeshProUGUI m_tomatoFilterText;
        private TextMeshProUGUI m_emptyText;
        private TextMeshProUGUI m_previewName;
        private TextMeshProUGUI m_previewCardState;
        private TextMeshProUGUI m_previewScene;
        private TextMeshProUGUI m_resultText;
        private Image m_previewImage;
        private Button m_confirmButton;
        private TextMeshProUGUI m_confirmButtonText;
        private RectTransform m_sceneTabContent;
        private LoopGridView m_gridView;
        private Action<GalleryRoleEntry> m_onRoleClicked;
        private DesktopPetWindowController m_windowController;
        private BattlePanel m_battlePanel;
        private GameMainView m_gameMainView;
        private RectTransform m_root;
        private bool m_integratedMode;
        private bool m_integratedInitialized;

        private void Awake()
        {
            Application.runInBackground = true;
            m_integratedMode = GameProcess.Instance != null && GameProcess.Instance.DesktopPetMode;
            if (m_integratedMode == false)
                ApplySavedWindowSize();
            m_onRoleClicked = OnRoleClicked;
        }

        private void Start()
        {
            if (m_integratedMode)
                return;

            StartCoroutine(ApplySavedWindowPosition());
            DatabaseManager.instance.Initialize(
                () => LocalizationManager.instance.Initlization(
                    () => SettingsManager.instance.Initlization(InitializeGallery)));
        }

        internal void InitializeIntegrated(DesktopPetWindowController windowController)
        {
            if (m_integratedInitialized)
                return;

            m_integratedInitialized = true;
            m_integratedMode = true;
            m_windowController = windowController;
            m_battlePanel = FindFirstObjectByType<BattlePanel>();
            m_gameMainView = m_battlePanel != null ? m_battlePanel.GetComponentInParent<GameMainView>() : null;
            TextMeshProUGUI existingText = UIManager.instance != null
                ? UIManager.instance.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
            m_fontAsset = existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;
            IntegratedInstance = this;
            GalleryStateChangeNotifier.StateChanged += OnIntegratedStateChanged;
        }

        internal bool ToggleIntegrated()
        {
            if (m_integratedInitialized == false || m_windowController == null)
                return false;

            if (m_canvas != null && m_canvas.gameObject.activeSelf)
            {
                CloseIntegrated();
                return true;
            }

            m_windowController.SetGalleryExpanded(true);
            SetIntegratedGameplayVisible(false);
            if (m_canvas == null)
                BuildUi();
            else
                m_canvas.gameObject.SetActive(true);

            ApplyIntegratedRootPosition();
            ApplySnapshot(GalleryStateUtility.Capture(GetCurrentBattleModel()));
            SetConnectionState(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log("[Codex驗證] 圖鑑已開啟，請確認紙框邊角、文字可讀性、篩選與角色切換。", this);
#endif
            return true;
        }

        private void CloseIntegrated()
        {
            if (m_canvas != null)
                m_canvas.gameObject.SetActive(false);
            m_windowController.SetGalleryExpanded(false);
            SetIntegratedGameplayVisible(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log("[Codex驗證] 圖鑑已關閉，請確認主畫面恢復顯示。", this);
#endif
        }

        private void SetIntegratedGameplayVisible(bool isVisible)
        {
            if (m_integratedMode == false)
                return;

            m_gameMainView ??= m_battlePanel != null ? m_battlePanel.GetComponentInParent<GameMainView>() : null;
            if (m_gameMainView != null)
                m_gameMainView.SetGalleryVisible(isVisible == false);

            GameInputGate.SetGameplayInputPaused(isVisible == false);
        }

        private void ApplyIntegratedRootPosition()
        {
            if (m_root == null || m_windowController == null)
                return;

            m_root.anchoredPosition = m_windowController.GalleryViewportOrigin;
        }

        private BattleModel GetCurrentBattleModel()
        {
            m_battlePanel ??= FindFirstObjectByType<BattlePanel>();
            return m_battlePanel != null ? m_battlePanel.CurrentBattleModel : BattleModel.Normal;
        }

        private void OnIntegratedStateChanged()
        {
            if (m_canvas != null && m_canvas.gameObject.activeSelf)
                ApplySnapshot(GalleryStateUtility.Capture(GetCurrentBattleModel()));
        }

        private void InitializeGallery()
        {
            StorageManager.instance.SetStorageData(GalleryStateUtility.LoadDesktopPetStorageReadOnly());
            m_snapshot = GalleryStateUtility.Capture(BattleModel.Normal);
            BuildUi();
            ApplySnapshot(m_snapshot);

            m_endpoint = new GalleryPipeEndpoint(false);
            m_endpoint.Start();
        }

        private void Update()
        {
            if (m_integratedMode)
                return;

            if (m_endpoint == null)
                return;

            while (m_endpoint.TryDequeueConnectionChange(out bool connected))
            {
                SetConnectionState(connected);
                if (connected)
                {
                    m_endpoint.Send(
                        GalleryProtocol.Hello,
                        new GalleryHelloMessage { processRole = "gallery" });
                    TrySendPendingRequest();
                }
                else
                {
                    m_pendingRequestSent = false;
                }
            }

            while (m_endpoint.TryDequeueMessage(out GalleryEnvelope envelope))
                HandleMessage(envelope);

            if (m_pendingRequest != null && Time.unscaledTime >= m_pendingRequestDeadline)
            {
                m_pendingRequest = null;
                m_pendingRequestSent = false;
                SetResult(Localize("Gallery.ConnectTimeout", "連線逾時，請確認桌寵程式可正常啟動。"), true);
            }
        }

        private void HandleMessage(GalleryEnvelope envelope)
        {
            switch (envelope.type)
            {
                case GalleryProtocol.StateSnapshot:
                    if (GalleryPipeEndpoint.TryReadPayload(envelope, out GalleryStateSnapshot snapshot))
                        ApplySnapshot(snapshot);
                    break;
                case GalleryProtocol.SelectRoleResult:
                    if (GalleryPipeEndpoint.TryReadPayload(envelope, out GallerySelectRoleResult result))
                        ApplySelectRoleResult(result);
                    break;
                case GalleryProtocol.FocusGallery:
                    FocusWindow();
                    break;
            }
        }

        private void ApplySnapshot(GalleryStateSnapshot snapshot)
        {
            m_snapshot = snapshot ?? GalleryStateUtility.Capture(BattleModel.Normal);
            m_catalogModel.ApplySnapshot(
                m_snapshot,
                m_sortMode,
                m_selectedSceneKey,
                m_missingCardOnly,
                m_tomatoIncompleteOnly);
            RebuildSceneTabs();
            SelectBestVisibleRole();
            RefreshGrid(true);
            RefreshPreview();
        }

        private void ApplyCurrentView()
        {
            m_catalogModel.ApplyView(
                m_sortMode,
                m_selectedSceneKey,
                m_missingCardOnly,
                m_tomatoIncompleteOnly);
            SelectBestVisibleRole();
            RefreshGrid(true);
            RefreshPreview();
            RefreshControlLabels();
        }

        private void SelectBestVisibleRole()
        {
            IReadOnlyList<GalleryRoleEntry> entries = m_catalogModel.VisibleEntries;
            if (ContainsVisibleRole(m_selectedRoleKey))
                return;

            if (ContainsVisibleRole(m_snapshot?.currentRoleKey))
            {
                m_selectedRoleKey = m_snapshot.currentRoleKey;
                return;
            }

            m_selectedRoleKey = entries.Count > 0 ? entries[0].RoleData.key : string.Empty;
        }

        private bool ContainsVisibleRole(string roleKey)
        {
            if (string.IsNullOrEmpty(roleKey))
                return false;

            IReadOnlyList<GalleryRoleEntry> entries = m_catalogModel.VisibleEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].RoleData.key, roleKey, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void BuildUi()
        {
            CreateEventSystem();

            GameObject canvasObject = new GameObject("GalleryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            m_canvas = canvasObject.GetComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            RectTransform root;
            if (m_integratedMode)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = m_windowController.WindowScale;
                root = CreateRect("Root", canvasObject.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                root.pivot = Vector2.zero;
                root.sizeDelta = m_windowController.GameViewportSize;
                root.anchoredPosition = m_windowController.GalleryViewportOrigin;
            }
            else
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(k_defaultWindowWidth, k_defaultWindowHeight);
                scaler.matchWidthOrHeight = 0.5f;
                root = CreateRect("Root", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            m_root = root;
            Image rootImage = AddImage(root.gameObject, Color.white);
            Sprite panelSprite = MassageCatUiArt.GalleryPanel;
            if (panelSprite != null)
            {
                rootImage.sprite = panelSprite;
                rootImage.type = Image.Type.Sliced;
                rootImage.pixelsPerUnitMultiplier = 6f;
            }
            else
                rootImage.color = new Color32(246, 234, 197, 255);

            Vector2 topBarMin = m_integratedMode ? new Vector2(8f, -42f) : new Vector2(16f, -64f);
            Vector2 topBarMax = m_integratedMode ? new Vector2(-8f, -8f) : new Vector2(-16f, -12f);
            RectTransform topBar = CreateRect("TopBar", root, new Vector2(0f, 1f), Vector2.one, topBarMin, topBarMax);
            TextMeshProUGUI title = CreateText("Title", topBar, m_integratedMode ? 20f : 27f, TextAlignmentOptions.MidlineLeft);
            title.SetText(Localize("Gallery.Title", "對象圖鑑"));
            SetRect(title.rectTransform, new Vector2(0f, 0f), new Vector2(0.35f, 1f), Vector2.zero, Vector2.zero);

            m_connectionText = CreateText("Connection", topBar, 14f, TextAlignmentOptions.MidlineRight);
            SetRect(
                m_connectionText.rectTransform,
                new Vector2(m_integratedMode ? 0.42f : 0.72f, 0f),
                new Vector2(m_integratedMode ? 0.74f : 1f, 1f),
                Vector2.zero,
                Vector2.zero);

            if (m_integratedMode)
            {
                Button closeButton = CreateButton("CloseGallery", topBar, "關閉圖鑑", out TextMeshProUGUI closeText);
                SetRect((RectTransform)closeButton.transform, new Vector2(0.76f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
                AddButtonIcon(closeButton, closeText, MassageCatUiArt.GalleryClose);
                closeButton.onClick.AddListener(CloseIntegrated);
            }

            Vector2 controlsMin = m_integratedMode ? new Vector2(8f, -78f) : new Vector2(16f, -112f);
            Vector2 controlsMax = m_integratedMode ? new Vector2(-8f, -48f) : new Vector2(-16f, -70f);
            RectTransform controls = CreateRect("Controls", root, new Vector2(0f, 1f), Vector2.one, controlsMin, controlsMax);
            Button sortButton = CreateButton("SortButton", controls, string.Empty, out m_sortButtonText);
            SetRect((RectTransform)sortButton.transform, new Vector2(0f, 0f), new Vector2(0.34f, 1f), Vector2.zero, new Vector2(-6f, 0f));
            sortButton.onClick.AddListener(CycleSortMode);

            Button missingFilter = CreateButton("MissingFilter", controls, string.Empty, out m_missingFilterText);
            SetRect((RectTransform)missingFilter.transform, new Vector2(0.34f, 0f), new Vector2(0.66f, 1f), new Vector2(6f, 0f), new Vector2(-6f, 0f));
            missingFilter.onClick.AddListener(ToggleMissingCardFilter);

            Button tomatoFilter = CreateButton("TomatoFilter", controls, string.Empty, out m_tomatoFilterText);
            SetRect((RectTransform)tomatoFilter.transform, new Vector2(0.66f, 0f), Vector2.one, new Vector2(6f, 0f), Vector2.zero);
            tomatoFilter.onClick.AddListener(ToggleTomatoFilter);

            CreateSceneTabs(root);
            CreateGrid(root);
            CreatePreview(root);
            RefreshControlLabels();
            SetConnectionState(false);
        }

        private void CreateSceneTabs(RectTransform root)
        {
            Vector2 min = m_integratedMode ? new Vector2(8f, -116f) : new Vector2(16f, -164f);
            Vector2 max = m_integratedMode ? new Vector2(-8f, -86f) : new Vector2(-16f, -118f);
            RectTransform scrollRoot = CreateRect("SceneTabs", root, new Vector2(0f, 1f), Vector2.one, min, max);
            ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = CreateRect("Viewport", scrollRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image viewportImage = AddImage(viewport.gameObject, new Color(0f, 0f, 0f, 0.01f));
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            m_sceneTabContent = CreateRect("Content", viewport, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            m_sceneTabContent.pivot = new Vector2(0f, 0.5f);
            HorizontalLayoutGroup layoutGroup = m_sceneTabContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 6f;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = false;
            layoutGroup.childForceExpandHeight = true;
            layoutGroup.childForceExpandWidth = false;
            ContentSizeFitter fitter = m_sceneTabContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = m_sceneTabContent;
        }

        private void CreateGrid(RectTransform root)
        {
            Vector2 min = m_integratedMode ? new Vector2(8f, 8f) : new Vector2(16f, 16f);
            Vector2 max = m_integratedMode ? new Vector2(-156f, -124f) : new Vector2(-294f, -172f);
            RectTransform gridRoot = CreateRect("RoleGrid", root, Vector2.zero, Vector2.one, min, max);
            AddImage(gridRoot.gameObject, new Color32(225, 221, 178, 235));
            ScrollRect scrollRect = gridRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            RectTransform viewport = CreateRect("Viewport", gridRoot, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            Image viewportImage = AddImage(viewport.gameObject, Color.white);
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform content = CreateRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            m_gridView = gridRoot.gameObject.AddComponent<LoopGridView>();
            GameObject template = new GameObject(k_roleItemPrefabName, typeof(RectTransform), typeof(LoopGridViewItem), typeof(GalleryRoleItemView));
            template.transform.SetParent(gridRoot, false);
            template.GetComponent<RectTransform>().sizeDelta = new Vector2(132f, 112f);
            m_gridView.ItemPrefabDataList.Add(new GridViewItemPrefabConfData
            {
                mItemPrefab = template,
                mInitCreateCount = 20,
            });

            LoopGridViewSettingParam settings = new LoopGridViewSettingParam
            {
                mItemSize = new Vector2(132f, 112f),
                mItemPadding = new Vector2(8f, 8f),
                mPadding = new RectOffset(8, 8, 8, 8),
                mGridFixedType = GridFixedType.ColumnCountFixed,
                mFixedRowOrColumnCount = m_integratedMode ? 2 : 4,
            };
            m_gridView.InitGridView(0, GetGridItemByIndex, settings);

            m_emptyText = CreateText("EmptyState", gridRoot, 19f, TextAlignmentOptions.Center);
            SetRect(m_emptyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
            m_emptyText.SetText(Localize("Gallery.Empty", "沒有符合條件的已解鎖對象"));
        }

        private void CreatePreview(RectTransform root)
        {
            Vector2 min = m_integratedMode ? new Vector2(-148f, 8f) : new Vector2(-280f, 16f);
            Vector2 max = m_integratedMode ? new Vector2(-8f, -124f) : new Vector2(-16f, -172f);
            RectTransform preview = CreateRect("Preview", root, new Vector2(1f, 0f), Vector2.one, min, max);
            AddImage(preview.gameObject, new Color32(250, 239, 206, 235));

            m_previewImage = CreateImage("PreviewImage", preview);
            RectTransform imageRect = m_previewImage.rectTransform;
            imageRect.anchorMin = new Vector2(0.5f, 1f);
            imageRect.anchorMax = new Vector2(0.5f, 1f);
            imageRect.pivot = new Vector2(0.5f, 1f);
            imageRect.anchoredPosition = new Vector2(0f, m_integratedMode ? -8f : -22f);
            imageRect.sizeDelta = m_integratedMode ? new Vector2(72f, 72f) : new Vector2(158f, 158f);
            m_previewImage.preserveAspect = true;

            m_previewName = CreateText("PreviewName", preview, m_integratedMode ? 15f : 23f, TextAlignmentOptions.Center);
            SetRect(m_previewName.rectTransform, new Vector2(0f, 1f), Vector2.one, m_integratedMode ? new Vector2(6f, -104f) : new Vector2(14f, -226f), m_integratedMode ? new Vector2(-6f, -84f) : new Vector2(-14f, -184f));

            m_previewCardState = CreateText("PreviewCardState", preview, m_integratedMode ? 12f : 17f, TextAlignmentOptions.Center);
            SetRect(m_previewCardState.rectTransform, new Vector2(0f, 1f), Vector2.one, m_integratedMode ? new Vector2(6f, -124f) : new Vector2(14f, -270f), m_integratedMode ? new Vector2(-6f, -104f) : new Vector2(-14f, -228f));

            m_previewScene = CreateText("PreviewScene", preview, m_integratedMode ? 11f : 15f, TextAlignmentOptions.Center);
            SetRect(m_previewScene.rectTransform, new Vector2(0f, 1f), Vector2.one, m_integratedMode ? new Vector2(6f, -144f) : new Vector2(14f, -316f), m_integratedMode ? new Vector2(-6f, -124f) : new Vector2(-14f, -272f));

            m_resultText = CreateText("Result", preview, 14f, TextAlignmentOptions.Center);
            m_resultText.textWrappingMode = TextWrappingModes.Normal;
            SetRect(m_resultText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), m_integratedMode ? new Vector2(6f, 42f) : new Vector2(12f, 72f), m_integratedMode ? new Vector2(-6f, 62f) : new Vector2(-12f, 130f));

            m_confirmButton = CreateButton("Confirm", preview, string.Empty, out m_confirmButtonText);
            SetRect((RectTransform)m_confirmButton.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), m_integratedMode ? new Vector2(8f, 8f) : new Vector2(18f, 18f), m_integratedMode ? new Vector2(-8f, 36f) : new Vector2(-18f, 64f));
            m_confirmButton.onClick.AddListener(ConfirmSelection);
            m_confirmButtonText.SetText(Localize("Gallery.Confirm", "確認切換"));
        }

        private LoopGridViewItem GetGridItemByIndex(LoopGridView gridView, int itemIndex, int row, int column)
        {
            IReadOnlyList<GalleryRoleEntry> entries = m_catalogModel.VisibleEntries;
            if (itemIndex < 0 || itemIndex >= entries.Count)
                return null;

            LoopGridViewItem item = gridView.NewListViewItem(k_roleItemPrefabName);
            GalleryRoleItemView itemView = item.GetComponent<GalleryRoleItemView>();
            itemView.Bind(entries[itemIndex], m_selectedRoleKey, m_fontAsset, m_onRoleClicked);
            return item;
        }

        private void RebuildSceneTabs()
        {
            for (int i = m_sceneButtons.Count - 1; i >= 0; i--)
                Destroy(m_sceneButtons[i].gameObject);
            m_sceneButtons.Clear();
            m_sceneKeys.Clear();
            m_unlockedSceneKeys.Clear();

            if (m_snapshot?.roles != null)
            {
                for (int i = 0; i < m_snapshot.roles.Length; i++)
                {
                    GalleryRoleState state = m_snapshot.roles[i];
                    if (state == null || state.unlocked == false
                        || Database<RoleData>.TryLoad(state.roleKey, out RoleData roleData) == false)
                    {
                        continue;
                    }

                    string sceneKey = roleData.SceneReference.GetKey();
                    if (string.IsNullOrEmpty(sceneKey) == false)
                        m_unlockedSceneKeys.Add(sceneKey);
                }
            }

            if (string.IsNullOrEmpty(m_selectedSceneKey) == false
                && m_unlockedSceneKeys.Contains(m_selectedSceneKey) == false)
            {
                m_selectedSceneKey = string.Empty;
                m_catalogModel.ApplyView(
                    m_sortMode,
                    m_selectedSceneKey,
                    m_missingCardOnly,
                    m_tomatoIncompleteOnly);
            }

            AddSceneTab(string.Empty, Localize("Gallery.All", "全部"), null);
            Dictionary<string, ScenemapData>.ValueCollection sceneDatas = Database<ScenemapData>.GetAll();
            if (sceneDatas == null)
                return;

            List<ScenemapData> sortedScenes = new List<ScenemapData>(sceneDatas);
            sortedScenes.Sort(static (left, right) => left.SceneMapNo.CompareTo(right.SceneMapNo));
            for (int i = 0; i < sortedScenes.Count; i++)
            {
                ScenemapData sceneData = sortedScenes[i];
                if (m_unlockedSceneKeys.Contains(sceneData.key))
                    AddSceneTab(sceneData.key, sceneData.ScenemapName, sceneData.LevelSelectionThumbnail);
            }
        }

        private void AddSceneTab(string sceneKey, string label, Sprite thumbnail)
        {
            Button button = CreateButton("SceneTab", m_sceneTabContent, label, out TextMeshProUGUI labelText);
            LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = thumbnail != null ? 152f : 118f;

            if (thumbnail != null)
            {
                Image thumbnailImage = CreateImage("Thumbnail", button.transform);
                thumbnailImage.sprite = thumbnail;
                thumbnailImage.preserveAspect = true;
                thumbnailImage.raycastTarget = false;
                SetRect(
                    thumbnailImage.rectTransform,
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(6f, 4f),
                    new Vector2(42f, -4f));
                labelText.alignment = TextAlignmentOptions.MidlineLeft;
                SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(48f, 3f), new Vector2(-6f, -3f));
            }

            int sceneIndex = m_sceneKeys.Count;
            m_sceneKeys.Add(sceneKey);
            m_sceneButtons.Add(button);
            button.onClick.AddListener(() => SelectScene(sceneIndex));
            UpdateSceneButtonColor(button, string.Equals(sceneKey, m_selectedSceneKey, StringComparison.Ordinal));
        }

        private void SelectScene(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= m_sceneKeys.Count)
                return;

            m_selectedSceneKey = m_sceneKeys[sceneIndex];
            for (int i = 0; i < m_sceneButtons.Count; i++)
                UpdateSceneButtonColor(m_sceneButtons[i], i == sceneIndex);
            ApplyCurrentView();
        }

        private static void UpdateSceneButtonColor(Button button, bool selected)
        {
            Image image = button.targetGraphic as Image;
            if (image != null)
                image.color = selected ? new Color32(176, 191, 129, 255) : new Color32(246, 234, 197, 255);
        }

        private void OnRoleClicked(GalleryRoleEntry entry)
        {
            m_selectedRoleKey = entry.RoleData.key;
            m_gridView.RefreshAllShownItem();
            RefreshPreview();
            SetResult(string.Empty, false);
        }

        private void CycleSortMode()
        {
            m_sortMode = (GallerySortMode)(((int)m_sortMode + 1) % 3);
            ApplyCurrentView();
        }

        private void ToggleMissingCardFilter()
        {
            m_missingCardOnly = !m_missingCardOnly;
            ApplyCurrentView();
        }

        private void ToggleTomatoFilter()
        {
            m_tomatoIncompleteOnly = !m_tomatoIncompleteOnly;
            ApplyCurrentView();
        }

        private void ConfirmSelection()
        {
            if (string.IsNullOrEmpty(m_selectedRoleKey) || m_pendingRequest != null)
                return;

            if (m_integratedMode)
            {
                m_battlePanel ??= FindFirstObjectByType<BattlePanel>();
                if (m_battlePanel == null)
                {
                    SetResult(Localize("Gallery.NotReady", "桌寵尚未完成初始化，請稍後再試。"), true);
                    return;
                }

                bool accepted = m_battlePanel.TrySelectRoleFromGallery(m_selectedRoleKey, out string reason);
                if (accepted)
                {
                    SetResult(Localize("Gallery.SwitchSuccess", "已切換對象"), false);
                }
                else
                {
                    ApplySelectionFailure(reason);
                }
                return;
            }

            m_pendingRequest = new GallerySelectRoleRequest
            {
                requestId = m_nextRequestId++,
                roleKey = m_selectedRoleKey,
            };
            m_pendingRequestDeadline = Time.unscaledTime + k_connectionTimeout;
            m_pendingRequestSent = false;
            SetResult(Localize("Gallery.Connecting", "正在連接桌寵…"), false);

            if (m_endpoint.IsConnected == false)
                GalleryProcessLauncher.OpenDesktopPet();

            TrySendPendingRequest();
        }

        private void TrySendPendingRequest()
        {
            if (m_pendingRequest == null || m_pendingRequestSent || m_endpoint.IsConnected == false)
                return;

            m_pendingRequestSent = true;
            m_endpoint.Send(GalleryProtocol.SelectRoleRequest, m_pendingRequest);
            SetResult(Localize("Gallery.Switching", "正在切換對象…"), false);
        }

        private void ApplySelectRoleResult(GallerySelectRoleResult result)
        {
            if (m_pendingRequest == null || result.requestId != m_pendingRequest.requestId)
                return;

            m_pendingRequest = null;
            m_pendingRequestSent = false;
            if (result.accepted)
            {
                SetResult(Localize("Gallery.SwitchSuccess", "已切換對象"), false);
                return;
            }

            ApplySelectionFailure(result.reason);
        }

        private void ApplySelectionFailure(string reason)
        {
            string message = reason switch
            {
                "role_transition" => "對象正在進退場，請稍後再切換。",
                "normal_mode_only" => Localize("Gallery.NormalModeOnly", "只能在一般模式切換對象。"),
                "role_locked" => Localize("Gallery.RoleLocked", "此對象目前尚未解鎖。"),
                "role_not_found" => Localize("Gallery.RoleNotFound", "找不到此對象資料。"),
                _ => Localize("Gallery.NotReady", "桌寵尚未完成初始化，請稍後再試。"),
            };
            SetResult(message, true);
        }

        private void RefreshGrid(bool resetPosition)
        {
            int count = m_catalogModel.VisibleEntries.Count;
            m_gridView.SetListItemCount(count, resetPosition);
            m_gridView.RefreshAllShownItem();
            m_emptyText.gameObject.SetActive(count == 0);
        }

        private void RefreshPreview()
        {
            GalleryRoleEntry selectedEntry = null;
            IReadOnlyList<GalleryRoleEntry> entries = m_catalogModel.VisibleEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].RoleData.key, m_selectedRoleKey, StringComparison.Ordinal))
                {
                    selectedEntry = entries[i];
                    break;
                }
            }

            bool hasSelection = selectedEntry != null;
            m_previewImage.enabled = hasSelection && selectedEntry.RoleData.EnemyIcon != null;
            m_previewImage.sprite = hasSelection ? selectedEntry.RoleData.EnemyIcon : null;
            m_previewName.SetText(hasSelection ? selectedEntry.RoleData.RoleName : string.Empty);
            m_previewCardState.SetText(hasSelection
                ? selectedEntry.CardReward
                    ? Localize("Gallery.CardObtained", "卡片：已取得")
                    : Localize("Gallery.CardMissing", "卡片：未取得")
                : string.Empty);
            m_previewScene.SetText(hasSelection && selectedEntry.SceneData != null
                ? selectedEntry.SceneData.ScenemapName
                : string.Empty);
            m_confirmButton.interactable = hasSelection && m_pendingRequest == null;
        }

        private void RefreshControlLabels()
        {
            string sortLabel = m_sortMode switch
            {
                GallerySortMode.MissingCardFirst => Localize("Gallery.SortMissing", "缺卡優先"),
                GallerySortMode.TomatoIncompleteFirst => Localize("Gallery.SortTomato", "番茄未完成優先"),
                _ => Localize("Gallery.SortOrder", "固定順序"),
            };
            m_sortButtonText.SetText($"{Localize("Gallery.Sort", "排序")}：{sortLabel}");
            m_missingFilterText.SetText(
                $"{(m_missingCardOnly ? "✓" : "□")} {Localize("Gallery.FilterMissing", "只看缺卡")}");
            m_tomatoFilterText.SetText(
                $"{(m_tomatoIncompleteOnly ? "✓" : "□")} {Localize("Gallery.FilterTomato", "只看番茄未完成")}");
        }

        private void SetConnectionState(bool connected)
        {
            if (m_connectionText == null)
                return;

            m_connectionText.SetText(m_integratedMode
                ? "● 已整合至桌寵"
                : connected
                    ? Localize("Gallery.Online", "● 桌寵已連線")
                    : Localize("Gallery.Offline", "○ 離線瀏覽"));
            m_connectionText.color = connected
                ? new Color32(72, 104, 44, 255)
                : new Color32(113, 93, 67, 255);
        }

        private void SetResult(string message, bool isError)
        {
            if (m_resultText == null)
                return;
            m_resultText.SetText(message ?? string.Empty);
            m_resultText.color = isError ? new Color32(160, 57, 32, 255) : new Color32(70, 40, 23, 255);
            RefreshPreview();
        }

        private string Localize(string key, string fallback)
        {
            string localized = LocalizationManager.instance.GetLocalization(key);
            return string.IsNullOrEmpty(localized) || string.Equals(localized, key, StringComparison.Ordinal)
                ? fallback
                : localized;
        }

        private void CreateEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }

        private RectTransform CreateRect(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            SetRect(rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
            return rectTransform;
        }

        private static void SetRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static Image AddImage(GameObject gameObject, Color color)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Image CreateImage(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Image>();
        }

        private TextMeshProUGUI CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            if (m_fontAsset != null)
                text.font = m_fontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color32(70, 40, 23, 255);
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private Button CreateButton(
            string objectName,
            Transform parent,
            string label,
            out TextMeshProUGUI labelText)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = new Color32(246, 234, 197, 255);
            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            gameObject.AddComponent<UIButton>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color32(255, 248, 222, 255);
            colors.pressedColor = new Color32(204, 186, 143, 255);
            colors.selectedColor = new Color32(196, 207, 158, 255);
            colors.disabledColor = new Color32(192, 180, 155, 140);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            labelText = CreateText("Text", rectTransform, 15f, TextAlignmentOptions.Center);
            SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 3f), new Vector2(-6f, -3f));
            labelText.SetText(label);
            return button;
        }

        private void AddButtonIcon(Button button, TextMeshProUGUI labelText, Sprite sprite)
        {
            if (sprite == null)
                return;

            Image icon = CreateImage("Icon", button.transform);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(
                icon.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(6f, -18f),
                new Vector2(42f, 18f));
            labelText.rectTransform.offsetMin = new Vector2(42f, 3f);
        }

        private void ApplySavedWindowSize()
        {
            int width = PlayerPrefs.GetInt("Gallery.Window.Width", k_defaultWindowWidth);
            int height = PlayerPrefs.GetInt("Gallery.Window.Height", k_defaultWindowHeight);
            Screen.SetResolution(Mathf.Max(720, width), Mathf.Max(520, height), false);
        }

        private IEnumerator ApplySavedWindowPosition()
        {
            yield return null;
            yield return null;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            IntPtr windowHandle = Process.GetCurrentProcess().MainWindowHandle;
            if (windowHandle == IntPtr.Zero)
                yield break;

            int x = PlayerPrefs.GetInt("Gallery.Window.X", int.MinValue);
            int y = PlayerPrefs.GetInt("Gallery.Window.Y", int.MinValue);
            if (x != int.MinValue && y != int.MinValue)
            {
                SetWindowPos(windowHandle, IntPtr.Zero, x, y, 0, 0, k_swpNoSize | k_swpNoZOrder | k_swpShowWindow);
            }
#endif
        }

        private void FocusWindow()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            IntPtr windowHandle = Process.GetCurrentProcess().MainWindowHandle;
            if (windowHandle == IntPtr.Zero)
                return;
            ShowWindow(windowHandle, k_swRestore);
            SetForegroundWindow(windowHandle);
#endif
        }

        private void SaveWindowBounds()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            IntPtr windowHandle = Process.GetCurrentProcess().MainWindowHandle;
            if (windowHandle == IntPtr.Zero || GetWindowRect(windowHandle, out NativeRect rect) == false)
                return;

            PlayerPrefs.SetInt("Gallery.Window.X", rect.left);
            PlayerPrefs.SetInt("Gallery.Window.Y", rect.top);
            PlayerPrefs.SetInt("Gallery.Window.Width", Mathf.Max(720, rect.right - rect.left));
            PlayerPrefs.SetInt("Gallery.Window.Height", Mathf.Max(520, rect.bottom - rect.top));
            PlayerPrefs.Save();
#endif
        }

        private void OnApplicationQuit()
        {
            if (m_integratedMode == false)
                SaveWindowBounds();
            m_endpoint?.Dispose();
        }

        private void OnDestroy()
        {
            if (m_integratedMode)
            {
                GalleryStateChangeNotifier.StateChanged -= OnIntegratedStateChanged;
                if (IntegratedInstance == this)
                    IntegratedInstance = null;
                if (m_windowController != null && m_windowController.GalleryExpanded)
                {
                    m_windowController.SetGalleryExpanded(false);
                    SetIntegratedGameplayVisible(true);
                }
            }
            m_endpoint?.Dispose();
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private struct NativeRect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private const uint k_swpNoSize = 0x0001;
        private const uint k_swpNoZOrder = 0x0004;
        private const uint k_swpShowWindow = 0x0040;
        private const int k_swRestore = 9;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int command);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);
#endif
    }
}
