using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;

using ManagedServices;

namespace Outliner
{
/*
    public static class ActMan
    {
        private static IIActionManager actMan;

        private static Dictionary<string, IActionItem> shortcuts = new Dictionary<string, IActionItem>();



        static ActMan()
        {
            actMan = GlobalInterface.Instance.COREInterface.ActionManager;


            // do nothing if old format is used. as outliner supports this on a different code path
            if (string.IsNullOrEmpty(actMan.KeyboardFile)
                || actMan.KeyboardFile.EndsWith(".kbdx", StringComparison.InvariantCultureIgnoreCase)
                || actMan.KeyboardFile.EndsWith(".kbd", StringComparison.InvariantCultureIgnoreCase))
                return;
        }

        public static string GetShortcut()
        {


            //aMan.GetShortcutString(
            return string.Empty;
        }


        public static void FillShortcuts(KbdxFile file = null)
        {
            var keyConverter = new KeysConverter();

            for (int i = 0; i < actMan.NumActionTables; i++)
            {
                var table = actMan.GetTable(i);

                for (int j = 0; j < table.Count; j++)
                {
                    var item = table.GetActionByIndex(j);

                    //table.g

                    if (!string.IsNullOrEmpty(item.ShortcutString))
                    {
                        // handle multiple shortcuts in the string
                        foreach (var sc in item.ShortcutString.Split(','))
                        {
                            shortcuts.Add(sc, item);

                            try
                            {
                                Keys keys = (Keys)keyConverter.ConvertFromInvariantString(sc);

                            }
                            catch (Exception ex)
                            {


                            }

                            //  var kbdx = new KbdxShortcut( keys, item.MpTable_, table.Id_);

                            //  file.Add( item,

                        }
                    }
                }
            }
        }
    }


*/
    [XmlRoot("ADSK_KBD")]
    public class KbdxFile : ICollection<KbdxShortcut>
    {
        private List<KbdxShortcut> shortcuts;

        [XmlIgnore]
        public string File;

        public KbdxFile()
        {
            this.shortcuts = new List<KbdxShortcut>();
        }

        public bool ContainsShortcut(Keys k)
        {
            return this.shortcuts.Exists(a => (a.TableId == KbdxShortcut.MainTableId
                                              || a.TableId == KbdxShortcut.MacroTableId
                                              ) && a.Key == k);
        }

        public bool ContainsShortcut(string macroName, string macroCategory)
        {
            return this.shortcuts.Exists(a => a.MacroName == macroName && a.MacroCategory == macroCategory);
        }

        public bool ContainsShortcut(string macroName, string macroCategory, Keys k)
        {
            return this.shortcuts.Exists(a => a.TableId == KbdxShortcut.MacroTableId &&
                                              a.MacroName == macroName &&
                                              a.MacroCategory == macroCategory &&
                                              a.Key == k);
        }

        public KbdxShortcut GetShortcut(Keys k)
        {
            return this.shortcuts.FirstOrDefault(a => (a.TableId == KbdxShortcut.MainTableId ||
                                                       a.TableId == KbdxShortcut.MacroTableId) &&
                                                      a.Key == k);
        }


        /// <summary>
        /// Adds a hotkey assignment to the kbd file.
        /// </summary>
        /// <param name="macroName">The name of the macroscript to be called.</param>
        /// <param name="macroCategory">The category of the macroscript to be called.</param>
        /// <param name="k">The keys associated with the hotkey assignment.</param>
        /// <param name="replace">Whether to replace an assignment with the same keys if it already exists.</param>
        /// <returns>True if the hotkey assignment was added, false if the exact assignment already existed or if it should not be replaced.</returns>
        public bool AddShortcut(string macroName, string macroCategory, Keys k, bool replace)
        {
            KbdxShortcut existingAction = this.shortcuts.Find(a => (a.TableId == KbdxShortcut.MainTableId || a.TableId == KbdxShortcut.MacroTableId) && a.Key == k);
            if (existingAction != null)
            {
                if (!replace || (existingAction.MacroName == macroName && existingAction.MacroCategory == macroCategory))
                    return false;
                else
                    this.shortcuts.Remove(existingAction);
            }

            KbdxShortcut action = new KbdxShortcut(k, macroName, macroCategory);
            this.shortcuts.Add(action);

            return true;
        }

        /// <summary>
        /// Adds a hotkey assignment to the kbd file, replacing an action with the same keys if it already exists.
        /// </summary>
        /// <param name="macroName">The name of the macroscript to be called.</param>
        /// <param name="macroCategory">The category of the macroscript to be called.</param>
        /// <param name="keys">The keys associated with the hotkey assignment.</param>
        /// <returns>True if the hotkey assignment was added, false if the exact assignment already existed.</returns>
        public bool AddShortcut(string macroName, string macroCategory, Keys keys)
        {
            return AddShortcut(macroName, macroCategory, keys, true);
        }

        public int RemoveShortcut(string macroCategory)
        {
            bool removedAction = true;
            int numActionsRemoved = 0;
            while (removedAction)
            {
                KbdxShortcut action = this.shortcuts.Find(a => a.MacroCategory.Equals(macroCategory));
                if (removedAction = this.Remove(action))
                    numActionsRemoved++;
            }

            return numActionsRemoved;
        }

        public int RemoveShortcut(string macroName, string macroCategory)
        {
            bool removedAction = true;
            int numActionsRemoved = 0;
            while (removedAction)
            {
                KbdxShortcut action = this.shortcuts.Find(a => a.MacroName.Equals(macroName) && a.MacroCategory.Equals(macroCategory));
                if (removedAction = this.Remove(action))
                    numActionsRemoved++;
            }

            return numActionsRemoved;
        }
        public int RemoveShortcut(uint persistentId)
        {
            bool removedAction = true;
            int numActionsRemoved = 0;
            while (removedAction)
            {
                KbdxShortcut action = this.shortcuts.Find(a => a.ActionId == persistentId.ToString());
                if (removedAction = this.Remove(action))
                    numActionsRemoved++;
            }

            return numActionsRemoved;
        }

        public void ToXml(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                this.ToXml(fs);
            }
        }

        public void ToXml(Stream str)
        {
            XmlSerializer xs = new XmlSerializer(typeof(KbdxFile));
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            xs.Serialize(str, this, ns);
        }

        public static KbdxFile FromXml(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                KbdxFile kbdx = KbdxFile.FromXml(fs);
                kbdx.File = path;
                return kbdx;
            }
        }

        public static KbdxFile FromXml(Stream str)
        {
            XmlSerializer xs = new XmlSerializer(typeof(KbdxFile));
            return (KbdxFile)xs.Deserialize(str);
        }



        // <summary>
        /// Gets the currently active keyboard actions file from 3dsmax.
        /// </summary>
        public static string MaxGetActiveKbdxFile()
        {
            try
            {
                return MaxscriptSDK.ExecuteStringMaxscriptQuery("actionMan.getKeyboardFile()", MaxscriptSDK.ScriptSource.NonEmbedded);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return string.Empty;
            }
        }



        public static string MaxGetWriteableKbdxFile()
        {
            string kbdxFile = MaxscriptSDK.ExecuteStringMaxscriptQuery("actionMan.getKeyboardFile()", MaxscriptSDK.ScriptSource.NonEmbedded);
            try
            {
                MaxscriptSDK.ExecuteMaxscriptCommand("actionMan.saveKeyboardFile @\"" + kbdxFile + "\"", MaxscriptSDK.ScriptSource.NonEmbedded);
            }
            catch
            {
                string fileName = Path.GetFileName(kbdxFile);
                kbdxFile = Path.Combine(PathSDK.GetDirectoryPath(PathSDK.DirectoryID.UI), fileName);
                MaxscriptSDK.ExecuteMaxscriptCommand("actionMan.saveKeyboardFile @\"" + kbdxFile + "\"", MaxscriptSDK.ScriptSource.NonEmbedded);
            }

            return kbdxFile;
        }

        public static KbdxFile ReadActiveKbdxFile()
        {
            return KbdxFile.FromXml(MaxGetActiveKbdxFile());
        }

        public static KbdxFile ReadWriteableKbdxFile()
        {
            return KbdxFile.FromXml(MaxGetWriteableKbdxFile());
        }

        /// <summary>
        /// Tells 3dsmax to load the keyboard actions file.
        /// </summary>
        public bool MaxLoadKbdxFile()
        {
            try
            {
                return MaxscriptSDK.ExecuteBooleanMaxscriptQuery("actionMan.loadKeyboardFile @\"" + this.File + "\"", MaxscriptSDK.ScriptSource.NonEmbedded);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }


        public void MaxExecuteAction(Keys k)
        {
            KbdxShortcut action = this.GetShortcut(k);
            if (action != null)
            {
                MaxActionItem actionItem = null;
                if (action.MacroName != null && action.MacroCategory != null)
                    actionItem = MaxActionItemResolver.ResolveMacroItem(action.MacroName, action.MacroCategory);
                else if (action.PersistentId != 0)
                    actionItem = MaxActionItemResolver.ResolveNativeItem(action.PersistentId, (uint)action.TableId);

                if (actionItem != null && actionItem.IsEnabled())
                    actionItem.Execute();
            }
        }

        #region ICollection members

        public void Add(KbdxShortcut item)
        {
            this.shortcuts.Add(item);
        }

        public void Clear()
        {
            this.shortcuts.Clear();
        }

        public bool Contains(KbdxShortcut item)
        {
            return this.shortcuts.Contains(item);
        }

        public void CopyTo(KbdxShortcut[] array, int arrayIndex)
        {
            this.shortcuts.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.shortcuts.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KbdxShortcut item)
        {
            return this.shortcuts.Remove(item);
        }

        public IEnumerator<KbdxShortcut> GetEnumerator()
        {
            return this.shortcuts.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.shortcuts.GetEnumerator();
        }

        #endregion
    }



}
