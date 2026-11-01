#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameCore.Database.Dependency
{
    [JsonObject(MemberSerialization.OptIn)]
    [Serializable]
    public class DependencyInfo : IComparable<DependencyInfo>, IEquatable<DependencyInfo>
    {
        public Type GroupType { get; private set; }

        public string Name => m_name;

        public long LocalId => m_localId;

        public string AssetPath => string.IsNullOrEmpty(m_assetPath) ? m_assetPath = AssetDatabase.GUIDToAssetPath(m_guid) : m_assetPath;

        /// <summary>Hash code generated from GUID and local file ID.</summary>
        public int ObjectHash { get; private set; }

        [InlineButton(nameof(OnSelectAsset), "Select", ShowIf = nameof(Asset))]
        [InlineButton(nameof(OnOpenAsset), "Open", ShowIf = nameof(Asset))]
        [HideLabel]
        [DisplayAsString]
        [SerializeField]
        [JsonProperty("Name")]
        private string m_name;

        [ReadOnly]
        [SerializeField]
        [JsonProperty("GUID")]
        private string m_guid;

        [ReadOnly]
        [SerializeField]
        [JsonProperty("Local ID")]
        private long m_localId;

        [LabelText(nameof(GroupType))]
        [ReadOnly]
        [SerializeField]
        [JsonProperty("Group Type")]
        private string m_groupTypeAQN;

        private string m_assetPath;
        private Object m_asset;

        private Object Asset
        {
            get
            {
                if (!m_asset)
                {
                    m_asset = AssetDatabase.LoadAssetAtPath<Object>(AssetPath);
                    if (m_asset && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_asset, out string guid, out long localId))
                    {
                        if (localId != m_localId)
                        {
                            foreach (var subasset in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetPath))
                            {
                                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(subasset, out guid, out localId) && localId == m_localId)
                                {
                                    m_asset = subasset;
                                    break;
                                }
                            }
                        }
                    }
                }
                return m_asset;
            }
        }

        public DependencyInfo(Type groupType, string name, string guid, long localId)
        {
            GroupType = groupType;
            m_name = name;
            m_guid = guid;
            m_localId = localId;
            ObjectHash = HashCode.Combine(m_guid, m_localId);
        }

        public int CompareTo(DependencyInfo other)
        {
            int result = GroupType.Name.CompareTo(other.GroupType.Name);
            if (result != 0) return result;
            result = m_name.CompareTo(other.m_name);
            if (result != 0) return result;
            result = m_guid.CompareTo(other.m_guid);
            if (result != 0) return result;
            result = m_localId.CompareTo(other.m_localId);
            return result;
        }

        public bool Equals(DependencyInfo other)
        {
            if (other == null)
                return false;
            return GroupType == other.GroupType && m_name == other.m_name && m_guid == other.m_guid && m_localId == other.m_localId;
        }

        public override bool Equals(object other) => other is DependencyInfo info && Equals(info);

        public override int GetHashCode() => HashCode.Combine(GroupType, m_name, m_guid, m_localId);

        public override string ToString()
        {
            return $"{GroupType}: {m_name}, GUID: {m_guid}, Local ID: {m_localId}, Path: {AssetPath}";
        }

        public static bool operator ==(DependencyInfo left, DependencyInfo right)
        {
            return EqualityComparer<DependencyInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(DependencyInfo left, DependencyInfo right) => !(left == right);

        public void OnOpenAsset()
        {
            if (!Asset)
                return;

            AssetDatabase.OpenAsset(Asset);
        }

        public void OnSelectAsset()
        {
            if (!Asset)
                return;

            Selection.activeObject = Asset;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(Asset);
        }

        public bool ValidateAsset()
        {
            if (GroupType != null && GroupType.IsSubclassOf(typeof(Data)))
                return IsLocalIdInSubassets(m_localId, AssetPath);
            else
                return File.Exists(AssetPath);
        }

        private static bool IsLocalIdInSubassets(long localId, string mainAssetPath)
        {
            var subassets = AssetDatabase.LoadAllAssetRepresentationsAtPath(mainAssetPath);
            foreach (var subasset in subassets)
            {
                if (!subasset)
                {
                    Debug.LogError(mainAssetPath);
                    continue;
                }
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(subasset, out _, out long id) && id == localId)
                    return true;
            }
            return false;
        }

        [OnSerializing]
        private void OnBeforeSerialize(StreamingContext context)
        {
            m_groupTypeAQN = GroupType?.AssemblyQualifiedName;
        }

        [OnDeserialized]
        private void OnAfterDeserialize(StreamingContext context)
        {
            GroupType = Type.GetType(m_groupTypeAQN);
            ObjectHash = HashCode.Combine(m_guid, m_localId);
        }
    }
}
#endif