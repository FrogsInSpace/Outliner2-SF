using System;
using System.Collections.Generic;
using System.Text;
using Outliner.DragDropHandlers;
using System.Drawing;

namespace Outliner.Scene
{
    public class OutlinerObject : OutlinerNode, IDisplayable
    {
        public OutlinerObject(OutlinerScene scene, int objectNr, int handle, int parentHandle, int layerHandle, int materialHandle,
                            string name, string objClass, string objSuperClass,
                            bool isGroupHead, bool isGroupMember,
                            bool isHidden, bool isFrozen, bool boxMode)
        {
            Scene = scene;
            ObjectNr = objectNr;

            Handle = handle;
            ParentHandle = parentHandle;
            LayerHandle = layerHandle;
            MaterialHandle = materialHandle;

            Name = name;
            Class = objClass;
            SuperClass = objSuperClass;

            IsGroupHead = isGroupHead;
            IsGroupMember = isGroupMember;

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
                    return Scene.GetObjectByHandle(ParentHandle);
            }
        }

        public override int ChildNodesCount => Scene.GetObjectChildNodesCount(Handle); 
        override public List<OutlinerNode> ChildNodes => Scene.GetObjectsByParentHandle(Handle);

        public int LayerHandle { get; set; }
        public OutlinerLayer Layer => Scene.GetLayerByHandle(LayerHandle);

        public int MaterialHandle { get; set; }
        public OutlinerMaterial Material => Scene.GetMaterialByHandle(MaterialHandle);

        override public string DisplayName
        {
            get
            {
                string n = string.IsNullOrEmpty(Name) ? "-unnamed-" : Name ;
                if (Class == OutlinerScene.XrefObjectType && (IsGroupMember || IsGroupHead))
                    return "{[ " + n + " ]}";

                if (Class == OutlinerScene.XrefObjectType)
                    return "{ " + n + " }";

                if (IsGroupMember || IsGroupHead)
                    return "[ " + n + " ]";

                return n;
            }
        }
        public override bool CanEditName => true;

        public override bool CanBeDeleted => true;

        public int ObjectNr { get; private set; }

        public string Class { get; set; }
        public string SuperClass { get; set; }

        public bool IsGroupHead { get; set; }
        public bool IsGroupMember { get; set; }


        public void SetIsGroupMemberRec(bool isGroupMember)
        {
            SetIsGroupMemberRec(this, isGroupMember);
        }
        private void SetIsGroupMemberRec(OutlinerObject o, bool isGroupMember)
        {
            o.IsGroupMember = isGroupMember;
            foreach (OutlinerNode c in o.ChildNodes)
            {
                if (c is OutlinerObject)
                    SetIsGroupMemberRec((OutlinerObject)c, isGroupMember);
            }
        }

        #region IDisplayable Members

        public bool IsHidden { get; set; }
        public bool IsFrozen { get; set; }
        public bool BoxMode { get; set; }

        #endregion
    }
}
