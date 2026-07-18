using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace GameCore.Database.Editor
{
    public class AssetControlsGUI<T> : DataEditorControlsGUI<T> where T : Data
    {
        private readonly IDataEditorWindow<T> m_editor;
        private T m_copiedData;

        private bool HasEditableData => m_editor.IsEditing;

        public AssetControlsGUI(IDataEditorWindow<T> editor)
        {
            m_editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        [BoxGroup("Asset")]
        [ButtonGroup("Asset/CRUD")]
        [Button("", ButtonSizes.Large, Icon = SdfIconType.FileEarmarkPlusFill)]
        [Tooltip("新增")]
        private void Create()
        {
            m_editor.NewEditingCopy();
        }

        [ShowIf(nameof(HasEditableData))]
        [BoxGroup("Asset")]
        [ButtonGroup("Asset/CRUD")]
        [GUIColor(0.3f, 1f, 0.3f)]
        [Button("", ButtonSizes.Large, Icon = SdfIconType.FileEarmarkPost)]
        [Tooltip("儲存")]
        private void Save()
        {
            m_editor.Save();
        }

        [ShowIf(nameof(HasEditableData))]
        [BoxGroup("Asset")]
        [ButtonGroup("Asset/CRUD")]
        [GUIColor(0, 0.5f, 1)]
        [Button("", ButtonSizes.Large, Icon = SdfIconType.Arrow90degLeft)]
        [Tooltip("復原")]
        private void Revert()
        {
            m_editor.Revert();
        }

        [ShowIf(nameof(HasEditableData))]
        [BoxGroup("Asset")]
        [ButtonGroup("Asset/CRUD")]
        [GUIColor(1, 0, 0)]
        [Button("", ButtonSizes.Large, Icon = SdfIconType.TrashFill)]
        [Tooltip("刪除")]
        private void Delete()
        {
            m_editor.Delete();
        }

        [EnableIf(nameof(HasEditableData))]
        [BoxGroup("Asset")]
        [ButtonGroup("Asset/Copy")]
        [Button("新增副本")]
        private void Duplicate()
        {
            m_editor.Duplicate();
        }

        private bool CanPaste => HasEditableData && m_copiedData;

        [EnableIf(nameof(HasEditableData))]
        [BoxGroup("Asset")]
        [ButtonGroup("Asset/Copy")]
        [Button("複製")]
        private void Copy()
        {
            m_editor.Copy(ref m_copiedData);
        }

        [EnableIf(nameof(CanPaste))]
        [BoxGroup("Asset")]
        [ButtonGroup("Asset/Copy")]
        [Button("貼上")]
        private void Paste()
        {
            if (!m_copiedData)
                return;

            m_editor.Paste(m_copiedData);
        }
    }
}