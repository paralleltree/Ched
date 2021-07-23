using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using Ched.UI.Shortcuts;

namespace Ched.UI.Windows
{
    /// <summary>
    /// Interaction logic for ShortcutSettingsWindow.xaml
    /// </summary>
    public partial class ShortcutSettingsWindow : Window
    {
        public ShortcutSettingsWindow()
        {
            InitializeComponent();
        }
    }

    public class ShortcutSettingsWindowViewModel : ViewModel
    {
        private ShortcutManagerHost shortcutManagerHost;
        private UserShortcutKeySource oldUserShortcutKeySource;
        private ShortcutDefinitionViewModel selectedShortcut;

        private ListCollectionView shortcutListView;

        public System.Windows.Input.ICommand SetShortcutKeyCommand { get; }

        public ListCollectionView ShortcutListView
        {
            get => shortcutListView;
            set
            {
                if (shortcutListView == value) return;
                shortcutListView = value;
                NotifyPropertyChanged();
            }
        }

        public ShortcutDefinitionViewModel SelectedShortcut
        {
            get => selectedShortcut;
            set
            {
                if (selectedShortcut == value) return;
                selectedShortcut = value;
                NotifyPropertyChanged();
            }
        }

        public ShortcutSettingsWindowViewModel(ShortcutManagerHost shortcutManagerHost)
        {
            SetShortcutKeyCommand = new SetShortcutKeyCommandImpl(this);
            this.shortcutManagerHost = shortcutManagerHost;
            oldUserShortcutKeySource = new UserShortcutKeySource(shortcutManagerHost.UserShortcutKeySource);
            var defs = shortcutManagerHost.ShortcutManager.CommandSource.Commands.Select(p => new ShortcutDefinitionViewModel(shortcutManagerHost, p));
            ShortcutListView = new ListCollectionView(defs.ToList());
        }

        public void ClearShortcut()
        {
            SelectedShortcut.ClearShortcutKey();
        }

        public void ResetAllShortcut()
        {
            shortcutManagerHost.UserShortcutKeySource = new UserShortcutKeySource();
            RefreshView();
        }

        protected void RefreshView() => ShortcutListView.Refresh();

        public void CancelEdit()
        {
            shortcutManagerHost.UserShortcutKeySource = oldUserShortcutKeySource;
        }

        public class SetShortcutKeyCommandImpl : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;

            private ShortcutSettingsWindowViewModel parentViewModel;

            public SetShortcutKeyCommandImpl(ShortcutSettingsWindowViewModel parent)
            {
                parentViewModel = parent;
            }

            public bool CanExecute(object parameter) => parentViewModel.SelectedShortcut != null;

            public void Execute(object parameter)
            {
                var e = (System.Windows.Input.KeyEventArgs)parameter;
                var key = ToWinFormsKey(e, System.Windows.Input.Keyboard.Modifiers);
                var keyCode = key & System.Windows.Forms.Keys.KeyCode;

                // Shift / Ctrl / Altのみは無効
                switch (keyCode)
                {
                    case System.Windows.Forms.Keys.None:
                    case System.Windows.Forms.Keys.ControlKey:
                    case System.Windows.Forms.Keys.LControlKey:
                    case System.Windows.Forms.Keys.RControlKey:
                    case System.Windows.Forms.Keys.ShiftKey:
                    case System.Windows.Forms.Keys.LShiftKey:
                    case System.Windows.Forms.Keys.RShiftKey:
                    case System.Windows.Forms.Keys.Alt:
                    case System.Windows.Forms.Keys.Menu:
                    case System.Windows.Forms.Keys.LMenu:
                    case System.Windows.Forms.Keys.RMenu:
                        return;
                }

                // 既に同じキーが別のコマンドへ割り当てられていれば解除
                if (parentViewModel.shortcutManagerHost.UserShortcutKeySource.ResolveCommand(key, out string _))
                {
                    parentViewModel.shortcutManagerHost.UserShortcutKeySource.UnregisterShortcut(key);
                    parentViewModel.RefreshView();
                }
                // 既に同じコマンドへ別のキーが割り当てられていれば解除
                if (parentViewModel.shortcutManagerHost.UserShortcutKeySource.ResolveShortcutKey(parentViewModel.SelectedShortcut.Command, out System.Windows.Forms.Keys registeredKey))
                {
                    parentViewModel.shortcutManagerHost.UserShortcutKeySource.UnregisterShortcut(registeredKey);
                }
                parentViewModel.SelectedShortcut.Key = key;
                e.Handled = true;
            }

            private System.Windows.Forms.Keys ToWinFormsKey(System.Windows.Input.KeyEventArgs e, System.Windows.Input.ModifierKeys modifiers)
            {
                var actualKey = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
                var key = (System.Windows.Forms.Keys)System.Windows.Input.KeyInterop.VirtualKeyFromKey(actualKey);
                if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control)) key |= System.Windows.Forms.Keys.Control;
                if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift)) key |= System.Windows.Forms.Keys.Shift;
                if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt)) key |= System.Windows.Forms.Keys.Alt;
                return key;
            }
        }
    }

    public class ShortcutDefinitionViewModel : ViewModel
    {
        private ShortcutManagerHost ShortcutManagerHost { get; }

        public string Command { get; }
        public string Name
        {
            get
            {
                if (!ShortcutManagerHost.ShortcutManager.CommandSource.ResolveCommandName(Command, out string name)) throw new InvalidOperationException();
                return name;
            }
        }
        public System.Windows.Forms.Keys Key
        {
            get
            {
                if (!ShortcutManagerHost.ShortcutManager.ResolveShortcutKey(Command, out System.Windows.Forms.Keys key))
                    return System.Windows.Forms.Keys.None;
                return key;
            }
            set
            {
                ShortcutManagerHost.UserShortcutKeySource.RegisterShortcut(Command, value);
                NotifyPropertyChanged();
            }
        }

        public ShortcutDefinitionViewModel(ShortcutManagerHost shortcutManagerHost, string command)
        {
            ShortcutManagerHost = shortcutManagerHost;
            Command = command;
        }

        public void ClearShortcutKey()
        {
            if (Key == System.Windows.Forms.Keys.None) return;
            ShortcutManagerHost.UserShortcutKeySource.UnregisterShortcut(Key);
            NotifyPropertyChanged(nameof(Key));
        }
    }
}
