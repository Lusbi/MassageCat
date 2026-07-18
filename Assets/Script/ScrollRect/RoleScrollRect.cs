using System.Collections.Generic;
using GameCore.Database;
using SuperScrollView;
using UnityEngine;

public class RoleScrollRect : MonoBehaviour
{
    [SerializeField] private LoopListView2 m_loopListview2;
    private DataSourceMgr<RoleInfo> m_dataSourceMgr;
    private List<RoleInfo> m_filteredDataList = null;
    private bool m_isInitialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void Start()
    {
        EnsureInitialized();
    }

    private bool EnsureInitialized()
    {
        if (m_isInitialized)
            return true;

        if (m_loopListview2 == null)
            return false;

        var datas = Database<RoleData>.GetAll();
        m_dataSourceMgr = new DataSourceMgr<RoleInfo>(datas.Count);
        m_filteredDataList = new List<RoleInfo>();
        foreach (var roleData in datas)
        {
            RoleInfo enemyInfo = new RoleInfo();
            enemyInfo.roleData = roleData;
            m_filteredDataList.Add(enemyInfo);
        }
        m_filteredDataList.Sort((x, y) =>
        {
            return x.roleData.roleSortId.CompareTo(y.roleData.roleSortId);
        });
        m_loopListview2.InitListView(m_dataSourceMgr.TotalItemCount, OnGetItemByIndex);
        m_isInitialized = true;
        return true;
    }

    private LoopListViewItem2 OnGetItemByIndex(LoopListView2 view, int index)
    {
        if (m_filteredDataList == null || index < 0 || index >= m_filteredDataList.Count)
            return null;

        LoopListViewItem2 item = view.NewListViewItem("RoleItemHUD");
        if (item == null)
        {
            return null;
        }

        RoleItemHUD roleItemHUD = item.GetComponent<RoleItemHUD>();
        if (item.IsInitHandlerCalled == false)
        {
            item.IsInitHandlerCalled = true;
            roleItemHUD.Initlization();
        }

        roleItemHUD.Apply(m_filteredDataList[index]);
        return item;
    }

    /// <summary>
    /// 更新列表裡的項目
    /// </summary>
    internal void ListUpdate()
    {
        if (EnsureInitialized() == false)
            return;

        m_loopListview2.RefreshAllShownItem();
    }

    /// <summary>
    /// 將 ScrollRect 中的角色顯示移至畫面最左方
    /// </summary>
    /// <param name="data"></param>
    internal void ListMoveToTop(RoleData data)
    {
        if (EnsureInitialized() == false || data == null)
            return;

        int targetIndex = -1;
        for (int i = 0; i < m_filteredDataList.Count; i++)
        {
            if (m_filteredDataList[i].roleData == data)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex >= 0)
            m_loopListview2.MovePanelToItemIndex(targetIndex, 0f);
    }
}
