using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLib
{
    // 
    public class VisitTree<T>
    {
        T value;
        List<VisitTree<T>> children = new List<VisitTree<T>>();
        VisitTree<T> parent;

        public T Value { get { return value; } }
        public VisitTree<T> Parent { get { return parent; } }

        public VisitTree(T value)
        {
            this.value = value;
        }

        public void Add(VisitTree<T> t)
        {
            t.parent = this;
            children.Add(t);
        }

        public void Remove(VisitTree<T> t)
        {
            t.parent = null;
            children.Remove(t);
        }

        /// <summary>
        /// 現在のノードと、その子孫のノードに対して再帰的にvisitを適用する
        /// </summary>
        /// <param name="visit">(現在のノードの値, 親ノード) => 新しいノードの値</param>
        public void Apply(Func<T, VisitTree<T>, T> visit, Func<T, T> visitOnRoot)
        {
            if (parent == null)
                value = visitOnRoot(value);
            else
                value = visit(value, parent);
        
            foreach (var child in children)
                child.Apply(visit, visitOnRoot);
        }

        public List<VisitTree<T>> CopyChildren()
        {
            return new List<VisitTree<T>>(children);
        }


    }
}
