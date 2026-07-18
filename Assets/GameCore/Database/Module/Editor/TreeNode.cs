using System.Collections.Generic;

namespace GameCore.Database.Editor
{
    public class TreeNode<T>
    {
        public T Value { get; set; }
        public Tree<T> SuperTree { get; set; }
        public Tree<T> Subtree => m_subtree;
        public TreeNode<T> Parent => SuperTree?.Root;

        private Tree<T> m_subtree;

        public TreeNode(T value, Tree<T> superTree)
        {
            Value = value;
            SuperTree = superTree;
        }

        public Tree<T> AddSubtree(IEnumerable<T> collection) => m_subtree ??= new Tree<T>(this, collection);

        public Tree<T> GetOrAddSubTree() => m_subtree ??= new Tree<T>(this);
    }
}