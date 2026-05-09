using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Outliner.Scene
{
    public abstract class OutlinerNode
    {
        public virtual OutlinerScene Scene { get; protected set; }

        public virtual Outliner.DragDropHandlers.DragDropHandler DragDropHandler { get; set; }

        public virtual int Handle { get; protected set; }
        public virtual int ParentHandle { get; set; }

        public virtual bool IsRootNode { get { return ParentHandle == OutlinerScene.RootHandle; } }

        public virtual string Name { get; set; }
        public abstract string DisplayName { get; }
        public abstract bool CanEditName { get; }

        public abstract bool CanBeDeleted { get; }

        public virtual OutlinerNode Parent { get { return null; } }
        public abstract int ChildNodesCount { get; }
        public abstract List<OutlinerNode> ChildNodes { get; }

        public virtual bool Filtered { get; set; }
    }
}
