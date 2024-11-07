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


        override public OutlinerNode Parent
        {
            get
            {
                if (ParentHandle == OutlinerScene.RootHandle)
                    return null;
                else
                    return Scene.GetLayerByHandle(ParentHandle);
            }
        }

        public override int ChildNodesCount
        {
            get { return Scene.GetLayerChildNodesCount(Handle); }
        }

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

        public List<OutlinerNode> ChildLayers
        {
            get
            {
                return Scene.GetLayersByParentHandle(Handle);
            }
        }

        public List<OutlinerNode> ChildObjects
        {
            get
            {
                return Scene.GetObjectsByLayerHandle(Handle);
            }
        }

        override public string DisplayName
        {
            get
            {
                if (IsDefaultLayer) return "0 (default)";
                return Name;
            }
        }
        override public bool CanEditName
        {
            get
            {
                return !IsDefaultLayer;
            }
        }

        public override bool CanBeDeleted
        {
            get { return !IsDefaultLayer; }
        }

        public bool IsActive { get; set; }

        public bool IsDefaultLayer
        {
            get
            {
                return Name == "0";
            }
        }



        #region IDisplayable Members

        public bool IsHidden { get; set; }
        public bool IsFrozen { get; set; }
        public bool BoxMode { get; set; }

        #endregion


    }
}
