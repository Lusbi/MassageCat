using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GameCore.Database.Editor
{
    public class Tree<T> : IEnumerable<TreeNode<T>>
    {
        public int ChildrenCount => m_nodes.Count;
        public TreeNode<T> Root { get; set; }

        private readonly List<TreeNode<T>> m_nodes;

        public Tree(TreeNode<T> root = null)
        {
            Root = root;
            m_nodes = new List<TreeNode<T>>();
        }

        public Tree(TreeNode<T> root, IEnumerable<TreeNode<T>> tree)
        {
            Root = root;
            m_nodes = new List<TreeNode<T>>(tree);
        }

        public Tree(TreeNode<T> root, IEnumerable<T> collection)
        {
            Root = root;
            m_nodes = new List<TreeNode<T>>(collection.Select(x => new TreeNode<T>(x, this)));
        }

        public TreeNode<T> Add(T value)
        {
            TreeNode<T> node = new(value, this);
            m_nodes.Add(node);
            return node;
        }

        public void Add(TreeNode<T> node) => m_nodes.Add(node);

        public void Clear() => m_nodes.Clear();

        public bool Contains(T value, bool checkSubtrees = true)
        {
            int index = m_nodes.FindIndex(x => EqualityComparer<T>.Default.Equals(x.Value, value));
            if (index != -1)
                return true;
            if (checkSubtrees)
            {
                foreach (var n in m_nodes)
                {
                    if (n.Subtree?.Contains(value) ?? false)
                        return true;
                }
            }
            return false;
        }

        public TreeNode<T> FindChild(T value) => m_nodes.Find(x => EqualityComparer<T>.Default.Equals(x.Value, value));

        public TreeNode<T> FindLastChild(T value) => m_nodes.FindLast(x => EqualityComparer<T>.Default.Equals(x.Value, value));

        public bool Remove(T value, bool checkSubtrees = true)
        {
            int index = m_nodes.FindIndex(x => EqualityComparer<T>.Default.Equals(x.Value, value));
            if (index != -1)
            {
                m_nodes.RemoveAt(index);
                if (m_nodes.Count == 0 && Root?.SuperTree is Tree<T> superTree)
                    superTree.Remove(Root, false);
                return true;
            }
            if (checkSubtrees)
            {
                foreach (var n in m_nodes)
                {
                    if (n.Subtree?.Remove(value) ?? false)
                        return true;
                }
            }
            return false;
        }

        public bool Remove(TreeNode<T> node, bool checkSubtrees = true)
        {
            if (m_nodes.Remove(node))
                return true;
            if (checkSubtrees)
            {
                foreach (var n in m_nodes)
                {
                    if (n.Subtree?.Remove(node) ?? false)
                        return true;
                }
            }
            return false;
        }

        public bool Remove(IEnumerable<T> pathToLeaf)
        {
            var enumerator = pathToLeaf.GetEnumerator();
            bool hasNext = enumerator.MoveNext();
            T current;
            Tree<T> tree = this;
            Stack<Tree<T>> stack = new();
            stack.Push(this);
            while (hasNext)
            {
                current = enumerator.Current;
                hasNext = enumerator.MoveNext();
                if (hasNext)
                {
                    var child = tree.FindChild(current);
                    if (child == null || child.Subtree == null)
                        return false;
                    tree = child.Subtree;
                    stack.Push(tree);
                }
                else if (!tree.Remove(current, false))
                {
                    return false;
                }
            }

            while (stack.TryPop(out tree))
            {
                if (tree.m_nodes.Count == 0 && stack.TryPeek(out var superTree))
                    superTree.m_nodes.Remove(tree.Root);
            }
            return true;
        }

        public IEnumerable<TreeNode<T>> Traverse()
        {
            if (Root != null)
                yield return Root;
            foreach (var node in m_nodes)
            {
                if (node.Subtree != null)
                {
                    foreach (var node1 in node.Subtree.Traverse())
                        yield return node1;
                }
                else
                    yield return node;
            }
        }

        #region IEnumerable<TreeNode<T>>
        public IEnumerator<TreeNode<T>> GetEnumerator() => m_nodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}