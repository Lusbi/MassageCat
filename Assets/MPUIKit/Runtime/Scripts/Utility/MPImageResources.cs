using UnityEngine;

namespace MPUIKIT
{
    /// <summary>
    /// Resources 僅保存設定，預設 Sprite 的實體貼圖存放於美術目錄。
    /// </summary>
    internal sealed class MPImageResources : ScriptableObject
    {
        [SerializeField] private Sprite m_emptySprite;

        internal Sprite EmptySprite => m_emptySprite;
    }
}
