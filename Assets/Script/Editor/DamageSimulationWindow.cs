using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using GameCore.Database;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MassageCat.EditorTools
{
    internal sealed class DamageSimulationWindow : EditorWindow
    {
        private const string k_roleRepositoryPath = "Assets/AssetResources/Database/Lusbi/RoleRepository.asset";
        private const string k_scenemapRepositoryPath = "Assets/AssetResources/Database/Lusbi/ScenemapRepository.asset";
        private const string k_gameDefaultSettingPath = "Assets/AssetResources/Setting/GameDefaultSetting.asset";
        private const string k_tomatoSettingPath = "Assets/AssetResources/Setting/TomatoSetting.asset";
        private const string k_spellQuestionPath = "Assets/AssetResources/Setting/SpellQuestion.asset";
        private const string k_inBattlePrefabPath = "Assets/2DArt/UI/InBattle.prefab";

        private const int k_expectedRoleCount = 58;
        private const int k_expectedZoneCount = 11;
        private const double k_defaultInputsPerSecond = 5d;
        private const double k_defaultWordsPerMinute = 40d;

        private const float k_sortWidth = 44f;
        private const float k_zoneWidth = 108f;
        private const float k_roleWidth = 190f;
        private const float k_requiredHealValueWidth = 135f;
        private const float k_routeWidth = 205f;
        private const float k_rowHeight = 50f;

        private static readonly Comparison<RoleData> s_roleComparison = CompareRoles;
        private static readonly List<string> s_routeNames = new List<string>
        {
            "\u666e\u901a",
            "\u5b57\u5f59",
            "\u756a\u8304",
        };

        [SerializeField] private DamageSimulationRoute m_referenceRoute = DamageSimulationRoute.Normal;
        [SerializeField] private double m_targetHours;
        [SerializeField] private double m_inputsPerSecond = k_defaultInputsPerSecond;
        [SerializeField] private double m_wordsPerMinute = k_defaultWordsPerMinute;
        [SerializeField] private int m_baseHeal = 1;
        [SerializeField] private int m_spellMultiplier = 50;
        [SerializeField] private int m_debugMultiplier = 1;
        [SerializeField] private bool m_hasInitializedState;
        [SerializeField] private bool m_isUsingOriginalPreview = true;

        private readonly List<string> m_dataErrors = new List<string>(8);
        private readonly List<string> m_dataWarnings = new List<string>(4);
        private readonly StringBuilder m_csvBuilder = new StringBuilder(32768);

        private DamageSimulationRoleInput[] m_roles = Array.Empty<DamageSimulationRoleInput>();
        private BigInteger[] m_originalRequiredHealValues = Array.Empty<BigInteger>();
        private BigInteger[] m_previewRequiredHealValues = Array.Empty<BigInteger>();
        private BigInteger[] m_candidateRequiredHealValues = Array.Empty<BigInteger>();
        private DamageSimulationResult m_result;

        private int m_spellUnlockSortId;
        private int m_tomatoUnlockSortId;
        private int m_zoneCount;
        private decimal m_bestMultiplier = decimal.One;
        private decimal m_achievedSeconds;
        private bool m_calculationValid;
        private string m_calculationError = string.Empty;

        private DropdownField m_referenceRouteField;
        private DoubleField m_targetHoursField;
        private DoubleField m_inputsPerSecondField;
        private DoubleField m_wordsPerMinuteField;
        private IntegerField m_baseHealField;
        private IntegerField m_spellMultiplierField;
        private IntegerField m_debugMultiplierField;
        private VisualElement m_dataMessageContainer;
        private HelpBox m_calculationHelpBox;
        private Label m_statusLabel;
        private Label m_normalSummaryLabel;
        private Label m_spellSummaryLabel;
        private Label m_tomatoSummaryLabel;
        private ListView m_roleListView;
        private Button m_exportButton;
        private bool m_isUpdatingControls;

        [MenuItem("Tools/Massage Cat/Damage Simulator")]
        private static void OpenWindow()
        {
            DamageSimulationWindow window = GetWindow<DamageSimulationWindow>();
            window.titleContent = new GUIContent("Damage Simulator");
            window.minSize = new UnityEngine.Vector2(1260f, 560f);
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            ReloadDataInternal(!m_hasInitializedState);
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 8f;
            rootVisualElement.style.paddingRight = 8f;
            rootVisualElement.style.paddingTop = 6f;
            rootVisualElement.style.paddingBottom = 6f;

            rootVisualElement.Add(CreateHeader());
            rootVisualElement.Add(CreateInputPanel());

            m_dataMessageContainer = new VisualElement();
            m_dataMessageContainer.style.marginTop = 4f;
            rootVisualElement.Add(m_dataMessageContainer);

            m_calculationHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            m_calculationHelpBox.style.marginTop = 4f;
            rootVisualElement.Add(m_calculationHelpBox);

            rootVisualElement.Add(CreateSummaryPanel());
            rootVisualElement.Add(CreateListHeader());

            m_roleListView = new ListView
            {
                itemsSource = m_roles,
                makeItem = CreateRoleRow,
                bindItem = BindRoleRow,
                fixedItemHeight = k_rowHeight,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.None,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
            };
            m_roleListView.style.flexGrow = 1f;
            rootVisualElement.Add(m_roleListView);

            UpdateControlsWithoutNotify();
            RefreshOutputUI();
        }

        private VisualElement CreateHeader()
        {
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            Label title = new Label("Massage Cat \u50b7\u5bb3\u8207\u901a\u95dc\u6642\u9593\u9810\u89bd");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16f;
            title.style.flexGrow = 1f;
            header.Add(title);

            m_statusLabel = new Label();
            m_statusLabel.style.marginRight = 8f;
            header.Add(m_statusLabel);

            Button reloadButton = new Button(ReloadFromAssets)
            {
                text = "\u91cd\u65b0\u8f09\u5165\u8cc7\u7522",
            };
            header.Add(reloadButton);

            m_exportButton = new Button(ExportCsv)
            {
                text = "\u532f\u51fa CSV",
            };
            m_exportButton.style.marginLeft = 4f;
            header.Add(m_exportButton);
            return header;
        }

        private VisualElement CreateInputPanel()
        {
            VisualElement panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            panel.style.flexWrap = Wrap.Wrap;
            panel.style.marginTop = 6f;
            panel.style.paddingTop = 6f;
            panel.style.paddingBottom = 2f;
            panel.style.borderTopWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderTopColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            panel.style.borderBottomColor = new Color(0.25f, 0.25f, 0.25f, 1f);

            m_referenceRouteField = new DropdownField("\u57fa\u6e96\u8def\u7dda", s_routeNames, (int)m_referenceRoute);
            ConfigureInputField(m_referenceRouteField, 170f);
            m_referenceRouteField.RegisterValueChangedCallback(OnReferenceRouteChanged);
            panel.Add(m_referenceRouteField);

            m_targetHoursField = new DoubleField("\u76ee\u6a19\u5c0f\u6642");
            ConfigureInputField(m_targetHoursField, 180f);
            m_targetHoursField.RegisterValueChangedCallback(OnTargetHoursChanged);
            panel.Add(m_targetHoursField);

            m_inputsPerSecondField = new DoubleField("\u666e\u901a\uff0f\u756a\u8304\u8f38\u5165\u6b21\u6578\uff0f\u79d2");
            ConfigureInputField(m_inputsPerSecondField, 225f);
            m_inputsPerSecondField.RegisterValueChangedCallback(OnInputsPerSecondChanged);
            panel.Add(m_inputsPerSecondField);

            m_wordsPerMinuteField = new DoubleField("\u6210\u529f WPM");
            ConfigureInputField(m_wordsPerMinuteField, 170f);
            m_wordsPerMinuteField.RegisterValueChangedCallback(OnWordsPerMinuteChanged);
            panel.Add(m_wordsPerMinuteField);

            m_baseHealField = new IntegerField("\u57fa\u790e\u7642\u7652\u503c");
            ConfigureInputField(m_baseHealField, 160f);
            m_baseHealField.RegisterValueChangedCallback(OnBaseHealChanged);
            panel.Add(m_baseHealField);

            m_spellMultiplierField = new IntegerField("\u5b57\u5f59\u500d\u7387");
            ConfigureInputField(m_spellMultiplierField, 160f);
            m_spellMultiplierField.RegisterValueChangedCallback(OnSpellMultiplierChanged);
            panel.Add(m_spellMultiplierField);

            m_debugMultiplierField = new IntegerField("Debug \u500d\u7387");
            ConfigureInputField(m_debugMultiplierField, 165f);
            m_debugMultiplierField.RegisterValueChangedCallback(OnDebugMultiplierChanged);
            panel.Add(m_debugMultiplierField);

            return panel;
        }

        private VisualElement CreateSummaryPanel()
        {
            VisualElement panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            panel.style.marginTop = 6f;
            panel.style.marginBottom = 6f;

            m_normalSummaryLabel = CreateSummaryLabel();
            m_spellSummaryLabel = CreateSummaryLabel();
            m_tomatoSummaryLabel = CreateSummaryLabel();
            panel.Add(m_normalSummaryLabel);
            panel.Add(m_spellSummaryLabel);
            panel.Add(m_tomatoSummaryLabel);
            return panel;
        }

        private static Label CreateSummaryLabel()
        {
            Label label = new Label();
            label.style.flexGrow = 1f;
            label.style.flexBasis = 0f;
            label.style.minHeight = 58f;
            label.style.paddingLeft = 8f;
            label.style.paddingRight = 8f;
            label.style.paddingTop = 5f;
            label.style.paddingBottom = 5f;
            label.style.marginRight = 4f;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);
            return label;
        }

        private static VisualElement CreateListHeader()
        {
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.height = 24f;
            header.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            header.Add(CreateHeaderCell("#", k_sortWidth));
            header.Add(CreateHeaderCell("\u5340\u57df", k_zoneWidth));
            header.Add(CreateHeaderCell("\u89d2\u8272", k_roleWidth));
            header.Add(CreateHeaderCell("\u76ee\u524d\u7642\u7652\u9700\u6c42\u503c", k_requiredHealValueWidth));
            header.Add(CreateHeaderCell("\u5efa\u8b70\u7642\u7652\u9700\u6c42\u503c", k_requiredHealValueWidth));
            header.Add(CreateHeaderCell("\u666e\u901a\u8def\u7dda", k_routeWidth));
            header.Add(CreateHeaderCell("\u5b57\u5f59\u8def\u7dda", k_routeWidth));
            header.Add(CreateHeaderCell("\u756a\u8304\u8def\u7dda", k_routeWidth));
            return header;
        }

        private static Label CreateHeaderCell(string text, float width)
        {
            Label label = new Label(text);
            label.style.width = width;
            label.style.minWidth = width;
            label.style.maxWidth = width;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.paddingLeft = 4f;
            return label;
        }

        private static VisualElement CreateRoleRow()
        {
            return new RoleRowElement();
        }

        private void BindRoleRow(VisualElement element, int index)
        {
            if (!(element is RoleRowElement row) || index < 0 || index >= m_roles.Length)
                return;

            DamageSimulationRoleResult roleResult = m_result != null && index < m_result.RoleResults.Length
                ? m_result.RoleResults[index]
                : default;
            BigInteger previewRequiredHealValue = index < m_previewRequiredHealValues.Length
                ? m_previewRequiredHealValues[index]
                : BigInteger.Zero;
            row.Bind(m_roles[index], previewRequiredHealValue, roleResult, m_calculationValid);
        }

        private static void ConfigureInputField(BaseField<string> field, float width)
        {
            field.style.width = width;
            field.style.marginRight = 6f;
            field.style.marginBottom = 4f;
        }

        private static void ConfigureInputField(BaseField<double> field, float width)
        {
            field.style.width = width;
            field.style.marginRight = 6f;
            field.style.marginBottom = 4f;
        }

        private static void ConfigureInputField(BaseField<int> field, float width)
        {
            field.style.width = width;
            field.style.marginRight = 6f;
            field.style.marginBottom = 4f;
        }

        private void ReloadFromAssets()
        {
            ReloadDataInternal(true);
            if (m_roleListView != null)
            {
                m_roleListView.itemsSource = m_roles;
                m_roleListView.Rebuild();
            }
            UpdateControlsWithoutNotify();
            RefreshOutputUI();
        }

        private void ReloadDataInternal(bool resetInputs)
        {
            m_dataErrors.Clear();
            m_dataWarnings.Clear();
            m_zoneCount = 0;
            m_spellUnlockSortId = 0;
            m_tomatoUnlockSortId = 0;

            GameDefaultSetting gameSetting = AssetDatabase.LoadAssetAtPath<GameDefaultSetting>(k_gameDefaultSettingPath);
            TomatoSetting tomatoSetting = AssetDatabase.LoadAssetAtPath<TomatoSetting>(k_tomatoSettingPath);
            SpellQuestion spellQuestion = AssetDatabase.LoadAssetAtPath<SpellQuestion>(k_spellQuestionPath);
            GameObject inBattlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_inBattlePrefabPath);

            if (!gameSetting)
                m_dataErrors.Add($"\u627e\u4e0d\u5230 GameDefaultSetting\uff1a{k_gameDefaultSettingPath}");
            if (!tomatoSetting)
                m_dataErrors.Add($"\u627e\u4e0d\u5230 TomatoSetting\uff1a{k_tomatoSettingPath}");
            if (!spellQuestion)
                m_dataErrors.Add($"\u627e\u4e0d\u5230 SpellQuestion\uff1a{k_spellQuestionPath}");
            if (!inBattlePrefab)
                m_dataErrors.Add($"\u627e\u4e0d\u5230\u6230\u9b25 Prefab\uff1a{k_inBattlePrefabPath}");

            List<RoleData> roleAssets = LoadRoleAssets();
            Dictionary<string, int> sceneNumbers = LoadSceneNumbers();
            Dictionary<string, decimal> tomatoDurations = BuildTomatoDurationMap(tomatoSetting);
            HashSet<int> spellLevels = BuildSpellLevelSet(spellQuestion);
            LoadUnlockSortIds(inBattlePrefab, roleAssets);
            BuildRoleInputs(roleAssets, sceneNumbers, tomatoDurations, spellLevels);

            if (m_roles.Length != k_expectedRoleCount)
                m_dataWarnings.Add($"\u76ee\u524d\u8b80\u53d6 {m_roles.Length} \u540d\u89d2\u8272\uff1b\u5efa\u8b70\u78ba\u8a8d\u662f\u5426\u4ecd\u70ba\u9810\u671f\u7684 {k_expectedRoleCount} \u540d\u3002");
            if (m_zoneCount != k_expectedZoneCount)
                m_dataWarnings.Add($"\u76ee\u524d\u8b80\u53d6 {m_zoneCount} \u500b\u5340\u57df\uff1b\u5efa\u8b70\u78ba\u8a8d\u662f\u5426\u4ecd\u70ba\u9810\u671f\u7684 {k_expectedZoneCount} \u500b\u3002");

            if (resetInputs || !m_hasInitializedState)
            {
                m_referenceRoute = DamageSimulationRoute.Normal;
                m_inputsPerSecond = k_defaultInputsPerSecond;
                m_wordsPerMinute = k_defaultWordsPerMinute;
                m_baseHeal = gameSetting ? gameSetting.defaultClickHeal : 1;
                m_spellMultiplier = gameSetting ? gameSetting.defaultSpellHeal : 50;
                m_debugMultiplier = gameSetting ? gameSetting.debugHealRate : 1;
                m_isUsingOriginalPreview = true;
                m_hasInitializedState = true;
            }

            AllocateCalculationBuffers();
            CopyRequiredHealValues(m_originalRequiredHealValues, m_previewRequiredHealValues);

            if (m_dataErrors.Count > 0 || !TryBuildSettings(out DamageSimulationSettings settings))
            {
                m_calculationValid = false;
                m_calculationError = m_dataErrors.Count > 0
                    ? "\u5fc5\u8981\u8cc7\u6599\u7f3a\u5931\uff0c\u8acb\u5148\u4fee\u6b63\u4e0b\u65b9\u932f\u8aa4\u3002"
                    : "\u6a21\u64ec\u53c3\u6578\u7121\u6548\uff0c\u6578\u503c\u5fc5\u9808\u5927\u65bc 0\u3002";
                return;
            }

            DamageSimulationCalculator.CalculateAll(m_roles, m_originalRequiredHealValues, settings, m_result);
            DamageSimulationRouteResult normalResult = m_result.Normal;
            if (resetInputs || m_targetHours <= 0d || double.IsNaN(m_targetHours) || double.IsInfinity(m_targetHours))
            {
                m_targetHours = normalResult.IsValid && !normalResult.DurationOverflow
                    ? (double)(normalResult.TotalSeconds / 3600m)
                    : 1d;
            }

            if (m_isUsingOriginalPreview)
            {
                m_bestMultiplier = decimal.One;
                m_achievedSeconds = m_result.GetRoute(m_referenceRoute).TotalSeconds;
                ValidateFinalResult();
            }
            else
            {
                RecalculateFromTarget();
            }
        }

        private List<RoleData> LoadRoleAssets()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(k_roleRepositoryPath);
            List<RoleData> roles = new List<RoleData>(assets.Length);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is RoleData role)
                    roles.Add(role);
            }

            if (roles.Count == 0)
            {
                m_dataErrors.Add($"RoleRepository \u4e2d\u627e\u4e0d\u5230\u4efb\u4f55\u89d2\u8272\uff1a{k_roleRepositoryPath}");
                return roles;
            }

            roles.Sort(s_roleComparison);
            for (int i = 0; i < roles.Count; i++)
            {
                int expectedSortId = i + 1;
                if (roles[i].roleSortId != expectedSortId)
                {
                    m_dataErrors.Add($"\u89d2\u8272\u6392\u5e8f\u7f3a\u5931\u6216\u91cd\u8907\uff1b\u7d22\u5f15 {i} \u9810\u671f roleSortId={expectedSortId}\uff0c\u5be6\u969b\u70ba {roles[i].roleSortId}\uff08{roles[i].key}\uff09\u3002");
                    break;
                }
            }
            return roles;
        }

        private Dictionary<string, int> LoadSceneNumbers()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(k_scenemapRepositoryPath);
            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < assets.Length; i++)
            {
                if (!(assets[i] is ScenemapData scene))
                    continue;

                if (result.ContainsKey(scene.key))
                {
                    m_dataErrors.Add($"ScenemapRepository \u5305\u542b\u91cd\u8907 Key\uff1a{scene.key}");
                    continue;
                }
                result.Add(scene.key, scene.SceneMapNo);
            }

            if (result.Count == 0)
                m_dataErrors.Add($"ScenemapRepository \u4e2d\u627e\u4e0d\u5230\u4efb\u4f55\u5340\u57df\uff1a{k_scenemapRepositoryPath}");
            return result;
        }

        private Dictionary<string, decimal> BuildTomatoDurationMap(TomatoSetting setting)
        {
            Dictionary<string, decimal> result = new Dictionary<string, decimal>(StringComparer.Ordinal);
            if (!setting || setting.tomatoReferences == null)
                return result;

            TomatoReference[] references = setting.tomatoReferences;
            for (int i = 0; i < references.Length; i++)
            {
                TomatoReference reference = references[i];
                if (reference == null || reference.roleKeys == null)
                    continue;

                decimal seconds = decimal.Round((decimal)reference.minute * 60m, 4, MidpointRounding.AwayFromZero);
                for (int j = 0; j < reference.roleKeys.Length; j++)
                {
                    string roleKey = reference.roleKeys[j];
                    if (string.IsNullOrEmpty(roleKey))
                        continue;
                    if (result.ContainsKey(roleKey))
                    {
                        m_dataErrors.Add($"TomatoSetting \u5305\u542b\u91cd\u8907\u89d2\u8272 Key\uff1a{roleKey}");
                        continue;
                    }
                    result.Add(roleKey, seconds);
                }
            }
            return result;
        }

        private static HashSet<int> BuildSpellLevelSet(SpellQuestion spellQuestion)
        {
            HashSet<int> result = new HashSet<int>();
            if (!spellQuestion || spellQuestion.levelQuestionInfos == null)
                return result;

            List<LevelQuestionInfo> levels = spellQuestion.levelQuestionInfos;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelQuestionInfo level = levels[i];
                if (level != null && level.questions != null && level.questions.Count > 0)
                    result.Add(level.level);
            }
            return result;
        }

        private void LoadUnlockSortIds(GameObject inBattlePrefab, List<RoleData> roles)
        {
            if (!inBattlePrefab)
                return;

            TomatoButton tomatoButton = inBattlePrefab.GetComponentInChildren<TomatoButton>(true);
            SpellButton spellButton = inBattlePrefab.GetComponentInChildren<SpellButton>(true);
            if (!tomatoButton)
                m_dataErrors.Add("InBattle.prefab \u627e\u4e0d\u5230 TomatoButton\u3002");
            if (!spellButton)
                m_dataErrors.Add("InBattle.prefab \u627e\u4e0d\u5230 SpellButton\u3002");

            string tomatoFlagKey = ReadUnlockFlagKey(tomatoButton);
            string spellFlagKey = ReadUnlockFlagKey(spellButton);
            m_tomatoUnlockSortId = FindRoleSortIdForFlag(roles, tomatoFlagKey);
            m_spellUnlockSortId = FindRoleSortIdForFlag(roles, spellFlagKey);

            if (m_tomatoUnlockSortId <= 0)
                m_dataErrors.Add($"\u627e\u4e0d\u5230\u756a\u8304\u89e3\u9396\u65d7\u6a19\u5c0d\u61c9\u7684\u89d2\u8272\uff1a{tomatoFlagKey}");
            if (m_spellUnlockSortId <= 0)
                m_dataErrors.Add($"\u627e\u4e0d\u5230\u5b57\u5f59\u89e3\u9396\u65d7\u6a19\u5c0d\u61c9\u7684\u89d2\u8272\uff1a{spellFlagKey}");
        }

        private void BuildRoleInputs(
            List<RoleData> roleAssets,
            Dictionary<string, int> sceneNumbers,
            Dictionary<string, decimal> tomatoDurations,
            HashSet<int> spellLevels)
        {
            m_roles = new DamageSimulationRoleInput[roleAssets.Count];
            m_originalRequiredHealValues = new BigInteger[roleAssets.Count];
            HashSet<string> zones = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < roleAssets.Count; i++)
            {
                RoleData role = roleAssets[i];
                string zone = role.SceneReference != null ? role.SceneReference.GetKey() : string.Empty;
                if (string.IsNullOrEmpty(zone))
                    m_dataErrors.Add($"\u89d2\u8272\u7f3a\u5c11\u5340\u57df\u53c3\u8003\uff1a{role.key}");
                else
                    zones.Add(zone);

                BigInteger requiredHealValue = BigInteger.Zero;
                try
                {
                    requiredHealValue = role.RequiredHealValue.Value;
                }
                catch (Exception exception)
                {
                    m_dataErrors.Add($"\u7121\u6cd5\u89e3\u6790\u89d2\u8272\u7642\u7652\u9700\u6c42\u503c\uff1a{role.key}\uff08{exception.Message}\uff09");
                }
                if (requiredHealValue <= BigInteger.Zero)
                    m_dataErrors.Add($"\u89d2\u8272\u7642\u7652\u9700\u6c42\u503c\u5fc5\u9808\u5927\u65bc 0\uff1a{role.key}");

                decimal tomatoSeconds = decimal.Zero;
                if (!tomatoDurations.TryGetValue(role.key, out tomatoSeconds) || tomatoSeconds <= decimal.Zero)
                    m_dataErrors.Add($"TomatoSetting \u7f3a\u5c11\u89d2\u8272\u6642\u9593\uff1a{role.key}");

                bool hasSpellQuestions = sceneNumbers.TryGetValue(zone, out int sceneNumber) && spellLevels.Contains(sceneNumber);
                if (role.roleSortId > m_spellUnlockSortId && !hasSpellQuestions)
                    m_dataErrors.Add($"\u5b57\u5f59\u8def\u7dda\u7f3a\u5c11\u5c0d\u61c9\u984c\u5eab\uff1a{role.key} / {zone}");

                m_roles[i] = new DamageSimulationRoleInput
                {
                    SortId = role.roleSortId,
                    Zone = zone,
                    RoleKey = role.key,
                    OriginalRequiredHealValue = requiredHealValue,
                    SatisfyBonus = role.SatisfyBonus,
                    TomatoBonus = role.TomatoBonus,
                    TomatoSeconds = tomatoSeconds,
                    HasSpellQuestions = hasSpellQuestions,
                };
                m_originalRequiredHealValues[i] = requiredHealValue;
            }

            m_zoneCount = zones.Count;
        }

        private void AllocateCalculationBuffers()
        {
            int count = m_roles.Length;
            if (m_originalRequiredHealValues.Length != count)
                m_originalRequiredHealValues = new BigInteger[count];
            m_previewRequiredHealValues = new BigInteger[count];
            m_candidateRequiredHealValues = new BigInteger[count];
            m_result = new DamageSimulationResult(count);
        }

        private void OnReferenceRouteChanged(ChangeEvent<string> eventData)
        {
            if (m_isUpdatingControls)
                return;
            m_referenceRoute = (DamageSimulationRoute)m_referenceRouteField.index;
            OnSimulationInputChanged();
        }

        private void OnTargetHoursChanged(ChangeEvent<double> eventData)
        {
            if (m_isUpdatingControls)
                return;
            m_targetHours = eventData.newValue;
            OnSimulationInputChanged();
        }

        private void OnInputsPerSecondChanged(ChangeEvent<double> eventData)
        {
            if (m_isUpdatingControls)
                return;
            m_inputsPerSecond = eventData.newValue;
            OnSimulationInputChanged();
        }

        private void OnWordsPerMinuteChanged(ChangeEvent<double> eventData)
        {
            if (m_isUpdatingControls)
                return;
            m_wordsPerMinute = eventData.newValue;
            OnSimulationInputChanged();
        }

        private void OnBaseHealChanged(ChangeEvent<int> eventData)
        {
            if (m_isUpdatingControls)
                return;
            m_baseHeal = eventData.newValue;
            OnSimulationInputChanged();
        }

        private void OnSpellMultiplierChanged(ChangeEvent<int> eventData)
        {
            if (m_isUpdatingControls)
                return;
            m_spellMultiplier = eventData.newValue;
            OnSimulationInputChanged();
        }

        private void OnDebugMultiplierChanged(ChangeEvent<int> eventData)
        {
            if (m_isUpdatingControls)
                return;
            m_debugMultiplier = eventData.newValue;
            OnSimulationInputChanged();
        }

        private void OnSimulationInputChanged()
        {
            m_isUsingOriginalPreview = false;
            RecalculateFromTarget();
            RefreshOutputUI();
        }

        private void RecalculateFromTarget()
        {
            m_calculationValid = false;
            m_calculationError = string.Empty;
            if (m_dataErrors.Count > 0)
            {
                m_calculationError = "\u5fc5\u8981\u8cc7\u6599\u7f3a\u5931\uff0c\u7121\u6cd5\u91cd\u65b0\u8a08\u7b97\u3002";
                return;
            }
            if (!TryBuildSettings(out DamageSimulationSettings settings))
            {
                m_calculationError = "\u8f38\u5165\u901f\u5ea6\u3001WPM \u8207\u6240\u6709\u50b7\u5bb3\u76f8\u95dc\u6578\u503c\u5fc5\u9808\u5927\u65bc 0\u3002";
                return;
            }
            if (!TryConvertPositiveDoubleToDecimal(m_targetHours, out decimal targetHours) ||
                targetHours > decimal.MaxValue / 3600m)
            {
                m_calculationError = "\u76ee\u6a19\u5c0f\u6642\u8d85\u51fa\u53ef\u8a08\u7b97\u7bc4\u570d\u3002";
                return;
            }

            decimal targetSeconds = targetHours * 3600m;
            bool solved = DamageSimulationCalculator.TrySolveTarget(
                m_roles,
                m_originalRequiredHealValues,
                settings,
                m_referenceRoute,
                targetSeconds,
                m_previewRequiredHealValues,
                m_candidateRequiredHealValues,
                m_result,
                out m_bestMultiplier,
                out m_achievedSeconds,
                out bool durationOverflow,
                out DamageSimulationError error,
                out int errorRoleIndex);

            if (!solved)
            {
                m_calculationError = FormatError(error, errorRoleIndex);
                return;
            }

            DamageSimulationCalculator.CalculateAll(m_roles, m_previewRequiredHealValues, settings, m_result);
            DamageSimulationRouteResult referenceResult = m_result.GetRoute(m_referenceRoute);
            m_achievedSeconds = referenceResult.TotalSeconds;
            if (durationOverflow || referenceResult.DurationOverflow)
            {
                m_calculationError = "\u901a\u95dc\u6642\u9593\u8d85\u51fa decimal \u53ef\u986f\u793a\u7bc4\u570d\u3002";
                return;
            }

            ValidateFinalResult();
        }

        private void ValidateFinalResult()
        {
            if (!m_result.Normal.IsValid)
            {
                m_calculationError = FormatError(m_result.Normal.Error, m_result.Normal.ErrorRoleIndex);
                m_calculationValid = false;
                return;
            }
            if (!m_result.Spell.IsValid)
            {
                m_calculationError = FormatError(m_result.Spell.Error, m_result.Spell.ErrorRoleIndex);
                m_calculationValid = false;
                return;
            }
            if (!m_result.Tomato.IsValid)
            {
                m_calculationError = FormatError(m_result.Tomato.Error, m_result.Tomato.ErrorRoleIndex);
                m_calculationValid = false;
                return;
            }

            m_calculationValid = true;
            m_calculationError = string.Empty;
        }

        private bool TryBuildSettings(out DamageSimulationSettings settings)
        {
            settings = default;
            if (m_baseHeal <= 0 || m_spellMultiplier <= 0 || m_debugMultiplier <= 0)
                return false;
            if (m_spellUnlockSortId <= 0 || m_tomatoUnlockSortId <= 0)
                return false;
            if (!TryConvertPositiveDoubleToDecimal(m_inputsPerSecond, out decimal inputsPerSecond))
                return false;
            if (!TryConvertPositiveDoubleToDecimal(m_wordsPerMinute, out decimal wordsPerMinute))
                return false;

            settings = new DamageSimulationSettings
            {
                BaseHeal = m_baseHeal,
                SpellMultiplier = m_spellMultiplier,
                DebugMultiplier = m_debugMultiplier,
                InputsPerSecond = inputsPerSecond,
                WordsPerMinute = wordsPerMinute,
                SpellUnlockSortId = m_spellUnlockSortId,
                TomatoUnlockSortId = m_tomatoUnlockSortId,
            };
            return true;
        }

        private void UpdateControlsWithoutNotify()
        {
            if (m_referenceRouteField == null)
                return;

            m_isUpdatingControls = true;
            m_referenceRouteField.index = (int)m_referenceRoute;
            m_referenceRouteField.SetValueWithoutNotify(s_routeNames[(int)m_referenceRoute]);
            m_targetHoursField.SetValueWithoutNotify(m_targetHours);
            m_inputsPerSecondField.SetValueWithoutNotify(m_inputsPerSecond);
            m_wordsPerMinuteField.SetValueWithoutNotify(m_wordsPerMinute);
            m_baseHealField.SetValueWithoutNotify(m_baseHeal);
            m_spellMultiplierField.SetValueWithoutNotify(m_spellMultiplier);
            m_debugMultiplierField.SetValueWithoutNotify(m_debugMultiplier);
            m_isUpdatingControls = false;
        }

        private void RefreshOutputUI()
        {
            if (m_statusLabel == null)
                return;

            m_statusLabel.text = $"\u89d2\u8272 {m_roles.Length}\uff5c\u5340\u57df {m_zoneCount}\uff5c\u756a\u8304\u89e3\u9396 #{m_tomatoUnlockSortId}\uff5c\u5b57\u5f59\u89e3\u9396 #{m_spellUnlockSortId}";
            RefreshDataMessages();

            bool hasValidData = m_dataErrors.Count == 0;
            SetInputFieldsEnabled(hasValidData);
            m_exportButton?.SetEnabled(m_calculationValid);

            if (!m_calculationValid)
            {
                m_calculationHelpBox.messageType = HelpBoxMessageType.Error;
                m_calculationHelpBox.text = string.IsNullOrEmpty(m_calculationError)
                    ? "\u76ee\u524d\u7121\u6cd5\u7522\u751f\u6a21\u64ec\u7d50\u679c\u3002"
                    : m_calculationError;
                m_normalSummaryLabel.text = "\u666e\u901a\u8def\u7dda\n\u7121\u6709\u6548\u7d50\u679c";
                m_spellSummaryLabel.text = "\u5b57\u5f59\u8def\u7dda\n\u7121\u6709\u6548\u7d50\u679c";
                m_tomatoSummaryLabel.text = "\u756a\u8304\u8def\u7dda\n\u7121\u6709\u6548\u7d50\u679c";
            }
            else
            {
                decimal targetSeconds = TryConvertPositiveDoubleToDecimal(m_targetHours, out decimal targetHours)
                    ? targetHours * 3600m
                    : decimal.Zero;
                decimal delta = m_achievedSeconds >= targetSeconds
                    ? m_achievedSeconds - targetSeconds
                    : targetSeconds - m_achievedSeconds;
                string direction = m_achievedSeconds >= targetSeconds ? "+" : "-";
                m_calculationHelpBox.messageType = HelpBoxMessageType.Info;
                m_calculationHelpBox.text =
                    $"\u57fa\u6e96\uff1a{s_routeNames[(int)m_referenceRoute]}\uff5c\u75b2\u52de\u500d\u7387\uff1a{m_bestMultiplier.ToString("0.################", CultureInfo.InvariantCulture)}\uff5c" +
                    $"\u76ee\u6a19\uff1a{FormatDuration(targetSeconds)}\uff5c\u5be6\u969b\uff1a{FormatDuration(m_achievedSeconds)}\uff5c\u8aa4\u5dee\uff1a{direction}{FormatDuration(delta)}\u3002" +
                    "\n\u7b2c N \u96bb\u4f7f\u7528\u524d N-1 \u96bb\u7684\u7d2f\u7a4d\u52a0\u6210\uff1b\u81ea\u8eab\u52a0\u6210\u5f9e\u4e0b\u4e00\u96bb\u751f\u6548\u3002";

                m_normalSummaryLabel.text = FormatRouteSummary("\u666e\u901a\u8def\u7dda", m_result.Normal);
                m_spellSummaryLabel.text = FormatRouteSummary("\u5b57\u5f59\u8def\u7dda", m_result.Spell);
                m_tomatoSummaryLabel.text = FormatRouteSummary("\u756a\u8304\u8def\u7dda", m_result.Tomato);
            }

            m_roleListView?.RefreshItems();
        }

        private void RefreshDataMessages()
        {
            if (m_dataMessageContainer == null)
                return;

            m_dataMessageContainer.Clear();
            for (int i = 0; i < m_dataErrors.Count; i++)
                m_dataMessageContainer.Add(new HelpBox(m_dataErrors[i], HelpBoxMessageType.Error));
            for (int i = 0; i < m_dataWarnings.Count; i++)
                m_dataMessageContainer.Add(new HelpBox(m_dataWarnings[i], HelpBoxMessageType.Warning));
        }

        private void SetInputFieldsEnabled(bool enabled)
        {
            m_referenceRouteField?.SetEnabled(enabled);
            m_targetHoursField?.SetEnabled(enabled);
            m_inputsPerSecondField?.SetEnabled(enabled);
            m_wordsPerMinuteField?.SetEnabled(enabled);
            m_baseHealField?.SetEnabled(enabled);
            m_spellMultiplierField?.SetEnabled(enabled);
            m_debugMultiplierField?.SetEnabled(enabled);
        }

        private void ExportCsv()
        {
            if (!m_calculationValid)
                return;

            string defaultName = $"MassageCat_DamageSimulation_{DateTime.Now:yyyyMMdd_HHmmss}";
            string path = EditorUtility.SaveFilePanel("\u532f\u51fa\u50b7\u5bb3\u6a21\u64ec CSV", string.Empty, defaultName, "csv");
            if (string.IsNullOrEmpty(path))
                return;

            BuildCsv();
            File.WriteAllText(path, m_csvBuilder.ToString(), new UTF8Encoding(true));
            EditorUtility.DisplayDialog("Damage Simulator", $"CSV \u5df2\u6210\u529f\u532f\u51fa\uff1a\n{path}", "\u78ba\u5b9a");
        }

        private void BuildCsv()
        {
            m_csvBuilder.Clear();
            AppendCsvRow(
                "RowType", "SortId", "Zone", "RoleKey", "CurrentRequiredHealValue", "SuggestedRequiredHealValue",
                "NormalAction", "NormalDamage", "NormalHits", "NormalSeconds",
                "SpellAction", "SpellDamage", "SpellHits", "SpellWords", "SpellSeconds",
                "TomatoAction", "TomatoDamage", "TomatoHits", "TomatoSessions", "TomatoSeconds");

            for (int i = 0; i < m_roles.Length; i++)
            {
                DamageSimulationRoleInput role = m_roles[i];
                DamageSimulationRoleResult result = m_result.RoleResults[i];
                AppendCsvRow(
                    "Role",
                    role.SortId.ToString(CultureInfo.InvariantCulture),
                    role.Zone,
                    role.RoleKey,
                    role.OriginalRequiredHealValue.ToString(CultureInfo.InvariantCulture),
                    m_previewRequiredHealValues[i].ToString(CultureInfo.InvariantCulture),
                    GetActionName(result.Normal.ActionType),
                    result.Normal.DamagePerAction.ToString(CultureInfo.InvariantCulture),
                    result.Normal.RequiredNormalHits.ToString(CultureInfo.InvariantCulture),
                    result.Normal.Seconds.ToString(CultureInfo.InvariantCulture),
                    GetActionName(result.Spell.ActionType),
                    result.Spell.DamagePerAction.ToString(CultureInfo.InvariantCulture),
                    result.Spell.RequiredNormalHits.ToString(CultureInfo.InvariantCulture),
                    result.Spell.RequiredSpellWords.ToString(CultureInfo.InvariantCulture),
                    result.Spell.Seconds.ToString(CultureInfo.InvariantCulture),
                    GetActionName(result.Tomato.ActionType),
                    result.Tomato.DamagePerAction.ToString(CultureInfo.InvariantCulture),
                    result.Tomato.RequiredNormalHits.ToString(CultureInfo.InvariantCulture),
                    result.Tomato.RequiredTomatoSessions.ToString(CultureInfo.InvariantCulture),
                    result.Tomato.Seconds.ToString(CultureInfo.InvariantCulture));
            }

            AppendRouteSummaryCsv("Normal Total", m_result.Normal);
            AppendRouteSummaryCsv("Spell Total", m_result.Spell);
            AppendRouteSummaryCsv("Tomato Total", m_result.Tomato);
            AppendCsvRow(
                "Setting", string.Empty, string.Empty, "ReferenceRoute", s_routeNames[(int)m_referenceRoute],
                "TargetHours", m_targetHours.ToString("R", CultureInfo.InvariantCulture),
                "RequiredHealValueMultiplier", m_bestMultiplier.ToString(CultureInfo.InvariantCulture),
                "InputsPerSecond", m_inputsPerSecond.ToString("R", CultureInfo.InvariantCulture),
                "WordsPerMinute", m_wordsPerMinute.ToString("R", CultureInfo.InvariantCulture),
                "BaseHeal", m_baseHeal.ToString(CultureInfo.InvariantCulture),
                "SpellMultiplier", m_spellMultiplier.ToString(CultureInfo.InvariantCulture),
                "DebugMultiplier", m_debugMultiplier.ToString(CultureInfo.InvariantCulture),
                string.Empty);
        }

        private void AppendRouteSummaryCsv(string name, DamageSimulationRouteResult result)
        {
            AppendCsvRow(
                "Summary", string.Empty, string.Empty, name, string.Empty, string.Empty,
                "NormalHits", string.Empty, result.TotalNormalHits.ToString(CultureInfo.InvariantCulture), result.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                "SpellWords", string.Empty, string.Empty, result.TotalSpellWords.ToString(CultureInfo.InvariantCulture), result.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                "TomatoSessions", string.Empty, string.Empty, result.TotalTomatoSessions.ToString(CultureInfo.InvariantCulture), result.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        private void AppendCsvRow(params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                    m_csvBuilder.Append(',');
                AppendCsvField(values[i]);
            }
            m_csvBuilder.AppendLine();
        }

        private void AppendCsvField(string value)
        {
            m_csvBuilder.Append('"');
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char character = value[i];
                    if (character == '"')
                        m_csvBuilder.Append("\"\"");
                    else
                        m_csvBuilder.Append(character);
                }
            }
            m_csvBuilder.Append('"');
        }

        private string FormatError(DamageSimulationError error, int roleIndex)
        {
            string roleName = roleIndex >= 0 && roleIndex < m_roles.Length
                ? $"\uff08{m_roles[roleIndex].RoleKey}\uff09"
                : string.Empty;
            switch (error)
            {
                case DamageSimulationError.InvalidInputRate:
                    return "\u666e\u901a\uff0f\u756a\u8304\u8f38\u5165\u901f\u5ea6\u5fc5\u9808\u5927\u65bc 0\u3002";
                case DamageSimulationError.InvalidWordsPerMinute:
                    return "\u6210\u529f WPM \u5fc5\u9808\u5927\u65bc 0\u3002";
                case DamageSimulationError.InvalidDamage:
                    return string.Concat("\u7642\u7652\u503c\u6216\u500d\u7387\u7121\u6548", roleName, "\u3002");
                case DamageSimulationError.InvalidHealth:
                    return string.Concat("\u89d2\u8272\u7642\u7652\u9700\u6c42\u503c\u5fc5\u9808\u5927\u65bc 0", roleName, "\u3002");
                case DamageSimulationError.MissingSpellQuestions:
                    return string.Concat("\u7f3a\u5c11\u5b57\u5f59\u984c\u5eab", roleName, "\u3002");
                case DamageSimulationError.MissingTomatoDuration:
                    return string.Concat("\u7f3a\u5c11\u756a\u8304\u6642\u9593", roleName, "\u3002");
                case DamageSimulationError.TomatoSessionCapacityZero:
                    return string.Concat("\u76ee\u524d\u8f38\u5165\u901f\u5ea6\u5728\u8a72\u756a\u8304\u6642\u6bb5\u5167\u7121\u6cd5\u7522\u751f\u4e00\u6b21\u6709\u6548\u8f38\u5165", roleName, "\u3002");
                default:
                    return "\u6a21\u64ec\u8a08\u7b97\u5931\u6557\u3002";
            }
        }

        private static string FormatRouteSummary(string routeName, DamageSimulationRouteResult result)
        {
            if (!result.IsValid)
                return $"{routeName}\n\u7121\u6709\u6548\u7d50\u679c";

            StringBuilder builder = new StringBuilder(128);
            builder.Append(routeName).Append("\n\u6642\u9593\uff1a").Append(FormatDuration(result.TotalSeconds));
            if (result.TotalNormalHits > BigInteger.Zero)
                builder.Append("\uff5c\u666e\u901a\uff1a").Append(FormatBigInteger(result.TotalNormalHits));
            if (result.TotalSpellWords > BigInteger.Zero)
                builder.Append("\uff5c\u6210\u529f\u8a5e\uff1a").Append(FormatBigInteger(result.TotalSpellWords));
            if (result.TotalTomatoSessions > BigInteger.Zero)
                builder.Append("\uff5c\u756a\u8304\u56de\u5408\uff1a").Append(FormatBigInteger(result.TotalTomatoSessions));
            return builder.ToString();
        }

        private static string FormatStep(in DamageSimulationStepResult step)
        {
            if (step.ActionType == DamageSimulationActionType.None)
                return "-";

            BigInteger count;
            switch (step.ActionType)
            {
                case DamageSimulationActionType.SpellWord:
                    count = step.RequiredSpellWords;
                    break;
                case DamageSimulationActionType.TomatoSession:
                    count = step.RequiredTomatoSessions;
                    break;
                default:
                    count = step.RequiredNormalHits;
                    break;
            }

            return $"{GetActionName(step.ActionType)}\uff5c\u50b7\u5bb3 {FormatBigInteger(step.DamagePerAction)}\uff5c\u6b21\u6578 {FormatBigInteger(count)}\n{FormatDuration(step.Seconds)}";
        }

        private static string FormatDuration(decimal seconds)
        {
            if (seconds == decimal.MaxValue)
                return "\u8d85\u51fa\u53ef\u986f\u793a\u7bc4\u570d";
            if (seconds < decimal.Zero)
                seconds = decimal.Zero;

            decimal years = decimal.Floor(seconds / 31536000m);
            seconds -= years * 31536000m;
            decimal days = decimal.Floor(seconds / 86400m);
            seconds -= days * 86400m;
            decimal hours = decimal.Floor(seconds / 3600m);
            seconds -= hours * 3600m;
            decimal minutes = decimal.Floor(seconds / 60m);
            seconds -= minutes * 60m;

            if (years > decimal.Zero)
                return $"{years.ToString("N0", CultureInfo.InvariantCulture)} \u5e74 {days} \u5929 {hours} \u5c0f\u6642";
            if (days > decimal.Zero)
                return $"{days} \u5929 {hours} \u5c0f\u6642 {minutes} \u5206";
            if (hours > decimal.Zero)
                return $"{hours} \u5c0f\u6642 {minutes} \u5206 {decimal.Round(seconds, 1)} \u79d2";
            if (minutes > decimal.Zero)
                return $"{minutes} \u5206 {decimal.Round(seconds, 1)} \u79d2";
            return $"{decimal.Round(seconds, 2)} \u79d2";
        }

        private static string FormatBigInteger(BigInteger value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string GetActionName(DamageSimulationActionType actionType)
        {
            switch (actionType)
            {
                case DamageSimulationActionType.NormalHit:
                    return "\u666e\u901a";
                case DamageSimulationActionType.SpellWord:
                    return "\u5b57\u5f59";
                case DamageSimulationActionType.TomatoSession:
                    return "\u756a\u8304";
                default:
                    return "\u7121";
            }
        }

        private static bool TryConvertPositiveDoubleToDecimal(double value, out decimal result)
        {
            result = decimal.Zero;
            if (value <= 0d || double.IsNaN(value) || double.IsInfinity(value))
                return false;

            return decimal.TryParse(
                value.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result) && result > decimal.Zero;
        }

        private static string ReadUnlockFlagKey(Component component)
        {
            if (!component)
                return string.Empty;

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty referenceProperty = serializedObject.FindProperty("m_flagReference");
            SerializedProperty keyProperty = referenceProperty?.FindPropertyRelative("m_key");
            return keyProperty != null ? keyProperty.stringValue : string.Empty;
        }

        private static int FindRoleSortIdForFlag(List<RoleData> roles, string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey))
                return 0;

            for (int i = 0; i < roles.Count; i++)
            {
                FlagReference flagReference = roles[i].SatisfyToAddFlagReference;
                if (flagReference != null && string.Equals(flagReference.GetKey(), flagKey, StringComparison.Ordinal))
                    return roles[i].roleSortId;
            }
            return 0;
        }

        private static int CompareRoles(RoleData left, RoleData right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left == null)
                return -1;
            if (right == null)
                return 1;
            return left.roleSortId.CompareTo(right.roleSortId);
        }

        private static void CopyRequiredHealValues(BigInteger[] source, BigInteger[] destination)
        {
            int count = Math.Min(source.Length, destination.Length);
            for (int i = 0; i < count; i++)
                destination[i] = source[i];
        }

        private sealed class RoleRowElement : VisualElement
        {
            private readonly Label m_sortLabel;
            private readonly Label m_zoneLabel;
            private readonly Label m_roleLabel;
            private readonly Label m_currentRequiredHealValueLabel;
            private readonly Label m_previewRequiredHealValueLabel;
            private readonly Label m_normalLabel;
            private readonly Label m_spellLabel;
            private readonly Label m_tomatoLabel;

            public RoleRowElement()
            {
                style.flexDirection = FlexDirection.Row;
                style.height = k_rowHeight;
                m_sortLabel = CreateCell(k_sortWidth);
                m_zoneLabel = CreateCell(k_zoneWidth);
                m_roleLabel = CreateCell(k_roleWidth);
                m_currentRequiredHealValueLabel = CreateCell(k_requiredHealValueWidth);
                m_previewRequiredHealValueLabel = CreateCell(k_requiredHealValueWidth);
                m_normalLabel = CreateCell(k_routeWidth);
                m_spellLabel = CreateCell(k_routeWidth);
                m_tomatoLabel = CreateCell(k_routeWidth);
                Add(m_sortLabel);
                Add(m_zoneLabel);
                Add(m_roleLabel);
                Add(m_currentRequiredHealValueLabel);
                Add(m_previewRequiredHealValueLabel);
                Add(m_normalLabel);
                Add(m_spellLabel);
                Add(m_tomatoLabel);
            }

            public void Bind(
                DamageSimulationRoleInput role,
                BigInteger previewRequiredHealValue,
                DamageSimulationRoleResult result,
                bool hasValidResult)
            {
                m_sortLabel.text = role.SortId.ToString(CultureInfo.InvariantCulture);
                m_zoneLabel.text = role.Zone;
                m_roleLabel.text = role.RoleKey;
                m_currentRequiredHealValueLabel.text = FormatBigInteger(role.OriginalRequiredHealValue);
                m_previewRequiredHealValueLabel.text = FormatBigInteger(previewRequiredHealValue);
                m_normalLabel.text = hasValidResult ? FormatStep(result.Normal) : "-";
                m_spellLabel.text = hasValidResult ? FormatStep(result.Spell) : "-";
                m_tomatoLabel.text = hasValidResult ? FormatStep(result.Tomato) : "-";

                tooltip = $"{role.RoleKey}\n\u76ee\u524d\uff1a{role.OriginalRequiredHealValue}\n\u5efa\u8b70\uff1a{previewRequiredHealValue}";
            }

            private static Label CreateCell(float width)
            {
                Label label = new Label();
                label.style.width = width;
                label.style.minWidth = width;
                label.style.maxWidth = width;
                label.style.paddingLeft = 4f;
                label.style.paddingRight = 3f;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.overflow = Overflow.Hidden;
                return label;
            }
        }
    }
}
