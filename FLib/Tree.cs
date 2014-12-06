using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLib
{
    // 
    public class Tree<T>
    {
        T value;
        List<Tree<T>> children = new List<Tree<T>>();
        Tree<T> parent;

        public T Value { get { return value; } }
        public Tree<T> Parent { get { return parent; } }

        public Tree(T value)
        {
            this.value = value;
        }

        public void Add(Tree<T> t)
        {
            t.parent = this;
            children.Add(t);
        }

        public void Remove(Tree<T> t)
        {
            t.parent = null;
            children.Remove(t);
        }

        /// <summary>
        /// 現在のノードと、その子孫のノードに対して再帰的にvisitを適用する
        /// </summary>
        /// <param name="visit">(現在のノードの値, 親ノード) => 新しいノードの値</param>
        public void Apply(Func<T, Tree<T>, T> visit, Func<T, T> visitOnRoot)
        {
            if (parent == null)
                value = visitOnRoot(value);
            else
                value = visit(value, parent);
        
            foreach (var child in children)
                child.Apply(visit, visitOnRoot);
        }

        public List<Tree<T>> CopyChildren()
        {
            return new List<Tree<T>>(children);
        }


    }
}
