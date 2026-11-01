using GameCore.UI;
using System;
using UnityEngine;

public class MenuButtonActions : MonoBehaviour
{
    [SerializeField] private UIButton m_btn;

    private void Awake()
    {
        m_btn.onClicked = OnClick;
    }

    private void OnClick(UIButton button)
    {
        
    }
}
