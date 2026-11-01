using JSL.UnityGUIEvent.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace GameCore.Database.Editor
{
    class DataMenuDrawer<T> : DataEditorControlsGUI<T>, IDataSelector<T> where T : Data
    {
        private static class Styles
        {
            public static readonly Color leafBackgroundTint = new(0.55f, 0.55f, 0.55f, 0.2f);
            public static readonly Color leafSelectedTint = new(0.17f, 0.36f, 0.53f);
            public static readonly Color leafTextTint = new(0.75f, 0.75f, 0.75f);
            public static readonly Color splitterTint = new(0.12f, 0.12f, 0.12f);
            public static readonly GUIStyle queryResult;
            public static readonly GUIStyle queryPreview;
            public static GUIStyle LeafEntry
            {
                get
                {
                    leafEntry.contentOffset = new Vector2(LEAF_OFFSETX + EditorGUI.indentLevel * 15f, 0f);
                    return leafEntry;
                }
            }
            private const float LEAF_OFFSETX = 13.5f;
            private static readonly GUIStyle leafEntry;

            static Styles()
            {
                queryResult = EditorStyles.miniLabel;
                queryPreview = new GUIStyle(EditorStyles.linkLabel)
                {
                    fontStyle = FontStyle.Italic,
                    fontSize = 10
                };
                leafEntry = new GUIStyle(EditorStyles.iconButton)
                {
                    fixedWidth = 0f,
                    fixedHeight = EditorGUIUtility.singleLineHeight,
                    alignment = TextAnchor.MiddleLeft,
                    imagePosition = ImagePosition.TextOnly
                };
                leafEntry.normal.textColor = leafTextTint;
                leafEntry.hover.textColor = Color.white;
                leafEntry.richText = true;
            }
        }

        private struct Entry : IEquatable<Entry>
        {
            public string name;
            public bool isExpanded;
            public string Key { get; set; }

            public Entry(string name, bool isExpanded)
            {
                this.name = name;
                this.isExpanded = isExpanded;
                int index = name.IndexOf(Data.COMMENT_SEPARATOR);
                Key = index != -1 ? name[..index] : string.Empty;
            }

            public readonly bool Equals(Entry other)
            {
                if (!string.IsNullOrEmpty(Key))
                    return Key == other.Key;
                return name == other.name;
            }

            public override readonly string ToString() => name;
        }

        protected override bool DrawsPropertyTree => false;

        private const double SEARCH_DELAY_SECONDS = 0.35D;
        private const int MAX_INPUT_BUFFER_COUNT = 20;
        private const string QUERIED_COUNT_FORMAT = "{0} 筆項目";
        private const string PREVIEW_COUNT_FORMAT = "{0} 筆結果";
        private readonly SearchField m_searchField = new();
        private readonly LinkedList<string> m_inputBuffer = new();
        private readonly Tree<Entry> m_tree = new();
        private readonly List<DataSearchOptions<T>> m_searchOptionsList = new();
        private readonly IDataEditorWindow<T> m_dataEditor;
        private readonly AnimBool m_showSearchOptions;
        private readonly AnimFloat m_scrollToEntry;
        private Vector2 m_scrollPos;
        private Entry m_selectedEntry;
        private string m_queryText;
        private string m_trimmedQueryText;
        private LinkedListNode<string> m_currentInput;
        private bool m_isSearchInputChanged;
        private bool m_readsInputBuffer;
        private Predicate<T> m_queryPredicate;
        private double m_queryTextChangedTime;
        private bool m_updates;
        private bool m_areSearchOptionsExpanded;
        private bool m_hasFolder;
        private bool m_isAnyEntryCollapsed;
        private int m_queriedCount;
        private int m_previewCount = -1;
        private Rect m_scrollViewRect;
        private Rect m_selectedEntryRect;
        private bool m_updatesScrollPos;

        private bool HasQuery => !string.IsNullOrWhiteSpace(m_trimmedQueryText) || m_queryPredicate != null;
        private string QueryText
        {
            get => m_queryText;
            set
            {
                m_queryText = value;
                m_trimmedQueryText = value.Trim();
                m_queryTextChangedTime = EditorApplication.timeSinceStartup;
                m_updates = true;
            }
        }

        internal DataMenuDrawer(IDataEditorWindow<T> dataEditor, string queryText = null)
        {
            m_dataEditor = dataEditor;
            if (!string.IsNullOrWhiteSpace(queryText))
            {
                m_queryText = queryText;
                m_trimmedQueryText = queryText.Trim();
            }
            BuildTree();
            m_searchField.downOrUpArrowKeyPressed += OnSearchFieldDownOrUpArrowKeyPressed;
            m_showSearchOptions = new AnimBool(m_areSearchOptionsExpanded, m_dataEditor.Repaint) { speed = 2.5f };
            m_scrollToEntry = new AnimFloat(0f, () =>
            {
                m_scrollPos.y = m_scrollToEntry.value;
                m_dataEditor.Repaint();
            })
            { speed = 2.5f };
            m_searchOptionsList.Add(new DefaultDataSearchOptions<T>());
            m_searchOptionsList.AddRange(TypeCache.GetTypesDerivedFrom<DataSearchOptions<T>>().Select(x => (DataSearchOptions<T>)Activator.CreateInstance(x)));
            m_searchOptionsList.TrimExcess();
            foreach (var options in m_searchOptionsList)
                options.OnChanged += OnSearchOptionsChanged;
        }

        internal override void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string entered = m_searchField.OnGUI(QueryText);
                    if (entered != QueryText && !m_readsInputBuffer)
                    {
                        m_isSearchInputChanged = true;
                        QueryText = entered;
                    }

                    GUIContent content = EditorGUIUtility.TrIconContent(m_areSearchOptionsExpanded ? "d_icon dropdown" : "d_back", "進階搜尋選項");
                    if (EditorGUILayout.DropdownButton(content, FocusType.Keyboard, EditorStyles.miniButton, GUILayout.Width(26f)))
                    {
                        m_areSearchOptionsExpanded ^= true;
                        m_showSearchOptions.target = m_areSearchOptionsExpanded;
                    }
                }

                using (EditorGUILayout.FadeGroupScope group = new(m_showSearchOptions.faded))
                {
                    if (group.visible)
                    {
                        foreach (var options in m_searchOptionsList)
                            options.OnGUI();

                        EditorGUILayout.Space();
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            DrawQueryResultLabel(m_previewCount, PREVIEW_COUNT_FORMAT, Styles.queryPreview);
                            GUILayout.FlexibleSpace();
                            GUIContent content = EditorGUIUtility.TrTextContent("清除", "重設搜尋選項");
                            if (EditorGUILayout.DropdownButton(content, FocusType.Keyboard, EditorStyles.miniButton, GUILayout.Width(54f)))
                            {
                                if (m_previewCount > -1)
                                {
                                    m_previewCount = -1;
                                    m_queryPredicate = null;
                                    foreach (var options in m_searchOptionsList)
                                        options.Reset();
                                    m_updates = true;
                                }
                            }
                            content = EditorGUIUtility.TrTextContent("套用", "依搜尋選項篩選");
                            if (EditorGUILayout.DropdownButton(content, FocusType.Keyboard, EditorStyles.miniButton, GUILayout.Width(54f)))
                                m_updates = true;
                        }
                    }
                }

                if (m_areSearchOptionsExpanded)
                    EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawQueryResultLabel(m_queriedCount, QUERIED_COUNT_FORMAT, Styles.queryResult);
                    GUILayout.FlexibleSpace();
                    if (m_hasFolder)
                    {
                        GUIContent content = m_isAnyEntryCollapsed ? EditorGUIUtility.TrTextContent("全部展開", "展開所有項目") : EditorGUIUtility.TrTextContent("全部收起", "折疊所有項目");
                        if (EditorGUILayout.DropdownButton(content, FocusType.Keyboard, EditorStyles.miniButton, GUILayout.Width(72f)))
                        {
                            Entry entry;
                            foreach (var node in m_tree.Traverse())
                            {
                                entry = node.Value;
                                entry.isExpanded = m_isAnyEntryCollapsed;
                                node.Value = entry;
                            }
                        }
                    }
                }
            }

            using (EditorGUILayout.ScrollViewScope scope = new(m_scrollPos))
            {
                m_scrollPos = scope.scrollPosition;
                m_isAnyEntryCollapsed = false;
                DrawTree(m_tree);
            }

            if (UnityEngine.Event.current.type == EventType.Repaint)
            {
                m_scrollViewRect = GUILayoutUtility.GetLastRect();
                if (m_updatesScrollPos)
                {
                    m_updatesScrollPos = false;
                    m_scrollToEntry.value = m_scrollPos.y;
                    m_scrollToEntry.target = ComputeScrollPosY();
                }
            }
        }

        internal void Update()
        {
            if (!m_updates)
                return;

            if (EditorApplication.timeSinceStartup - m_queryTextChangedTime >= SEARCH_DELAY_SECONDS)
            {
                m_updates = false;
                m_readsInputBuffer = false;
                BuildTree();
                if (m_isSearchInputChanged && !string.IsNullOrWhiteSpace(m_trimmedQueryText))
                {
                    m_isSearchInputChanged = false;
                    m_currentInput = m_inputBuffer.AddFirst(m_trimmedQueryText);
                    if (m_inputBuffer.Count > MAX_INPUT_BUFFER_COUNT)
                        m_inputBuffer.RemoveLast();
                }

                m_dataEditor.Repaint();
            }
        }

        private void BuildTree()
        {
            var comparer = NaturalComparer.Default;
            IEnumerable<string> titles;
            bool hasQuery = HasQuery;
            if (hasQuery)
                titles = DatabaseEditorUtils.EnumerateData<T>().Where(Filter).Select(x => x.GetTitle()).OrderBy(x => x, comparer);
            else
                titles = DatabaseEditorUtils.EnumerateData<T>().Select(x => x.GetTitle()).OrderBy(x => x, comparer);

            m_hasFolder = false;
            m_queriedCount = 0;
            m_tree.Clear();
            foreach (string title in titles)
            {
                FindOrAddEntryPath(m_tree, title, out _, hasQuery);
                m_queriedCount++;
            }
        }

        private float ComputeScrollPosY()
        {
            float lowerBound = m_selectedEntryRect.y + m_selectedEntryRect.height;
            if (lowerBound > m_scrollPos.y + m_scrollViewRect.height)
            {
                return m_selectedEntryRect.y - (m_scrollViewRect.height - m_selectedEntryRect.height);
            }
            if (m_selectedEntryRect.y < m_scrollPos.y)
            {
                return m_selectedEntryRect.y;
            }
            return m_scrollPos.y;
        }

        private void DrawTree(Tree<Entry> tree)
        {
            if (tree == null)
                return;

            foreach (var node in tree)
                DrawNode(node);
        }

        private void DrawNode(TreeNode<Entry> node)
        {
            if (node == null)
                return;

            Rect rect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
            DrawSplitter(rect, 1f);
            Entry entry = node.Value;
            if (node.Subtree != null)
            {
                bool isExpanded = EditorGUILayout.Foldout(entry.isExpanded, entry.name, true);
                using (new EditorGUI.IndentLevelScope())
                {
                    if (isExpanded)
                        DrawTree(node.Subtree);
                    else
                        m_isAnyEntryCollapsed = true;

                    if (isExpanded != entry.isExpanded)
                    {
                        entry.isExpanded = isExpanded;
                        node.Value = entry;
                    }
                }
            }
            else
            {
                rect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                Color color;
                if (!entry.Equals(m_selectedEntry))
                {
                    color = Styles.leafBackgroundTint;
                }
                else
                {
                    color = Styles.leafSelectedTint;
                    if (UnityEngine.Event.current.type == EventType.Repaint)
                        m_selectedEntryRect = rect;
                }
                EditorGUI.DrawRect(rect, color);
                if (EditorGUI.DropdownButton(rect, EditorGUIUtility.TrTextContent(entry.name), FocusType.Keyboard, Styles.LeafEntry))
                {
                    m_selectedEntry = entry;
                    m_dataEditor.OnDataSelected(DatabaseEditorUtils.LoadData<T>(entry.Key));
                }
            }
        }

        private void DrawQueryResultLabel(int count, string format, GUIStyle style)
        {
            string result = count > -1 ? string.Format(format, count) : string.Empty;
            EditorGUILayout.LabelField(result, style, GUILayout.MaxWidth(70f));
        }

        private void DrawSplitter(Rect r, float thickness)
        {
            r.height = thickness;
            EditorGUI.DrawRect(r, Styles.splitterTint);
        }

        private IEnumerable<Entry> EnumerateEntries(string path, bool expandsEntry = false)
        {
            string[] split;
            int index = path.LastIndexOf(Data.COMMENT_SEPARATOR);
            if (index != -1)
            {
                int count = path.Take(index).Count(x => x == Data.PATH_SEPARATOR) + 1;
                split = path.Split(Data.PATH_SEPARATOR, count, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                split = path.Split(Data.PATH_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
            }
            for (int i = 0; i < split.Length; i++)
                yield return new Entry(split[i], expandsEntry);
        }

        private bool Filter(T data)
        {
            if (string.IsNullOrWhiteSpace(m_trimmedQueryText) || data.Filter(m_trimmedQueryText))
                return m_queryPredicate?.GetInvocationList().Cast<Predicate<T>>().All(x => x(data)) ?? true;
            return false;
        }

        private TreeNode<Entry> FindOrAddEntryPath(Tree<Entry> tree, string path, out bool found, bool expandsEntry = false)
        {
            found = false;
            if (tree == null || string.IsNullOrWhiteSpace(path))
                return null;

            var entries = EnumerateEntries(path, expandsEntry);
            var last = entries.LastOrDefault();
            if (string.IsNullOrWhiteSpace(last.Key))
            {
                Debug.LogError("The value of leaf has no key.");
                return null;
            }

            foreach (var node in tree.Traverse())
            {
                if (node.Subtree == null && node.Value.Equals(last))
                {
                    found = true;
                    TreeNode<Entry> parent = node.Parent;
                    Entry value;
                    while (parent != null)
                    {
                        value = parent.Value;
                        value.isExpanded = expandsEntry;
                        parent.Value = value;
                        parent = parent.Parent;
                    }
                    return node;
                }
            }

            Entry current;
            TreeNode<Entry> leaf = null;
            var enumerator = entries.GetEnumerator();
            bool hasNext = enumerator.MoveNext();
            while (hasNext)
            {
                current = enumerator.Current;
                hasNext = enumerator.MoveNext();
                if (hasNext)
                {
                    var child = tree.FindChild(current);
                    if (child == null)
                    {
                        tree = tree.Add(current).GetOrAddSubTree();
                    }
                    else
                    {
                        if (child.Subtree == null)
                        {
                            Debug.LogError($"Child {child.Value} was a leaf, but given path {path} attempts to make it a parent. Aborting.");
                            return null;
                        }
                        else
                        {
                            var entry = child.Value;
                            entry.isExpanded = expandsEntry;
                            child.Value = entry;
                            tree = child.Subtree;
                        }
                    }
                }
                else
                {
                    leaf = tree.FindChild(current);
                    found = leaf != null && leaf.Subtree == null;
                    if (!found)
                    {
                        leaf ??= tree.Add(current);
                        if (leaf.Subtree != null)
                            Debug.LogWarning($"Path {path} leads to a existed parent.");
                    }
                }
            }

            if (leaf?.Parent != null)
                m_hasFolder = true;
            return leaf;
        }

        private void OnSearchFieldDownOrUpArrowKeyPressed()
        {
            if (m_currentInput == null)
                return;
            var e = UnityEngine.Event.current;
            if (e.IsKeyDown(KeyCode.DownArrow))
                m_currentInput = m_currentInput.Previous ?? m_inputBuffer.Last;
            else if (e.IsKeyDown(KeyCode.UpArrow))
                m_currentInput = m_currentInput.Next ?? m_inputBuffer.First;
            //HACK: Set keyboardControl to zero temporarily for refreshing the query text
            EditorGUIUtility.keyboardControl = 0;
            m_readsInputBuffer = true;
            QueryText = m_currentInput.Value;
            m_searchField.SetFocus();
        }

        private void OnSearchOptionsChanged(DataSearchOptions<T> searchOptions)
        {
            if (searchOptions.Enabled)
            {
                m_queryPredicate -= searchOptions.Filter;
                m_queryPredicate += searchOptions.Filter;
            }
            else
            {
                m_queryPredicate -= searchOptions.Filter;
            }
            m_previewCount = HasQuery ? DatabaseEditorUtils.EnumerateData<T>().Count(Filter) : -1;
        }

        #region IDataSelector<T>
        void IDataSelector<T>.Focus() => m_updatesScrollPos = true;

        void IDataSelector<T>.OnDelete(T data)
        {
            if (!EditorUtility.IsPersistent(data))
                data = DatabaseEditorUtils.LoadData<T>(data.key);
            if (!data) return;

            if (m_tree.Remove(EnumerateEntries(data.GetTitle())))
            {
                m_queriedCount--;
                if (m_previewCount > -1 && HasQuery && Filter(data))
                    m_previewCount--;
            }
        }

        void IDataSelector<T>.OnSave(T data, bool removesOldEntry, string oldKey)
        {
            string title = data.GetTitle();
            Entry entry = EnumerateEntries(title).Last();
            if (!string.IsNullOrEmpty(oldKey) && entry.Key != oldKey)
                entry = new Entry(entry.name, entry.isExpanded) { Key = oldKey };

            bool isRemoved = removesOldEntry && m_tree.Remove(entry);
            m_selectedEntry = FindOrAddEntryPath(m_tree, title, out bool found, true).Value;
            m_updatesScrollPos = true;
            if (isRemoved || found)
                return;

            m_queriedCount++;
            if (m_previewCount > -1 && HasQuery && Filter(data))
                m_previewCount++;
        }

        void IDataSelector<T>.Refresh() => BuildTree();

        void IDataSelector<T>.Select(T data)
        {
            if (!data)
            {
                m_selectedEntry = default;
                return;
            }

            m_selectedEntry = FindOrAddEntryPath(m_tree, data.GetTitle(), out _, true).Value;
            m_updatesScrollPos = true;
        }

        bool IDataSelector<T>.TryGetData(out T data)
        {
            if (string.IsNullOrEmpty(m_selectedEntry.Key))
                data = null;
            else
                data = DatabaseEditorUtils.LoadData<T>(m_selectedEntry.Key);
            return data != null;
        }
        #endregion
    }
}