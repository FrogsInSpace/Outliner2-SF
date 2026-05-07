using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace Outliner.Resources
{
    public static class EmbeddedBitmapResources
    {
        private const string ResourcePrefix = "Outliner.Resources.";
        private static readonly string[] ValidExtensions = new [] { ".png", ".gif", ".bmp" };

        private static readonly Assembly Assembly = typeof(EmbeddedBitmapResources).Assembly;
        private static readonly Dictionary<string, string> ManifestNames = BuildManifestNameLookup();

        private static Bitmap _buyMeACoffee;


        public static Bitmap BuyMeACoffee
        {
            get
            {
                if( _buyMeACoffee == null )
                    _buyMeACoffee = Load("images", "BuyMeACoffee");

                return _buyMeACoffee;
            }
        }


        internal static Bitmap Load(string folderName, string resourceName, string extension=".png")
        {
            return LoadByKey(folderName + "." + resourceName + extension) as Bitmap;
        }

        internal static IEnumerable<KeyValuePair<string, Bitmap>> LoadSet(string folderName, string postfix = ".png" )
        {
            string prefix = folderName + ".";
            List<string> keys = ManifestNames.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && key.EndsWith(postfix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string key in keys)
            {
                string resourceName = key.Substring(prefix.Length, key.Length - prefix.Length - postfix.Length);
                yield return new KeyValuePair<string, Bitmap>(resourceName, LoadByKey(key) as Bitmap);
            }
        }

        private static Dictionary<string, string> BuildManifestNameLookup()
        {
            Dictionary<string, string> manifestNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string manifestName in Assembly.GetManifestResourceNames())
            {
                if(!manifestName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                    continue;

                if( ValidExtensions.FirstOrDefault( e => e.Equals( Path.GetExtension(manifestName) , StringComparison.OrdinalIgnoreCase)) == null )
                    continue;

                string key = manifestName.Substring(ResourcePrefix.Length);
                manifestNames[key] = manifestName;
            }

            return manifestNames;
        }

        private static Bitmap LoadByKey(string key)
        {
            string manifestName;
            if (!ManifestNames.TryGetValue(key, out manifestName))
                throw new MissingManifestResourceException("Missing embedded bitmap resource: " + ResourcePrefix + key);

            using (Stream stream = Assembly.GetManifestResourceStream(manifestName))
            {
                if (stream == null)
                    throw new MissingManifestResourceException("Unable to open embedded bitmap resource: " + manifestName);


                using (var image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
        }

    }
}
