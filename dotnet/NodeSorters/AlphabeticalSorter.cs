using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Outliner.Scene;

namespace Outliner.NodeSorters
{
    public class AlphabeticalSorter : IComparer
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        static extern int StrCmpLogicalW(string x, string y);

        public int Compare(object x, object y)
        {
            if ((x is TreeNode) && (y is TreeNode))
            {
                object xTag = ((TreeNode)x).Tag;
                object yTag = ((TreeNode)y).Tag;

                if ((xTag is OutlinerNode) && (yTag is OutlinerNode))
                {
                    bool xIsLayer = xTag is OutlinerLayer;
                    bool yIsLayer = yTag is OutlinerLayer;

                    if (xIsLayer && !yIsLayer)
                        return -1;
                    else if (!xIsLayer && yIsLayer)
                        return 1;
                    else
                        return StrCmpLogicalW(((OutlinerNode)xTag).Name, ((OutlinerNode)yTag).Name);
                }
            }

            return 0;
        }
    }
}
