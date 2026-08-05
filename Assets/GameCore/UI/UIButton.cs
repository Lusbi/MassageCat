using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    [System.Flags]
    public enum ButtonStateType
    {
        OnClick = 1 << 1,
        OnHover = 1 << 2,
        OnSelect = 1 << 3,
        OnLock = 1 << 4,
    }


    [RequireComponent(typeof(UIEventListener))]
    public class UIButton : MonoBehaviour
    {
        private const float k_pressedScale = 0.94f;
        private const float k_hoverScale = 1.04f;
        private const float k_selectedScale = 1.07f;
        private const float k_pressDuration = 0.06f;
        private const float k_releaseDuration = 0.14f;

        public ButtonStateType buttonStateType { get; protected set; }
        public RectTransform rectTransform { get; protected set; }

        private bool m_hover = false;
        private bool m_selected = false;
        private bool m_locked = false;
        private bool m_pointerDown;
        private bool m_hasScaleFx;
        private bool m_hasFallbackTarget;
        private Vector3 m_normalScale;
        private Vector3 m_fallbackTargetScale;
        private Tween m_fallbackScaleTween;

#if UNITY_EDITOR
        [ContextMenu("Reset UIButton")]
        public void ResetUIButton()
        {
            m_fxBases = GetComponents<ButtonFXBase>();
        }
#endif

        [SerializeField] private ButtonFXBase[] m_fxBases = null;

        public object data { get; set; } = null;
        public UIEventListener eventListener { get; protected set; }

        /// <summary>
        /// 停留狀態
        /// </summary>
        public bool hover
        {
            get
            {
                return m_hover;
            }
            set
            {
                if (m_hover == value)
                {
                    onHover?.Invoke(this, m_hover);
                    return;
                }

                if (m_locked)
                {
                    m_hover = false;
                    ApplyFallbackVisualState();
                    return;
                }

                m_hover = value;
                ApplyRestingFxState();
                ApplyFallbackVisualState();
                onHover?.Invoke(this, m_hover);
            }
        }

        /// <summary>
        /// 選擇狀態
        /// </summary>
        public bool selected
        {
            get
            {
                return m_selected;
            }
            set
            {
                if (m_selected == value)
                {
                    return;
                }

                m_selected = value;
                ApplyRestingFxState();
                ApplyFallbackVisualState();
            }
        }

        /// <summary>
        /// 鎖定狀態
        /// </summary>
        public bool locked
        {
            get
            {
                return m_locked;
            }
            set
            {
                // 不重複執行
                if (m_locked == value)
                {
                    return;
                }

                m_locked = value;
                if (m_locked)
                {
                    m_pointerDown = false;
                    m_hover = false;
                    m_selected = false;
                }

                int effectCount = m_fxBases == null ? 0 : m_fxBases.Length;
                for (int i = 0; i < effectCount; i++)
                {
                    ButtonFXBase fxBase = m_fxBases[i];
                    if (fxBase == null)
                        continue;
                    fxBase.onLocked(m_locked);
                }

                ApplyFallbackVisualState();
            }
        }

        /// <summary>
        /// 快顯提示位置
        /// </summary>
        private Vector2 m_tipPos = Vector2.zero;
        private bool setTipPos = false;

        public Action<UIButton> onClicked;
        public Action<UIButton> onRightClicked;
        public Action<UIButton, Vector2> onClickDown;
        public Action<UIButton, Vector2> onClickUp;
        public Action<UIButton, bool> onHover;

        [Tooltip("按鍵文字")]
        public Text buttonText;
        [Tooltip("按鍵文字")]
        public TextMeshProUGUI textMeshProUGUI;

        private void OnEnable()
        {
            if (m_fxBases == null || m_fxBases.Length == 0)
                m_fxBases = GetComponents<ButtonFXBase>();

            m_hasScaleFx = false;
            for (int i = 0, count = m_fxBases.Length; i < count; i++)
            {
                if (m_fxBases[i] is ButtonFXScale)
                {
                    m_hasScaleFx = true;
                    break;
                }
            }

            m_pointerDown = false;
            ApplyRestingFxState();
            ApplyFallbackVisualState();
        }

        private void OnDisable()
        {
            if (m_fallbackScaleTween.isAlive)
                m_fallbackScaleTween.Stop();

            m_pointerDown = false;
            m_hover = false;
            m_hasFallbackTarget = false;
            if (rectTransform != null && m_hasScaleFx == false)
                rectTransform.localScale = m_normalScale;
        }

        public void SetButtonText(string text)
        {
            if (buttonText == null)
            {
                buttonText = GetComponentInChildren<Text>();
            }
            if (buttonText != null)
            {
                buttonText.text = text;
            }

            if (textMeshProUGUI == null)
            {
                textMeshProUGUI = GetComponentInChildren<TextMeshProUGUI>();
            }
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.SetText(text);
            }
        }

        /// <summary>
        /// 取得quickTip顯示位置
        /// </summary>
        public Vector2 GetTipPos()
        {
            if (!setTipPos)
            {
                setTipPos = true;
            }

            return m_tipPos;
        }

        public void Show(bool show)
        {
            //selected = false;
            //hover = false;

            gameObject.SetActive(show);
        }

        public void ClearEvents()
        {
            onClicked = null;
            onRightClicked = null;
            onClickDown = null;
            onClickUp = null;
            onHover = null;
        }
        /// <summary>
        /// 將 Button 狀態重設為 Normal 
        /// 再依據 Hover、Select、Lock 狀態設定
        /// </summary>
        public void ResetButtonState()
        {
            hover = false;
            selected = false;
            locked = false;
        }

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            m_normalScale = transform.localScale;

            eventListener = UIEventListener.Get(gameObject);
            eventListener.onClick = OnClick;
            eventListener.onRightClick = OnRightClick;
            eventListener.onDown = OnPointerDown;
            eventListener.onUp = OnPointerUp;
            eventListener.onEnter = OnPointerEnter;
            eventListener.onExit = OnPointerExit;
        }

        private void OnClick(GameObject obj)
        {
            if (m_locked)
            {
                return;
            }

            onClicked?.Invoke(this);
        }
        private void OnRightClick(GameObject obj)
        {
            if (m_locked)
            {
                return;
            }

            onRightClicked?.Invoke(this);
        }
        private void OnPointerDown(GameObject obj, Vector2 position)
        {
            if (m_locked)
            {
                return;
            }

            m_pointerDown = true;
            for (int i = 0, count = m_fxBases.Length; i < count; i++)
            {
                ButtonFXBase fxBase = m_fxBases[i];
                if (fxBase != null)
                    fxBase.onClickDown();
            }

            ApplyFallbackVisualState();

            if (onClickDown != null)
            {
                onClickDown(this, position);
            }
        }
        private void OnPointerUp(GameObject obj, Vector2 position)
        {
            if (m_locked)
            {
                return;
            }

            m_pointerDown = false;
            for (int i = 0, count = m_fxBases.Length; i < count; i++)
            {
                ButtonFXBase fxBase = m_fxBases[i];
                if (fxBase != null)
                    fxBase.onClickUp();
            }

            ApplyRestingFxState();
            ApplyFallbackVisualState();

            if (onClickUp != null)
            {
                onClickUp(this, position);
            }
        }
        private void OnPointerEnter(GameObject obj)
        {
            hover = true;
        }
        private void OnPointerExit(GameObject obj)
        {
            hover = false;
        }

        private void ApplyRestingFxState()
        {
            if (m_pointerDown || m_locked || m_fxBases == null)
                return;

            for (int i = 0, count = m_fxBases.Length; i < count; i++)
            {
                ButtonFXBase fxBase = m_fxBases[i];
                if (fxBase == null)
                    continue;

                if (m_selected)
                    fxBase.onSelected(true);
                else
                    fxBase.onHover(m_hover);
            }
        }

        private void ApplyFallbackVisualState()
        {
            if (rectTransform == null || m_hasScaleFx)
                return;

            float scale = 1f;
            if (m_locked == false)
            {
                if (m_pointerDown)
                    scale = k_pressedScale;
                else if (m_selected)
                    scale = k_selectedScale;
                else if (m_hover)
                    scale = k_hoverScale;
            }

            Vector3 targetScale = m_normalScale * scale;
            if (m_hasFallbackTarget && m_fallbackTargetScale == targetScale)
                return;

            m_hasFallbackTarget = true;
            m_fallbackTargetScale = targetScale;
            if (m_fallbackScaleTween.isAlive)
                m_fallbackScaleTween.Stop();

            float duration = m_pointerDown ? k_pressDuration : k_releaseDuration;
            Ease ease = m_pointerDown ? Ease.OutCubic : Ease.OutBack;
            m_fallbackScaleTween = Tween.Scale(
                rectTransform,
                rectTransform.localScale,
                targetScale,
                duration,
                ease,
                useUnscaledTime: true);
        }

    }
}
