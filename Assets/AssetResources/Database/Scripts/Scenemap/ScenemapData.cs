using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GameCore.Database
{
    [Serializable]
    public class ScenemapData : Data
    {
        [SerializeField]
        private int m_scenemapNo;
        [SerializeField]
        private string m_scenemapName = string.Empty;
        [SerializeField]
        private string m_scenemapDescription = string.Empty;
        [SerializeField]
        [LabelText("背景圖片")]
        private Sprite m_imgBgReference;

        [SerializeField]
        [LabelText("場景載入物件")]
        private GameObject m_spawnObjectReference;

        [SerializeField]
        [LabelText("關卡選擇小圖")]
        private Sprite m_levelSelectionThumbnail;

        [SerializeField]
        [LabelText("解鎖條件")]
        private FlagReference m_flagReference;

        [SerializeField]
        private string m_imgBgKey;

        public int SceneMapNo => m_scenemapNo;
        public string ScenemapName => m_scenemapName;
        public string ScenemapDescription => m_scenemapDescription;
        public Sprite ImgBgReference => m_imgBgReference;
        public GameObject SpawnObjectReference => m_spawnObjectReference;
        public Sprite LevelSelectionThumbnail => m_levelSelectionThumbnail;


        private List<RoleData> m_cachedEnemies;
        public IReadOnlyList<RoleData> enemies
        {
            get
            {
                if (m_cachedEnemies == null || m_cachedEnemies.Count == 0)
                    LoadRoleDatas();
                return m_cachedEnemies.AsReadOnly();
            }
        }

        void LoadRoleDatas()
        {
            m_cachedEnemies = new List<RoleData>();

            var roles = Database<RoleData>.GetAll();
            foreach (RoleData roleData in roles)
            {
                if (roleData.SceneReference.GetKey() == key)
                {
                    m_cachedEnemies.Add(roleData);
                }
            }
        }

        public bool SceneUnlockValid()
        {
            if (m_flagReference.Exists() == false)
                return true;
            return StorageManager.instance.StorageData.GetFlagStorageValue(m_flagReference.GetKey()) > 0;
        }
#if UNITY_EDITOR

        public void SetSprite()
        {
            // 背景限定使用正式路徑，避免同名搜尋先選到 Foreground 圖片或其 Prefab。
            string path = $"Assets/2DArt/Scene/{m_imgBgKey}.png";
            Sprite background = m_imgBgReference;
            if (background == null || AssetDatabase.GetAssetPath(background) != path)
            {
                background = null;
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                float largestArea = 0f;
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is Sprite sprite)
                    {
                        float area = sprite.rect.width * sprite.rect.height;
                        if (area > largestArea)
                        {
                            background = sprite;
                            largestArea = area;
                        }
                    }
                }
            }

            if (background == null)
            {
                Debug.LogWarning("[Codex驗證] 正式背景路徑沒有可用 Sprite，保留原引用；請檢查場景背景素材。", this);
                return;
            }

            m_imgBgReference = background;
            m_levelSelectionThumbnail = background;
            Debug.Log("[Codex驗證] 場景背景與縮圖已對應正式背景路徑；請確認前景仍使用獨立 Prefab。", this);
        }

        public override string GetTitle([CallerMemberName] string memberName = null)
        {
            return base.GetTitle();
        }
#endif
    }
}
