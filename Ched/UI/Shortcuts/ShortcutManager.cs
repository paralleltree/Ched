using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI.Shortcuts
{
    public class ShortcutManager
    {
        public IShortcutCommandSource CommandSource { get; set; } = new NullShortcutCommandSource();
        public IShortcutKeySource DefaultKeySource { get; set; } = new NullShortcutKeySource();
        public IShortcutKeySource UserKeySource { get; set; } = new NullShortcutKeySource();

        public bool ExecuteCommand(Keys key)
        {
            bool Resolve(IShortcutKeySource source)
            {
                if (source.ResolveCommand(key, out string command))
                {
                    return CommandSource.ExecuteCommand(command);
                }
                return false;
            }

            // User, Defaultの順にトラバースしてひっかける
            return Resolve(UserKeySource) || Resolve(DefaultKeySource);
        }

        public bool ResolveShortcutKey(string command, out Keys key)
        {
            return UserKeySource.ResolveShortcutKey(command, out key) || DefaultKeySource.ResolveShortcutKey(command, out key);
        }
    }
}
