using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI.Shortcuts
{
    public interface IShortcutKeySource
    {
        /// <summary>
        /// 指定のショートカットキーに対応するコマンドを取得します。
        /// </summary>
        /// <param name="key">コマンドを取得するショートカットキー</param>
        /// <param name="command">ショートカットキーに対応するコマンド</param>
        /// <returns>ショートカットキーに対応するコマンドが存在すればTrue</returns>
        bool ResolveCommand(Keys key, out string command);

        /// <summary>
        /// 指定のコマンドに対応するショートカットキーを取得します。
        /// </summary>
        /// <param name="command">ショートカットキーを取得するコマンド</param>
        /// <param name="key">コマンドに対応するショートカットキー</param>
        /// <returns>コマンドに対応するショートカットキーが存在すればTrue</returns>
        bool ResolveShortcutKey(string command, out Keys key);
    }

    public class NullShortcutKeySource : IShortcutKeySource
    {
        public bool ResolveCommand(Keys key, out string command)
        {
            command = null;
            return false;
        }

        public bool ResolveShortcutKey(string command, out Keys key)
        {
            key = Keys.None;
            return false;
        }
    }

    public abstract class ShortcutKeySource : IShortcutKeySource
    {
        private Dictionary<Keys, string> KeyMap { get; } = new Dictionary<Keys, string>();
        private Dictionary<string, HashSet<Keys>> CommandMap { get; } = new Dictionary<string, HashSet<Keys>>();

        public IEnumerable<Keys> ShortcutKeys => KeyMap.Keys;

        public ShortcutKeySource()
        {
        }

        protected ShortcutKeySource(ShortcutKeySource other)
        {
            foreach (var item in other.KeyMap)
            {
                RegisterShortcut(item.Value, item.Key);
            }
        }

        protected void RegisterShortcut(string command, Keys key)
        {
            // キーの重複を確認。コマンドは重複してもよい(異なるキーから同じコマンドを呼び出しても問題ない)
            if (KeyMap.ContainsKey(key)) throw new InvalidOperationException("The shortcut key has already been registered.");
            KeyMap.Add(key, command);
            if (!CommandMap.ContainsKey(command)) CommandMap.Add(command, new HashSet<Keys>());
            CommandMap[command].Add(key);
        }

        protected void UnregisterShortcut(Keys key)
        {
            if (!KeyMap.ContainsKey(key)) throw new InvalidOperationException("The shortcut key is not registered.");
            string command = KeyMap[key];
            KeyMap.Remove(key);
            CommandMap[command].Remove(key);
        }

        public bool ResolveCommand(Keys key, out string command) => KeyMap.TryGetValue(key, out command);

        public bool ResolveShortcutKey(string command, out Keys key)
        {
            key = Keys.None;
            if (!CommandMap.TryGetValue(command, out HashSet<Keys> keys)) return false;
            if (keys.Count > 0)
            {
                key = keys.First();
                return true;
            }
            return false;
        }
    }

    public class UserShortcutKeySource : ShortcutKeySource
    {
        public UserShortcutKeySource()
        {
        }

        public UserShortcutKeySource(string jsonText)
        {
            var shortcuts = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ShortcutDefinition>>(jsonText);
            foreach (var item in shortcuts)
            {
                RegisterShortcut(item.Command, item.ShortcutKey);
            }
        }

        public UserShortcutKeySource(UserShortcutKeySource other) : base(other)
        {
        }

        public new void RegisterShortcut(string command, Keys key)
        {
            base.RegisterShortcut(command, key);
        }

        public new void UnregisterShortcut(Keys key)
        {
            base.UnregisterShortcut(key);
        }

        public string DumpShortcutKeys()
        {
            var binds = ShortcutKeys.Select(p =>
            {
                if (!ResolveCommand(p, out string command)) throw new InvalidOperationException();
                return new ShortcutDefinition(command, p);
            });
            return Newtonsoft.Json.JsonConvert.SerializeObject(binds, Newtonsoft.Json.Formatting.Indented);
        }

        public UserShortcutKeySource Clone() => new UserShortcutKeySource(this);
    }

    public class DefaultShortcutKeySource : ShortcutKeySource
    {
        public DefaultShortcutKeySource()
        {
            RegisterShortcut(Commands.NewFile, Keys.Control | Keys.N);
            RegisterShortcut(Commands.OpenFile, Keys.Control | Keys.O);
            RegisterShortcut(Commands.Save, Keys.Control | Keys.S);
            RegisterShortcut(Commands.SaveAs, Keys.Control | Keys.Shift | Keys.S);

            RegisterShortcut(Commands.Undo, Keys.Control | Keys.Z);
            RegisterShortcut(Commands.Redo, Keys.Control | Keys.Y);
            RegisterShortcut(Commands.Redo, Keys.Control | Keys.Shift | Keys.Z);

            RegisterShortcut(Commands.Cut, Keys.Control | Keys.X);
            RegisterShortcut(Commands.Copy, Keys.Control | Keys.C);
            RegisterShortcut(Commands.Paste, Keys.Control | Keys.V);
            RegisterShortcut(Commands.PasteFlip, Keys.Control | Keys.Shift | Keys.V);

            RegisterShortcut(Commands.SelectAll, Keys.Control | Keys.A);

            RegisterShortcut(Commands.RemoveSelectedNotes, Keys.Delete);

            RegisterShortcut(Commands.SwitchScorePreviewMode, Keys.Control | Keys.P);

            RegisterShortcut(Commands.PlayPreview, Keys.Space);

            RegisterShortcut(Commands.ShowHelp, Keys.F1);
        }
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class ShortcutDefinition
    {
        [Newtonsoft.Json.JsonProperty("command")]
        private string command;

        [Newtonsoft.Json.JsonProperty("key")]
        private Keys shortcutKey;

        public string Command => command;
        public Keys ShortcutKey => shortcutKey;

        public ShortcutDefinition(string command, Keys key)
        {
            this.command = command;
            this.shortcutKey = key;
        }
    }
}
