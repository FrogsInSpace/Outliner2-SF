using Autodesk.Max;

using Timer = System.Windows.Forms.Timer;

namespace Outliner.Scene
{
    public class OutlinerScene
    {
        private const int SanitizerInterval = 250;

        #region Type string constants

        public const string ObjectType = "Object";
        public const string XrefObjectType = "ReferenceTarget";//getClassname for xrefobject returns referencetarget?? "XRefObject";
        public const string LayerType = "Layer";
        public const string MaterialType = "Material";
        public const string XrefMaterialType = "XRef";

        public const string BipedType = "";//getClassName for biped returns empty string?? "Biped_Object";
        public const string BoneType = "Bone";
        public const string CameraType = "camera";
        public const string ContainerType = "Container";
        public const string GeometryType = "GeometryClass";
        public const string HelperType = "helper";
        public const string LightType = "light";
        public const string NurbsPtSurfType = "Point Surf";
        public const string NurbsCvSurfType = "CV Surf";
        public const string PatchEditableType = "PatchObject";
        public const string PatchQuadType = "QuadPatchObject";
        public const string PatchTriType = "TriPatchObject";
        public const string PArrayType = "PArray";
        public const string PBlizzardType = "Blizzard";
        public const string PCloudType = "PCloud";
        public const string PfSourceType = "PF Source";
        public const string PSnowType = "Snow";
        public const string PSprayType = "Spray";
        public const string PSuperSprayType = "SuperSpray";
        public const string PBirthTextureType = "Birth Texture";
        public const string PSpeedByIconType = "SpeedByIcon";
        public const string PGroupSelectionType = "Group Select";
        public const string PFindTargetType = "Find Target";
        public const string PInitialStateType = "Initial State";
        public const string ParticlePaintType = "Particle Paint";
        public const string ShapeType = "shape";
        public const string SpacewarpType = "SpacewarpObject";
        public const string TargetType = "Target";
        public const string PowerNurbsPrefixType = "Pwr_";

        public const string ThreeDxConnexionCamName = "3DxStudio Perspective";
        private readonly HashSet<string> hidden_particle_classes = new HashSet<string>()
        {
            "Age Test", "Birth", "Birth Paint", "Birth Script", "Cache", "Collision", "Collision Spawn",
            "DeleteParticles", "DisplayParticles", "Event", "Force", "Go To Rotation", "Group Operator",
            "Keep Apart", "Lock/Bond", "Mapping", "Material Dynamic", "Material Frequency", "Mapping Object",
            "Material Static", "Notes", "Particle_Bitmap", "Particle View", "ParticleGroup", "PFArrow", "PFEngine",
            "PFActionListPool", "Placement Paint", "Position Icon", "Position Object", "PView_Manager", "Rotation", "RenderParticles",
            "ScaleParticles", "Scale Test", "Script Operator", "Script Test", "Send Out", "Shape Facing", "Shape Instance",
            "ShapeLibrary", "Shape Mark", "shapeStandard", "Spawn", "Speed", "Speed By Surface", "Speed Test", "Spin",
            "Split Amount", "Split Group", "Split Selected", "Split Source"
        };

        #endregion

        public const int RootHandle = -1;
        public const int UnassignedHandle = -1;

        internal event Action<OutlinerLayer> LayerNameSynced;
        internal event Action<OutlinerLayer, OutlinerLayer> CurrentLayerChanged;


        protected int objectCounter;

        protected Dictionary<int, OutlinerObject> objects;
        protected Dictionary<int, OutlinerLayer> layers;
        protected Dictionary<int, OutlinerMaterial> materials;

        protected Dictionary<int, List<int>> objects_by_parentHandle;
        protected Dictionary<int, List<int>> objects_by_layerHandle;
        protected Dictionary<int, List<int>> objects_by_materialHandle;

        protected Dictionary<int, List<int>> layers_by_parentHandle;

        protected Dictionary<int, List<int>> materials_by_parentHandle;

        private readonly IILayerManager _layerMgr;
        private UIntPtr _lastCurrentLayer;

        private Timer _sanitizeTimer;

        public OutlinerScene()
        {
            objectCounter = 0;

            objects = new Dictionary<int, OutlinerObject>();
            layers = new Dictionary<int, OutlinerLayer>();
            materials = new Dictionary<int, OutlinerMaterial>();

            objects_by_parentHandle = new Dictionary<int, List<int>>();
            objects_by_layerHandle = new Dictionary<int, List<int>>();
            objects_by_materialHandle = new Dictionary<int, List<int>>();

            layers_by_parentHandle = new Dictionary<int, List<int>>();
            materials_by_parentHandle = new Dictionary<int, List<int>>();

            AddMaterial(new OutlinerMaterial(this, UnassignedHandle, RootHandle, "", ""));

            _layerMgr = GlobalInterface.Instance.COREInterface14.LayerManager;
            _lastCurrentLayer = GlobalInterface.Instance.Animatable.GetHandleByAnim( _layerMgr.CurrentLayer);

            _sanitizeTimer = new Timer();
            _sanitizeTimer.Interval = SanitizerInterval;
            _sanitizeTimer.Tick += (s, e) => SanitizeLayers();
        }

        public void Clear()
        {

            _sanitizeTimer?.Stop();

            objectCounter = 0;

            objects.Clear();
            layers.Clear();
            ClearMaterials();

            objects_by_parentHandle.Clear();
            objects_by_layerHandle.Clear();
            objects_by_materialHandle.Clear();

            layers_by_parentHandle.Clear();
        }

        public void ClearMaterials()
        {
            materials.Clear();
            materials_by_parentHandle.Clear();
            AddMaterial(new OutlinerMaterial(this, UnassignedHandle, RootHandle, "", ""));
        }


        private int _isSyncing = 0;
        public bool SanitizeLayers()
        {
            if (Interlocked.Exchange(ref _isSyncing, 1) == 1)
                return false; // already running, skip

            var current = GlobalInterface.Instance.Animatable.GetHandleByAnim(_layerMgr.CurrentLayer);

            if (current != null && current != _lastCurrentLayer)
            {
                CurrentLayerChanged?.Invoke(GetLayerByHandle((int)current), GetLayerByHandle((int)_lastCurrentLayer));
                _lastCurrentLayer = current;
            }


            bool didSync = false;

            try
            {

                if (layers.Count == 0 )
                    return false;

                foreach( var h in layers.Keys )
                {
                    var olLayer = layers[h];

                    var maxLayer = GlobalInterface.Instance.Animatable.GetAnimByHandle( (UIntPtr) h ) as IILayer;

                    if( olLayer == null || maxLayer == null )
                        continue;       

                    if( !string.Equals( olLayer.Name, maxLayer.Name, StringComparison.Ordinal))
                    {
                        olLayer.Name = maxLayer.Name;

                        didSync = true;
                        LayerNameSynced?.Invoke(olLayer);
                    }
                }
            }
            finally
            {
                _isSyncing=0;

            }
            return didSync;
        }

        #region Objects, RootObjects, Layers, Materials

        public List<OutlinerNode> RootObjects => GetObjectsByParentHandle(RootHandle);

        public List<OutlinerObject> Objects => new List<OutlinerObject>(objects.Values);

        public List<OutlinerNode> RootLayers => GetLayersByParentHandle(RootHandle);

        public List<OutlinerLayer> Layers => new List<OutlinerLayer>(layers.Values);

        public List<OutlinerNode> RootMaterials => GetMaterialsByParentHandle(RootHandle);

        public List<OutlinerMaterial> Materials => new List<OutlinerMaterial>(materials.Values);

        #endregion


        #region GetObjectByHandle, GetLayerByHandle, GetMaterialByHandle

        //Should only be used if you're not sure what type the node will be, using of GetObjectByHandle, GetLayerByHandle etc is preferred.
        public OutlinerNode GetNodeByHandle(int handle)
        {
            OutlinerNode node = GetObjectByHandle(handle);
            if (node == null)
                node = GetLayerByHandle(handle);
            if (node == null)
                node = GetMaterialByHandle(handle);
            return node;
        }

        public OutlinerObject GetObjectByHandle(int handle)
        {
            if( objects.TryGetValue(handle, out OutlinerObject obj))
                return obj;
            return null;
        }

        public OutlinerLayer GetLayerByHandle(int handle)
        {
            if(layers.TryGetValue(handle, out OutlinerLayer layer))
                return layer;

            return null;
        }

        public OutlinerMaterial GetMaterialByHandle(int handle)
        {
            if( materials.TryGetValue(handle, out OutlinerMaterial mat))
                return mat;
            return null;
        }

        #endregion


        #region GetObjectsByParentHandle, GetObjectsByLayerHandle, GetObjectsByMaterialHandle, GetLayersByParentHandle, GetMaterialsByParentHandle

        public List<OutlinerNode> GetObjectsByParentHandle(int handle)
        {
            return getNodesFromDict(objects_by_parentHandle, handle, GetObjectByHandle);
        }

        public List<OutlinerNode> GetObjectsByLayerHandle(int handle)
        {
            return getNodesFromDict(objects_by_layerHandle, handle, GetObjectByHandle);
        }

        public List<OutlinerNode> GetObjectsByMaterialHandle(int handle)
        {
            return getNodesFromDict(objects_by_materialHandle, handle, GetObjectByHandle);
        }

        public List<OutlinerNode> GetLayersByParentHandle(int handle)
        {
            return getNodesFromDict(layers_by_parentHandle, handle, GetLayerByHandle);
        }

        public List<OutlinerNode> GetMaterialsByParentHandle(int handle)
        {
            return getNodesFromDict(materials_by_parentHandle, handle, GetMaterialByHandle);
        }

        #endregion


        #region GetChildNodesCount

        public int GetObjectChildNodesCount(int objectHandle)
        {
            return getNodesFromDictCount(objects_by_parentHandle, objectHandle);
        }

        public int GetLayerChildNodesCount(int layerHandle)
        {
            return getNodesFromDictCount(objects_by_layerHandle, layerHandle) + getNodesFromDictCount(layers_by_parentHandle, layerHandle);
        }

        public int GetMaterialChildNodesCount(int materialHandle)
        {
            return getNodesFromDictCount(objects_by_materialHandle, materialHandle) + getNodesFromDictCount(materials_by_parentHandle, materialHandle);
        }

        #endregion


        #region AddObject, AddLayer, AddMaterial

        private bool CanAddObject(OutlinerObject obj)
        {
            return !objects.ContainsKey(obj.Handle)
                && (obj.Name != ThreeDxConnexionCamName || obj.SuperClass != CameraType)
                && !hidden_particle_classes.Contains(obj.Class);
        }

        public void AddObject(OutlinerObject obj)
        {
            if (CanAddObject(obj))
            {
                objects.Add(obj.Handle, obj);

                addHandleToListInDict(obj.Handle, objects_by_parentHandle, obj.ParentHandle);
                addHandleToListInDict(obj.Handle, objects_by_layerHandle, obj.LayerHandle);
                addHandleToListInDict(obj.Handle, objects_by_materialHandle, obj.MaterialHandle);
            }
        }

        public void AddObject(int handle, int parentHandle, int layerHandle, int materialHandle,
                              string name, string objClass, string objSuperClass,
                              bool isGroupHead, bool isGroupMember,
                              bool isHidden, bool isFrozen, bool boxMode)
        {
            OutlinerObject obj = new OutlinerObject(this, ++objectCounter, handle, parentHandle, layerHandle, materialHandle, name, objClass, objSuperClass, isGroupHead, isGroupMember, isHidden, isFrozen, boxMode);
            this.AddObject(obj);
        }


        public void AddLayer(OutlinerLayer layer)
        {
            if (!layers.ContainsKey(layer.Handle))
            {
                _sanitizeTimer.Enabled = true;

                layers.Add(layer.Handle, layer);

                addHandleToListInDict(layer.Handle, layers_by_parentHandle, layer.ParentHandle);
            }
        }

        public void AddLayer(int handle, int parentHandle, string name, bool isActive, bool isHidden, bool isFrozen, bool boxMode)
        {
            OutlinerLayer layer = new OutlinerLayer(this, handle, parentHandle, name, isActive, isHidden, isFrozen, boxMode);
            AddLayer(layer);
        }


        public void AddMaterial(OutlinerMaterial mat)
        {
            if (!materials.ContainsKey(mat.Handle))
            {
                materials.Add(mat.Handle, mat);

                addHandleToListInDict(mat.Handle, materials_by_parentHandle, mat.ParentHandle);
            }
        }

        public void AddMaterial(int handle, int parentHandle, string name, string type)
        {
            OutlinerMaterial mat = new OutlinerMaterial(this, handle, parentHandle, name, type);
            AddMaterial(mat);
        }

        #endregion


        #region RemoveNode, RemoveObject, RemoveLayer, RemoveMaterial

        public void RemoveNode(OutlinerNode node)
        {
            if (node is OutlinerObject)
                RemoveObject((OutlinerObject)node);
            else if (node is OutlinerLayer)
                RemoveLayer((OutlinerLayer)node);
            else if (node is OutlinerMaterial)
                RemoveMaterial((OutlinerMaterial)node);
        }

        public void RemoveObject(OutlinerObject obj)
        {
            objects.Remove(obj.Handle);
            removeHandleFromListInDict(obj.Handle, objects_by_parentHandle, obj.ParentHandle);
            removeHandleFromListInDict(obj.Handle, objects_by_layerHandle, obj.LayerHandle);
            removeHandleFromListInDict(obj.Handle, objects_by_materialHandle, obj.MaterialHandle);
        }

        public void RemoveLayer(OutlinerLayer layer)
        {
            layers.Remove(layer.Handle);
            objects_by_layerHandle.Remove(layer.Handle);
            removeHandleFromListInDict(layer.Handle, layers_by_parentHandle, layer.Handle);
        }

        public void RemoveMaterial(OutlinerMaterial mat)
        {
            materials.Remove(mat.Handle);
            objects_by_materialHandle.Remove(mat.Handle);
            removeHandleFromListInDict(mat.Handle, materials_by_parentHandle, mat.ParentHandle);
        }

        #endregion


        #region SetObjectParent, SetObjectLayer, SetObjectMaterial, SetLayerParent

        public void SetObjectParentHandle(OutlinerObject obj, int newParentHandle)
        {
            removeHandleFromListInDict(obj.Handle, objects_by_parentHandle, obj.ParentHandle);
            addHandleToListInDict(obj.Handle, objects_by_parentHandle, newParentHandle);
            obj.ParentHandle = newParentHandle;
        }

        public void SetObjectLayerHandle(OutlinerObject obj, int newLayerHandle)
        {
            removeHandleFromListInDict(obj.Handle, objects_by_layerHandle, obj.LayerHandle);
            addHandleToListInDict(obj.Handle, objects_by_layerHandle, newLayerHandle);
            obj.LayerHandle = newLayerHandle;
        }

        public void SetObjectMaterialHandle(OutlinerObject obj, int newMaterialHandle)
        {
            removeHandleFromListInDict(obj.Handle, objects_by_materialHandle, obj.MaterialHandle);
            addHandleToListInDict(obj.Handle, objects_by_materialHandle, newMaterialHandle);
            obj.MaterialHandle = newMaterialHandle;
        }

        public void SetLayerParentHandle(OutlinerLayer layer, int newParentHandle)
        {
            removeHandleFromListInDict(layer.Handle, layers_by_parentHandle, layer.ParentHandle);
            addHandleToListInDict(layer.Handle, layers_by_parentHandle, newParentHandle);
            layer.ParentHandle = newParentHandle;
        }

        #endregion


        #region IsValidLayerName, IsValidMaterialName

        public bool IsValidLayerName(OutlinerLayer editingLayer, string newName)
        {
            if ( string.IsNullOrEmpty( newName))
                return false;

            foreach (KeyValuePair<int, OutlinerLayer> kvp in layers)
            {
                if (string.Compare(kvp.Value.Name, newName, true) == 0 && kvp.Value != editingLayer)
                    return false;
            }
            return true;
        }

        public bool IsValidLayerName(int layerHandle, string newName)
        {
            OutlinerLayer layer;
            if (layers.TryGetValue(layerHandle, out layer))
                return IsValidLayerName(layer, newName);
            else
                return false;
        }

        public bool IsValidMaterialName(OutlinerMaterial editingMaterial, string newName)
        {
            if (string.IsNullOrEmpty(newName))
                return false;

            foreach (KeyValuePair<int, OutlinerMaterial> kvp in materials)
            {
                if (string.Compare(kvp.Value.Name, newName, true) == 0 && kvp.Value != editingMaterial)
                    return false;
            }
            return true;
        }

        public bool IsValidMaterialName(int materialHandle, string newName)
        {
            OutlinerMaterial mat;
            if (materials.TryGetValue(materialHandle, out mat))
                return IsValidMaterialName(mat, newName);
            else
                return false;
        }

        public bool ContainsMaterial(int materialHandle)
        {
            return materials.ContainsKey(materialHandle);
        }

        #endregion



        #region List helper functions

        private delegate OutlinerNode getNodeFn(int handle);
        private List<OutlinerNode> getNodesFromDict(Dictionary<int, List<int>> dict, int listHandle, getNodeFn nodeFn)
        {
            List<int> nodeHandles;
            if (dict.TryGetValue(listHandle, out nodeHandles))
            {
                List<OutlinerNode> nodes = new List<OutlinerNode>(nodeHandles.Count);
                foreach (int handle in nodeHandles)
                {
                    OutlinerNode node = nodeFn(handle);
                    if (node != null)
                        nodes.Add(node);
                }
                return nodes;
            }
            else
                return new List<OutlinerNode>(0);
        }

        private int getNodesFromDictCount(Dictionary<int, List<int>> dict, int listHandle)
        {
            List<int> nodeHandles;
            if (dict.TryGetValue(listHandle, out nodeHandles))
                return nodeHandles.Count;
            else
                return 0;
        }

        private void addHandleToListInDict(int handle, Dictionary<int, List<int>> dict, int listHandle)
        {
            List<int> list;
            if (!dict.TryGetValue(listHandle, out list))
            {
                list = new List<int>();
                dict.Add(listHandle, list);
            }
            list.Add(handle);
        }


        private void removeHandleFromListInDict(int handle, Dictionary<int, List<int>> dict, int listHandle)
        {
            List<int> list;
            if (dict.TryGetValue(listHandle, out list))
            {
                list.Remove(handle);
            }
        }

        #endregion
    }
}
