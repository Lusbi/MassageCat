using UnityEngine;

using PrimeTween;

namespace GameCore.UI
{
    [AddComponentMenu("UI/ButtonFX/Scale"), RequireComponent(typeof(UIButton))]
    public class ButtonFXScale : ButtonFXBase
    {
        private const float k_pressDuration = 0.06f;
        private const float k_releaseDuration = 0.14f;

        [SerializeField] private RectTransform m_rectTransform = null;
        [SerializeField] private ScaleSet m_vectorSet = new ScaleSet();

        private Tween m_scaleTween;

        private void Awake()
        {
            if (m_rectTransform == null)
            {
                m_rectTransform = GetComponent<RectTransform>();
            }
        }

        public override void onClickDown()
        {
            AnimateScale(m_vectorSet.Click, true);
        }

        public override void onClickUp()
        {
            AnimateScale(m_isHover ? m_vectorSet.Hover : m_vectorSet.Normal, false);
        }

        public override void onHover(bool isHovered)
        {
            m_isHover = isHovered;
            AnimateScale(isHovered ? m_vectorSet.Hover : m_vectorSet.Normal, false);
        }

        public override void onSelected(bool isSelected)
        {
            AnimateScale(isSelected ? m_vectorSet.Select : m_vectorSet.Normal, false);
        }

        public override void onLocked(bool isLocked)
        {
            AnimateScale(isLocked ? m_vectorSet.Lock : m_vectorSet.Normal, false);
        }

        private void OnDisable()
        {
            if (m_scaleTween.isAlive)
                m_scaleTween.Stop();

            if (m_rectTransform != null)
                m_rectTransform.localScale = m_vectorSet.Normal;
        }

        private void AnimateScale(Vector3 targetScale, bool isPressed)
        {
            if (m_rectTransform == null)
                return;

            if (m_scaleTween.isAlive)
                m_scaleTween.Stop();

            m_scaleTween = Tween.Scale(
                m_rectTransform,
                m_rectTransform.localScale,
                targetScale,
                isPressed ? k_pressDuration : k_releaseDuration,
                isPressed ? Ease.OutCubic : Ease.OutBack,
                useUnscaledTime: true);
        }
    }
}
