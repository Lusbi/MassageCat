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
            string[] guids = AssetDatabase.FindAssets($"t:Sprite {m_imgBgKey}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_imgBgReference = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                m_levelSelectionThumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            else
            {
                Debug.LogWarning($"No sprite found for key: {m_imgBgKey}");
                m_imgBgReference = null;
            }
        }

        public override string GetTitle([CallerMemberName] string memberName = null)
        {
            return base.GetTitle();
        }
#endif
    }
}