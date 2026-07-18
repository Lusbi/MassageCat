using UnityEngine;

namespace GameCore.UI
{
    [AddComponentMenu("UI/ButtonFX/Scale"), RequireComponent(typeof(UIButton))]
    public class ButtonFXScale : ButtonFXBase
    {
        [SerializeField] private RectTransform m_rectTransform = null;
        [SerializeField] private ScaleSet m_vectorSet = new ScaleSet();

        private void Awake()
        {
            if (m_rectTransform == null)
            {
                m_rectTransform = GetComponent<RectTransform>();
            }
        }

        public override void onClickDown()
        {
            if (m_rectTransform == null)
                return;

            m_rectTransform.localScale = m_vectorSet.Click;
        }

        public override void onClickUp()
        {
            if (m_rectTransform == null)
                return;

            m_rectTransform.localScale = (m_isHover) ? m_vectorSet.Hover : m_vectorSet.Normal;
        }

        public override void onHover(bool isHovered)
        {
            if (m_rectTransform == null)
                return;

            m_isHover = isHovered;

            m_rectTransform.localScale = isHovered ? m_vectorSet.Hover : m_vectorSet.Normal;
        }

        public override void onSelected(bool isSelected)
        {
            if (m_rectTransform == null)
                return;
            
            m_rectTransform.localScale = isSelected ? m_vectorSet.Select : m_vectorSet.Normal;
        }

        public override void onLocked(bool isLocked)
        {
            if (m_rectTransform == null)
                return;

            m_rectTransform.localScale = isLocked ? m_vectorSet.Lock : m_vectorSet.Normal;
        }
    }
}