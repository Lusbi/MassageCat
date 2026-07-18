using System;
using UnityEngine;

namespace GameCore.Database
{
    [Serializable]
    public abstract partial class Data : ScriptableObject, IComparable<Data>
    {
#if UNITY_EDITOR
        [HideInInspector]
#endif
        public string key;

        public int CompareTo(Data other)
        {
            if (other == null)
            {
                return 1;
            }
            else
            {
                if (key.Length == other.key.Length)
                {
                    return string.Compare(key, other.key, StringComparison.Ordinal);
                }
                else
                {
                    return key.Length.CompareTo(other.key.Length);
                }
            }
        }
    }
}