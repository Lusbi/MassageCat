#if UNITY_EDITOR
using GameCore;
using GameCore.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace FS3.EditorTool.DeveloperTool
{
    [System.Flags]
    public enum UIType
    {
        None = 0,
        Text = 1 << 2,
        UIButton = 1 << 3,
        All = Text | UIButton,
    }

    public class DeveloperUITool : DeveloperToolBase
    {
        public override string DeveloperName => DeveloperConfig.DEVELOPER_UI;

        private UIType curUIType;

        [TabGroup("Design")][HideLabel, PropertyOrder(5), Title("基本")] public AddUIEditorUseCase addUIEditorUseCase;
        [TabGroup("Design")][HideLabel, PropertyOrder(10), ShowIf(nameof(HasText)), Title("文字")] public TextMeshProUGUIEditorUseCase textMeshProUGUIEditorUseCase;
        [TabGroup("Design")][HideLabel, PropertyOrder(11), ShowIf(nameof(HasUIButton)), Title("按鈕")] public UIButtonCreateEditorUseCase uIButtonCreateEditorUseCase;

        [TabGroup("Sprite Asset")]
        [HideLabel, PropertyOrder(100)]
        public TMP_SpriteAssetUseCase tMP_SpriteAssetUseCase = new TMP_SpriteAssetUseCase();


        private bool HasText => (curUIType & UIType.Text) == UIType.Text;
        private bool HasUIButton => (curUIType & UIType.UIButton) == UIType.UIButton;

        public override void Initlization()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            tMP_SpriteAssetUseCase = new TMP_SpriteAssetUseCase();
            // 初次執行時，先檢查一次
            OnSelectionChanged();
            //colorGammaToLinnerUseCase ??= new ColorGammaToLinnerUseCase();
        }

        private void OnSelectionChanged()
        {
            curUIType = UIType.None;
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                if (gameObject.TryGetComponent(out TextMeshProUGUI textMeshProUGUI))
                {
                    curUIType |= UIType.Text;
                }
                if (gameObject.TryGetComponent(out UIButton component))
                {
                    curUIType |= UIType.UIButton;
                }
            }

            addUIEditorUseCase ??= new AddUIEditorUseCase();
            addUIEditorUseCase.Apply(Selection.gameObjects, OnSelectionChanged);

            if (HasText)
            {
                textMeshProUGUIEditorUseCase ??= new TextMeshProUGUIEditorUseCase();
                textMeshProUGUIEditorUseCase.Apply(Selection.gameObjects);
            }
            else
            {
                textMeshProUGUIEditorUseCase?.Clear();
            }
            if (HasUIButton)
            {
                uIButtonCreateEditorUseCase ??= new UIButtonCreateEditorUseCase();
                uIButtonCreateEditorUseCase.Apply(Selection.gameObjects);
            }
            else
            {
                uIButtonCreateEditorUseCase?.Clear();
            }

        }
    }
}
#endif // UNITY_EDITOR