using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms.VisualStyles;

using Outliner.DragDropHandlers;
using Outliner.Scene;
using Outliner.Resources;

using Autodesk.Max;

using Timer = System.Windows.Forms.Timer;

namespace Outliner
{
    public partial class TreeView : System.Windows.Forms.TreeView
    {
        private Dictionary<OutlinerNode, TreeNode> _treeNodes;
        private HashSet<OutlinerNode> _selectedNodes;
        private HashSet<int> _expandedNodeHandles;
        private bool _restoringExpandedStates;

        private Timer _updateTimer;
        private bool _updateWaitingForSort;
        private Timer _sortTimer;
        private Timer _ensureSelectionVisibleTimer;
        private EnsureSelectionVisibleAction _ensureSelectionVisibleAction;
        private bool _ensureSelectionVisibleWaitingForSort;


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        private readonly IntPtr maxMainHandle;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        public OutlinerScene Scene { get; private set; }
        public TreeStyle Style { get; private set; }
        public OutlinerFilter Filter { get; private set; }

        private OutlinerListMode _listMode;
        public OutlinerListMode ListMode
        {
            get => _listMode;
            set
            {
                if( value == _listMode )
                    return;    

                Scene.LayerNameSynced -= OnLayerNameSynced;
                Scene.CurrentLayerChanged -= OnActiveLayerChanged;

                if( value == OutlinerListMode.Layer )
                {
                    Scene.CurrentLayerChanged += OnActiveLayerChanged;
                    Scene.LayerNameSynced += OnLayerNameSynced;
                }

                _listMode = value;
            }

        }

        #region Node buttons

        public IconClickAction IconClickAction { get; set; }

        public bool ShowNodeIcon
        {
            get { return _icons != null && _icons.Count > 0; }
        }

        #region Hide button

        private bool _showNodeHideButton;
        public bool ShowNodeHideButton
        {
            get { return _showNodeHideButton; }
            set
            {
                _showNodeHideButton = value;
                CheckBoxes = (value && ShowNodeFreezeButton) || (value && ShowNodeBoxModeButton) || (ShowNodeFreezeButton && ShowNodeBoxModeButton);
                AutoIndent();
                Invalidate();
            }
        }

        private NodeButtonsLocation _nodeHideButtonLocation = NodeButtonsLocation.BeforeNode;
        public NodeButtonsLocation NodeHideButtonLocation
        {
            get { return _nodeHideButtonLocation; }
            set { _nodeHideButtonLocation = value; Invalidate(); }
        }


        private bool _invertNodeHideButton;
        public bool InvertNodeHideButton
        {
            get { return _invertNodeHideButton; }
            set
            {
                _invertNodeHideButton = value;
                if (ShowNodeHideButton) Invalidate();
            }
        }

        #endregion

        #region Freeze button

        private bool _showNodeFreezeButton;
        public bool ShowNodeFreezeButton
        {
            get { return _showNodeFreezeButton; }
            set
            {
                _showNodeFreezeButton = value;
                CheckBoxes = (value && ShowNodeHideButton) || (value && ShowNodeBoxModeButton) || (ShowNodeHideButton && ShowNodeBoxModeButton);
                AutoIndent();
                Invalidate();
            }
        }

        private NodeButtonsLocation _nodeFreezeButtonLocation = NodeButtonsLocation.BeforeNode;
        public NodeButtonsLocation NodeFreezeButtonLocation
        {
            get { return _nodeFreezeButtonLocation; }
            set { _nodeFreezeButtonLocation = value; Invalidate(); }
        }

        #endregion

        #region BoxMode button

        private bool _showNodeBoxModeButton;
        public bool ShowNodeBoxModeButton
        {
            get { return _showNodeBoxModeButton; }
            set
            {
                _showNodeBoxModeButton = value;
                CheckBoxes = (value && ShowNodeHideButton) || (value && ShowNodeFreezeButton) || (ShowNodeHideButton && ShowNodeFreezeButton);
                AutoIndent();
                Invalidate();
            }
        }

        private NodeButtonsLocation _nodeBoxModeButtonLocation = NodeButtonsLocation.AfterNode;
        public NodeButtonsLocation NodeBoxModeButtonLocation
        {
            get { return _nodeBoxModeButtonLocation; }
            set { _nodeBoxModeButtonLocation = value; Invalidate(); }
        }

        #endregion

        #region Add button
        private bool _showNodeAddButton = true;
        public bool ShowNodeAddButton
        {
            get { return _showNodeAddButton; }
            set
            {
                _showNodeAddButton = value;
                CheckBoxes = (value && ShowNodeHideButton) || (value && ShowNodeFreezeButton) || (ShowNodeHideButton && ShowNodeFreezeButton);
                Invalidate();
            }
        }
        private NodeButtonsLocation _nodeAddButtonLocation = NodeButtonsLocation.AfterNode;
        public NodeButtonsLocation NodeAddButtonLocation
        {
            get { return _nodeAddButtonLocation; }
            set { _nodeAddButtonLocation = value; Invalidate(); }
        }
        #endregion

        #endregion

        public bool HighlighLastSelectedObject { get; set; }
        public Keys ExpandHierarchyKey { get; set; }
        public MouseButtons DragMouseButton { get; set; }
        public DoubleClickAction DoubleClickAction { get; set; }

        #region AutoExpand

        private bool _autoExpandHierarchy = false;
        public bool AutoExpandHierarchy
        {
            get => _autoExpandHierarchy;
            set
            {
                _autoExpandHierarchy = value;
                if (value && ListMode == OutlinerListMode.Hierarchy)
                    ExpandAll();
            }
        }
        private bool _autoExpandLayer = false;
        public bool AutoExpandLayer
        {
            get => _autoExpandLayer;
            set
            {
                _autoExpandLayer = value;
                if (value && ListMode == OutlinerListMode.Layer)
                    ExpandAll();
            }
        }


        private bool _autoExpandMaterial = false;
        public bool AutoExpandMaterial
        {
            get => _autoExpandMaterial;
            set
            {
                _autoExpandMaterial = value;
                if (value && ListMode == OutlinerListMode.Material)
                    ExpandAll();
            }
        }

        #endregion

        private bool _hideGroupMembersLayerMode = true;
        public bool HideGroupMembersLayerMode
        {
            get =>  _hideGroupMembersLayerMode;
            set
            {
                _hideGroupMembersLayerMode = value;
                FillTree();
            }
        }


        public TreeView()
        {
            InitializeComponent();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            AllowDrop = true;

            Scene = new OutlinerScene();
            Style = new TreeStyle(this);
            Filter = new OutlinerFilter(this);

            ListMode = OutlinerListMode.Hierarchy;

            IconClickAction = IconClickAction.Hide;
            ShowNodeHideButton = true;
            InvertNodeHideButton = false;
            ShowNodeFreezeButton = true;
            ShowNodeBoxModeButton = false;

            HighlighLastSelectedObject = false;

            AutoExpandHierarchy = false;
            AutoExpandLayer = false;
            AutoExpandMaterial = false;

            ExpandHierarchyKey = Keys.Control;
            DragMouseButton = MouseButtons.Middle;
            DoubleClickAction = DoubleClickAction.Rename;


            _treeNodes = new Dictionary<OutlinerNode, TreeNode>();
            _selectedNodes = new HashSet<OutlinerNode>();
            _expandedNodeHandles = new HashSet<int>();

            _treeDragDropHandler = new TreeDragDropHandler(this, Scene);

            _updateTimer = new Timer();
            _updateTimer.Interval = 40;
            _updateTimer.Tick += new EventHandler(updateTimer_Tick);

            _sortTimer = new Timer();
            _sortTimer.Interval = 60;
            _sortTimer.Tick += new EventHandler(sortTimer_Tick);

            _updateWaitingForSort = false;

            _ensureSelectionVisibleTimer = new Timer();
            _ensureSelectionVisibleTimer.Interval = 50;
            _ensureSelectionVisibleTimer.Tick += new EventHandler(_ensureSelectionVisibleTimer_Tick);
            _ensureSelectionVisibleAction = EnsureSelectionVisibleAction.None;
            _ensureSelectionVisibleWaitingForSort = false;

            // Retrieve the 3ds Max main window handle using the SDK
            maxMainHandle = ManagedServices.AppSDK.GetMaxHWND();
        }


        public TreeView(IContainer container) : this()
        {
            container.Add(this);
        }

        private void DetachTimerHandlers()
        {
            if (_updateTimer != null)
                _updateTimer.Tick -= new EventHandler(updateTimer_Tick);
            if (_sortTimer != null)
                _sortTimer.Tick -= new EventHandler(sortTimer_Tick);
        }

        protected override void WndProc(ref Message m)
        {
            //base.WndProc(ref m);


            const int WM_KEYDOWN = 0x0100;
            const int WM_KEYUP = 0x0101;

            if (m.Msg == WM_KEYDOWN || m.Msg == WM_KEYUP)
            {
                ManagedServices.AppSDK.TranslateAndDispatchMessageToApplication(ref m);
                SetFocus(this.Handle);
            }
            base.WndProc(ref m);
        }


        // Avoid tooltips popping up when a treenode's text is outside the bounds (gives draw error)
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parms = base.CreateParams;
                parms.Style |= 0x80; // Turn on TVS_NOTOOLTIPS 
                return parms;
            }
        }

        #region Helper functions

        public int GetNodeCount()
        {
            return _treeNodes.Count;
        }

        // Re-applies the node style for all nodes in the tree.
        public void ResetAllNodesStyle()
        {
            ResetNodeStyleIntern(this.Nodes);
            HighlightAllParents();
        }

        private void ResetNodeStyleIntern(TreeNodeCollection col)
        {
            foreach (TreeNode tn in col)
            {
                Style.SetNodeImageKey(tn);
                Style.SetNodeColorAuto(tn);
                ResetNodeStyleIntern(tn.Nodes);
            }
        }


        internal OutlinerNode[] SelectedOutlinerNodes
        {
            get { return _selectedNodes.ToArray(); }
        }

        private void RestoreSelection(OutlinerNode[] selection)
        {
            _selectedNodes.Clear();

            foreach (OutlinerNode n in selection)
            {
                SelectNode(n, true);
            }

            BeginTimedEnsureSelectionVisible(EnsureSelectionVisibleAction.SelectionChanged);
            _selectionChanged = false;
        }

        private void RestoreExpandedStates()
        {
            _restoringExpandedStates = true;
            if ( (ListMode == OutlinerListMode.Hierarchy && AutoExpandHierarchy)|| (ListMode == OutlinerListMode.Layer && AutoExpandLayer))
            {
                ExpandAll();
            }
            else
            {
                foreach (int handle in _expandedNodeHandles)
                {
                    OutlinerNode n = Scene.GetNodeByHandle(handle);

                    if( n == null || !_treeNodes.TryGetValue(n, out TreeNode tn))
                        continue;

                    if( tn!= null && !tn.IsExpanded )
                        tn.Expand();
                }
            }
            _restoringExpandedStates = false;
        }


        internal bool IsNodeSelected(OutlinerNode n)
        {
            if (n == null)
                return false;

            return _selectedNodes.Contains(n);
        }

        internal bool IsNodeSelected(TreeNode tn)
        {
            if (tn == null || !(tn.Tag is OutlinerNode))
                return false;

            return IsNodeSelected((OutlinerNode)tn.Tag);
        }



        internal bool IsParentOfSelectedNode(TreeNode tn, bool entireHierarchy)
        {
            if (tn != null)
            {
                foreach (OutlinerNode n in _selectedNodes)
                {
                    TreeNode cn;
                    if (_treeNodes.TryGetValue(n, out cn))
                    {
                        if (!entireHierarchy)
                        {
                            if (cn.Parent == tn)
                                return true;
                        }
                        else
                        {
                            while (cn != null)
                            {
                                cn = cn.Parent;
                                if (cn == tn)
                                    return true;
                            }
                        }
                    }
                }
            }
            return false;
        }



        internal bool IsChildOfSelectedNode(TreeNode tn)
        {
            if (tn != null)
            {
                TreeNode parentNode = tn.Parent;
                while (parentNode != null)
                {
                    if (IsNodeSelected(parentNode))
                        return true;

                    parentNode = parentNode.Parent;
                }
            }
            return false;
        }


        internal bool IsChildOfSelectedNode(OutlinerNode n)
        {
            if (n == null)
                return false;

            TreeNode tn;
            if (_treeNodes.TryGetValue(n, out tn))
                return IsChildOfSelectedNode(tn);
            else
                return false;
        }


        internal bool IsChildOfNode(TreeNode cn, TreeNode pn)
        {
            if (cn != null && pn != null)
            {
                while (cn != null)
                {
                    if (cn == pn)
                        return true;
                    cn = cn.Parent;
                }
            }
            return false;
        }



        private OutlinerNode GetHighestParentToAdd(OutlinerNode o)
        {
            if (ListMode == OutlinerListMode.Layer)
                return o;
            else
            {
                // Keeping prevParentNode reference (previous parentNode inspected) to avoid having to do dictionary lookup twice for each iteration.
                OutlinerNode parentNode = o;
                OutlinerNode prevParentNode = o;
                while (parentNode != null && !_treeNodes.ContainsKey(parentNode))
                {
                    prevParentNode = parentNode;
                    parentNode = parentNode.Parent;
                }
                return prevParentNode;
            }
        }

        private OutlinerNode GetHighestParentToRemove(OutlinerNode o)
        {
            if (ListMode == OutlinerListMode.Layer)
                return o;
            else
            {
                // Keeping prevParentNode reference (previous parentNode inspected) to avoid having to do dictionary lookup twice for each iteration.
                OutlinerNode parentNode = o.Parent;
                OutlinerNode prevParentNode = o;
                while (parentNode != null && !this.Filter.ShowNode(parentNode))
                {
                    prevParentNode = parentNode;
                    parentNode = parentNode.Parent;
                }
                return prevParentNode;
            }
        }


        internal List<int> getChildNodeHandlesRecursive(OutlinerNode node)
        {
            List<int> childHandles = new List<int>();
            foreach (OutlinerNode cn in node.ChildNodes)
            {
                childHandles.Add(cn.Handle);
                childHandles.AddRange(getChildNodeHandlesRecursive(cn));
            }
            return childHandles;
        }


        private bool hasCollapsedParents(TreeNode tn)
        {
            if (tn == null)
                return false;

            TreeNode parent = tn.Parent;
            while (parent != null)
            {
                if (!parent.IsExpanded)
                    return true;

                parent = parent.Parent;
            }
            return false;
        }



        internal bool canAddSelectionToLayer(OutlinerLayer layer)
        {
            if (layer == null)
                return false;

            foreach (OutlinerNode n in _selectedNodes)
            {
                if (n is OutlinerObject && ((OutlinerObject)n).LayerHandle != layer.Handle)
                    return true;
                else if (n is OutlinerLayer && n.Handle != layer.Handle && n.ParentHandle != layer.Handle && !((OutlinerLayer)n).IsDefaultLayer)
                    return true;
            }

            return false;
        }

        #endregion






        #region Paint

        private Pen _dottedLinePen;
        new public Color LineColor
        {
            get { return base.LineColor; }
            set
            {
                base.LineColor = value;
                createLinePen();
            }
        }

        private SolidBrush _backgroundBrush;
        public override Color BackColor
        {
            get { return base.BackColor; }
            set
            {
                _backgroundBrush = new SolidBrush(value);
                base.BackColor = value;
            }
        }

        private void createLinePen()
        {
            _dottedLinePen = new Pen(LineColor);
            _dottedLinePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
        }


        internal void InvalidateTreeNode(TreeNode tn)
        {
            Rectangle r = new Rectangle(0, tn.Bounds.Y, ClientSize.Width, ClientSize.Height);
            Invalidate(r);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (_backgroundBrush == null)
                _backgroundBrush = new SolidBrush(BackColor);

            pevent.Graphics.FillRectangle(_backgroundBrush, pevent.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Nodes.Count == 0)
                return;

            if (_dottedLinePen == null)
                createLinePen();

            int curY = e.ClipRectangle.Y;
            while (curY <= e.ClipRectangle.Bottom)
            {
                TreeNode tn = GetNodeAt(0, curY);

                if (tn == null)
                    break;

                Rectangle tnBounds = tn.Bounds;
                if (tnBounds.Width != 0 && tnBounds.Height != 0)
                {
                    if (tn.ImageKey == "")
                        Style.SetNodeImageKey(tn);
                    else
                        DrawCustomNode(tn, tnBounds, e.Graphics);
                }

                curY += ItemHeight;
            }
        }

        protected void DrawCustomNode(TreeNode tn, Rectangle tnBounds, Graphics graphics)
        {
            object tnTag = tn.Tag;
            int scrlPosX = GetScrollPos(Handle, H_SCROLL);
            //Draw vertical line segments for parent nodes with next nodes.
            int xPos = Indent * tn.Level + _plusMinPadding - scrlPosX;
            TreeNode parent = tn.Parent;
            while (parent != null)
            {
                xPos -= Indent;
                if (parent.NextNode != null)
                {
                    int x = xPos + HalfPlusMinSize;
                    graphics.DrawLine(_dottedLinePen, x, tnBounds.Y, x, tnBounds.Bottom);
                }
                parent = parent.Parent;
            }

            //Draw L / T shaped line in front of node.
            int lineX = Indent * tn.Level + _plusMinPadding + HalfPlusMinSize - scrlPosX;
            int nodeYMid = tnBounds.Y + (int)Math.Ceiling(ItemHeight / 2f) - 1;
            int vlineStartY;
            int vlineEndY;

            if (tn.Parent == null && tn.Index == 0)
                vlineStartY = nodeYMid;
            else
                vlineStartY = tnBounds.Y;

            if (tn.NextNode == null)
                vlineEndY = nodeYMid;
            else
                vlineEndY = tnBounds.Bottom;

            graphics.DrawLine(_dottedLinePen, lineX, vlineStartY, lineX, vlineEndY);
            graphics.DrawLine(_dottedLinePen, lineX, nodeYMid, lineX + HalfPlusMinSize + _plusMinPadding - 1, nodeYMid);


            //Draw plusminus.
            if (tn.GetNodeCount(false) != 0)
            {
                if (Application.RenderWithVisualStyles)
                {
                    VisualStyleElement element = (tn.IsExpanded) ? VisualStyleElement.TreeView.Glyph.Opened : VisualStyleElement.TreeView.Glyph.Closed;
                    VisualStyleRenderer renderer = new VisualStyleRenderer(element);
                    renderer.DrawBackground(graphics, GetPlusMinusBounds(tn, false));
                }
                else
                {
                    DrawPlusMinus(graphics, GetPlusMinusBounds(tn, false), tn.IsExpanded);
                }
            }



            //Draw icon.
            if (ShowNodeIcon)
            {
                Bitmap img;
                string imgKey = tn.ImageKey;
                if (tnTag is OutlinerNode && ((OutlinerNode)tnTag).Filtered)
                {
                    imgKey += "_filtered";
                }
                else if (!ShowNodeHideButton && IconClickAction == IconClickAction.Hide)
                {
                    if (tnTag is IDisplayable && ((IDisplayable)tnTag).IsHidden)
                        imgKey += "_hidden";
                }
                else if (!ShowNodeFreezeButton && IconClickAction == IconClickAction.Freeze)
                {
                    if (tnTag is IDisplayable && ((IDisplayable)tnTag).IsFrozen)
                        imgKey += "_hidden";
                }
                if (_icons.TryGetValue(imgKey, out img))
                    graphics.DrawImage(img, GetImageBounds(tn));
            }

            //Draw background & Text.
            if (!tn.IsEditing)
            {
                Rectangle txtBgBounds = GetTextBackgroundBounds(tn, false);
                Point txtLocation = txtBgBounds.Location;

                Font f = Font;
                if (tn.Tag is IDisplayable && ((IDisplayable)tn.Tag).IsFrozen)
                {
                    f = Style.FrozenFont;
                    txtLocation.X -= 2;
                }
                if (HighlighLastSelectedObject && tn.Tag == _lastSelectedObject && IsNodeSelected(tn))
                    f = new Font(Font, f.Style | FontStyle.Bold);

                SizeF txtSize = graphics.MeasureString(tn.Text, f);
                txtBgBounds.Width = (int)txtSize.Width + 1;

                txtLocation.Y += (int)((float)ItemHeight - txtSize.Height) / 2 + 1;

                Color foreColor;
                if (tnTag is OutlinerNode && ((OutlinerNode)tnTag).Filtered)
                    foreColor = Color.FromArgb(50, tn.ForeColor.R, tn.ForeColor.G, tn.ForeColor.B);
                else
                    foreColor = tn.ForeColor;

                using (SolidBrush bgBrush = new SolidBrush(tn.BackColor), fgBrush = new SolidBrush(foreColor))
                {
                    graphics.FillRectangle(bgBrush, txtBgBounds);
                    graphics.DrawString(tn.Text, f, fgBrush, txtLocation);
                }
            }

            // Draw Node Buttons.
            if (tnTag is IDisplayable)
            {
                using (SolidBrush bgBrush = new SolidBrush(Style.BackColor))
                {
                    Rectangle r;
                    if (ShowNodeHideButton)
                    {
                        r = GetHideButtonBounds(tn);
                        bool isHidden = ((IDisplayable)tnTag).IsHidden || (tnTag is OutlinerObject && ((OutlinerObject)tnTag).Layer.IsHidden);
                        if ((isHidden && !InvertNodeHideButton) || (!isHidden && InvertNodeHideButton))
                            graphics.DrawImage(OutlinerResources.hide_button, r);
                        else
                            graphics.DrawImage(OutlinerResources.hide_button_disabled, r);
                    }

                    if (ShowNodeFreezeButton)
                    {
                        r = GetFreezeButtonBounds(tn);

                        if (((IDisplayable)tnTag).IsFrozen || (tnTag is OutlinerObject && ((OutlinerObject)tnTag).Layer.IsFrozen))
                            graphics.DrawImage(OutlinerResources.freeze_button, r);
                        else
                            graphics.DrawImage(OutlinerResources.freeze_button_disabled, r);
                    }

                    if (ShowNodeBoxModeButton)
                    {
                        r = GetBoxModeButtonBounds(tn);
                        if (((IDisplayable)tnTag).BoxMode)
                            graphics.DrawImage(OutlinerResources.boxmode_button, r);
                        else
                            graphics.DrawImage(OutlinerResources.boxmode_button_disabled, r);
                    }

                    if (tnTag is OutlinerLayer && ShowNodeAddButton)
                    {
                        r = GetAddButtonBounds(tn);

                        if (canAddSelectionToLayer((OutlinerLayer)tnTag))
                            graphics.DrawImage(OutlinerResources.add_button, r);
                        else
                            graphics.DrawImage(OutlinerResources.add_button_disabled, r);
                    }
                }
            }
        }


        private void DrawPlusMinus(Graphics graphics, Rectangle bounds, bool isExpanded)
        {
            ControlPaint.DrawButton(graphics, bounds, ButtonState.Normal);

            bounds.Width -= 1;
            bounds.Height -= 1;
            graphics.DrawLine(SystemPens.ControlText, bounds.X + 2, bounds.Y + bounds.Height / 2, bounds.X + bounds.Width - 2, bounds.Y + bounds.Height / 2);
            if (!isExpanded)
                graphics.DrawLine(SystemPens.ControlText, bounds.X + bounds.Width / 2, bounds.Y + 2, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height - 2);
        }

        #endregion



        #region Node Bounds methods

        private int _indent = 12;
        public new int Indent
        {
            get { return _indent; }
            set { _indent = value; }
        }
        private void AutoIndent()
        {
            if (ShowNodeHideButton && NodeHideButtonLocation == NodeButtonsLocation.BeforeNode)
                Indent = 12;
            else if (ShowNodeFreezeButton && NodeFreezeButtonLocation == NodeButtonsLocation.BeforeNode)
                Indent = 14;
            else if (ShowNodeBoxModeButton && NodeBoxModeButtonLocation == NodeButtonsLocation.BeforeNode)
                Indent = 12;
            else
                Indent = _iconSize.Width;
        }

        private const int PlusMinSize = 9;
        private const int HalfPlusMinSize = PlusMinSize / 2;
        private int _plusMinPadding = 3;
        private int _iconSpacing = 1;
        private int _nodeButtonSpacing = 1;


        protected Rectangle GetNodeBounds(TreeNode tn)
        {
            Rectangle tnBounds = tn.Bounds;
            Rectangle b = new Rectangle();
            b.X = Indent * tn.Level - GetScrollPos(Handle, H_SCROLL);
            b.Y = tnBounds.Y;
            b.Width = _plusMinPadding * 2 + PlusMinSize + tnBounds.Width - 1;
            if (ShowNodeIcon) b.Width += _iconSpacing * 2 + _iconSize.Width;
            if (tn.Tag is IDisplayable)
            {
                if (ShowNodeHideButton && NodeHideButtonLocation != NodeButtonsLocation.AlignRight) b.Width += _hideButtonSize.Width;
                if (ShowNodeFreezeButton && NodeFreezeButtonLocation != NodeButtonsLocation.AlignRight) b.Width += _freezeButtonSize.Width;
                if (ShowNodeBoxModeButton && NodeBoxModeButtonLocation != NodeButtonsLocation.AlignRight) b.Width += _boxModeButtonSize.Width;
                if (ShowNodeAddButton && NodeAddButtonLocation != NodeButtonsLocation.AlignRight && tn.Tag is OutlinerLayer) b.Width += _addButtonSize.Width;
            }
            b.Height = ItemHeight;
            return b;
        }


        private Rectangle GetPlusMinusBounds(TreeNode tn, bool includePadding)
        {
            if (tn == null)
                return Rectangle.Empty;

            Rectangle r = new Rectangle();

            if (includePadding)
            {
                r.X = Indent * tn.Level - GetScrollPos(Handle, H_SCROLL);
                r.Y = tn.Bounds.Y;
                r.Width = PlusMinSize + 2 * _plusMinPadding;
                r.Height = PlusMinSize;
            }
            else
            {
                r.X = Indent * tn.Level + _plusMinPadding - GetScrollPos(Handle, H_SCROLL);
                r.Y = tn.Bounds.Y + (ItemHeight - PlusMinSize) / 2;
                r.Width = PlusMinSize;
                r.Height = PlusMinSize;
            }

            return r;
        }


        private Rectangle GetNodeButtonsBeforeImageBounds(TreeNode tn)
        {
            Rectangle r = new Rectangle();
            r.X = GetPlusMinusBounds(tn, true).Right;
            r.Y = tn.Bounds.Y;
            int buttonsBeforeImage = 0;
            if (tn.Tag is IDisplayable)
            {
                if (ShowNodeHideButton && NodeHideButtonLocation == NodeButtonsLocation.BeforeNode)
                {
                    r.Width += _hideButtonSize.Width;
                    buttonsBeforeImage++;
                }
                if (ShowNodeFreezeButton && NodeFreezeButtonLocation == NodeButtonsLocation.BeforeNode)
                {
                    r.Width += _freezeButtonSize.Width;
                    buttonsBeforeImage++;
                }
                if (ShowNodeBoxModeButton && NodeBoxModeButtonLocation == NodeButtonsLocation.BeforeNode)
                {
                    r.Width += _boxModeButtonSize.Width;
                    buttonsBeforeImage++;
                }
                if (ShowNodeAddButton && NodeAddButtonLocation == NodeButtonsLocation.BeforeNode && tn.Tag is OutlinerLayer)
                {
                    r.Width += _addButtonSize.Width;
                    buttonsBeforeImage++;
                }

                r.Width += _nodeButtonSpacing * buttonsBeforeImage;
            }
            return r;
        }




        private Rectangle GetImageBounds(TreeNode tn)
        {
            if (tn == null || !ShowNodeIcon)
                return Rectangle.Empty;

            Rectangle r = new Rectangle();
            r.X = GetNodeButtonsBeforeImageBounds(tn).Right + _iconSpacing;
            r.Y = tn.Bounds.Y + (ItemHeight - _iconSize.Height) / 2;
            r.Width = _iconSize.Width;
            r.Height = _iconSize.Height;

            return r;
        }




        private Rectangle GetTextBackgroundBounds(TreeNode tn, bool includeIconSpacing)
        {
            if (tn == null)
                return Rectangle.Empty;

            Rectangle r = tn.Bounds;

            r.X = GetImageBounds(tn).Right;

            if (includeIconSpacing)
                r.Width += _iconSpacing;
            else
                r.X += _iconSpacing;

            return r;
        }

        private Size _hideButtonSize = new Size(8, 16);
        private Rectangle GetHideButtonBounds(TreeNode tn)
        {
            if (tn == null || !ShowNodeHideButton || !(tn.Tag is IDisplayable))
                return Rectangle.Empty;

            Rectangle r = new Rectangle();
            if (NodeHideButtonLocation == NodeButtonsLocation.BeforeNode)
                r.X = GetPlusMinusBounds(tn, true).Right;// + _nodeButtonSpacing;
            else if (NodeHideButtonLocation == NodeButtonsLocation.AfterNode)
                r.X = GetTextBackgroundBounds(tn, true).Right;
            else if (NodeHideButtonLocation == NodeButtonsLocation.AlignRight)
            {
                r.X = ClientRectangle.Right - _hideButtonSize.Width - _nodeButtonSpacing;
                if (ShowNodeFreezeButton && NodeFreezeButtonLocation == NodeButtonsLocation.AlignRight) r.X -= _freezeButtonSize.Width + _nodeButtonSpacing;
                if (ShowNodeBoxModeButton && NodeBoxModeButtonLocation == NodeButtonsLocation.AlignRight) r.X -= _boxModeButtonSize.Width + _nodeButtonSpacing;
                if (ShowNodeAddButton && NodeAddButtonLocation == NodeButtonsLocation.AlignRight)
                    if (tn.Tag is OutlinerLayer) r.X -= _addButtonSize.Width + _nodeButtonSpacing;
            }

            r.Y = tn.Bounds.Y + (ItemHeight - _hideButtonSize.Height) / 2;
            r.Size = _hideButtonSize;

            return r;
        }



        private Size _freezeButtonSize = new Size(12, 16);
        private Rectangle GetFreezeButtonBounds(TreeNode tn)
        {
            if (tn == null || !ShowNodeFreezeButton || !(tn.Tag is IDisplayable))
                return Rectangle.Empty;

            Rectangle r = new Rectangle();
            if (NodeFreezeButtonLocation == NodeButtonsLocation.BeforeNode)
            {
                r.X = GetPlusMinusBounds(tn, true).Right;// +_nodeButtonSpacing;
                if (ShowNodeHideButton && NodeHideButtonLocation == NodeButtonsLocation.BeforeNode) r.X += _hideButtonSize.Width + _nodeButtonSpacing;
            }
            else if (NodeFreezeButtonLocation == NodeButtonsLocation.AfterNode)
            {
                r.X = GetTextBackgroundBounds(tn, false).Right;
                if (ShowNodeHideButton && NodeHideButtonLocation == NodeButtonsLocation.AfterNode) r.X += _hideButtonSize.Width + _nodeButtonSpacing;
            }
            else if (NodeFreezeButtonLocation == NodeButtonsLocation.AlignRight)
            {
                r.X = ClientRectangle.Right - _freezeButtonSize.Width - _nodeButtonSpacing;
                if (ShowNodeBoxModeButton && NodeBoxModeButtonLocation == NodeButtonsLocation.AlignRight) r.X -= _boxModeButtonSize.Width + _nodeButtonSpacing;
                if (ShowNodeAddButton && NodeAddButtonLocation == NodeButtonsLocation.AlignRight) r.X -= _addButtonSize.Width + _nodeButtonSpacing;
            }

            r.Y = tn.Bounds.Y + (ItemHeight - _freezeButtonSize.Height) / 2;
            r.Size = _freezeButtonSize;

            return r;
        }


        private Size _boxModeButtonSize = new Size(9, 16);
        private Rectangle GetBoxModeButtonBounds(TreeNode tn)
        {
            if (tn == null)
                return Rectangle.Empty;

            Rectangle r = new Rectangle();

            if (NodeBoxModeButtonLocation == NodeButtonsLocation.BeforeNode)
            {
                r.X = GetPlusMinusBounds(tn, true).Right;// +_nodeButtonSpacing;
                if (ShowNodeHideButton && NodeHideButtonLocation == NodeButtonsLocation.BeforeNode) r.X += _hideButtonSize.Width + _nodeButtonSpacing;
                if (ShowNodeFreezeButton && NodeFreezeButtonLocation == NodeButtonsLocation.BeforeNode) r.X += _freezeButtonSize.Width + _nodeButtonSpacing;
            }
            else if (NodeBoxModeButtonLocation == NodeButtonsLocation.AfterNode)
            {
                r.X = GetTextBackgroundBounds(tn, false).Right;
                if (ShowNodeHideButton && NodeHideButtonLocation == NodeButtonsLocation.AfterNode) r.X += _hideButtonSize.Width + _nodeButtonSpacing;
                if (ShowNodeFreezeButton && NodeFreezeButtonLocation == NodeButtonsLocation.AfterNode) r.X += _freezeButtonSize.Width + _nodeButtonSpacing;
            }
            else if (NodeBoxModeButtonLocation == NodeButtonsLocation.AlignRight)
            {
                r.X = ClientRectangle.Right - _boxModeButtonSize.Width - _nodeButtonSpacing;
                if (ShowNodeAddButton && NodeAddButtonLocation == NodeButtonsLocation.AlignRight) r.X -= _addButtonSize.Width + _nodeButtonSpacing;
            }

            r.Y = tn.Bounds.Y + (ItemHeight - _boxModeButtonSize.Height) / 2;
            r.Size = _boxModeButtonSize;

            return r;
        }


        private Size _addButtonSize = new Size(9, 16);
        private Rectangle GetAddButtonBounds(TreeNode tn)
        {
            if (tn == null)
                return Rectangle.Empty;

            Rectangle r = new Rectangle();

            if (NodeAddButtonLocation == NodeButtonsLocation.BeforeNode)
            {
                r.X = GetPlusMinusBounds(tn, true).Right;// +_nodeButtonSpacing;
                if (ShowNodeHideButton && NodeHideButtonLocation == NodeButtonsLocation.BeforeNode) r.X += _hideButtonSize.Width + _nodeButtonSpacing;
                if (ShowNodeFreezeButton && NodeFreezeButtonLocation == NodeButtonsLocation.BeforeNode) r.X += _freezeButtonSize.Width + _nodeButtonSpacing;
                if (ShowNodeBoxModeButton && NodeBoxModeButtonLocation == NodeButtonsLocation.BeforeNode) r.X += _boxModeButtonSize.Width + _nodeButtonSpacing;
            }
            else if (NodeAddButtonLocation == NodeButtonsLocation.AfterNode)
            {
                r.X = GetTextBackgroundBounds(tn, false).Right;
                if (ShowNodeHideButton && NodeHideButtonLocation == NodeButtonsLocation.AfterNode) r.X += _hideButtonSize.Width + _nodeButtonSpacing;
                if (ShowNodeFreezeButton && NodeFreezeButtonLocation == NodeButtonsLocation.AfterNode) r.X += _freezeButtonSize.Width + _nodeButtonSpacing;
                if (ShowNodeBoxModeButton && NodeBoxModeButtonLocation == NodeButtonsLocation.AfterNode) r.X += _boxModeButtonSize.Width + _nodeButtonSpacing;
            }
            else if (NodeAddButtonLocation == NodeButtonsLocation.AlignRight)
            {
                r.X = ClientRectangle.Right - _addButtonSize.Width - _nodeButtonSpacing;
            }

            r.Y = tn.Bounds.Y + (ItemHeight - _addButtonSize.Height) / 2;
            r.Size = _addButtonSize;

            return r;
        }



        private bool IsClickOnImage(TreeNode tn, MouseEventArgs e)
        {
            if (tn == null || !ShowNodeIcon)
                return false;

            return GetImageBounds(tn).Contains(e.Location);
        }

        private bool IsClickOnHideButton(TreeNode tn, MouseEventArgs e)
        {
            if (tn == null || !ShowNodeHideButton)
                return false;

            return GetHideButtonBounds(tn).Contains(e.Location);
        }

        private bool IsClickOnFreezeButton(TreeNode tn, MouseEventArgs e)
        {
            if (tn == null || !ShowNodeFreezeButton)
                return false;

            return GetFreezeButtonBounds(tn).Contains(e.Location);
        }

        private bool IsClickOnBoxModeButton(TreeNode tn, MouseEventArgs e)
        {
            if (tn == null || !ShowNodeBoxModeButton)
                return false;

            return GetBoxModeButtonBounds(tn).Contains(e.Location);
        }

        private bool IsClickOnAddButton(TreeNode tn, MouseEventArgs e)
        {
            if (tn == null || !ShowNodeAddButton || !(tn.Tag is OutlinerLayer))
                return false;

            return GetAddButtonBounds(tn).Contains(e.Location);
        }

        private bool IsClickOnText(TreeNode tn, MouseEventArgs e)
        {
            if (tn == null)
                return false;

            return GetTextBackgroundBounds(tn, true).Contains(e.Location);
        }

        private bool IsClickOnPlusMinus(TreeNode tn, MouseEventArgs e)
        {
            if (tn == null)
                return false;

            return GetPlusMinusBounds(tn, false).Contains(e.Location);
        }



        private bool IsDragOnNode(TreeNode tn, Point e)
        {
            if (tn == null)
                return false;

            return GetNodeBounds(tn).Contains(e);
        }

        private bool IsDragLeftOfPlusMinus(TreeNode tn, Point e)
        {
            if (tn == null)
                return false;

            int boundLeft = Indent * tn.Level + _plusMinPadding - GetScrollPos(Handle, H_SCROLL);
            if (tn.GetNodeCount(false) == 0)
                boundLeft += PlusMinSize;

            return e.X < boundLeft;
        }

        #endregion



        #region MultiSelect

        private bool _handlingMouseClick = false;
        private int _numMouseClicks = 0;
        private bool _nodeProcessedOnMouseDown = false;
        private bool _selectionChanged = false;
        private TreeNode _mostRecentSelectedNode;
        private OutlinerObject _lastSelectedObject;

        // Override the OnBeforeSelect event to cancel it (we'll do the selecting thank you).
        protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }



        #region OnMouseDown, OnMouseUp

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _handlingMouseClick = true;
            _numMouseClicks = e.Clicks;

            TreeNode tn = GetNodeAt(e.X, e.Y);


            if (IsClickOnHideButton(tn, e))
            {
                _nodeProcessedOnMouseDown = true;
                OnHideButtonClick(tn, e);
            }
            else if (IsClickOnFreezeButton(tn, e))
            {
                _nodeProcessedOnMouseDown = true;
                OnFreezeButtonClick(tn, e);
            }
            else if (IsClickOnBoxModeButton(tn, e))
            {
                _nodeProcessedOnMouseDown = true;
                OnBoxModeButtonClick(tn, e);
            }
            else if (IsClickOnAddButton(tn, e))
            {
                _nodeProcessedOnMouseDown = true;
                OnAddButtonClick(tn, e);
            }
            else if (IsClickOnText(tn, e))
            {
                if (!IsNodeSelected(tn))
                {
                    _nodeProcessedOnMouseDown = true;
                    ProcessNodeClick(tn, e, Control.ModifierKeys);
                }

                if ((e.Button & MouseButtons.Right) == MouseButtons.Right)
                {
                    _nodeProcessedOnMouseDown = true;
                    showContextMenu(PointToScreen(e.Location), tn.Tag as OutlinerNode);
                }
                else if ((e.Button & DragMouseButton) == DragMouseButton && (DragMouseButton & MouseButtons.Middle) == MouseButtons.Middle && _numMouseClicks == 1)
                    OnItemDrag(new ItemDragEventArgs(e.Button, tn));
            }
            else if (IsClickOnImage(tn, e))
            {
                _nodeProcessedOnMouseDown = true;
                OnNodeIconClick(tn, e);
            }
            else if (IsClickOnPlusMinus(tn, e))
            {
                _internalExpandCollapse = true;
                this.BeginUpdate();
                if (tn.IsExpanded)
                    tn.Collapse(!((Control.ModifierKeys & ExpandHierarchyKey) == ExpandHierarchyKey));
                else
                {
                    if ((Control.ModifierKeys & ExpandHierarchyKey) == ExpandHierarchyKey)
                        tn.ExpandAll();
                    else
                        tn.Expand();
                }
                this.EndUpdate();
                _internalExpandCollapse = false;
            }
            else if (Control.ModifierKeys == Keys.None)
            {
                _nodeProcessedOnMouseDown = true;

                //Deselect nodes unless right-mouse button was clicked, 
                //or unless the click was directly after closing the context-menu.
                if ((e.Button & MouseButtons.Right) != MouseButtons.Right && _selectedNodes.Count > 0
                    && DateTime.Now.Ticks - ContextMenus.ClosedTicks > TimeSpan.FromMilliseconds(5).Ticks)
                {
                    this.UnselectAllNodes();
                    OnSelectionChanged();
                }
                OnBackgroundClick(e);
            }

            base.OnMouseDown(e);
        }


        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!_nodeProcessedOnMouseDown)
            {
                TreeNode tn = this.GetNodeAt(e.X, e.Y);

                // Mouse click has not been handled by the mouse down event, so do it here. This is the case when
                // a selected node was clicked again; in that case we handle that click here because in case the
                // user is dragging the node, we should not put it in edit mode.					

                if (tn != null && IsClickOnText(tn, e))
                {
                    this.ProcessNodeClick(tn, e, Control.ModifierKeys);
                    if ((e.Button & MouseButtons.Right) == MouseButtons.Right)
                        showContextMenu(PointToScreen(e.Location), tn.Tag as OutlinerNode);
                    else if ((e.Button & DragMouseButton) == DragMouseButton && (DragMouseButton & MouseButtons.Middle) == MouseButtons.Middle)
                    {
                        //OnItemDrag(new ItemDragEventArgs(e.Button, tn));
                    }
                }
            }

            _nodeProcessedOnMouseDown = false;

            _handlingMouseClick = false;

            base.OnMouseUp(e);
        }

        #endregion


        #region ProcessNodeRange

        /// <summary>
        /// Processes a node range.
        /// </summary>
        /// <param name="startNode">Start node of range.</param>
        /// <param name="endNode">End node of range.</param>
        /// <param name="e">MouseEventArgs.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="tva">TreeViewAction.</param>
        /// <param name="allowStartEdit">True if node can go to edit mode, false if not.</param>
        private void ProcessNodeClick(TreeNode node, MouseEventArgs e, Keys keys)
        {
            _selectionChanged = false; // prepare for OnSelectionsChanged

            if ((e.Button & MouseButtons.Left) == MouseButtons.Left || (e.Button & DragMouseButton) == DragMouseButton || ((e.Button & MouseButtons.Right) == MouseButtons.Right && !IsNodeSelected(node)))
            {
                if (((keys & Keys.Control) != Keys.Control) && ((keys & Keys.Shift) != Keys.Shift))
                {
                    // CTRL and SHIFT not held down

                    // If it was a double click, select node and suspend further processing					
                    if (_numMouseClicks == 2)
                    {
                        base.OnMouseDown(e);
                        return;
                    }

                    UnselectAllNodes();
                    SelectNode(node, true);
                }
                else if (((keys & Keys.Control) == Keys.Control) && ((keys & Keys.Shift) != Keys.Shift))
                    //Control pressed
                    SelectNode(node, !IsNodeSelected(node));
                else if (((keys & Keys.Control) != Keys.Control) && ((keys & Keys.Shift) == Keys.Shift))
                {
                    // SHIFT pressed
                    if (_mostRecentSelectedNode == null)
                    {
                        _mostRecentSelectedNode = node;
                    }

                    SelectNodesInsideRange(_mostRecentSelectedNode, node);
                }
            }

            OnSelectionChanged();
        }

        #endregion


        #region Internal selection methods

        internal void SelectNode(OutlinerNode node, bool select)
        {
            if (node == null)
                return;

            TreeNode tn;
            if (_treeNodes.TryGetValue(node, out tn))
                SelectNode(node, tn, select);
        }

        internal void SelectNode(TreeNode tn, bool select)
        {
            if (tn == null || (!(tn.Tag is OutlinerNode)))
                return;

            SelectNode((OutlinerNode)tn.Tag, tn, select);
        }

        internal void SelectNode(OutlinerNode node, TreeNode tn, bool select)
        {
            if (node == null || tn == null)
                return;

            if (select)
            {
                this.Style.SetNodeColor(tn, NodeColor.Selected);

                if (!_selectedNodes.Contains(node))
                {
                    _selectedNodes.Add(node);
                    if (node is OutlinerObject)
                        _lastSelectedObject = (OutlinerObject)node;
                }

                HighlightParents(node);

                _selectionChanged = true;

                _mostRecentSelectedNode = tn;
            }
            else
            {
                _selectedNodes.Remove(node);
                _selectionChanged = true;

                if (IsParentOfSelectedNode(tn, true))
                    Style.SetNodeColor(tn, NodeColor.ParentOfSelected);
                else
                    Style.SetNodeColor(tn, NodeColor.Default);

                RemoveParentHighlights(node);
            }
        }



        private void SelectNodesInsideRange(TreeNode startNode, TreeNode endNode)
        {
            if (startNode == null || endNode == null)
                return;

            // Calculate start node and end node
            TreeNode firstNode = null;
            TreeNode lastNode = null;
            if (startNode.Bounds.Y < endNode.Bounds.Y)
            {
                firstNode = startNode;
                lastNode = endNode;
            }
            else
            {
                firstNode = endNode;
                lastNode = startNode;
            }

            // Select each node in range
            SelectNode(firstNode, true);
            TreeNode tnTemp = firstNode;
            while (tnTemp != lastNode)
            {
                tnTemp = tnTemp.NextVisibleNode;
                if (tnTemp != null)
                {
                    SelectNode(tnTemp, true);
                }
            }
            SelectNode(lastNode, true);
        }


        private void SelectAllNodes()
        {
            foreach (KeyValuePair<OutlinerNode, TreeNode> kvp in _treeNodes)
            {
                SelectNode(kvp.Key, kvp.Value, true);
            }
        }

        private void UnselectAllNodes()
        {
            OutlinerNode[] nodesToUnselect = _selectedNodes.ToArray();

            foreach (OutlinerNode n in nodesToUnselect)
            {
                SelectNode(n, false);
            }
        }


        private void SelectChildNodes()
        {
            OutlinerNode[] sel = _selectedNodes.ToArray();
            foreach (OutlinerNode n in sel)
            {
                SelectChildNodesIntern(n, n);
            }

            OnSelectionChanged();
        }

        private void SelectChildNodesIntern(OutlinerNode startingNode, OutlinerNode n)
        {
            if (n.ChildNodesCount > 0)
            {
                List<OutlinerNode> c = n.ChildNodes;
                foreach (OutlinerNode o in c)
                {
                    SelectNode(o, true);
                    if (!(startingNode is OutlinerLayer && o is OutlinerObject))
                        SelectChildNodesIntern(startingNode, o);
                }
            }
        }

        #endregion



        protected void OnSelectionChanged()
        {
            if (_selectionChanged )
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(SelectedObjectHandles, SelectedLayerHandlesIndirect, SelectedMaterialHandles));
        }



        protected void OnLayerNameSynced(OutlinerLayer layer)
        {
            if (layer == null || _listMode != OutlinerListMode.Layer)
                return;

            SetLayerName(layer.Handle, layer.Name);
        }

        protected void OnActiveLayerChanged(OutlinerLayer newActive , OutlinerLayer prevActive )
        {
            if (_listMode != OutlinerListMode.Layer)
                return;

            if( prevActive != null )
                SetLayerActive(prevActive.Handle, false);

            if( newActive != null )
                SetLayerActive(newActive.Handle, true);
        }


        #endregion



        #region OnItemDrag, OnDragEnter, OnDragOver, OnDragDrop

        // Use "Scroll" enum value as "None", so that DragDrop event is fired even when a droptarget is not valid.
        internal const DragDropEffects DragDropEffectsNone = DragDropEffects.Scroll;
        internal const DragDropEffects AllowedDragDropEffects = DragDropEffects.Copy | DragDropEffects.Link | DragDropEffects.Move;

        private TreeDragDropHandler _treeDragDropHandler;
        private TreeNode _dragDropTargetNode;
        private Color _dragDropTargetForeColor;
        private Color _dragDropTargetBackColor;
        private DragDropHandler _dragDropHandler;
        private DateTime _dragOverStartTime;
        private HashSet<TreeNode> _dragOverExpandedNodes;

        private DateTime _lastScrollTime;
        private const int WM_HSCROLL = 276;
        private const int WM_VSCROLL = 277;
        private readonly TimeSpan fastScrollSpan = TimeSpan.FromMilliseconds(100);
        private readonly TimeSpan slowScrollSpan = TimeSpan.FromMilliseconds(300);

        protected override void OnItemDrag(ItemDragEventArgs e)
        {
            bool canDragDrop = true;

            // If the button pressed isn't the preferred drag-drop button, don't start dragdrop.
            if ((e.Button & DragMouseButton) != DragMouseButton)
                canDragDrop = false;
            else
            {
                // Check if all nodes in the selection can be dragged.
                foreach (OutlinerNode n in _selectedNodes)
                {
                    if (n.DragDropHandler == null || !n.DragDropHandler.AllowDrag)
                    {
                        canDragDrop = false;
                        break;
                    }
                }
            }

            if (canDragDrop)
            {
                Point mousePos = PointToClient(Control.MousePosition);
                if (IsClickOnText((TreeNode)e.Item, new MouseEventArgs(e.Button, 1, mousePos.X, mousePos.Y, 0)))
                {
                    OutlinerNode[] selOutlinerNodes = SelectedOutlinerNodes;

                    _dragOverExpandedNodes = new HashSet<TreeNode>();
                    DoDragDrop(selOutlinerNodes, AllowedDragDropEffects);
                }
            }

            base.OnItemDrag(e);
        }



        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            drgevent.Effect = DragDropEffectsNone;
            base.OnDragEnter(drgevent);
        }


        // Restore previous dragDropTarget colors.
        private void RestorePreviousDragDropTargetColor()
        {
            if (_dragDropTargetNode != null)
            {
                Style.SetNodeColor(_dragDropTargetNode, _dragDropTargetForeColor, _dragDropTargetBackColor);
            }
        }

        private void RestoreDragDropExpandedStates()
        {
            _internalExpandCollapse = true;

            foreach (TreeNode tn in _dragOverExpandedNodes)
            {
                if (_dragDropTargetNode == null || !IsChildOfNode(_dragDropTargetNode, tn))
                    tn.Collapse();
            }
            _dragOverExpandedNodes.Clear();

            _internalExpandCollapse = false;
        }



        protected override void OnDragOver(DragEventArgs drgevent)
        {
            // Get node under cursor.
            Point targetPoint = this.PointToClient(new Point(drgevent.X, drgevent.Y));
            TreeNode targetNode = this.GetNodeAt(targetPoint);

            if (IsDragLeftOfPlusMinus(targetNode, targetPoint))
            {
                drgevent.Effect = DragDropEffectsNone;
                RestorePreviousDragDropTargetColor();
                _dragDropTargetNode = null;
            }
            else
            {
                if (!IsDragOnNode(targetNode, targetPoint))
                    targetNode = null;

                if (targetNode != _dragDropTargetNode || targetNode == null)
                {
                    RestorePreviousDragDropTargetColor();

                    _dragOverStartTime = DateTime.Now;
                    _dragDropTargetNode = targetNode;
                    if (targetNode != null)
                    {
                        _dragDropTargetForeColor = targetNode.ForeColor;
                        _dragDropTargetBackColor = targetNode.BackColor;
                    }

                    // Store the dragdrophandler for the current target.
                    if (_dragDropTargetNode != null)
                        _dragDropHandler = ((OutlinerNode)_dragDropTargetNode.Tag).DragDropHandler;
                    else
                        _dragDropHandler = _treeDragDropHandler;
                }

                // Get dragdrop effect for targetnode. 
                // Note: if the node is selected, or is a child of the selection, it can never be a valid droptarget -> DragDropEffects.None.
                //TODO move isnodeselected and ischildofselectednode to dragdrophandler (?)
                if (IsNodeSelected(_dragDropTargetNode) || IsParentOfSelectedNode(_dragDropTargetNode, false) || IsChildOfSelectedNode(_dragDropTargetNode))
                    drgevent.Effect = DragDropEffectsNone;
                else
                    drgevent.Effect = _dragDropHandler.GetDragDropEffect(drgevent.Data);

                if (drgevent.Effect != DragDropEffectsNone && _dragDropTargetNode != null)
                    this.Style.SetNodeColor(_dragDropTargetNode, NodeColor.LinkTarget);
            }


            // Auto expand
            if (_dragDropTargetNode != null && !_dragDropTargetNode.IsExpanded)
            {
                if (DateTime.Now - _dragOverStartTime > TimeSpan.FromMilliseconds(750))
                {
                    _internalExpandCollapse = true;
                    _dragDropTargetNode.Expand();
                    _internalExpandCollapse = false;
                    _dragOverExpandedNodes.Add(_dragDropTargetNode);
                }
            }


            // Scroll
            if (_lastScrollTime == null)
                _lastScrollTime = DateTime.Now;

            TimeSpan dScrollTime = DateTime.Now - _lastScrollTime;
            int _scrollBounds = ItemHeight;

            int scroll = -1;
            if (targetPoint.X < _scrollBounds * 2 && dScrollTime > slowScrollSpan)
                scroll = 0;
            else if (targetPoint.X > Bounds.Width - _scrollBounds * 2 && dScrollTime > slowScrollSpan)
                scroll = 1;

            if (targetPoint.X < _scrollBounds && dScrollTime > fastScrollSpan)
                scroll = 0;
            else if (targetPoint.X > Bounds.Width - _scrollBounds && dScrollTime > fastScrollSpan)
                scroll = 1;

            if (scroll != -1)
            {
                SendMessage(Handle, WM_HSCROLL, scroll, 0);
                _lastScrollTime = DateTime.Now;
            }

            scroll = -1;
            if (targetPoint.Y < _scrollBounds * 2 && dScrollTime > slowScrollSpan)
                scroll = 0;
            else if (targetPoint.Y > Bounds.Height - _scrollBounds * 2 && dScrollTime > slowScrollSpan)
                scroll = 1;

            if (targetPoint.Y < _scrollBounds && dScrollTime > fastScrollSpan)
                scroll = 0;
            else if (targetPoint.Y > Bounds.Height - _scrollBounds && dScrollTime > fastScrollSpan)
                scroll = 1;

            if (scroll != -1)
            {
                SendMessage(Handle, WM_VSCROLL, scroll, 0);
                _lastScrollTime = DateTime.Now;
            }

            base.OnDragOver(drgevent);
        }



        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            this.BeginUpdate();

            RestorePreviousDragDropTargetColor();

            // Handle drop.
            if (drgevent.Effect != DragDropEffectsNone && _dragDropHandler != null)
                _dragDropHandler.ItemDropped(drgevent.Data);

            RestoreDragDropExpandedStates();

            _dragDropTargetNode = null;

            this.EndUpdate();

            base.OnDragDrop(drgevent);
        }


        protected override void OnDragLeave(EventArgs e)
        {
            this.BeginUpdate();

            RestorePreviousDragDropTargetColor();

            _dragDropTargetNode = null;

            RestoreDragDropExpandedStates();

            this.EndUpdate();

            base.OnDragLeave(e);
        }

        #endregion



        #region Expand & Collapse

        private bool _internalExpandCollapse = false;

        // Override DefWndProc to block double-click expand if necessary.
        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == 515 && DoubleClickAction != DoubleClickAction.Expand)
            {
                /* WM_LBUTTONDBLCLK BLOCKED */
            }
            else
                base.DefWndProc(ref m);
        }


        protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
        {
            if (_handlingMouseClick && !_internalExpandCollapse && !_restoringExpandedStates)
                e.Cancel = true;
            base.OnBeforeExpand(e);
        }
        protected override void OnBeforeCollapse(TreeViewCancelEventArgs e)
        {
            if (_handlingMouseClick && !_internalExpandCollapse)
                e.Cancel = true;
            base.OnBeforeCollapse(e);
        }

        protected override void OnAfterExpand(TreeViewEventArgs e)
        {
            if (!_restoringExpandedStates && e.Node.Tag is OutlinerNode)
            {
                int handle = ((OutlinerNode)e.Node.Tag).Handle;
                if (!_expandedNodeHandles.Contains(handle))
                    _expandedNodeHandles.Add(handle);
            }

            base.OnAfterExpand(e);
        }

        protected override void OnAfterCollapse(TreeViewEventArgs e)
        {
            if (e.Node.Tag is OutlinerNode)
                _expandedNodeHandles.Remove(((OutlinerNode)e.Node.Tag).Handle);

            base.OnAfterCollapse(e);
        }

        #endregion




        #region OnNodeImageClick, OnNodeMouseClick, OnBackgrounClick, OnNodeMouseDoubleClick

        //Toggles the hidden state of a treenode (after a mouseup event on it's image).
        private void OnNodeIconClick(TreeNode tn, MouseEventArgs e)
        {
            if (IconClickAction == IconClickAction.Hide)
                OnHideButtonClick(tn, e);
            else if (IconClickAction == IconClickAction.Freeze)
                OnFreezeButtonClick(tn, e);
            else if (IconClickAction == IconClickAction.SetActive)
            {
                if (tn.Tag is OutlinerLayer)
                {
                    OutlinerLayer layer = (OutlinerLayer)tn.Tag;
                    layer.IsActive = !layer.IsActive;

                    LayerActiveChanged?.Invoke(this, new NodePropertyChangedEventArgs(new int[1] { layer.Handle }, "isActive", layer.IsActive));
                }
            }
        }

        //Returns true if an IDisplayable of a node can be set. That is, when
        //its property is not overridden by its (parent-)layer.
        private bool canSetProperty(OutlinerNode n, string propName)
        {
            IDisplayable objToInspect = null;
            if (n is OutlinerObject)
                objToInspect = ((OutlinerObject)n).Layer;
            else if (n is OutlinerLayer)
            {
                if (n.Parent == null)
                    return true;
                else if (!(n.Parent is IDisplayable))
                    return false;
                else
                    objToInspect = (IDisplayable)n.Parent;
            }
            else if (n is IDisplayable)
                objToInspect = (IDisplayable)n;
            else
                return false;

            System.Reflection.PropertyInfo propInfo = objToInspect.GetType().GetProperty(propName);
            if (propInfo.PropertyType == typeof(bool))
                return !(bool)propInfo.GetValue(objToInspect, null);
            else
                return false;
        }

        private void OnHideButtonClick(TreeNode tn, MouseEventArgs e)
        {
            if (!(tn.Tag is OutlinerNode) || !(tn.Tag is IDisplayable) || (e.Button & MouseButtons.Left) != MouseButtons.Left)
                return;

            OutlinerNode node = (OutlinerNode)tn.Tag;
            bool hidden = !((IDisplayable)node).IsHidden;
            List<int> handles = new List<int>();


            if ((Control.ModifierKeys & Keys.Control) != Keys.Control && IsNodeSelected(node))
            {
                // Store selection in array, because HideNode could remove treenodes if filter is set to not show hidden nodes.
                OutlinerNode[] selNodes = SelectedOutlinerNodes;
                foreach (OutlinerNode n in selNodes)
                {
                    if (this.canSetProperty(n, "IsHidden"))
                    {
                        if (n is OutlinerObject && ((OutlinerObject)n).IsGroupHead)
                        {
                            HideNodeRecursive(n, hidden);
                            handles.Add(n.Handle);
                            handles.AddRange(getChildNodeHandlesRecursive(n));
                        }
                        else
                        {
                            HideNode(n, hidden);
                            handles.Add(n.Handle);
                        }
                    }
                }
            }
            else if (this.canSetProperty(node, "IsHidden"))
            {
                if (node is OutlinerObject && ((OutlinerObject)node).IsGroupHead)
                {
                    HideNodeRecursive(node, hidden);
                    handles.Add(node.Handle);
                    handles.AddRange(getChildNodeHandlesRecursive(node));
                }
                else
                {
                    HideNode(node, hidden);
                    handles.Add(node.Handle);
                }
            }

            if (NodeHidden != null && handles.Count > 0)
                NodeHidden(this, new NodePropertyChangedEventArgs(handles.ToArray(), "isHidden", hidden));
        }



        private void OnFreezeButtonClick(TreeNode tn, MouseEventArgs e)
        {
            if (!(tn.Tag is OutlinerNode) || !(tn.Tag is IDisplayable) || (e.Button & MouseButtons.Left) != MouseButtons.Left)
                return;

            OutlinerNode node = (OutlinerNode)tn.Tag;
            bool frozen = !((IDisplayable)node).IsFrozen;
            List<int> handles = new List<int>();


            if ((Control.ModifierKeys & Keys.Control) != Keys.Control && IsNodeSelected(node))
            {
                // Store selection in array, because FreezeNode could remove treenodes if filter is set to not show frozen nodes.
                OutlinerNode[] selNodes = SelectedOutlinerNodes;
                foreach (OutlinerNode n in selNodes)
                {
                    if (this.canSetProperty(n, "IsFrozen"))
                    {
                        if (n is OutlinerObject && ((OutlinerObject)n).IsGroupHead)
                        {
                            FreezeNodeRecursive(n, frozen);
                            handles.Add(n.Handle);
                            handles.AddRange(getChildNodeHandlesRecursive(n));
                        }
                        else
                        {
                            FreezeNode(n, frozen);
                            handles.Add(n.Handle);
                        }
                    }
                }
            }
            else if (this.canSetProperty(node, "IsFrozen"))
            {
                if (node is OutlinerObject && ((OutlinerObject)node).IsGroupHead)
                {
                    FreezeNodeRecursive(node, frozen);
                    handles.Add(node.Handle);
                    handles.AddRange(getChildNodeHandlesRecursive(node));
                }
                else
                {
                    FreezeNode(node, frozen);
                    handles.Add(node.Handle);
                }
            }

            if (NodeFrozen != null && handles.Count > 0)
                NodeFrozen(this, new NodePropertyChangedEventArgs(handles.ToArray(), "isFrozen", frozen));
        }



        private void OnBoxModeButtonClick(TreeNode tn, MouseEventArgs e)
        {
            if (!(tn.Tag is OutlinerNode) || !(tn.Tag is IDisplayable) || (e.Button & MouseButtons.Left) != MouseButtons.Left)
                return;

            OutlinerNode node = (OutlinerNode)tn.Tag;
            bool boxMode = !((IDisplayable)node).BoxMode;
            List<int> handles = new List<int>();


            if ((Control.ModifierKeys & Keys.Control) != Keys.Control && IsNodeSelected(node))
            {
                // Store selection in array, because FreezeNode could remove treenodes if filter is set to not show frozen nodes.
                OutlinerNode[] selNodes = SelectedOutlinerNodes;
                foreach (OutlinerNode n in selNodes)
                {
                    if (n is IDisplayable)
                    {
                        handles.Add(n.Handle);

                        if (n is OutlinerObject && ((OutlinerObject)n).IsGroupHead)
                        {
                            SetBoxModeNodeRecursive(n, boxMode);
                            handles.AddRange(getChildNodeHandlesRecursive(n));
                        }
                        else
                            SetBoxModeNode(n, boxMode);
                    }
                }
            }
            else
            {
                handles.Add(node.Handle);

                if (node is OutlinerObject && ((OutlinerObject)node).IsGroupHead)
                {
                    SetBoxModeNodeRecursive(node, boxMode);
                    handles.AddRange(getChildNodeHandlesRecursive(node));
                }
                else
                    SetBoxModeNode(node, boxMode);
            }

            if (NodeBoxModeChanged != null)
                NodeBoxModeChanged(this, new NodePropertyChangedEventArgs(handles.ToArray(), "boxMode", boxMode));
        }


        private void OnAddButtonClick(TreeNode tn, MouseEventArgs e)
        {
            if (tn.Tag is OutlinerLayer)
            {
                int layerHandle = (tn.Tag as OutlinerLayer).Handle;
                List<int> objectHandles = new List<int>();
                List<int> layerHandles = new List<int>();
                foreach (OutlinerNode node in _selectedNodes)
                {
                    if (node.Handle != layerHandle)
                    {
                        if (node is OutlinerObject)
                        {
                            SetObjectLayer(node.Handle, layerHandle);
                            objectHandles.Add(node.Handle);
                        }
                        else if (node is OutlinerLayer && !((OutlinerLayer)node).IsDefaultLayer)
                        {
                            TreeNode cn;
                            if (_treeNodes.TryGetValue(node, out cn) && !IsChildOfNode(tn, cn))
                            {
                                SetLayerParent(node.Handle, layerHandle);
                                layerHandles.Add(node.Handle);
                            }
                        }
                    }
                }

                if (objectHandles.Count > 0)
                    RaiseObjectLayerChangedEvent(new NodeLinkedEventArgs(objectHandles.ToArray(), layerHandle));
                if (layerHandles.Count > 0)
                    RaiseLayerLinkedEvent(new NodeLinkedEventArgs(layerHandles.ToArray(), layerHandle));
            }

        }


        protected void OnBackgroundClick(MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Right) == MouseButtons.Right && (this._selectedNodes.Count > 0 || this.ListMode == OutlinerListMode.Layer))
                showContextMenu(PointToScreen(e.Location), null);
        }

        protected override void OnNodeMouseDoubleClick(TreeNodeMouseClickEventArgs e)
        {
            if (IsClickOnText(e.Node, e))
            {
                if (this.DoubleClickAction == DoubleClickAction.Rename)
                    TreeNodeBeginEdit(e.Node);
                else if (DoubleClickAction == DoubleClickAction.Expand)
                {
                    _internalExpandCollapse = true;
                    e.Node.Toggle();
                    _internalExpandCollapse = false;
                }
            }

            base.OnNodeMouseDoubleClick(e);
        }

        #endregion




        #region ContextMenus

        private void showContextMenu(Point pos, OutlinerNode clickedNode)
        {
            int selCount = _selectedNodes.Count;
            int selObjCount = 0;
            int selLayerCount = 0;
            int selMatCount = 0;
            bool showObjectItems = false;
            bool showLayerItems = false;
            bool showMaterialItems = false;
            bool showIDisplayableItems = false;
            bool selHasChildnodes = false;
            bool selContainsActiveLayer = false;
            bool selCanDelete = false;
            bool selAllHidden = true;
            bool selAllUnhidden = true;
            bool selAllFrozen = true;
            bool selAllUnfrozen = true;
            bool selCanUnlink = false;
            bool selCanUngroup = false;
            bool selCanEditMat = false;

            foreach (OutlinerNode n in _selectedNodes)
            {
                if (n.ChildNodesCount > 0)
                    selHasChildnodes = true;

                if (n.CanBeDeleted)
                    selCanDelete = true;

                if (n is OutlinerObject)
                {
                    selObjCount++;
                    showObjectItems = true;
                    if (!n.IsRootNode)
                        selCanUnlink = true;
                    if (((OutlinerObject)n).IsGroupHead)
                        selCanUngroup = true;
                }
                else if (n is OutlinerLayer)
                {
                    showLayerItems = true;
                    selLayerCount++;
                    if (((OutlinerLayer)n).IsActive)
                        selContainsActiveLayer = true;
                }
                else if (n is OutlinerMaterial)
                {
                    showMaterialItems = true;
                    selMatCount++;
                    if (!((OutlinerMaterial)n).IsUnassigned)
                        selCanEditMat = true;
                }

                if (n is IDisplayable)
                {
                    if (((IDisplayable)n).IsHidden)
                        selAllUnhidden = false;
                    else
                        selAllHidden = false;

                    if (((IDisplayable)n).IsFrozen)
                        selAllUnfrozen = false;
                    else
                        selAllFrozen = false;

                    showIDisplayableItems = true;
                }
            }

            ContextMenus.ShowContextMenu(pos, clickedNode, showObjectItems, showLayerItems,
                                                           showMaterialItems, showIDisplayableItems,
                                                           showObjectItems || showLayerItems || showMaterialItems,
                                                           showObjectItems || showLayerItems);

            if (showObjectItems || showLayerItems)
            {
                foreach (OutlinerLayer layer in Scene.Layers)
                {
                    ToolStripItem item = ContextMenus.AddSelectionToMenu.Items.Add(layer.Name, OutlinerResources.layer);
                    item.Name = "addSelToExistingLayer";
                    item.Tag = layer.Handle;
                }
            }

            ContextMenus.SelectChildnodesItem.Enabled = selHasChildnodes;
            ContextMenus.SetActiveLayerItem.Enabled = selLayerCount == 1 && !selContainsActiveLayer;
            ContextMenus.CreateNewLayerItem.Visible = ListMode == OutlinerListMode.Layer;
            ContextMenus.EditMaterialItem.Enabled = selMatCount == 1 && selCanEditMat;
            ContextMenus.RenameItem.Enabled = selCount == 1 && _selectedNodes.First().CanEditName;
            ContextMenus.DeleteItem.Enabled = selCanDelete;
            ContextMenus.AddToNewLayerItem.Enabled = selObjCount > 0 || selLayerCount > 1 || (selLayerCount == 1 && !((OutlinerLayer)_selectedNodes.First()).IsDefaultLayer);
            ContextMenus.HideItem.Enabled = !selAllHidden;
            ContextMenus.UnhideItem.Enabled = !selAllUnhidden;
            ContextMenus.FreezeItem.Enabled = !selAllFrozen;
            ContextMenus.UnfreezeItem.Enabled = !selAllUnfrozen;
            ContextMenus.UnlinkItem.Enabled = selCanUnlink;
            ContextMenus.UngroupItem.Enabled = selCanUngroup;
            ContextMenus.DisplayShowInVptItem.Enabled = selCanEditMat;

            Console.WriteLine("Add eventhandlers");
            ContextMenus.MainMenu.ItemClicked += new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.EditMaterialMenu.ItemClicked += new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.AddSelectionToMenu.ItemClicked += new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.DisplayPropertiesMenu.ItemClicked += new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.RenderPropertiesMenu.ItemClicked += new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.MainMenu.Closed += new ToolStripDropDownClosedEventHandler(Menu_Closed);

            if (ContextMenuOpened != null)
                ContextMenuOpened(this, new EventArgs());
        }



        private void ContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == ContextMenus.SelectChildnodesItem)
                SelectChildNodes();
            else if (e.ClickedItem == ContextMenus.RenameItem)
            {
                TreeNode tn;
                if (_treeNodes.TryGetValue(_selectedNodes.First(), out tn))
                    TreeNodeBeginEdit(tn);
            }

            if (ContextMenuItemClicked != null)
                ContextMenuItemClicked(sender, new ContextMenuItemClickedEventArgs((ContextMenuStrip)sender, e.ClickedItem));
        }


        private void Menu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            Console.WriteLine("Remove Eventhandlers");
            ContextMenus.MainMenu.ItemClicked -= new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.EditMaterialMenu.ItemClicked -= new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.AddSelectionToMenu.ItemClicked -= new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.DisplayPropertiesMenu.ItemClicked -= new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.RenderPropertiesMenu.ItemClicked -= new ToolStripItemClickedEventHandler(ContextMenu_ItemClicked);
            ContextMenus.MainMenu.Closed -= new ToolStripDropDownClosedEventHandler(Menu_Closed);
        }

        #endregion




        #region LabelEdit

        private const int TVM_GETEDITCONTROL = 0x110F;

        private class LabelEditWindowHook : NativeWindow
        {
            private const int WM_GETDLGCODE = 135;
            private const int DLGC_WANTALLKEYS = 0x0004;

            public LabelEditWindowHook() { }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_GETDLGCODE)
                    m.Result = (IntPtr)DLGC_WANTALLKEYS;
                else
                    base.WndProc(ref m);
            }
        }

        private LabelEditWindowHook m_Hook = new LabelEditWindowHook();


        // Function to begin editing the TreeNode's name.
        // Begins by setting the treenode.Text to Name rather than DisplayName (to avoid editing curly braces for example).
        private void TreeNodeBeginEdit(TreeNode tn)
        {
            if (tn.Tag is OutlinerNode && ((OutlinerNode)tn.Tag).CanEditName && !tn.IsEditing)
            {
                // Make sure endupdate and sort are called before putting the node into labeledit mode.
                if (_updateTimer.Enabled)
                    updateTimer_Tick(_updateTimer, new EventArgs());
                if (_sortTimer.Enabled)
                    sortTimer_Tick(_sortTimer, new EventArgs());

                tn.EnsureVisible();
                tn.Text = ((OutlinerNode)tn.Tag).Name;

                this.LabelEdit = true;
                tn.BeginEdit();
            }
        }


        protected override void OnBeforeLabelEdit(NodeLabelEditEventArgs e)
        {
            IntPtr handle = (IntPtr)SendMessage(this.Handle, TVM_GETEDITCONTROL, 0, 0);
            if (handle != IntPtr.Zero)
                m_Hook.AssignHandle(handle);

            base.OnBeforeLabelEdit(e);
        }


        protected override void OnAfterLabelEdit(NodeLabelEditEventArgs e)
        {
            m_Hook.ReleaseHandle();

            OutlinerNode node = (OutlinerNode)e.Node.Tag;
            bool continueEditing = false;
            if (e.Label != null && e.Label != node.Name)
            {
                if (e.Label.Length == 0)
                {
                    continueEditing = true;
                }
                else if (node is OutlinerLayer && !Scene.IsValidLayerName((OutlinerLayer)node, e.Label))
                {
                    MessageBox.Show(OutlinerResources.InvalidLayerNameMessage, OutlinerResources.InvalidLayerNameTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continueEditing = true;
                }
                else if (node is OutlinerMaterial && !Scene.IsValidMaterialName((OutlinerMaterial)node, e.Label))
                {
                    MessageBox.Show(OutlinerResources.InvalidMaterialNameMessage, OutlinerResources.InvalidMaterialNameTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continueEditing = true;
                }
                else
                {
                    node.Name = e.Label;
                    if (_treeViewNodeSorter is Outliner.NodeSorters.AlphabeticalSorter)
                        BeginTimedSort();

                    if (NodeRenamed != null)
                        NodeRenamed(this, new NodeRenamedEventArgs(node.Handle, e.Label));
                }
            }

            e.CancelEdit = true;
            e.Node.Text = node.DisplayName;

            if (continueEditing)
                e.Node.BeginEdit();
            else
                this.LabelEdit = false;
        }

        #endregion


        #region ScrollPosition

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        protected static extern int GetScrollPos(IntPtr hWnd, int nBar);
        [DllImport("user32.dll")]
        protected static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private const int H_SCROLL = 0;
        private const int V_SCROLL = 1;

        private Point GetScrollPosition()
        {
            return new Point(GetScrollPos(this.Handle, H_SCROLL), GetScrollPos(this.Handle, V_SCROLL));
        }

        private void SetScrollPosition(Point scrollPosition)
        {
            SetScrollPos(this.Handle, H_SCROLL, scrollPosition.X, true);
            SetScrollPos(this.Handle, V_SCROLL, scrollPosition.Y, true);
        }

        #endregion

        #region IconSet

        private IconSet _iconSet;
        private Dictionary<string, Bitmap> _icons;
        private Size _iconSize = Size.Empty;

        private bool _invertIcons = false;
        public bool InvertIcons
        {
            get { return _invertIcons; }
            set
            {
                _invertIcons = value;
                IconSet = _iconSet;
            }
        }

        public IconSet IconSet
        {
            get { return _iconSet; }
            set
            {
                _iconSet = value;
                _icons = new Dictionary<string, Bitmap>();
                _iconSize = Size.Empty;

                IEnumerable<KeyValuePair<string, Bitmap>> bitmaps = null;

                switch(value)
                {
                    default:
                    case IconSet.Max_16x16:
                        bitmaps = MaxIcons16x16.GetBitmaps();
                        break;
                    case IconSet.Maya_16x16:
                        bitmaps = MayaIcons16x16.GetBitmaps();
                        break;
                    case IconSet.Maya_20x20:
                        bitmaps = MayaIcons20x20.GetBitmaps();
                        break;
                    case IconSet.SceneExplorer_16x16:
                        bitmaps = SceneExplorerIcons16x16.GetBitmaps();
                        break;
                    case IconSet.SceneExplorer_20x20:
                        bitmaps = SceneExplorerIcons20x20.GetBitmaps();
                        break;
                    case IconSet.SceneExplorer_32x32:
                        bitmaps = SceneExplorerIcons32x32.GetBitmaps();
                        break;
                };

                if (bitmaps != null)
                {
                    foreach (KeyValuePair<string, Bitmap> entry in bitmaps)
                    {
                        if (_iconSize == Size.Empty)
                            _iconSize = entry.Value.Size;

                        Bitmap b = entry.Value;
                        if (InvertIcons)
                        {
                            BitmapProcessing.Desaturate(b);
                            BitmapProcessing.Invert(b);
                            BitmapProcessing.Brightness(b, 101);
                        }

                        Bitmap b_hidden = new Bitmap(b);
                        BitmapProcessing.Opacity(b_hidden, 100);

                        Bitmap b_filtered = new Bitmap(b);
                        BitmapProcessing.Opacity(b_filtered, 50);

                        _icons.Add(entry.Key, b);
                        _icons.Add(entry.Key + "_hidden", b_hidden);
                        _icons.Add(entry.Key + "_filtered", b_filtered);
                    }
                }

                AutoIndent();
                this.ItemHeight = (_iconSize.Height > 0) ? _iconSize.Height + 2 : 16;

                // Create dummy imagelist to 'correct' labeledit textbox position.
                ImageList = new ImageList();
            }
        }


        #endregion



        #region HighlightParents

        internal void HighlightParents(OutlinerNode n)
        {
            if ((ListMode != OutlinerListMode.Hierarchy && ListMode != OutlinerListMode.Layer) || n == null)
                return;

            TreeNode tn;
            if (_treeNodes.TryGetValue(n, out tn))
            {
                tn = tn.Parent;

                while (tn != null && tn.Tag is OutlinerNode && !IsNodeSelected((OutlinerNode)tn.Tag))
                {
                    Style.SetNodeColor(tn, NodeColor.ParentOfSelected);
                    tn = tn.Parent;
                }
            }
        }

        internal void HighlightAllParents()
        {
            foreach (OutlinerNode n in _selectedNodes)
                HighlightParents(n);
        }


        internal void RemoveParentHighlights(OutlinerNode n)
        {
            if ((ListMode != OutlinerListMode.Hierarchy && ListMode != OutlinerListMode.Layer) || n == null)
                return;

            TreeNode tn;
            if (_treeNodes.TryGetValue(n, out tn))
            {
                tn = tn.Parent;
                while (tn != null && !IsNodeSelected(tn) && !IsParentOfSelectedNode(tn, true))
                {
                    Style.SetNodeColor(tn, NodeColor.Default);
                    tn = tn.Parent;
                }
            }
        }

        #endregion


        #region BeginTimedUpdate, BeginTimedSort, new TreeViewNodeSorter

        internal void BeginTimedUpdate()
        {
            if (!_updateTimer.Enabled)
            {
                this.BeginUpdate();

                _updateTimer.Start();
            }
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            _updateTimer.Stop();
            if (!_sortTimer.Enabled)
                this.EndUpdate();
            else
                _updateWaitingForSort = true;
        }


        internal void BeginTimedSort()
        {
            if (!_sortTimer.Enabled)
                _sortTimer.Start();
        }

        private void sortTimer_Tick(object sender, EventArgs e)
        {
            _sortTimer.Stop();
            _internalExpandCollapse = true;
            Point pos = GetScrollPosition();

            base.TreeViewNodeSorter = _treeViewNodeSorter;
            base.TreeViewNodeSorter = null;
            base.Sorted = false;

            SetScrollPosition(pos);
            _internalExpandCollapse = false;

            if (_updateWaitingForSort)
            {
                updateTimer_Tick(_updateTimer, new EventArgs());
                _updateWaitingForSort = false;
            }

            if (_ensureSelectionVisibleWaitingForSort)
            {
                _ensureSelectionVisibleTimer_Tick(_ensureSelectionVisibleTimer, new EventArgs());
                _ensureSelectionVisibleWaitingForSort = false;
            }
        }

        private IComparer _treeViewNodeSorter;
        public new IComparer TreeViewNodeSorter
        {
            get { return null; }
            set
            {
                _treeViewNodeSorter = value;
                sortTimer_Tick(this, new EventArgs());
            }
        }

        public new bool Sorted
        {
            get { return false; }
            set { }
        }

        public new void Sort()
        {
            sortTimer_Tick(this, new EventArgs());
        }


        internal void BeginTimedEnsureSelectionVisible(EnsureSelectionVisibleAction action)
        {
            _ensureSelectionVisibleAction |= action;
            if (!_ensureSelectionVisibleTimer.Enabled)
                _ensureSelectionVisibleTimer.Start();
        }

        void _ensureSelectionVisibleTimer_Tick(object sender, EventArgs e)
        {
            _ensureSelectionVisibleTimer.Stop();
            if (!_sortTimer.Enabled)
            {
                EnsureSelectionVisible(_ensureSelectionVisibleAction);
                _ensureSelectionVisibleAction = EnsureSelectionVisibleAction.None;
            }
            else
                _ensureSelectionVisibleWaitingForSort = true;
        }



        #endregion






        #region FillTree, CreateTreeNodeForOutlinerNode, AddLayersToTree, AddObjectsToTreeNodeCollection

        public void ClearTree()
        {
            // Clear nodes.
            this.Nodes.Clear();
            // Clear outlinernode-treenode dictionary
            _treeNodes.Clear();
            // Clear selection.
            _selectedNodes.Clear();
        }

        public void FillTree()
        {
            BeginUpdate();

            // Store selection.
            OutlinerNode[] selection = SelectedOutlinerNodes;

            ClearTree();

            // Set sorter, so nodes are added in sorted order.
            if (_treeViewNodeSorter != null)
                base.TreeViewNodeSorter = _treeViewNodeSorter;

            // Fill the tree.
            if (ListMode == OutlinerListMode.Hierarchy)
                AddObjectsToTreeNodeCollection(Nodes, Scene.RootObjects, true);
            else if (ListMode == OutlinerListMode.Layer)
                AddLayersToTree();
            else if (ListMode == OutlinerListMode.Material)
                AddMaterialsToTree();

            // Avoid unintended sorting by base TreeView.
            base.TreeViewNodeSorter = null;
            base.Sorted = false;


            // Restore expanded states.
            RestoreExpandedStates();

            // Restore selection
            RestoreSelection(selection);

            EndUpdate();
        }



        private TreeNode CreateTreeNodeForOutlinerNode(OutlinerNode n)
        {
            TreeNode tn = new TreeNode(n.DisplayName);

            n.DragDropHandler = DragDropHandler.GetDragDropHandlerForNode(this, n);
            tn.Tag = n;

            Style.SetNodeColor(tn, NodeColor.Default);

            return tn;
        }




        private void AddLayersToTree()
        {
            List<OutlinerNode> layers = Scene.RootLayers;
            List<TreeNode> treeNodes = new List<TreeNode>();

            foreach (OutlinerNode layer in layers)
            {
                if (Filter.ShowNode(layer))
                {
                    TreeNode tn = CreateTreeNodeForOutlinerNode(layer);

                    // Add node to outlinernode/treenode dictionary.
                    _treeNodes.Add(layer, tn);

                    // Add all objects belonging to the layer to the layers nodecollection.
                    AddObjectsToTreeNodeCollection(tn.Nodes, layer.ChildNodes, false);

                    treeNodes.Add(tn);
                }
            }

            Nodes.AddRange(treeNodes.ToArray());
        }



        private void AddMaterialsToTree()
        {
            List<OutlinerNode> materials = Scene.RootMaterials;

            List<TreeNode> treeNodes = new List<TreeNode>(materials.Count);

            foreach (OutlinerNode m in materials)
            {
                if (Filter.ShowNode(m))
                {
                    TreeNode tn = CreateTreeNodeForOutlinerNode(m);

                    _treeNodes.Add(m, tn);

                    AddObjectsToTreeNodeCollection(tn.Nodes, m.ChildNodes, false);

                    treeNodes.Add(tn);
                }
            }

            Nodes.AddRange(treeNodes.ToArray());
        }






        public void AddObjectsToTreeNodeCollection(TreeNodeCollection col, List<OutlinerNode> nodes, bool addChildNodes)
        {
            if (nodes == null || nodes.Count == 0)
                return;

            List<TreeNode> newNodes = new List<TreeNode>();

            foreach (OutlinerNode node in nodes)
            {
                if (!_treeNodes.ContainsKey(node) && Filter.ShowNode(node) && (!(ListMode == OutlinerListMode.Layer) || !HideGroupMembersLayerMode || !(node is OutlinerObject) || !((OutlinerObject)node).IsGroupMember))
                {
                    TreeNode tn = CreateTreeNodeForOutlinerNode(node);

                    // Add node to outlinernode/treenode dictionary.
                    _treeNodes.Add(node, tn);

                    newNodes.Add(tn);

                    // Recursively go through childnodes.
                    if ((addChildNodes && node.ChildNodesCount > 0) || !(node is OutlinerObject))
                        AddObjectsToTreeNodeCollection(tn.Nodes, node.ChildNodes, addChildNodes);
                }
            }

            col.AddRange(newNodes.ToArray());
        }


        #endregion


        #region AddObjectToTree, AddLayerToTree, AddMaterialToTree

        internal void AddObjectToTree(OutlinerObject obj)
        {
            if (obj == null)
                return;

            if (!Filter.ShowNode(obj))
                return;

            TreeNodeCollection parentCollection = null;

            if (ListMode == OutlinerListMode.Hierarchy)
            {
                if (obj.IsRootNode)
                    parentCollection = this.Nodes;
                else
                {
                    obj = (OutlinerObject)GetHighestParentToAdd(obj);
                    TreeNode tn;
                    if (obj.IsRootNode)
                        parentCollection = this.Nodes;
                    else
                    {
                        OutlinerNode parentNode = obj.Parent;
                        if (parentNode != null && _treeNodes.TryGetValue(parentNode, out tn))
                            parentCollection = tn.Nodes;
                    }
                }
            }
            else if (ListMode == OutlinerListMode.Layer)
            {
                OutlinerLayer layer = obj.Layer;
                TreeNode layerTreeNode;
                if (layer != null && _treeNodes.TryGetValue(layer, out layerTreeNode))
                    parentCollection = layerTreeNode.Nodes;
            }
            else if (ListMode == OutlinerListMode.Material)
            {
                OutlinerMaterial m = obj.Material;
                if (m != null)
                {
                    TreeNode matNode;
                    if (_treeNodes.TryGetValue(m, out matNode))
                        parentCollection = matNode.Nodes;
                    else
                        AddMaterialToTree(m);
                }
            }

            if (parentCollection != null && obj != null)
            {
                BeginTimedUpdate();
                BeginTimedSort();
                AddObjectsToTreeNodeCollection(parentCollection, new List<OutlinerNode>(1) { obj }, (ListMode == OutlinerListMode.Hierarchy));
            }
        }


        internal void AddLayerToTree(OutlinerLayer layer)
        {
            if (layer == null || ListMode != OutlinerListMode.Layer)
                return;

            BeginTimedUpdate();
            BeginTimedSort();

            TreeNodeCollection parentCollection = null;
            OutlinerNode layerParent = layer.Parent;
            if (layerParent != null)
            {
                TreeNode parentNode;
                if (_treeNodes.TryGetValue(layerParent, out parentNode))
                    parentCollection = parentNode.Nodes;
            }
            else
                parentCollection = this.Nodes;


            if (parentCollection != null)
            {
                AddObjectsToTreeNodeCollection(parentCollection, new List<OutlinerNode>(1) { layer }, false);
            }
        }

        internal void AddMaterialToTree(OutlinerMaterial material)
        {
            if (material == null || ListMode != OutlinerListMode.Material)
                return;

            if (Filter.ShowNode(material))
            {
                BeginTimedUpdate();
                BeginTimedSort();

                TreeNode tn = CreateTreeNodeForOutlinerNode(material);

                // Add all objects belonging to the layer to the layers nodecollection.
                this.AddObjectsToTreeNodeCollection(tn.Nodes, material.ChildNodes, false);

                _treeNodes.Add(material, tn);
                this.Nodes.Add(tn);
            }
        }
        #endregion


        #region RemoveNodeFromTree

        internal void RemoveNodeFromTree(OutlinerNode n, bool removeReferences)
        {
            if (n == null)
                return;

            //     n.MarkedForDelete = true;

            if (ListMode == OutlinerListMode.Hierarchy)
                n = GetHighestParentToRemove(n);

            TreeNode tn;
            if (_treeNodes.TryGetValue(n, out tn))
                RecursiveRemoveTreeNode(n, tn, removeReferences);
        }


        private void RecursiveRemoveTreeNode(OutlinerNode n, TreeNode tn, bool removeReferences)
        {
            RecursiveRemoveTreeNodeIntern(n, tn, removeReferences);
            tn.Remove();
        }
        private void RecursiveRemoveTreeNodeIntern(OutlinerNode n, TreeNode tn, bool removeReferences)
        {
            //     n.MarkedForDelete = true;

            if (removeReferences)
            {
                this._expandedNodeHandles.Remove(n.Handle);
                _selectedNodes.Remove(n);
            }
            RemoveParentHighlights(n);
            _treeNodes.Remove(n);

            foreach (TreeNode cn in tn.Nodes)
                RecursiveRemoveTreeNodeIntern((OutlinerNode)cn.Tag, cn, removeReferences);
        }

        #endregion





        #region HideNode, FreezeNode, LinkObject, SetObjectMaterial

        internal void HideNode(OutlinerNode n, bool hidden)
        {
            if (n is IDisplayable && ((IDisplayable)n).IsHidden != hidden)
            {
                ((IDisplayable)n).IsHidden = hidden;

                ApplyFilter(n, ListMode == OutlinerListMode.Hierarchy,
                               ListMode == OutlinerListMode.Layer,
                               ListMode == OutlinerListMode.Material);

                if (_treeViewNodeSorter is Outliner.NodeSorters.VisibilitySorter)
                    BeginTimedSort();
            }
        }



        internal void HideNodeRecursive(OutlinerNode n, bool hidden)
        {
            HideNode(n, hidden);
            foreach (OutlinerNode cn in n.ChildNodes)
            {
                HideNodeRecursive(cn, hidden);
            }
        }

        internal void FreezeNode(OutlinerNode n, bool frozen)
        {
            if (n is IDisplayable && ((IDisplayable)n).IsFrozen != frozen)
            {
                ((IDisplayable)n).IsFrozen = frozen;

                ApplyFilter(n, ListMode == OutlinerListMode.Hierarchy,
                               ListMode == OutlinerListMode.Layer,
                               ListMode == OutlinerListMode.Material);
            }
        }

        internal void FreezeNodeRecursive(OutlinerNode n, bool frozen)
        {
            FreezeNode(n, frozen);

            foreach (OutlinerNode cn in n.ChildNodes)
                FreezeNodeRecursive(cn, frozen);
        }


        internal void ApplyFilter(OutlinerNode n, bool recurseObjects, bool recurseLayers, bool recurseMaterials)
        {
            bool showNode = this.Filter.ShowNode(n);
            TreeNode tn;
            _treeNodes.TryGetValue(n, out tn);

            if (tn != null)
            {
                if (showNode)
                {
                    Style.SetNodeColorAuto(tn);
                    InvalidateTreeNode(tn);
                }
                else
                    RemoveNodeFromTree(n, true);
            }
            else
            {
                if (showNode)
                {
                    if (n is OutlinerObject && (ListMode != OutlinerListMode.Layer || !HideGroupMembersLayerMode || !((OutlinerObject)n).IsGroupMember))
                        AddObjectToTree((OutlinerObject)n);
                    else if (n is OutlinerLayer && ListMode == OutlinerListMode.Layer)
                        AddLayerToTree((OutlinerLayer)n);
                }
            }

            if ((recurseObjects && n is OutlinerObject) || (recurseLayers && n is OutlinerLayer) || (recurseMaterials && n is OutlinerMaterial))
            {
                foreach (OutlinerNode c in n.ChildNodes)
                    ApplyFilter(c, recurseObjects, recurseLayers, recurseMaterials);
            }
        }



        internal void SetBoxModeNode(OutlinerNode n, bool boxMode)
        {
            if (n is IDisplayable)
            {
                ((IDisplayable)n).BoxMode = boxMode;
                TreeNode tn;
                if (_treeNodes.TryGetValue(n, out tn))
                    InvalidateTreeNode(tn);
            }
        }

        internal void SetBoxModeNodeRecursive(OutlinerNode n, bool boxMode)
        {
            SetBoxModeNode(n, boxMode);

            foreach (OutlinerNode cn in n.ChildNodes)
                SetBoxModeNodeRecursive(cn, boxMode);
        }


        internal void LinkObject(OutlinerObject obj, int newParentHandle, bool group, bool isGroupMember)
        {
            RemoveNodeFromTree(obj, false);

            OutlinerNode oldParent = obj.Parent;

            Scene.SetObjectParentHandle(obj, newParentHandle);

            if (oldParent != null)
                ApplyFilter(oldParent, true, false, false);

            if (group)
                obj.SetIsGroupMemberRec(isGroupMember);

            AddObjectToTree(obj);

            RestoreExpandedStates();

            if (IsNodeSelected(obj))
            {
                SelectNode(obj, true);
                _selectionChanged = false;
                if (ListMode == OutlinerListMode.Hierarchy)
                    BeginTimedEnsureSelectionVisible(EnsureSelectionVisibleAction.HierarchyChanged);
            }
        }


        internal void SetObjectLayer(OutlinerObject obj, int newLayerHandle)
        {
            if (ListMode == OutlinerListMode.Layer)
            {
                BeginTimedUpdate();
                BeginTimedSort();

                RemoveNodeFromTree(obj, false);
                Scene.SetObjectLayerHandle(obj, newLayerHandle);
                AddObjectToTree(obj);

                if (IsNodeSelected(obj))
                {
                    SelectNode(obj, true);
                    _selectionChanged = false;
                    BeginTimedEnsureSelectionVisible(EnsureSelectionVisibleAction.LayerChanged);
                }
            }
            else
            {
                Scene.SetObjectLayerHandle(obj, newLayerHandle);
                if (_treeViewNodeSorter is Outliner.NodeSorters.LayerSorter) BeginTimedSort();
            }
        }


        internal void SetObjectMaterial(OutlinerObject obj, int newMatHandle)
        {
            if (ListMode == OutlinerListMode.Material)
            {
                OutlinerMaterial oldMat = obj.Material;

                RemoveNodeFromTree(obj, false);
                Scene.SetObjectMaterialHandle(obj, newMatHandle);
                AddObjectToTree(obj);

                if (oldMat != null && oldMat.ChildNodes.Count == 0)
                {
                    RemoveNodeFromTree(oldMat, true);
                    if (!oldMat.IsUnassigned)
                        Scene.RemoveMaterial(oldMat);
                }

                // Reselect the node if necessary.
                if (IsNodeSelected(obj))
                {
                    SelectNode(obj, true);
                    _selectionChanged = false;
                    BeginTimedEnsureSelectionVisible(EnsureSelectionVisibleAction.MaterialChanged);
                }
            }
            else
            {
                Scene.SetObjectMaterialHandle(obj, newMatHandle);
                if (_treeViewNodeSorter is Outliner.NodeSorters.MaterialSorter) BeginTimedSort();
            }
        }


        internal void SetLayerParent(OutlinerLayer layer, int newParentHandle)
        {
            RemoveNodeFromTree(layer, false);

            Scene.SetLayerParentHandle(layer, newParentHandle);

            AddLayerToTree(layer);

            RestoreExpandedStates();

            if (IsNodeSelected(layer))
            {
                SelectNode(layer, true);
                _selectionChanged = false;
            }
        }


        #endregion

        #region Events

        public event SelectionChangedEventHandler SelectionChanged;
        public event NodeLinkedEventHandler ObjectLinked;
        public event NodeLinkedEventHandler ObjectLayerChanged;
        public event NodeGroupedEventHandler ObjectGrouped;
        public event NodeGroupedEventHandler ObjectAddedToContainer;
        public event NodePropertyChangedEventHandler ObjectMaterialChanged;
        public event NodeRenamedEventHandler NodeRenamed;
        public event NodePropertyChangedEventHandler NodeHidden;
        public event NodePropertyChangedEventHandler NodeFrozen;
        public event NodePropertyChangedEventHandler NodeBoxModeChanged;
        public event NodeLinkedEventHandler SpaceWarpBound;
        public event NodeLinkedEventHandler LayerLinked;
        public event NodePropertyChangedEventHandler LayerActiveChanged;

        public event EventHandler ContextMenuOpened;
        public event ContextMenuItemClickedEventHandler ContextMenuItemClicked;

#if (DEBUG)
        public event DebugEventHandler DebugEvent;
#endif

        internal void RaiseObjectLinkedEvent(NodeLinkedEventArgs e)
        {
            if (ObjectLinked != null)
                ObjectLinked(this, e);
        }

        internal void RaiseObjectGroupedEvent(NodeGroupedEventArgs e)
        {
            if (ObjectGrouped != null)
                ObjectGrouped(this, e);
        }

        internal void RaiseObjectAddedToContainerEvent(NodeGroupedEventArgs e)
        {
            if (ObjectAddedToContainer != null)
                ObjectAddedToContainer(this, e);
        }

        internal void RaiseObjectLayerChangedEvent(NodeLinkedEventArgs e)
        {
            if (ObjectLayerChanged != null)
                ObjectLayerChanged(this, e);
        }

        internal void RaiseObjectMaterialChangedEvent(NodePropertyChangedEventArgs e)
        {
            if (ObjectMaterialChanged != null)
                ObjectMaterialChanged(this, e);
        }

        internal void RaiseLayerLinkedEvent(NodeLinkedEventArgs e)
        {
            if (LayerLinked != null)
                LayerLinked(this, e);
        }

        internal void RaiseSpaceWarpBoundEvent(NodeLinkedEventArgs e)
        {
            if (SpaceWarpBound != null)
                SpaceWarpBound(this, e);
        }

        #endregion




        #region EnsureSelectionVisible


        public ExpandPolicy ExpandSelectionPolicyHierarchy = ExpandPolicy.WhenNecessary;
        public ExpandPolicy ExpandSelectionPolicyLayer = ExpandPolicy.WhenNecessary;
        public ExpandPolicy ExpandSelectionPolicyMaterial = ExpandPolicy.WhenNecessary;



        internal void EnsureSelectionVisible(EnsureSelectionVisibleAction action)
        {
            if (_selectedNodes.Count == 0 || action == EnsureSelectionVisibleAction.None)
                return;

            bool ensureSelVisible = false;

            if ((action & EnsureSelectionVisibleAction.SelectionChanged) == EnsureSelectionVisibleAction.SelectionChanged)
                ensureSelVisible = true;
            else if ((action & EnsureSelectionVisibleAction.HierarchyChanged) == EnsureSelectionVisibleAction.HierarchyChanged)
                ensureSelVisible = true;
            else if ((action & EnsureSelectionVisibleAction.LayerChanged) == EnsureSelectionVisibleAction.LayerChanged)
                ensureSelVisible = true;
            else if ((action & EnsureSelectionVisibleAction.MaterialChanged) == EnsureSelectionVisibleAction.MaterialChanged)
                ensureSelVisible = true;

            if (ensureSelVisible)
            {
                List<TreeNode> selectedTreeNodes = new List<TreeNode>(_selectedNodes.Count);
                ExpandPolicy expPolicy = ExpandPolicy.Never;
                if (ListMode == OutlinerListMode.Hierarchy)
                {
                    if (AutoExpandHierarchy)
                        expPolicy = ExpandPolicy.Always;
                    else
                        expPolicy = ExpandSelectionPolicyHierarchy;
                }
                else if (ListMode == OutlinerListMode.Layer)
                {
                    if (AutoExpandLayer)
                        expPolicy = ExpandPolicy.Always;
                    else
                        expPolicy = ExpandSelectionPolicyLayer;
                }
                else if (ListMode == OutlinerListMode.Material)
                {
                    if (AutoExpandMaterial)
                        expPolicy = ExpandPolicy.Always;
                    else
                        expPolicy = ExpandSelectionPolicyMaterial;
                }


                _internalExpandCollapse = true;
                bool shouldExpandSelection = true;
                foreach (OutlinerNode n in _selectedNodes)
                {
                    TreeNode tn;
                    if (_treeNodes.TryGetValue(n, out tn))
                    {
                        selectedTreeNodes.Add(tn);

                        if (expPolicy == ExpandPolicy.WhenNecessary && !hasCollapsedParents(tn))
                            shouldExpandSelection = false;
                        else if (expPolicy == ExpandPolicy.Always && hasCollapsedParents(tn))
                        {
                            TreeNode p = tn.Parent;
                            while (p != null)
                            {
                                p.Expand();
                                p = p.Parent;
                            }
                        }
                    }
                }


                if (expPolicy == ExpandPolicy.WhenNecessary && shouldExpandSelection && selectedTreeNodes.Count > 0)
                {
                    TreeNode tn = selectedTreeNodes[0].Parent;
                    while (tn != null)
                    {
                        tn.Expand();
                        tn = tn.Parent;
                    }
                }

                _internalExpandCollapse = false;


                List<Rectangle> treeNodeBounds = new List<Rectangle>(_selectedNodes.Count);
                TreeNode highestNode = null;
                int highestNodeY = int.MaxValue;
                foreach (TreeNode tn in selectedTreeNodes)
                {
                    Rectangle bounds = tn.Bounds;
                    treeNodeBounds.Add(bounds);
                    if (bounds.Y < highestNodeY && !hasCollapsedParents(tn))
                    {
                        highestNode = tn;
                        highestNodeY = bounds.Y;
                    }
                }

                if (highestNode != null)
                {
                    highestNode.EnsureVisible();
                    TreeNode lowestNode = null;
                    int lowestNodeY = int.MinValue;
                    for (int i = 0; i < selectedTreeNodes.Count; i++)
                    {
                        TreeNode tn = selectedTreeNodes[i];
                        if (tn != highestNode)
                        {
                            Rectangle bounds = treeNodeBounds[i];
                            if (((bounds.Y + bounds.Height) - highestNodeY) < ClientRectangle.Height && bounds.Y > lowestNodeY && !hasCollapsedParents(tn))
                            {
                                lowestNode = tn;
                                lowestNodeY = bounds.Y;
                            }
                        }
                    }

                    if (lowestNode != null)
                    {
                        lowestNode.EnsureVisible();
                    }
                }
            }
        }


        #endregion


        //Functions called from 3dsmax:
        // Objects, layers and materials will only be referred to by their handle (GetHandleByAnim).

        // Selection functions
        #region GetSelectedNodeHandles, SelectedObjectHandles, SelectedLayerHandles, SelectedMaterialHandles, GetSelectedParentObjectHandles

        public int[] GetSelectedNodeHandles(bool includeObjects, bool includeLayers, bool includeMaterials)
        {
            List<int> nodeHandles = new List<int>();
            foreach (OutlinerNode n in _selectedNodes)
            {
                if (n is OutlinerObject && includeObjects)
                    nodeHandles.Add(n.Handle);
                else if (n is OutlinerLayer && includeLayers)
                    nodeHandles.Add(n.Handle);
                else if (n is OutlinerMaterial && includeMaterials && !((OutlinerMaterial)n).IsUnassigned)
                    nodeHandles.Add(n.Handle);
            }
            return nodeHandles.ToArray();
        }

        public int[] SelectedNodeHandles
        {
            get { return GetSelectedNodeHandles(true, true, true); }
        }

        public int[] SelectedObjectHandles
        {
            get { return GetSelectedNodeHandles(true, false, false); }
        }

        public int[] SelectedLayerHandles
        {
            get { return GetSelectedNodeHandles(false, true, false); }
        }

        public int[] SelectedMaterialHandles
        {
            get { return GetSelectedNodeHandles(false, false, true); }
        }

        public int[] SelectedLayerHandlesIndirect
        {
            get
            {
                List<int> layerHandles = new List<int>();
                foreach (OutlinerNode n in _selectedNodes)
                {
                    if (n is OutlinerLayer)
                    {
                        layerHandles.Add(n.Handle);
                        addChildLayerHandlesRecursive(n, ref layerHandles);
                    }
                }
                return layerHandles.ToArray();
            }
        }

        private void addChildLayerHandlesRecursive(OutlinerNode layer, ref List<int> handles)
        {
            foreach (OutlinerNode n in layer.ChildNodes)
            {
                if (n is OutlinerLayer)
                {
                    handles.Add(n.Handle);
                    addChildLayerHandlesRecursive(n, ref handles);
                }
            }
        }


        public int[] GetSelectedParentObjectHandles()
        {
            List<int> handles = new List<int>();

            foreach (OutlinerNode n in _selectedNodes)
            {
                if (n is OutlinerObject && !IsChildOfSelectedNode(n))
                {
                    handles.Add(n.Handle);
                }
            }

            return handles.ToArray();
        }

        #endregion



        #region SelectObject, SelectObjectRange, SelectAll

        public void SelectObject(int handle, bool select)
        {
            OutlinerObject obj = this.Scene.GetObjectByHandle(handle);
            if (obj != null)
                SelectNode(obj, select);

            _selectionChanged = false;
        }

        public void SelectObjectRange(int[] handles, bool select)
        {
            foreach (int handle in handles)
            {
                SelectObject(handle, select);
            }
            _selectionChanged = false;
            BeginTimedEnsureSelectionVisible(EnsureSelectionVisibleAction.SelectionChanged);
        }

        public void SelectLayer(int handle, bool select)
        {
            OutlinerLayer layer = Scene.GetLayerByHandle(handle);
            if (layer != null)
                SelectNode(layer, select);
            _selectionChanged = false;
        }

        public void SelectLayerRange(int[] handles, bool select)
        {
            foreach (int handle in handles)
            {
                SelectLayer(handle, select);
            }
            _selectionChanged = false;
        }

        public void SelectAll(bool select)
        {
            if (select)
                SelectAllNodes();
            else
                UnselectAllNodes();

            _selectionChanged = false;
        }

        #endregion

        #region SelectFilteredNodes

        public void SelectFilteredNodes()
        {
            this.BeginTimedUpdate();

            // Deselect all selected nodes.
            UnselectAllNodes();

            if (Filter.NameFilter != string.Empty)
            {
                RegexOptions options = Filter.NameFilterCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                foreach (KeyValuePair<OutlinerNode, TreeNode> kvp in _treeNodes)
                {
                    if (Regex.IsMatch(kvp.Key.Name, Filter.NameFilter, options))
                    {
                        SelectNode(kvp.Key, kvp.Value, true);
                    }
                }
            }
            EnsureSelectionVisible(EnsureSelectionVisibleAction.SelectionChanged);
            OnSelectionChanged();
        }
            
        #endregion


        #region ExpandedNodeHandles

        public int[] ExpandedNodeHandles
        {
            get
            {
                return this._expandedNodeHandles.ToArray();
            }
            set
            {
                if (!(_autoExpandHierarchy && ListMode == OutlinerListMode.Hierarchy) && !(_autoExpandLayer && ListMode == OutlinerListMode.Layer))
                {
                    BeginTimedUpdate();
                    this._expandedNodeHandles = new HashSet<int>(value);
                    this.RestoreExpandedStates();
                }
            }
        }

        #endregion



        // Generic functions

        #region EditNodeName

        public void EditNodeName(int handle)
        {
            OutlinerNode n = Scene.GetNodeByHandle(handle);
            if (n == null)
                return;

            if (_treeNodes.TryGetValue(n, out TreeNode tn))
                TreeNodeBeginEdit(tn);
        }

        #endregion

        //Function wrapper to avoid having to make 3 function calls from 3dsmax.
        public void SetNodeDisplayProperties(int handle, bool isHidden, bool isFrozen, bool boxMode)
        {
            OutlinerNode n = this.Scene.GetNodeByHandle(handle);
            if (n is IDisplayable)
            {
                ((IDisplayable)n).BoxMode = boxMode;

                if (n is OutlinerObject)
                {
                    SetObjectHidden(handle, isHidden);
                    SetObjectFrozen(handle, isFrozen);
                }
                else if (n is OutlinerLayer)
                {
                    SetLayerHidden(handle, isHidden);
                    SetLayerFrozen(handle, isFrozen);
                }
            }
        }

        //Object functions.
        #region AddObjectToTree, DeleteObject

        public void AddObjectToTree(int handle)
        {
            OutlinerObject obj = Scene.GetObjectByHandle(handle);
            AddObjectToTree(obj);
        }

        public void DeleteObject(int handle)
        {
            OutlinerObject obj = Scene.GetObjectByHandle(handle);
            if (obj != null)
            {
                BeginTimedUpdate();
                OutlinerMaterial m = obj.Material;
                if (m != null && m.ChildNodesCount == 1)
                {
                    if (ListMode == OutlinerListMode.Material)
                        RemoveNodeFromTree(m, true);
                    else
                        RemoveNodeFromTree(obj, true);

                    Scene.RemoveMaterial(m);
                }
                else
                    RemoveNodeFromTree(obj, true);

                Scene.RemoveObject(obj);
                obj = null;
            }
        }

        #endregion


        #region SetObjectName, SetLayerName

        public void SetObjectName(int handle, string name)
        {
            OutlinerObject obj = Scene.GetObjectByHandle(handle);
            if (obj == null)
                return;

            obj.Name = name;

            TreeNode tn;
            if (_treeNodes.TryGetValue(obj, out tn))
            {
                BeginTimedUpdate();
                BeginTimedSort();
                tn.Text = obj.DisplayName;
            }
        }

        #endregion


        #region SetObjectHidden, SetObjectFrozen

        public void SetObjectHidden(int handle, bool hidden)
        {
            OutlinerNode n = Scene.GetObjectByHandle(handle);

            if (n == null)
                return;

            BeginTimedUpdate();
            if (_treeViewNodeSorter is Outliner.NodeSorters.VisibilitySorter)
                BeginTimedSort();

            HideNode(n, hidden);
        }


        public void SetObjectFrozen(int handle, bool frozen)
        {
            OutlinerNode n = Scene.GetObjectByHandle(handle);

            if (n == null)
                return;

            BeginTimedUpdate();

            FreezeNode(n, frozen);
        }

        #endregion


        #region SetObjectParent

        public void SetObjectParent(int handle, int newParentHandle)
        {
            OutlinerObject obj = Scene.GetObjectByHandle(handle);
            if (obj == null)
                return;

            if (ListMode == OutlinerListMode.Hierarchy)
            {
                BeginTimedUpdate();
                BeginTimedSort();

                LinkObject(obj, newParentHandle, false, obj.IsGroupMember);
            }
            else
                Scene.SetObjectParentHandle(obj, newParentHandle);
        }

        public void SetObjectIsGroupMember(int handle, bool isGroupMember)
        {
            OutlinerObject obj = Scene.GetObjectByHandle(handle);
            if (obj == null)
                return;

            obj.IsGroupMember = isGroupMember;

            TreeNode tn;
            if (_treeNodes.TryGetValue(obj, out tn))
            {
                BeginTimedUpdate();
                tn.Text = obj.DisplayName;

                if (ListMode == OutlinerListMode.Layer && HideGroupMembersLayerMode)
                {
                    if (isGroupMember)
                        RemoveNodeFromTree(obj, true);
                    else
                        AddObjectToTree(obj);
                }
            }
        }

        #endregion


        #region SetObjectClass

        public void SetObjectClass(int handle, string className, string superClassName)
        {
            OutlinerObject obj = Scene.GetObjectByHandle(handle);
            if (obj == null)
                return;

            obj.Class = className;
            obj.SuperClass = superClassName;

            TreeNode tn;
            if (_treeNodes.TryGetValue(obj, out tn))
            {
                Style.SetNodeImageKey(tn);
                BeginTimedUpdate();
            }

            bool showNode = Filter.ShowNode(obj);

            if (showNode && tn == null)
                AddObjectToTree(obj);
            else if (!showNode && tn != null)
                RemoveNodeFromTree(obj, true);
        }

        #endregion


        #region SetObjectLayer

        public void SetObjectLayer(int handle, int newLayerHandle)
        {
            OutlinerObject n = Scene.GetObjectByHandle(handle);
            if (n == null)
                return;

            if (newLayerHandle != n.LayerHandle)
                SetObjectLayer(n, newLayerHandle);
        }

        #endregion


        #region SetObjectMaterial

        public void SetObjectMaterial(int handle, int materialHandle)
        {
            OutlinerObject obj = Scene.GetObjectByHandle(handle);
            if (obj == null)
                return;

            SetObjectMaterial(obj, materialHandle);
        }

        #endregion



        // Layer functions.
        #region AddLayerToTree, DeleteLayer

        public void AddLayerToTree(int layerHandle)
        {
            OutlinerLayer layer = Scene.GetLayerByHandle(layerHandle);
            if (layer != null)
                AddLayerToTree(layer);
        }

        public void DeleteLayer(int layerHandle)
        {
            OutlinerLayer layer = Scene.GetLayerByHandle(layerHandle);
            if (layer != null)
            {
                BeginTimedUpdate();

                List<OutlinerNode> childLayers = layer.ChildLayers;
                if (childLayers.Count > 0)
                    BeginTimedSort();

                foreach (OutlinerNode clayer in childLayers)
                {
                    if (clayer is OutlinerLayer)
                        SetLayerParent((OutlinerLayer)clayer, OutlinerScene.RootHandle);
                }

                RemoveNodeFromTree(layer, true);
                Scene.RemoveLayer(layer);

                layer = null;
            }
        }

        #endregion


        #region SetLayerName

        public void SetLayerName(int layerHandle, string newName)
        {
            OutlinerLayer layer = Scene.GetLayerByHandle(layerHandle);
            if (layer == null)
                return;

            if (Scene.IsValidLayerName(layer, newName))
            {
                layer.Name = newName;

                if (ListMode == OutlinerListMode.Layer)
                {
                    TreeNode tn;
                    if (_treeNodes.TryGetValue(layer, out tn))
                    {
                        BeginTimedUpdate();
                        BeginTimedSort();
                        tn.Text = layer.DisplayName;
                    }
                }
                else if (_treeViewNodeSorter is Outliner.NodeSorters.LayerSorter)
                    BeginTimedSort();
            }
        }

        #endregion


        #region SetLayerHidden, SetLayerFrozen, SetLayerActive

        public void SetLayerHidden(int layerHandle, bool isHidden)
        {
            OutlinerLayer layer = Scene.GetLayerByHandle(layerHandle);
            if (layer == null || layer.IsHidden == isHidden)
                return;

            layer.IsHidden = isHidden;

            if (ListMode == OutlinerListMode.Layer)
            {
                TreeNode tn;
                if (_treeNodes.TryGetValue(layer, out tn))
                {
                    BeginTimedUpdate();
                    Style.SetNodeImageKey(tn);
                    Style.SetNodeColorAuto(tn);
                    if (_treeViewNodeSorter is Outliner.NodeSorters.VisibilitySorter)
                        BeginTimedSort();
                }
            }
        }

        public void SetLayerFrozen(int layerHandle, bool isFrozen)
        {
            OutlinerLayer layer = Scene.GetLayerByHandle(layerHandle);
            if (layer == null || layer.IsFrozen == isFrozen)
                return;

            layer.IsFrozen = isFrozen;

            if (ListMode == OutlinerListMode.Layer)
            {
                TreeNode tn;
                if (_treeNodes.TryGetValue(layer, out tn))
                {
                    BeginTimedUpdate();
                    Style.SetNodeColorAuto(tn);
                }
            }
        }

        public void SetLayerActive(int layerHandle, bool isActive)
        {
            OutlinerLayer layer = this.Scene.GetLayerByHandle(layerHandle);
            if (layer == null)
                return;

            layer.IsActive = isActive;

            if (ListMode == OutlinerListMode.Layer)
            {
                if (_treeNodes.TryGetValue(layer, out TreeNode tn))
                {
                    BeginTimedUpdate();
                    this.Style.SetNodeImageKey(tn);
                }
            }
        }

        #endregion


        #region SetLayerParent

        public void SetLayerParent(int layerHandle, int parentLayerHandle)
        {
            OutlinerLayer layer = Scene.GetLayerByHandle(layerHandle);
            if (layer == null)
                return;

            if (ListMode == OutlinerListMode.Layer)
            {
                BeginTimedUpdate();
                BeginTimedSort();
                SetLayerParent(layer, parentLayerHandle);
            }
            else
                Scene.SetLayerParentHandle(layer, parentLayerHandle);
        }

        #endregion


        // Material functions.
        #region AddMaterialToTree

        public void AddMaterialToTree(int materialHandle)
        {
            OutlinerMaterial mat = Scene.GetMaterialByHandle(materialHandle);
            if (mat != null)
                AddMaterialToTree(mat);
        }

        #endregion


        #region SetMaterialName

        public void SetMaterialName(int handle, string newName)
        {
            OutlinerMaterial mat = Scene.GetMaterialByHandle(handle);
            if (mat == null)
                return;

            mat.Name = newName;

            if (ListMode == OutlinerListMode.Material)
            {
                if (_treeNodes.TryGetValue(mat, out TreeNode tn))
                {
                    BeginTimedUpdate();
                    BeginTimedSort();
                    tn.Text = mat.DisplayName;
                }
            }
            else if (_treeViewNodeSorter is Outliner.NodeSorters.MaterialSorter)
                BeginTimedSort();
        }


        #endregion
    }
}
