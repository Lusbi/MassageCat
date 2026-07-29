using System;
using System.Collections.Generic;
using System.IO;
using GameCore.Database;
using UnityEngine;

namespace MassageCat.Gallery
{
    internal static class GalleryStateUtility
    {
        private const string k_desktopPetProductName = "WalkBrave";
        private const string k_storageFileName = "StorageData.json";

        internal static GalleryStateSnapshot Capture(BattleModel battleModel)
        {
            StorageData storageData = StorageManager.instance.StorageData;
            Dictionary<string, RoleData>.ValueCollection roleDatas = Database<RoleData>.GetAll();
            int roleCount = roleDatas != null ? roleDatas.Count : 0;
            GalleryRoleState[] roleStates = new GalleryRoleState[roleCount];

            int index = 0;
            if (roleDatas != null)
            {
                foreach (RoleData roleData in roleDatas)
                {
                    EnemyStorageData enemyStorageData = storageData.GetEnemyStorageData(roleData.key);
                    roleStates[index++] = new GalleryRoleState
                    {
                        roleKey = roleData.key,
                        unlocked = roleData.ValidateFlagReferenceConditions(),
                        cardReward = enemyStorageData != null && enemyStorageData.CardReward,
                        tomatoClear = enemyStorageData != null && enemyStorageData.goldenClear,
                    };
                }
            }

            return new GalleryStateSnapshot
            {
                currentRoleKey = storageData.BattleStorageData.EnemyKey,
                battleModel = (int)battleModel,
                roles = roleStates,
            };
        }

        internal static StorageData LoadDesktopPetStorageReadOnly()
        {
            string path = GetDesktopPetStoragePath();
            if (File.Exists(path) == false)
                return new StorageData();

            try
            {
                string json = File.ReadAllText(path);
                StorageData storageData = JsonUtility.FromJson<StorageData>(json);
                return storageData ?? new StorageData();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"讀取桌寵存檔失敗：{exception.Message}");
                return new StorageData();
            }
        }

        internal static string GetDesktopPetStoragePath()
        {
            DirectoryInfo galleryProductDirectory = Directory.GetParent(Application.persistentDataPath);
            if (galleryProductDirectory == null)
                return Path.Combine(Application.persistentDataPath, k_storageFileName);

            return Path.Combine(galleryProductDirectory.FullName, k_desktopPetProductName, k_storageFileName);
        }
    }

    internal static class GalleryStateChangeNotifier
    {
        internal static event Action StateChanged;

        internal static void Notify()
        {
            StateChanged?.Invoke();
        }
    }
}
