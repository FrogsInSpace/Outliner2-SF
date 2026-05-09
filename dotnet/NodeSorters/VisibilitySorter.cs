using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Outliner.Scene;

namespace Outliner.NodeSorters
{
    public class VisibilitySorter : IComparer
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        static extern int StrCmpLogicalW(string x, string y);

        public int Compare(object x, object y)
        {
            if ((x is TreeNode) && (y is TreeNode))
            {
                object xTag = ((TreeNode)x).Tag;
                object yTag = ((TreeNode)y).Tag;

                if ((xTag is IDisplayable) && (yTag is IDisplayable))
                {
                    bool xHidden = ((IDisplayable)xTag).IsHidden;
                    bool yHidden = ((IDisplayable)yTag).IsHidden;

                    if (!xHidden && yHidden)
                        return -1;
                    else if (xHidden && !yHidden)
                        return 1;
                    else if ((xTag is OutlinerNode) && (yTag is OutlinerNode))
                        return StrCmpLogicalW(((OutlinerNode)xTag).Name, ((OutlinerNode)yTag).Name);
                }
                else if ((xTag is OutlinerNode) && (yTag is OutlinerNode))
                    return StrCmpLogicalW(((OutlinerNode)xTag).Name, ((OutlinerNode)yTag).Name);
            }

            return 0;
        }
    }
}
