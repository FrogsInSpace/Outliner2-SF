using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Outliner.DragDropHandlers;

namespace Outliner.Scene
{
    public class OutlinerLayer : OutlinerNode, IDisplayable
    {
        public OutlinerLayer(OutlinerScene scene, int handle, int parentHandle, string name, bool isActive, bool isHidden, bool isFrozen, bool boxMode)
        {
            Scene = scene;
            Handle = handle;
            ParentHandle = parentHandle;
            Name = name;
            IsActive = isActive;

            IsHidden = isHidden;
            IsFrozen = isFrozen;
            BoxMode = boxMode;
        }

        override public OutlinerNode Parent => ParentHandle == OutlinerScene.RootHandle ? null : Scene.GetLayerByHandle(ParentHandle);

        public override int ChildNodesCount => Scene.GetLayerChildNodesCount(Handle); 

        override public List<OutlinerNode> ChildNodes
        {
            get
            {
                List<OutlinerNode> childLayers = ChildLayers;
                List<OutlinerNode> childObjects = ChildObjects;
                if (childLayers.Count == 0)
                    return childObjects;
                else
                {
                    childLayers.AddRange(childObjects);
                    return childLayers;
                }
            }
        }

        public List<OutlinerNode> ChildLayers => Scene.GetLayersByParentHandle(Handle);

        public List<OutlinerNode> ChildObjects => Scene.GetObjectsByLayerHandle(Handle);

        override public string DisplayName => IsDefaultLayer ? "0 (default)" : Name;

        override public bool CanEditName => !IsDefaultLayer;

        public override bool CanBeDeleted => !IsDefaultLayer;

        public bool IsActive { get; set; }

        public bool IsDefaultLayer => Name == "0";

        public bool IsHidden { get; set; }
        public bool IsFrozen { get; set; }
        public bool BoxMode { get; set; }

    }
}
