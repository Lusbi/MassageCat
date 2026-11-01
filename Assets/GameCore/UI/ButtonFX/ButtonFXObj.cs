using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    [AddComponentMenu("UI/ButtonFX/Gameobject"), RequireComponent(typeof(UIButton))]
    public class ButtonFXObj : ButtonFXBase
    {
        // 先設一組
        [SerializeField] private GameObjectSet gameObjectSet = null;

        public override void onClickDown()
        {
            if (gameObjectSet == null)
            {
                return;
            }

            if (gameObjectSet.Clicked)
            {
                gameObjectSet.Clicked.SetActive(true);
            }
        }

        public override void onClickUp()
        {
            if (gameObjectSet == null)
            {
                return;
            }

            if (gameObjectSet.Clicked)
            {
                gameObjectSet.Clicked.SetActive(false);
            }
        }

        public override void onHover(bool isHovered)
        {
            if (gameObjectSet == null)
            {
                return;
            }

            if (gameObjectSet.Hover)
            {
                gameObjectSet.Hover.SetActive(isHovered);
            }
        }

        public override void onLocked(bool isLocked)
        {
            if (gameObjectSet == null)
            {
                return;
            }

            if (gameObjectSet.Lock)
            {
                gameObjectSet.Lock.SetActive(isLocked);
            }
        }

        public override void onSelected(bool isSelected)
        {
            if (gameObjectSet == null)
            {
                return;
            }

            if (gameObjectSet.Selected)
            {
                gameObjectSet.Selected.SetActive(isSelected);
            }
        }
    }
}
