#if UNITY_EDITOR
using System;
using UnityEditor;

namespace GameCore.Database.Editor
{
    [Serializable]
    public sealed class NaturalComparer : StringComparer
    {
        public static NaturalComparer Default { get; private set; } = new();

        public override int Compare(string x, string y) => EditorUtility.NaturalCompare(x, y);

        public override bool Equals(string x, string y) => EditorUtility.NaturalCompare(x, y) == 0;

        public override int GetHashCode(string obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return obj.GetHashCode();
        }
    }
}
#endif