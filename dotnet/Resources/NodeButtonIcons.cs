using System.Collections.Generic;
using System.Drawing;

namespace Outliner.Resources
{
    internal static class NodeButtonIcons
    {
        private const string FolderName = "node_buttons";

        internal static IEnumerable<KeyValuePair<string, Bitmap>> GetBitmaps()
        {
            return EmbeddedBitmapResources.LoadSet(FolderName, ".png");
        }

        internal static Bitmap add_button { get { return EmbeddedBitmapResources.Load(FolderName, "add_button"); } }
        internal static Bitmap add_button_disabled { get { return EmbeddedBitmapResources.Load(FolderName, "add_button_Disabled"); } }
        internal static Bitmap boxmode_button { get { return EmbeddedBitmapResources.Load(FolderName, "boxmode_button"); } }
        internal static Bitmap boxmode_button_disabled { get { return EmbeddedBitmapResources.Load(FolderName, "boxmode_button_disabled"); } }
        internal static Bitmap freeze_button { get { return EmbeddedBitmapResources.Load(FolderName, "freeze_button"); } }
        internal static Bitmap freeze_button_disabled { get { return EmbeddedBitmapResources.Load(FolderName, "freeze_button_disabled"); } }
        internal static Bitmap hide_button { get { return EmbeddedBitmapResources.Load(FolderName, "hide_button"); } }
        internal static Bitmap hide_button_disabled { get { return EmbeddedBitmapResources.Load(FolderName, "hide_button_disabled"); } }
    }
}
