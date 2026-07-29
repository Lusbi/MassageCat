using System;
using PrimeTween;
using UnityEngine;

public class StrongHealEffect : MonoBehaviour
{
    private const string k_animationName = "StrongHealEffect";

    private Animation m_animation;
    private float m_animationDuration;
    private Action<StrongHealEffect> m_completeCallback;
    private Tween m_releaseTween;

    private void Awake()
    {
        m_animation = GetComponent<Animation>();
        AnimationClip animationClip = m_animation.GetClip(k_animationName);
        m_animationDuration = animationClip ? animationClip.length : 0f;
    }

    private void OnDisable()
    {
        if (m_releaseTween.isAlive)
            m_releaseTween.Stop();
        if (m_animation)
            m_animation.Stop();
    }

    public void Play(Action<StrongHealEffect> completeCallback)
    {
        if (m_releaseTween.isAlive)
            m_releaseTween.Stop();

        m_completeCallback = completeCallback;
        gameObject.SetActive(true);
        m_animation.clip = m_animation.GetClip(k_animationName);
        m_animation.Play(k_animationName);
        m_releaseTween = Tween.Delay(
            this,
            m_animationDuration,
            static effect => effect.Complete(),
            warnIfTargetDestroyed: false);
    }

    private void Complete()
    {
        Action<StrongHealEffect> completeCallback = m_completeCallback;
        m_completeCallback = null;
        completeCallback?.Invoke(this);
    }
}
