using UnityEngine;

namespace GameCore.UI
{
    [AddComponentMenu("UI/ButtonFX/Alpha(Image)"), RequireComponent(typeof(UIButton))]
    public class ButtonFXImageAlpha : ButtonFXBase
    {
        [SerializeField] private ImageSet imageSet = null;

        public override void onClickDown()
        {
            if (imageSet == null)
            {
                return;
            }

            if (imageSet.clicked != null)
            {
                imageSet.clicked.color = Color.white;
            }
        }

        public override void onClickUp()
        {
            if (imageSet == null)
            {
                return;
            }

            if (imageSet.clicked != null)
            {
                imageSet.clicked.color = Color.clear;
            }
        }

        public override void onHover(bool isHovered)
        {
            if (imageSet == null)
            {
                return;
            }

            if (imageSet.hover != null)
            {
                imageSet.hover.color = isHovered ? Color.white : Color.clear;
            }
        }

        public override void onLocked(bool isLocked)
        {
            if (imageSet == null)
            {
                return;
            }

            if (imageSet.locked != null)
            {
                imageSet.locked.color = isLocked ? Color.white : Color.clear;
            }
        }

        public override void onSelected(bool isSelected)
        {
            if (imageSet == null)
            {
                return;
            }

            if (imageSet.selected != null)
            {
                imageSet.selected.color = isSelected ? Color.white : Color.clear;
            }
        }
    }
}
