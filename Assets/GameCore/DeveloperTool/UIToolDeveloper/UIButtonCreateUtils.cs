#if UNITY_EDITOR
using System.Collections.Generic;
using GameCore.UI;
using GameCore.Utils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class UIButtonCreateEditorUseCase
{
    [ShowInInspector]
    public int uisCount => m_lists.Count;

    private List<UIButton> m_lists = new List<UIButton>();

    public void Clear() => m_lists.Clear();

    public void Apply(GameObject[] gameObjects)
    {
        Clear();
        foreach (GameObject go in gameObjects)
        {
            if (go.activeInHierarchy == false) continue;
            if (go.TryGetComponent(out UIButton uIButton))
            {
                m_lists.Add(uIButton);
            }
        }
    }

    [Button("效果_縮放"), ButtonGroup("按鈕效果1")]
    private void AddButtonFx_Scale()
    {
        foreach (UIButton uIButton in m_lists)
        {
            Undo.RecordObject(uIButton, $"Setting ButtonFx Scale - {uIButton.gameObject.name}");
            uIButton.gameObject.AddComponentIfNull<ButtonFXScale>();
            uIButton.ResetUIButton();
            EditorUtility.SetDirty(uIButton);
        }
    }

    [Button("效果_圖片"), ButtonGroup("按鈕效果1")]
    private void AddButtonFx_Sprite()
    {
        foreach (UIButton uIButton in m_lists)
        {
            Undo.RecordObject(uIButton, $"Setting ButtonFx Sprite - {uIButton.gameObject.name}");
            uIButton.gameObject.AddComponentIfNull<ButtonFXSprite>();
            uIButton.ResetUIButton();
            EditorUtility.SetDirty(uIButton);
        }
    }

    [Button("效果_音效"), ButtonGroup("按鈕效果1")]
    private void AddButtonFx_SFX()
    {
        foreach (UIButton uIButton in m_lists)
        {
            Undo.RecordObject(uIButton, $"Setting ButtonFx Sfx - {uIButton.gameObject.name}");
            //uIButton.gameObject.AddComponentIfNull<ButtonSFX>();
            uIButton.ResetUIButton();
            EditorUtility.SetDirty(uIButton);
        }
    }

    [Button("效果_開關"), ButtonGroup("按鈕效果2")]
    private void AddButtonFx_Active()
    {
        foreach (UIButton uIButton in m_lists)
        {
            Undo.RecordObject(uIButton, $"Setting ButtonFx Active - {uIButton.gameObject.name}");
            uIButton.gameObject.AddComponentIfNull<ButtonFXObj>();
            uIButton.ResetUIButton();
            EditorUtility.SetDirty(uIButton);
        }
    }

    [Button("效果_顏色"), ButtonGroup("按鈕效果2")]
    private void AddButtonFx_Color()
    {
        foreach (UIButton uIButton in m_lists)
        {
            Undo.RecordObject(uIButton, $"Setting ButtonFx Color - {uIButton.gameObject.name}");
            uIButton.gameObject.AddComponentIfNull<ButtonFXColor>();
            uIButton.ResetUIButton();
            EditorUtility.SetDirty(uIButton);
        }
    }
    [Button("效果_動畫"), ButtonGroup("按鈕效果2")]
    private void AddButtonFx_Animation()
    {
        foreach (UIButton uIButton in m_lists)
        {
            Undo.RecordObject(uIButton, $"Setting ButtonFx Animation - {uIButton.gameObject.name}");
            //uIButton.gameObject.AddComponentIfNull<ButtonFXAnimation>();
            uIButton.ResetUIButton();
            EditorUtility.SetDirty(uIButton);
        }
    }
}
#endif // UNITY_EDITOR