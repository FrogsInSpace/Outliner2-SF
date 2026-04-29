using System.Collections.Generic;
using System.Drawing;

namespace Outliner.Resources
{
    internal static class ContextMenuIcons
    {
        private const string FolderName = "contextmenu_icons";

        internal static IEnumerable<KeyValuePair<string, Bitmap>> GetBitmaps()
        {
            return EmbeddedBitmapResources.LoadSet(FolderName, ".png");
        }

        internal static Bitmap activelayer       { get { return EmbeddedBitmapResources.Load(FolderName, "activelayer"); } }
        internal static Bitmap advrename         { get { return EmbeddedBitmapResources.Load(FolderName, "advrename"); } }
        internal static Bitmap childnodes        { get { return EmbeddedBitmapResources.Load(FolderName, "childnodes"); } }
        internal static Bitmap delete            { get { return EmbeddedBitmapResources.Load(FolderName, "delete"); } }
        internal static Bitmap freeze            { get { return EmbeddedBitmapResources.Load(FolderName, "freeze"); } }
        internal static Bitmap hide              { get { return EmbeddedBitmapResources.Load(FolderName, "hide"); } }
        internal static Bitmap layer             { get { return EmbeddedBitmapResources.Load(FolderName, "layer"); } }
        internal static Bitmap newcontainer      { get { return EmbeddedBitmapResources.Load(FolderName, "newcontainer"); } }
        internal static Bitmap newgroup          { get { return EmbeddedBitmapResources.Load(FolderName, "newgroup"); } }
        internal static Bitmap newlayer          { get { return EmbeddedBitmapResources.Load(FolderName, "newlayer"); } }
        internal static Bitmap properties        { get { return EmbeddedBitmapResources.Load(FolderName, "properties"); } }
        internal static Bitmap rename            { get { return EmbeddedBitmapResources.Load(FolderName, "rename"); } }
        internal static Bitmap ungroup           { get { return EmbeddedBitmapResources.Load(FolderName, "ungroup"); } }
        internal static Bitmap unlink            { get { return EmbeddedBitmapResources.Load(FolderName, "unlink"); } }
    }
}
