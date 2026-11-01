#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameCore.AssetPostProcessor
{
    [CreateAssetMenu( fileName = "AssetPostProcessorSetting" , menuName = "GameCoreSettings/AssetPostProcessorSetting") ]
    public class AssetPostprocessorSettings : ScriptableObject
    {
        [SerializeField , HideLabel]
        public List<AssetPostProcessorInfo> postProcessorInfos = new List<AssetPostProcessorInfo>();

        private static AssetPostprocessorSettings m_settings;

        private void OnEnable()
        {
            TryLoadSettings();
        }

        public static AssetPostProcessorInfo GetProcessortInfo(AssetPostProcessorType assetPostProcessorType)
        {
            if (TryLoadSettings() == false)
            {
                return null;
            }
            return m_settings.postProcessorInfos.FirstOrDefault(x => x.assetPostProcessorType == assetPostProcessorType);
        }

        private static bool TryLoadSettings()
        {
            if (m_settings == null)
            {
                m_settings = AssetDataBaseUtils.GetAsset<AssetPostprocessorSettings>("t:AssetPostprocessorSettings");
            }
            return m_settings != null;
        }
    }
}
#endif