using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace Outliner
{
    [XmlType("shortcut")]
    [XmlRoot("shortcut")]
    public class KbdxShortcut
    {
        public const int MainTableId = 0;
        public const int MacroTableId = 647394;

        [XmlAttribute("fVirt")]
        public int ModKey;

        [XmlAttribute("accleleratorKey")] //This is not a typo on my side...
        public int AccelleratorKey;

        [XmlAttribute("actionID")]
        public string ActionId;

        [XmlAttribute("actionTableID")]
        public int TableId;

        [XmlIgnore]
        public uint PersistentId
        {
            get
            {
                try { return uint.Parse(this.ActionId); }
                catch { return 0; }
            }
        }

        [XmlIgnore]
        public string MacroName
        {
            get
            {
                if (this.ActionId.Contains('`'))
                    return this.ActionId.Split(new char[] { '`' })[0];
                else
                    return null;
            }
        }

        [XmlIgnore]
        public string MacroCategory
        {
            get
            {
                if (this.ActionId.Contains('`'))
                    return this.ActionId.Split(new char[] { '`' })[1];
                else
                    return null;
            }
        }

        [XmlIgnore]
        public Keys Key
        {
            get { return modKeycodeToKeys(this.ModKey) | keyCodeToKeys(this.AccelleratorKey); }
            set
            {
                this.ModKey = keysToModKeycode(value);
                this.AccelleratorKey = keysToKeyCode(value);
            }
        }


        public KbdxShortcut()
           : this(Keys.None, "0", KbdxShortcut.MainTableId) { }
        public KbdxShortcut(Keys key, string macroName, string macroCategory)
           : this(key, macroName + "`" + macroCategory, KbdxShortcut.MacroTableId) { }
        public KbdxShortcut(Keys key, string actionID, int actionTableID)
        {
            this.Key = key;
            this.ActionId = actionID;
            this.TableId = actionTableID;
        }



        private Keys keyCodeToKeys(int keyCode)
        {
            return (Keys)keyCode;
        }

        private int keysToKeyCode(Keys keys)
        {
            return (int)((keys ^ Keys.Modifiers) & Keys.KeyCode);
        }

        private Keys modKeycodeToKeys(int keycode)
        {
            Keys keys = Keys.None;
            if ((keycode & 4) == 4) keys |= Keys.Shift;
            if ((keycode & 8) == 8) keys |= Keys.Control;
            if ((keycode & 16) == 16) keys |= Keys.Alt;
            return keys;
        }
        private int keysToModKeycode(Keys keys)
        {
            int keycode = 3;
            if ((keys & Keys.Shift) == Keys.Shift) keycode += 4;
            if ((keys & Keys.Control) == Keys.Control) keycode += 8;
            if ((keys & Keys.Alt) == Keys.Alt) keycode += 16;
            return keycode;
        }
    }
}
