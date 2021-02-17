using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;

using Ched.Plugins;

namespace Ched.UI.Windows
{
    /// <summary>
    /// Interaction logic for DiagnosticsView.xaml
    /// </summary>
    public partial class DiagnosticsWindow : Window
    {
        public DiagnosticsWindow()
        {
            InitializeComponent();
        }
    }

    public class DiagnosticsWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string title;
        private string message;
        private ObservableCollection<Diagnostic> diagnostics;

        public string Title
        {
            get => title;
            set
            {
                if (value == title) return;
                title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public string Message
        {
            get => message;
            set
            {
                if (value == message) return;
                message = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            }
        }

        public ObservableCollection<Diagnostic> Diagnostics
        {
            get => diagnostics;
            set
            {
                if (value == diagnostics) return;
                diagnostics = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Diagnostics)));
            }
        }
    }

    public class DiagnosticViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Diagnostic source;

        public DiagnosticViewModel(Diagnostic diagnostic)
        {
            source = diagnostic;
        }

        public DiagnosticSeverity Severity => source.Severity;

        public string Message => source.Message;
    }
}
