using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI.Shortcuts
{
    public static class ShortcutExtensions
    {
        public static string ToShortcutChar(this Keys key) => key.ToShortcutString(key.IsMapRequired());
        public static string ToShortcutKey(this Keys key) => key.ToShortcutString(false);

        private static string ToShortcutString(this Keys key, bool toChar)
        {
            IEnumerable<string> Build()
            {
                if (key.HasFlag(Keys.Control)) yield return "Ctrl";
                if (key.HasFlag(Keys.Shift)) yield return "Shift";
                if (key.HasFlag(Keys.Alt)) yield return "Alt";

                var keyCode = key & Keys.KeyCode;
                switch (keyCode)
                {
                    case Keys.None:
                    case Keys.ControlKey:
                    case Keys.ShiftKey:
                        yield break;
                }

                yield return toChar ? keyCode.ToChar().ToString() : keyCode.ToString();
            }

            return string.Join("+", Build());
        }

        private static bool IsMapRequired(this Keys key)
        {
            var keyCode = key & Keys.KeyCode;

            if (keyCode >= Keys.D0 && keyCode <= Keys.D9) return true;
            if ((int)keyCode >= 0xba && (int)keyCode <= 0xe2) return true; // Oem key

            return false;
        }
    }

    // https://stackoverflow.com/questions/318777/c-sharp-how-to-translate-virtual-keycode-to-char
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        private static extern int MapVirtualKey(uint uCode, uint uMapType);

        public static char ToChar(this Keys key)
        {
            // Convert with MAPVK_VK_TO_CHAR
            return Convert.ToChar(MapVirtualKey((uint)key, 2));
        }
    }
}
