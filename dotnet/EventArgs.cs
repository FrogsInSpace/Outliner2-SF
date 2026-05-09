using System;
using System.Collections.Generic;
using System.Text;
using Outliner.Scene;
using System.Windows.Forms;

namespace Outliner
{
    public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);
    public class SelectionChangedEventArgs : EventArgs
    {
        public int[] SelectedObjectHandles { get; private set; }
        public int[] SelectedLayerHandles { get; private set; }
        public int[] SelectedMaterialHandles { get; private set; }

        public SelectionChangedEventArgs(int[] selectedObjectHandles, int[] selectedLayerHandles, int[] selectedMaterialHandles)
        {
            SelectedObjectHandles = selectedObjectHandles;
            SelectedLayerHandles = selectedLayerHandles;
            SelectedMaterialHandles = selectedMaterialHandles;
        }
    }


    public delegate void NodePropertyChangedEventHandler(object sender, NodePropertyChangedEventArgs e);
    public class NodePropertyChangedEventArgs : EventArgs
    {
        public int[] Handles { get; private set; }
        public string PropName { get; private set; }
        public object NewValue { get; private set; }

        public NodePropertyChangedEventArgs(int[] handles, string propName, object newValue)
        {
            Handles = handles;
            PropName = propName;
            NewValue = newValue;
        }
    }


    public delegate void NodeRenamedEventHandler(object sender, NodeRenamedEventArgs e);
    public class NodeRenamedEventArgs : EventArgs
    {
        public int Handle { get; private set; }
        public string Name { get; private set; }

        public NodeRenamedEventArgs(int handle, string name)
        {
            Handle = handle;
            this.Name = name;
        }
    }


    public delegate void NodeLinkedEventHandler(object sender, NodeLinkedEventArgs e);
    public class NodeLinkedEventArgs : EventArgs
    {
        public int[] Handles { get; private set; }
        public int TargetHandle { get; private set; }

        public NodeLinkedEventArgs(int[] handles, int targetHandle)
        {
            this.Handles = handles;
            this.TargetHandle = targetHandle;
        }
    }


    public delegate void NodeGroupedEventHandler(object sender, NodeGroupedEventArgs e);
    public class NodeGroupedEventArgs : NodeLinkedEventArgs
    {
        public bool IsGroupMember { get; private set; }
        public bool Linked { get; private set; }

        public NodeGroupedEventArgs(int[] nodeHandles, int targetHandle, bool isGroupMember, bool linked) : base(nodeHandles, targetHandle)
        {
            IsGroupMember = isGroupMember;
            Linked = linked;
        }
    }




    public delegate void ContextMenuItemClickedEventHandler(object sender, ContextMenuItemClickedEventArgs e);
    public class ContextMenuItemClickedEventArgs : EventArgs
    {
        public ContextMenuStrip Menu { get; private set; }
        public ToolStripItem ClickedItem { get; private set; }

        public ContextMenuItemClickedEventArgs(ContextMenuStrip menu, ToolStripItem clickedItem)
        {
            Menu = menu;
            ClickedItem = clickedItem;
        }
    }



    public delegate void DebugEventHandler(object sender, DebugEventArgs e);
    public class DebugEventArgs : EventArgs
    {
        public string Text1 { get; private set; }
        public string Text2 { get; private set; }
        public string Text3 { get; private set; }

        public DebugEventArgs(string text)
        {
            Text1 = text;
        }
        public DebugEventArgs(string text, string text2)
            : this(text)
        {
            Text2 = text2;
        }
        public DebugEventArgs(string text, string text2, string text3)
            : this(text, text2)
        {
            Text3 = text3;
        }
    }
}
