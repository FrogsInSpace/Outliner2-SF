using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Outliner.Scene
{
    public interface IDisplayable
    {
        bool IsHidden { get; set; }
        bool IsFrozen { get; set; }
        bool BoxMode { get; set; }
    }
}
