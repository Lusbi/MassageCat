using System;
using UnityEngine;

public class CriticleHit : MonoBehaviour
{
    private string m_aniName = "CriticleAttack";
    private Animation m_ani;

    void Awake()
    {
        m_ani = GetComponent<Animation>();
    }

    public void Play(Action callBack = null)
    {
        m_ani.Play(m_aniName);
        m_ani.clip = m_ani.GetClip(m_aniName);
        var time = m_ani.clip.length;
        if (callBack != null)
        {
            PrimeTween.Tween.Delay(time, () =>
            {
              callBack?.Invoke();  
            } );
        }
    }
}
