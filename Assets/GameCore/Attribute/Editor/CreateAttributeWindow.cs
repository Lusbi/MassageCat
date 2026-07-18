using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

namespace GameCore.Attribute.Editor
{
    [System.Serializable]
    public class AttributeTypeInfo
    {
        [HideLabel , HorizontalGroup]
        public string typeName;
        [HideLabel, HorizontalGroup]
        public int index;
        [HideLabel , HorizontalGroup]
        public bool removeToggle;

        public AttributeTypeInfo(AttributeType attributeType)
        {
            typeName = attributeType.ToString();
            index = (int)attributeType;
        }

        public AttributeTypeInfo(string typeName , int index)
        {
            this.typeName = typeName;
            this.index = index;
        }
    }

    public class CreateAttributeWindow : OdinEditorWindow
    {
        [ListDrawerSettings(HideRemoveButton =true , HideAddButton = true,DraggableItems = false , NumberOfItemsPerPage =20 , ShowPaging =true)]
        public List<AttributeTypeInfo> attributeTypeInfos = new List<AttributeTypeInfo>();

        [MenuItem(MenuItemConfig.TOOL_ATTRIBUTE, priority = 1)]
        private static void OpenWindow()
        {
            GetWindow<CreateAttributeWindow>().Show();
        }

        [Space(10)]
        [ReadOnly, HideLabel]
        public string errorMessage = string.Empty;

        [Space(10)]
        [LabelText("名字"), HorizontalGroup]
        public string attributeTypeName;
        [Space(10)]
        [LabelText("Key值"), HorizontalGroup]
        public int attributeTypeOrder = 0;

        [Space(10)]
        [LabelText("註解")]
        public string summary;
        [LabelText("替換公式")]
        public string formula;
        [HideLabel , Multiline(3) , ShowIf("hasFormula")]
        public string formula_ex;

        public bool hasFormula
        {
            get
            {
                return string.IsNullOrEmpty(formula) == false;
            }
        }

        [Button("公式替換後樣子檢查"), ShowIf("hasFormula")]
        private void ConvertFormulaWithEnum()
        {
            formula_ex = CreateAttributeUtils.ConvertFormulaWithEnum(formula);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetAttributeType();
        }

        private void ResetAttributeType()
        {
            attributeTypeInfos.Clear();
            // 收集資訊
            foreach (AttributeType attributeType in System.Enum.GetValues(typeof(AttributeType)))
            {
                AttributeTypeInfo attributeTypeInfo = new AttributeTypeInfo(attributeType);

                attributeTypeInfos.Add(attributeTypeInfo);
            }
        }

        private bool careteTemplate
        {
            get
            {
                if (string.IsNullOrEmpty(attributeTypeName))
                {
                    errorMessage = $"類型名稱不能為空值";
                    return false;
                }
                if (SortingCheck() == false)
                {
                    errorMessage = $"所設定的 index 有重複，請重新設定";
                    return false;
                }
                if (AttributeTypeCheck() == false)
                {
                    errorMessage = $"所設定的 名稱 有重複，請重新設定";
                    return false;
                }
                errorMessage = "沒有問題！";
                return true;
            }
        }

        private bool SortingCheck()
        {
            foreach (AttributeTypeInfo attributeTypeInfo in attributeTypeInfos)
            {
                if (attributeTypeInfo.index == attributeTypeOrder)
                {
                    return false;
                }
            }
            return true;
        }

        private bool AttributeTypeCheck()
        {
            foreach (AttributeTypeInfo attributeTypeInfo in attributeTypeInfos)
            {
                if (attributeTypeInfo.typeName == attributeTypeName)
                {
                    return false;
                }
            }
            return true;
        }

        [Button("建立模版") , ShowIf("careteTemplate")]
        private void Create ()
        {
            attributeTypeInfos.Add(new AttributeTypeInfo(attributeTypeName, attributeTypeOrder));
            CreateAttributeUtils.Create(attributeTypeName, attributeTypeInfos, formula, summary);
        }

        [Button("刪除勾選屬性")]
        public void Remove()
        {
            bool needRemove = false;
            foreach (AttributeTypeInfo attributeTypeInfo in attributeTypeInfos)
            {
                needRemove |= attributeTypeInfo.removeToggle;

            }

            if (needRemove == false)
            {
                if (EditorUtility.DisplayDialog("提示", $"沒有勾選要刪除的屬性。", "確認") == false)
                {
                    return;
                }
            }
            // 可能導致其它公式出現問題訊息提示
            if (EditorUtility.DisplayDialog("提示", $"刪除屬性可能導致部份公式出錯，需要手動調整。", "確認刪除") == false)
            {
                return;
            }

            CreateAttributeUtils.Remove(attributeTypeInfos);
        }
    }
}