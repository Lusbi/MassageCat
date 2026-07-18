using UnityEngine;
using PrimeTween;

namespace GameCore.UI
{
    public class UIViewBase : MonoBehaviour, IInitlization
    {
#if UNITY_EDITOR
        [ContextMenu("Reset UIView Reference")]
        private void ResetUIViewReference()
        {
            CacheUIViews();
        }
#endif
        protected virtual void CacheUIViews()
        {
            rectTransform = gameObject.GetComponent<RectTransform>();
            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            canvas = gameObject.GetComponent<Canvas>();
            viewAnimation = gameObject.GetComponent<Animation>();
        }
        public virtual string animation_enable { get { return "Enable"; } }
        public virtual string animation_disable { get { return "Disable"; } }

        public bool active { get; protected set; } = false;
        public Canvas canvas;
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;
        public Animation viewAnimation;
        public UIStatus currentStatus { get; protected set; } = UIStatus.None;

        //protected Tweener m_tweener;
        protected Tween m_tween;
        
        protected float m_alphaDurationTime = 0.5f;
        /// <summary>
        /// 是否含有介面開關動畫
        /// </summary>
        public bool HasAnimation { get { return viewAnimation != null; } }

        public virtual void Initlization(System.Action callBack = null)
        {
            
        }

        public void SetSortOrder(int sortingOrder)
        {
            if (canvas != null)
            {
                canvas.sortingOrder = sortingOrder;
            }
        }
        public virtual void Active(bool isActive) 
        {
            if (active == isActive)
            {
                return;
            }
            if (currentStatus == UIStatus.Animationing)
            {
                return;
            }

            active = isActive;

            if (active) ViewEnable(); else ViewDisable();
        }

        public virtual void ViewEnable()
        {
            currentStatus = UIStatus.Enable;

            //m_tweener?.Kill();
            
            if (HasAnimation)
            {
                AnimationClip animationClip = viewAnimation.GetClip(animation_enable);
                if (animationClip != null)
                {
                    Tween.Custom(0f, 1f, animationClip.length, AnimationTweener_Enable).OnComplete(TweenerComplate);
                    return;
                }
                Log.eLog.Error("[介面] 找不到開啟動畫檔 ..." + gameObject.name);
            }

            Tween.Custom(0f, 1f, m_alphaDurationTime, AlphaTweener).OnComplete(TweenerComplate);
        }
        public virtual void ViewDisable()
        {
            currentStatus = UIStatus.Disable;
            if (HasAnimation)
            {
                AnimationClip animationClip = viewAnimation.GetClip(animation_disable);
                if (animationClip != null)
                {
                    Tween.Custom(0f, 1f, animationClip.length, AnimationTweener_Disable).OnComplete(TweenerComplate);
                    return;
                }
                Log.eLog.Error("[介面] 找不到關閉動畫檔 ..." + gameObject.name);
            }

            Tween.Custom(1f, 0f, m_alphaDurationTime, AlphaTweener).OnComplete(TweenerComplate);
        }


        private void AnimationTweener_Enable(float time)
        {
            if (viewAnimation.isPlaying == false)
            {
                viewAnimation.Play(animation_enable);
                currentStatus = UIStatus.Animationing;
            }
        }

        private void AnimationTweener_Disable(float time)
        {
            if (viewAnimation.isPlaying == false)
            {
                viewAnimation.Play(animation_disable);
                currentStatus = UIStatus.Animationing;
            }
        }

        private void AlphaTweener(float time)
        {
            canvasGroup.alpha = time;
            currentStatus = UIStatus.Animationing;
        }

        private void TweenerComplate()
        {
            currentStatus = canvasGroup.alpha == 1 ? UIStatus.Enable : UIStatus.Disable;
            canvasGroup.blocksRaycasts = currentStatus == UIStatus.Enable;
        }
    }
}