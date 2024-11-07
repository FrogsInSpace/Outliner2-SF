using System.IO;
using System;

namespace Outliner
{
    public class MXSFileWatcher : FileSystemWatcher
    {
        public object MXSObject { get; set; }

        public void WatchFileChange(string file)
        {
            if (File.Exists(file))
            {
                FileInfo f = new FileInfo(file);
                Path = f.DirectoryName;
                Filter = f.Name;
                EnableRaisingEvents = true;
                NotifyFilter = NotifyFilters.LastWrite;
            }
        }
    }
}
