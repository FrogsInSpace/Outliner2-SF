using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Outliner.Scene;

namespace Outliner.DragDropHandlers
{
    public class ObjectDragDropHandler : DragDropHandler
    {
        public ObjectDragDropHandler(Outliner.TreeView tree, OutlinerObject data) : base(tree, null)
        {
            Data = data;
        }

        new protected OutlinerObject Data { get; set; }


        override public bool AllowDrag { get { return true; } }


        protected override bool IsDroppableNode(OutlinerNode o)
        {
            return (o is OutlinerObject);
        }

        public override bool IsValidDropTarget(IDataObject dragData)
        {
            if (Tree == null || Tree.ListMode != OutlinerListMode.Hierarchy)
                return false;
            else
                return base.IsValidDropTarget(dragData);
        }


        public override DragDropEffects GetDragDropEffect(IDataObject dragData)
        {
            if (IsValidDropTarget(dragData))
                return DragDropEffects.Link;
            else
                return Outliner.TreeView.DragDropEffectsNone;
        }


        public override bool ItemDropped(IDataObject dragData)
        {
            if (!IsValidDropTarget(dragData))
                return false;


            OutlinerNode[] droppedNodes = GetNodesFromDataObject(dragData);
            int[] droppedNodeHandles = new int[droppedNodes.Length];

            Tree.BeginTimedUpdate();
            Tree.BeginTimedSort();

            bool dispatchUngroupEvent = false;
            int i = 0;
            foreach (OutlinerNode n in droppedNodes)
            {
                if (((OutlinerObject)n).IsGroupMember)
                    dispatchUngroupEvent = true;

                Tree.LinkObject((OutlinerObject)n, Data.Handle, ((OutlinerObject)n).IsGroupMember, false);

                droppedNodeHandles[i] = ((OutlinerObject)n).Handle;
                i++;
            }

            if (dispatchUngroupEvent)
            {
                Tree.RaiseObjectGroupedEvent(new NodeGroupedEventArgs(droppedNodeHandles, Data.Handle, false, true));
                int[] childHandles = getChildHandles(droppedNodes);
                if (childHandles.Length > 0)
                    Tree.RaiseObjectGroupedEvent(new NodeGroupedEventArgs(childHandles, Data.Handle, false, false));
            }
            else
                Tree.RaiseObjectLinkedEvent(new NodeLinkedEventArgs(droppedNodeHandles, Data.Handle));


            return true;
        }
    }
}
