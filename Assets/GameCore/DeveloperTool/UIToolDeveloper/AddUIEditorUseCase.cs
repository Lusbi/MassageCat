using GameCore.UI;
using GameCore.Utils;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AddUIEditorUseCase
{
    private List<GameObject> m_lists = new List<GameObject>();

    private System.Action callBack;
    public void Apply(GameObject[] gameObjects, System.Action callBack = null)
    {
        m_lists.Clear();
        foreach (GameObject go in gameObjects)
        {
            if (go.activeInHierarchy == false) continue;
            m_lists.Add(go);
        }
        this.callBack = callBack;
    }

    [Button("Canvas View"), ButtonGroup("必備")]
    private void AddCanvas()
    {
        foreach (GameObject go in m_lists)
        {
            Canvas canvas = go.AddComponentIfNull<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = 100;

            GraphicRaycaster graphicRaycaster = go.AddComponentIfNull<GraphicRaycaster>();

            CanvasScaler canvasScaler = go.AddComponentIfNull<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(2560, 1440);
        }
    }

    [Button("CanvasGroup"), ButtonGroup("必備")]
    private void AddCanvasGroup()
    {
        foreach (GameObject go in m_lists)
        {
            CanvasGroup canvasGroup = go.AddComponentIfNull<CanvasGroup>();
        }
    }

    [Button("TextMeshPro"), ButtonGroup("常用")]
    private void AddTextMeshProUGUI()
    {
        foreach (GameObject go in m_lists)
        {
            go.AddComponentIfNull<TextMeshProUGUI>();
        }

        callBack?.Invoke();
    }

    [Button("UIButton"), ButtonGroup("常用")]
    private void AddUIButton()
    {
        foreach (GameObject go in m_lists)
        {
            go.AddComponentIfNull<UIButton>();
        }
        callBack?.Invoke();
    }

    [Button("Image"), ButtonGroup("常用")]
    private void AddImage()
    {
        foreach (GameObject go in m_lists)
        {
            go.AddComponentIfNull<Image>();
        }
        callBack?.Invoke();
    }

    [Button("RawImage"), ButtonGroup("常用")]
    private void AddRawImage()
    {
        foreach (GameObject go in m_lists)
        {
            go.AddComponentIfNull<Image>();
        }
        callBack?.Invoke();
    }

    [Button("開啟物件底下所有的 raycast"), ButtonGroup("工具")]
    private void EnableRaycast()
    {
        foreach (GameObject go in m_lists)
        {
            Graphic[] graphics = go.GetComponentsInChildren<Graphic>();
            foreach (Graphic graphic in graphics)
                graphic.raycastTarget = true;
        }
    }

    [Button("關閉物件底下所有的 raycast"), ButtonGroup("工具")]
    private void DisableRaycast()
    {
        foreach (GameObject go in m_lists)
        {
            Graphic[] graphics = go.GetComponentsInChildren<Graphic>();
            foreach (Graphic graphic in graphics)
                graphic.raycastTarget = false;
        }
    }
}
