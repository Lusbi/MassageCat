using System;
using UnityEngine;

public class UILoadProcess 
{
    private int m_reloadCount = 0;
    private bool m_loaded = false;
    private Action m_finishedCallBack;

    public UILoadProcess(Action callBack)
    {
        m_finishedCallBack = callBack;
        m_loaded = false;
        PreloadExecute();
    }

    public void PreloadExecute()
    {
        if (m_loaded)
        {
            return;
        }

        m_reloadCount = 1;
        new UIGameMain(UILoadFinished, UIEnum.GameMain);
    }

    private void UILoadFinished()
    {
        m_reloadCount--;
        m_loaded = m_reloadCount == 0;

        if (m_reloadCount == 0)
        {
            m_finishedCallBack?.Invoke();
        }
    }
}
