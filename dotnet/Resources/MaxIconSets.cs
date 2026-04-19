using System.Collections.Generic;
using System.Drawing;

namespace Outliner.Resources
{
    internal static class MaxIcons16x16
    {
        private const string FolderName = "max_icons";

        internal static IEnumerable<KeyValuePair<string, Bitmap>> GetBitmaps()
        {
            return EmbeddedBitmapResources.LoadSet(FolderName, "_16.png");
        }

        internal static Bitmap bone { get { return EmbeddedBitmapResources.Load(FolderName, "bone"); } }
        internal static Bitmap camera { get { return EmbeddedBitmapResources.Load(FolderName, "camera"); } }
        internal static Bitmap container { get { return EmbeddedBitmapResources.Load(FolderName, "container"); } }
        internal static Bitmap container_closed { get { return EmbeddedBitmapResources.Load(FolderName, "container_closed"); } }
        internal static Bitmap geometry { get { return EmbeddedBitmapResources.Load(FolderName, "geometry"); } }
        internal static Bitmap group { get { return EmbeddedBitmapResources.Load(FolderName, "group"); } }
        internal static Bitmap helper { get { return EmbeddedBitmapResources.Load(FolderName, "helper"); } }
        internal static Bitmap layer { get { return EmbeddedBitmapResources.Load(FolderName, "layer"); } }
        internal static Bitmap layer_active { get { return EmbeddedBitmapResources.Load(FolderName, "layer_active"); } }
        internal static Bitmap light { get { return EmbeddedBitmapResources.Load(FolderName, "light"); } }
        internal static Bitmap material { get { return EmbeddedBitmapResources.Load(FolderName, "material"); } }
        internal static Bitmap material_unassigned { get { return EmbeddedBitmapResources.Load(FolderName, "material_unassigned"); } }
        internal static Bitmap material_xref { get { return EmbeddedBitmapResources.Load(FolderName, "material_xref"); } }
        internal static Bitmap nurbs { get { return EmbeddedBitmapResources.Load(FolderName, "nurbs"); } }
        internal static Bitmap particle { get { return EmbeddedBitmapResources.Load(FolderName, "particle"); } }
        internal static Bitmap shape { get { return EmbeddedBitmapResources.Load(FolderName, "shape"); } }
        internal static Bitmap spacewarp { get { return EmbeddedBitmapResources.Load(FolderName, "spacewarp"); } }
        internal static Bitmap unknown { get { return EmbeddedBitmapResources.Load(FolderName, "unknown"); } }
        internal static Bitmap xref { get { return EmbeddedBitmapResources.Load(FolderName, "xref"); } }
        internal static Bitmap xref_group { get { return EmbeddedBitmapResources.Load(FolderName, "xref_group"); } }
    }

    internal static class MaxIcons32x32
    {
        private const string FolderName = "max_icons";

        internal static IEnumerable<KeyValuePair<string, Bitmap>> GetBitmaps()
        {
            return EmbeddedBitmapResources.LoadSet(FolderName, "_32.png");
        }

        internal static Bitmap bone { get { return EmbeddedBitmapResources.Load(FolderName, "bone"); } }
        internal static Bitmap camera { get { return EmbeddedBitmapResources.Load(FolderName, "camera"); } }
        internal static Bitmap container { get { return EmbeddedBitmapResources.Load(FolderName, "container"); } }
        internal static Bitmap container_closed { get { return EmbeddedBitmapResources.Load(FolderName, "container_closed"); } }
        internal static Bitmap geometry { get { return EmbeddedBitmapResources.Load(FolderName, "geometry"); } }
        internal static Bitmap group { get { return EmbeddedBitmapResources.Load(FolderName, "group"); } }
        internal static Bitmap helper { get { return EmbeddedBitmapResources.Load(FolderName, "helper"); } }
        internal static Bitmap layer { get { return EmbeddedBitmapResources.Load(FolderName, "layer"); } }
        internal static Bitmap layer_active { get { return EmbeddedBitmapResources.Load(FolderName, "layer_active"); } }
        internal static Bitmap light { get { return EmbeddedBitmapResources.Load(FolderName, "light"); } }
        internal static Bitmap material { get { return EmbeddedBitmapResources.Load(FolderName, "material"); } }
        internal static Bitmap material_unassigned { get { return EmbeddedBitmapResources.Load(FolderName, "material_unassigned"); } }
        internal static Bitmap material_xref { get { return EmbeddedBitmapResources.Load(FolderName, "material_xref"); } }
        internal static Bitmap nurbs { get { return EmbeddedBitmapResources.Load(FolderName, "nurbs"); } }
        internal static Bitmap particle { get { return EmbeddedBitmapResources.Load(FolderName, "particle"); } }
        internal static Bitmap shape { get { return EmbeddedBitmapResources.Load(FolderName, "shape"); } }
        internal static Bitmap shape_default { get { return EmbeddedBitmapResources.Load(FolderName, "shape_default"); } }
        internal static Bitmap spacewarp { get { return EmbeddedBitmapResources.Load(FolderName, "spacewarp"); } }
        internal static Bitmap unknown { get { return EmbeddedBitmapResources.Load(FolderName, "unknown"); } }
        internal static Bitmap xref { get { return EmbeddedBitmapResources.Load(FolderName, "xref"); } }
        internal static Bitmap xref_group { get { return EmbeddedBitmapResources.Load(FolderName, "xref_group"); } }
    }

}
