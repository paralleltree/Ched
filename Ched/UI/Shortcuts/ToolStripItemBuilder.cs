using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI.Shortcuts
{
    internal abstract class ToolStripItemBuilder<T>
    {
        private Dictionary<string, T> items = new Dictionary<string, T>();
        protected ShortcutManager ShortcutManager { get; }

        public ToolStripItemBuilder(ShortcutManager shortcutManager)
        {
            ShortcutManager = shortcutManager;
            ShortcutManager.ShortcutUpdated += OnShortcutUpdated;
        }

        private void OnShortcutUpdated(object sender, EventArgs e)
        {
            var shortcutManager = (ShortcutManager)sender;
            foreach (var item in items)
            {
                if (shortcutManager.ResolveShortcutKey(item.Key, out Keys key))
                {
                    UpdateShortcutKey(item.Value, key.ToShortcutChar());
                    continue;
                }
                UpdateShortcutKey(item.Value, "");
            }
        }

        public T BuildItem(string command, string commandName) => BuildItem(command, commandName, null);

        public T BuildItem(string command, string commandName, Image image)
        {
            var item = BuildItemInstance(command, commandName, image);
            items.Add(command, item);
            return item;
        }

        protected abstract T BuildItemInstance(string command, string commandName, Image image);
        protected abstract void UpdateShortcutKey(T item, string keyText);
    }

    internal class ToolStripMenuItemBuilder : ToolStripItemBuilder<ToolStripMenuItem>
    {
        public ToolStripMenuItemBuilder(ShortcutManager shortcutManager) : base(shortcutManager)
        {
        }

        protected override ToolStripMenuItem BuildItemInstance(string command, string commandName, Image image)
        {
            return new ToolStripMenuItem(commandName, null, (s, e) => ShortcutManager.CommandSource.ExecuteCommand(command));
        }

        protected override void UpdateShortcutKey(ToolStripMenuItem item, string keyText)
        {
            item.ShortcutKeyDisplayString = keyText;
        }
    }
}
