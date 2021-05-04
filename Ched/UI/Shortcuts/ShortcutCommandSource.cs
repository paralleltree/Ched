using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI.Shortcuts
{
    public interface IShortcutCommandSource
    {
        /// <summary>
        /// 指定のコマンドを実行します。
        /// </summary>
        /// <param name="command">実行するコマンド</param>
        /// <returns>コマンドが実行された場合はTrue</returns>
        bool ExecuteCommand(string command);

        bool ResolveCommandName(string command, out string name);
    }

    public class NullShortcutCommandSource : IShortcutCommandSource
    {
        // Do nothing
        public bool ExecuteCommand(string command) => true;

        public bool ResolveCommandName(string command, out string name)
        {
            name = null;
            return false;
        }
    }
}
