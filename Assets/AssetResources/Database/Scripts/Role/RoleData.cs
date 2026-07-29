using System;
using System.Collections.Generic;
using System.Numerics;
using BigMath;
using GameCore.Log;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameCore.Database
{
    public enum EnemyType
    {
        Normal,
        MiniBoss,
        Boss
    }

    [Serializable]
    public class RoleData : Data
    {
        [SerializeField]
        private int m_roleSortId = 0;
        [HideInInspector]
        [SerializeField]
        private string m_roleName = string.Empty;
        [HideInInspector]
        [SerializeField]
        private string m_roleDescription = string.Empty;
        [LabelText("敵人類型")]
        [SerializeField]
        private EnemyType m_enemyType = EnemyType.Normal;
        [LabelText("場景參考")]
        [SerializeField]
        private ScenemapReference m_sceneReference;
        [LabelText("療癒需求值")]
        [FormerlySerializedAs("m_hitCount")]
        [FormerlySerializedAs("m_fatigue")]
        [SerializeField]
        private string m_requiredHealValue = "0";
        [LabelText("敵人小圖示")]
        [SerializeField]
        private Sprite m_enemyIcon;
        [LabelText("敵人面向")]
        [SerializeField]
        private DirectionType m_directionType = DirectionType.Left;
        [LabelText("生成條件")]
        [SerializeField]
        private FlagReference[] m_flagReferenceConditions;
        [LabelText("擊殺後增加旗標")]
        [SerializeField]
        private FlagReference m_satisfyToAddFlagReference;
        [LabelText("擊殺增加傷害")]
        [SerializeField]
        private int m_satisfyBonus = 1;

        [LabelText("蕃茄鐘擊殺增加傷害")]
        [SerializeField]
        private int m_tomatoBouns = 1;

        [LabelText("死亡後觸發事件類型")]
        [SerializeField]
        private DieActionType m_dieActionType;

        [SerializeField , LabelText("死亡後變更敵人") , ShowIf(nameof(m_dieActionType) , DieActionType.ChangeStyle)]
        private string m_changeStyleKey;
        [SerializeField, LabelText("死亡後觸發劇情事件"), ShowIf(nameof(m_dieActionType), DieActionType.Drama)]
        private string m_dramaKey;

        [SerializeField]
        private string m_roleIconkey;

        [SerializeField , LabelText("對話劇情") , InlineEditor]
        private TalkScriptableObject m_talkScriptableObject;

        [SerializeField , TabGroup("卡片附加能力")]
        private List<RoleCardInfo> m_roleCardInfos = new List<RoleCardInfo>();

        public int roleSortId => m_roleSortId;
        public string RoleName => m_roleName;
        public string RoleDescription => m_roleDescription;
        public EnemyType EnemyType => m_enemyType;
        public ScenemapReference SceneReference => m_sceneReference;
        public BigNumber RequiredHealValue => new BigNumber(BigInteger.Parse(m_requiredHealValue));
        public DirectionType DirectionType => m_directionType;

        public FlagReference SatisfyToAddFlagReference => m_satisfyToAddFlagReference;
        public Sprite EnemyIcon => m_enemyIcon;
        public int SatisfyBonus => m_satisfyBonus;
        public int TomatoBonus => m_tomatoBouns;
        public TalkScriptableObject TalkScriptableObject => m_talkScriptableObject;
        public List<RoleCardInfo> RoleCardInfos => m_roleCardInfos;
        public bool ValidateFlagReferenceConditions()
        {
            if (m_flagReferenceConditions == null || m_flagReferenceConditions.Length == 0)
            {
                return true;
            }
            bool result = true;
            foreach (var flag in m_flagReferenceConditions)
            {
                if (flag.TryLoad(out var flagData))
                {
                    result &= StorageManager.instance.StorageData.GetFlagStorageValue(flagData.key) > 0;
                }
                else
                {
                    eLog.Error($"無旗標資料：{flag.GetKey()}");
                    result = false;
                }
            }
            return result;
        }

        // 取得卡片加成
        public float GetBonus(CardFormulaInfoType cardFormulaInfoType)
        {
            float bonus = 0;
            foreach (var cardInfo in m_roleCardInfos)
            {
                if (cardInfo.cardFormulaInfoType.HasFlag(cardFormulaInfoType))
                {
                    bonus += cardInfo.bonusValue;
                }
            }
            return bonus;
        }

        public string GetCardLocalization()
        {
            string localization = string.Empty;
            foreach (var cardInfo in m_roleCardInfos)
            {
                localization += cardInfo.GetLocalization() + "\n";
            }
            return localization.TrimEnd('\n');
        }

#if UNITY_EDITOR
        public void Editor_SetTalkScriptableObject(TalkScriptableObject talkScriptableObject)
        {
            m_talkScriptableObject = talkScriptableObject;
            EditorUtility.SetDirty(this);
        }

        public void SetSprite()
        {
            string[] guids = AssetDatabase.FindAssets($"t:Sprite {m_roleIconkey}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_enemyIcon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            else
            {
                Debug.LogWarning($"No sprite found for key: {m_roleIconkey}");
                m_enemyIcon = null;
            }
        }

        public override string GetLog()
        {
            return $"{base.GetLog()}, roleName = {m_roleName}, roleDescription = {m_roleDescription}, enemyType = {m_enemyType}, sceneReference = {m_sceneReference}, requiredHealValue = {m_requiredHealValue}";
        }
#endif
    }
}
