using System;
using PrimeTween;
using UnityEngine;

public class NormalHealEffect : MonoBehaviour
{
    private const float k_effectDuration = 0.25f;

    private GameObject[] m_effectObjects;
    private Action<NormalHealEffect> m_completeCallback;
    private Tween m_releaseTween;

    private void Awake()
    {
        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
        int effectCount = Mathf.Max(0, childTransforms.Length - 1);
        m_effectObjects = new GameObject[effectCount];
        for (int i = 0; i < effectCount; i++)
            m_effectObjects[i] = childTransforms[i + 1].gameObject;
    }

    private void OnEnable()
    {
        if (m_effectObjects == null || m_effectObjects.Length == 0)
            return;

        int selectedIndex = UnityEngine.Random.Range(0, m_effectObjects.Length);
        for (int i = 0; i < m_effectObjects.Length; i++)
        {
            GameObject effectObject = m_effectObjects[i];
            bool isSelected = i == selectedIndex;
            effectObject.SetActive(isSelected);
            if (isSelected)
                effectObject.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
        }
    }

    private void OnDisable()
    {
        if (m_releaseTween.isAlive)
            m_releaseTween.Stop();

        if (m_effectObjects == null)
            return;

        for (int i = 0; i < m_effectObjects.Length; i++)
            m_effectObjects[i].SetActive(false);
    }

    public void Play(Action<NormalHealEffect> completeCallback)
    {
        if (m_releaseTween.isAlive)
            m_releaseTween.Stop();

        m_completeCallback = completeCallback;
        gameObject.SetActive(true);
        m_releaseTween = Tween.Delay(
            this,
            k_effectDuration,
            static effect => effect.Complete(),
            warnIfTargetDestroyed: false);
    }

    private void Complete()
    {
        Action<NormalHealEffect> completeCallback = m_completeCallback;
        m_completeCallback = null;
        completeCallback?.Invoke(this);
    }
}
