using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.UI.Shortcuts
{
    public static class Commands
    {
        // MenuStrip
        public static string NewFile => "files.new";
        public static string OpenFile => "files.open";
        public static string Save => "files.save";
        public static string SaveAs => "files.saveAs";

        public static string Undo => "editor.action.undo";
        public static string Redo => "editor.action.redo";

        public static string Cut => "editor.action.clipboardCut";
        public static string Copy => "editor.action.clipboardCopy";
        public static string Paste => "editor.action.clipboardPaste";
        public static string PasteFlip => "editor.action.clipboardPasteFlip";

        public static string SelectAll => "editor.action.selectAll";

        public static string RemoveSelectedNotes => "editor.action.removeSelectedNotes";

        public static string SwitchScorePreviewMode => "editor.view.switchScorePreviewMode";

        public static string PlayPreview => "editor.view.playPreview";

        public static string ShowHelp => "application.showHelp";
    }
}
