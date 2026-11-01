#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GameCore;
using GameCore.Database;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class CardTestTool : DeveloperToolBase
{
    public override string DeveloperName => "卡片測試工具";

    [Title("卡片管理")]
    [InfoBox("此工具用於測試卡片能力系統，可以快速獲得或取消角色卡片")]
    
    [LabelText("選擇角色")]
    [ValueDropdown(nameof(GetAllRoles))]
    [OnValueChanged(nameof(OnRoleSelected))]
    public string selectedRoleKey;

    [ShowIf(nameof(HasSelectedRole))]
    [ReadOnly]
    [LabelText("角色名稱")]
    [PropertySpace(SpaceBefore = 10)]
    public string roleName;

    [ShowIf(nameof(HasSelectedRole))]
    [ReadOnly]
    [LabelText("卡片狀態")]
    public string cardStatus;

    [ShowIf(nameof(HasSelectedRole))]
    [ReadOnly]
    [LabelText("卡片能力")]
    [MultiLineProperty(5)]
    public string cardAbilities;

    private bool HasSelectedRole => !string.IsNullOrEmpty(selectedRoleKey);

    /// <summary>
    /// 獲取所有角色列表
    /// </summary>
    private IEnumerable<ValueDropdownItem<string>> GetAllRoles()
    {
        var roleDatas = Database<RoleData>.GetAll();
        if (roleDatas == null || roleDatas.Count == 0)
        {
            return new List<ValueDropdownItem<string>>();
        } 

        return roleDatas.Select(role =>
        {
            var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(role.key);
            bool hasCard = storageData?.CardReward ?? false;
            string status = hasCard ? "✓" : "✗";
            string displayName = $"{status} {role.RoleName} ({role.key})";
            
            return new ValueDropdownItem<string>(displayName, role.key);
        }).OrderBy(item => item.Text);
    }

    /// <summary>
    /// 當選擇角色時更新顯示資訊
    /// </summary>
    private void OnRoleSelected()
    {
        if (string.IsNullOrEmpty(selectedRoleKey))
        {
            roleName = string.Empty;
            cardStatus = string.Empty;
            cardAbilities = string.Empty;
            return;
        }

        if (Database<RoleData>.TryLoad(selectedRoleKey, out var roleData))
        {
            roleName = roleData.RoleName;
            
            var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(selectedRoleKey);
            bool hasCard = storageData?.CardReward ?? false;
            cardStatus = hasCard ? "已獲得 ✓" : "未獲得 ✗";
            
            cardAbilities = roleData.GetCardLocalization();
            if (string.IsNullOrEmpty(cardAbilities))
            {
                cardAbilities = "此角色沒有卡片能力";
            }
        }
        else
        {
            roleName = "找不到角色資料";
            cardStatus = string.Empty;
            cardAbilities = string.Empty;
        }
    }

    [PropertySpace(SpaceBefore = 20)]
    [Title("單一操作")]
    [Button(ButtonSizes.Large, Name = "獲得此卡片")]
    [ShowIf(nameof(CanGetCard))]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    private void GetSelectedCard()
    {
        if (string.IsNullOrEmpty(selectedRoleKey))
            return;

        // 確保該角色有 EnemyStorageData
        var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(selectedRoleKey);
        if (storageData == null)
        {
            // 如果不存在，建立一個新的
            StorageManager.instance.StorageData.AddEnemySatisfyCount(selectedRoleKey, BattleModel.Normal);
            storageData = StorageManager.instance.StorageData.GetEnemyStorageData(selectedRoleKey);
        }

        if (storageData != null)
        {
            storageData.GetCard();
            OnRoleSelected(); // 更新顯示
            Debug.Log($"已獲得卡片：{selectedRoleKey}");
        }
    }

    [Button(ButtonSizes.Large, Name = "取消此卡片")]
    [ShowIf(nameof(CanRemoveCard))]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    private void RemoveSelectedCard()
    {
        if (string.IsNullOrEmpty(selectedRoleKey))
            return;

        var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(selectedRoleKey);
        if (storageData != null)
        {
            storageData.RemoveCard();
            OnRoleSelected(); // 更新顯示
            Debug.Log($"已取消卡片：{selectedRoleKey}");
        }
    }

    private bool CanGetCard()
    {
        if (string.IsNullOrEmpty(selectedRoleKey))
            return false;

        var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(selectedRoleKey);
        return storageData?.CardReward != true;
    }

    private bool CanRemoveCard()
    {
        if (string.IsNullOrEmpty(selectedRoleKey))
            return false;

        var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(selectedRoleKey);
        return storageData?.CardReward == true;
    }

    [PropertySpace(SpaceBefore = 20)]
    [Title("批次操作")]
    [Button(ButtonSizes.Large, Name = "獲得所有卡片")]
    [GUIColor(0.3f, 0.7f, 0.3f)]
    private void GetAllCards()
    {
        var roleDatas = Database<RoleData>.GetAll();
        if (roleDatas == null || roleDatas.Count == 0)
        {
            Debug.LogWarning("沒有找到任何角色資料");
            return;
        }

        int count = 0;
        foreach (var roleData in roleDatas)
        {
            var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(roleData.key);
            if (storageData == null)
            {
                // 如果不存在，建立一個新的
                StorageManager.instance.StorageData.AddEnemySatisfyCount(roleData.key, BattleModel.Normal);
                storageData = StorageManager.instance.StorageData.GetEnemyStorageData(roleData.key);
            }

            if (storageData != null && !storageData.CardReward)
            {
                storageData.GetCard();
                count++;
            }
        }

        OnRoleSelected(); // 更新顯示
        Debug.Log($"已獲得 {count} 張卡片");
    }

    [Button(ButtonSizes.Large, Name = "清除所有卡片")]
    [GUIColor(0.7f, 0.3f, 0.3f)]
    private void RemoveAllCards()
    {
        var enemyStorageDatas = StorageManager.instance.StorageData.GetEnemyStorageDatas();
        if (enemyStorageDatas == null || enemyStorageDatas.Count == 0)
        {
            Debug.LogWarning("沒有找到任何存檔資料");
            return;
        }

        int count = 0;
        foreach (var storageData in enemyStorageDatas)
        {
            if (storageData.CardReward)
            {
                storageData.RemoveCard();
                count++;
            }
        }

        OnRoleSelected(); // 更新顯示
        Debug.Log($"已清除 {count} 張卡片");
    }

    [PropertySpace(SpaceBefore = 20)]
    [Title("卡片統計")]
    [Button(ButtonSizes.Medium, Name = "顯示卡片統計")]
    private void ShowCardStatistics()
    {
        var roleDatas = Database<RoleData>.GetAll();
        if (roleDatas == null || roleDatas.Count == 0)
        {
            Debug.LogWarning("沒有找到任何角色資料");
            return;
        }

        int totalCards = roleDatas.Count;
        int obtainedCards = 0;

        foreach (var roleData in roleDatas)
        {
            var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(roleData.key);
            if (storageData?.CardReward == true)
            {
                obtainedCards++;
            }
        }

        Debug.Log($"卡片統計：已獲得 {obtainedCards}/{totalCards} 張卡片 ({(float)obtainedCards / totalCards * 100:F1}%)");
    }
}
#endif
