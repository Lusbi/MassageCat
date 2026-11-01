#if UNITY_EDITOR
using System;
using UnityEditor;

namespace GameCore.AssetPostProcessor
{
    [Serializable]
    public abstract class GameCoreAssetPostprocessor : AssetPostprocessor
    {
        public abstract AssetPostProcessorType AssetPostProcessorType { get; } 

        public abstract void Execute();

        /// <summary>
        /// 路徑檢查
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool Valid<T>(T asset) where T : UnityEngine.Object
        {
            AssetPostProcessorInfo assetPostProcessorInfo = AssetPostprocessorSettings.GetProcessortInfo(AssetPostProcessorType);
            if (assetPostProcessorInfo == null)
            {
                return false;
            }
            // 不執行
            if (assetPostProcessorInfo.isWork == false)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            bool result = false;
            
            foreach (string folderName in assetPostProcessorInfo.folderPaths)
            {
                if (path.Contains(folderName))      // -> Contains vs IndexOf
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}
#endif