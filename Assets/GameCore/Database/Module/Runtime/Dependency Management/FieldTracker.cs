#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using GameCore.SystemType.Extensions;

namespace GameCore.Database.Dependency
{
    public class FieldInfoPath
    {
        internal bool containsArray;
        internal Type FinalType => m_list.Last?.Value.FieldType;

        private readonly LinkedList<FieldInfo> m_list;
        private readonly Type m_targetType;

        internal FieldInfoPath(Type targetType)
        {
            m_list = new LinkedList<FieldInfo>();
            m_targetType = targetType;
        }

        internal FieldInfoPath(FieldInfoPath other)
        {
            m_list = new LinkedList<FieldInfo>(other.m_list);
            m_targetType = other.m_targetType;
            containsArray = other.containsArray;
        }

        public FieldInfoPath Clone() => new(this);

        public override string ToString() => string.Join(" -> ", m_list.Select(x => x.Name));

        internal void Add(FieldInfo fieldInfo) => m_list.AddLast(fieldInfo);

        internal void Remove() => m_list.RemoveLast();

        /// <summary>
        /// Walks through path itself and retrieves value(s).
        /// </summary>
        /// <param name="obj">Object instance to get value from.</param>
        /// <param name="valueCallback">Callback invoked to propagate value(s). Returns true to interrupt.</param>
        internal void Walk(object obj, Func<object, bool> valueCallback)
        {
            if (valueCallback == null)
                return;

            if (containsArray)
            {
                WalkArray(obj, valueCallback);
                return;
            }

            foreach (var fieldInfo in m_list)
            {
                obj = fieldInfo.GetValue(obj);
                if (obj == null)
                    break;
            }
            valueCallback(obj);
        }

        private void WalkArray(object obj, Func<object, bool> valueCallback)
        {
            Queue<LinkedListNode<FieldInfo>> nodeQ = new();
            nodeQ.Enqueue(m_list.First);
            Queue<object> objQ = new();
            objQ.Enqueue(obj);
            LinkedListNode<FieldInfo> currentNode;
            object currentObj;
            FieldInfo fieldInfo;
            while (nodeQ.Count > 0)
            {
                currentNode = nodeQ.Dequeue();
                currentObj = objQ.Dequeue();
                fieldInfo = currentNode.Value;
                currentObj = fieldInfo.GetValue(currentObj);
                if (currentObj is IList list && fieldInfo.FieldType.TryGetElementTypeOfArrayOrList(out Type elementType))
                {
                    if (elementType.IsDerivedFrom(m_targetType))
                    {
                        foreach (var item in list)
                        {
                            if (valueCallback(item))
                                break;
                        }
                    }
                    else
                    {
                        var next = currentNode.Next;
                        foreach (var item in list)
                        {
                            if (item == null)
                                continue;
                            nodeQ.Enqueue(next);
                            objQ.Enqueue(item);
                        }
                    }
                }
                else if (currentNode.Next != null && currentObj != null)
                {
                    nodeQ.Enqueue(currentNode.Next);
                    objQ.Enqueue(currentObj);
                }
                else
                {
                    if (valueCallback(currentObj))
                        return;
                }
            }
        }
    }

    /// <summary>
    /// Tracks fields of <see cref="Type"/>s derived from specified source type.
    /// </summary>
    public class FieldTracker
    {
        private const BindingFlags BINDING_ATTR = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string PREDEFINED_ASSEMBLY_NAME = "Assembly-CSharp";
        private readonly Dictionary<Type, IList<FieldInfoPath>> m_trackedTypes = new();
        private readonly string[] m_scopedNamespacePrefixes;

        /// <summary>
        /// Tracks fields of <see cref="Type"/>s derived from source type.
        /// </summary>
        /// <param name="targetType">The <see cref="Type"/> from which <see cref="FieldInfo.FieldType"/>s of tracked fields are derived.</param>
        /// <param name="sourceType">The source parent <see cref="Type"/> to be scanned.</param>
        /// <param name="scopedNamespacePrefixes">Optional namespace prefixes to narrow the coverage of scan.</param>
        public FieldTracker(Type targetType, Type sourceType, string[] scopedNamespacePrefixes = null)
        {
            m_scopedNamespacePrefixes = scopedNamespacePrefixes;
            foreach (var type in TypeCache.GetTypesDerivedFrom(sourceType))
            {
                if (type.IsDefined(typeof(BypassDependencyScanAttribute)) || !IsNamespaceInScope(type))
                    continue;
                ScanSourceForTarget(type, targetType);
            }
        }

        public bool TryGetPaths(Type type, out IReadOnlyList<FieldInfoPath> paths)
        {
            if (m_trackedTypes.TryGetValue(type, out var value))
                paths = new ReadOnlyCollection<FieldInfoPath>(value);
            else
                paths = null;
            return paths != null;
        }

        internal IEnumerable<KeyValuePair<Type, IReadOnlyList<FieldInfoPath>>> GetTracked()
        {
            foreach (var kvp in m_trackedTypes)
                yield return new(kvp.Key, new ReadOnlyCollection<FieldInfoPath>(kvp.Value));
        }

        private bool IsNamespaceInScope(Type type)
        {
            if (m_scopedNamespacePrefixes == null || string.IsNullOrEmpty(type.Namespace))
                return true;

            return Array.Exists(m_scopedNamespacePrefixes, x => type.Namespace.StartsWith(x));
        }

        private bool IsScopedSerializableClass(Type type)
        {
            return type.IsClass && type.IsDefined(typeof(SerializableAttribute), false) && IsNamespaceInScope(type);
        }

        private bool IsTypeTrackable(Type type, Type targetType)
        {
            return type.IsDerivedFrom(targetType) || IsScopedSerializableClass(type);
        }

        private static bool IsUnityInspectable(FieldInfo fieldInfo)
        {
            if (fieldInfo.IsDefined(typeof(HideInInspector)))
                return false;
            if (fieldInfo.IsPublic)
                return !fieldInfo.IsDefined(typeof(NonSerializedAttribute), false);
            if (fieldInfo.IsFamily || fieldInfo.IsPrivate)
                return fieldInfo.IsDefined(typeof(SerializeField));
            return false;
        }

        private void ScanSourceForTarget(Type sourceType, Type targetType)
        {
            Queue<Type> typeQ = new();
            typeQ.Enqueue(sourceType);
            Queue<FieldInfoPath> pathQ = new();
            pathQ.Enqueue(new FieldInfoPath(targetType));
            Type fieldType;
            Type currentType;
            FieldInfoPath currentPath;
            while (typeQ.Count > 0)
            {
                currentType = typeQ.Dequeue();
                currentPath = pathQ.Dequeue();
                foreach (var fieldInfo in currentType.GetFields(BINDING_ATTR))
                {
                    if (!IsUnityInspectable(fieldInfo))
                        continue;

                    fieldType = fieldInfo.FieldType;
                    if (fieldType.TryGetElementTypeOfArrayOrList(out Type elementType))
                    {
                        if (!IsTypeTrackable(elementType, targetType))
                            continue;

                        var path = currentPath.Clone();
                        path.containsArray = true;
                        Track(sourceType, targetType, typeQ, pathQ, elementType, path, fieldInfo);
                    }
                    else
                    {
                        Track(sourceType, targetType, typeQ, pathQ, fieldType, currentPath, fieldInfo);
                    }
                }
            }

            void Track(Type sourceType, Type targetType, Queue<Type> typeQ, Queue<FieldInfoPath> pathQ, Type fieldType, FieldInfoPath currentPath, FieldInfo fieldInfo)
            {
                if (fieldType.IsDerivedFrom(targetType))
                {
                    var path = currentPath.Clone();
                    path.Add(fieldInfo);
                    if (!m_trackedTypes.TryGetValue(sourceType, out IList<FieldInfoPath> list))
                        m_trackedTypes.Add(sourceType, list = new List<FieldInfoPath>());

                    list.Add(path);
                }
                else if (IsScopedSerializableClass(fieldType))
                {
                    typeQ.Enqueue(fieldType);

                    var path = currentPath.Clone();
                    path.Add(fieldInfo);
                    pathQ.Enqueue(path);
                }
            }
        }
    }
}
#endif
