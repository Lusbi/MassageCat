using System;
using GameCore;
using UnityEngine;

[System.Serializable]
public class GameMainCanvasGroup : IInitlization
{
    [SerializeField] private bool defaultActive = false;
    [SerializeField] private CommandType commandType;
    [SerializeField] private CanvasGroup canvasGroup;

    public void Initlization(Action callBack = null)
    {
        SetActive(false);
    }

    public void SetActive(bool active)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GameObject.Find("GameMainCanvasGroup").GetComponent<CanvasGroup>();
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = active ? 1 : 0;
            canvasGroup.interactable = active;
            canvasGroup.blocksRaycasts = active;
        }
    }
}
