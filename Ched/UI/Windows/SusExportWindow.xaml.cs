using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Ched.Core;
using Ched.Components.Exporter;
using Ched.Localization;

namespace Ched.UI.Windows
{
    /// <summary>
    /// Interaction logic for SusExportWindow.xaml
    /// </summary>
    public partial class SusExportWindow : Window
    {
        public SusExportWindow()
        {
            InitializeComponent();
        }
    }

    public class SusExportWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SusArgs SusArgs { get; }
        private ScoreBook ScoreBook { get; }

        public string MusicFilter { get; } = Helpers.GetFilterString(FileFilterStrings.AudioFilter, SoundSource.SupportedExtensions);
        public string ImageFilter { get; } = Helpers.GetFilterString(FileFilterStrings.ImageFilter, new[] { ".jpg", ".png", ".bmp" });
        public IEnumerable<string> Levels { get; } = Enumerable.Range(1, 14).SelectMany(p => new string[] { p.ToString(), $"{p}+" });

        public string Title => ScoreBook.Title;
        public string Artist => ScoreBook.ArtistName;
        public string NotesDesigner => ScoreBook.NotesDesignerName;

        private SusArgs.Difficulty difficulty;
        public SusArgs.Difficulty Difficulty
        {
            get => difficulty;
            set
            {
                if (value == difficulty) return;
                difficulty = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Difficulty)));
            }
        }

        private string level;
        public string Level
        {
            get => level;
            set
            {
                if (value == level) return;
                level = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level)));
            }
        }

        private string songId;
        public string SongId
        {
            get => songId;
            set
            {
                if (value == songId) return;
                songId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SongId)));
            }
        }

        private string soundFileName;
        public string SoundFileName
        {
            get => soundFileName;
            set
            {
                if (value == soundFileName) return;
                soundFileName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SoundFileName)));
            }
        }

        private double soundOffset;
        public double SoundOffset
        {
            get => soundOffset;
            set
            {
                if (value == soundOffset) return;
                soundOffset = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SoundOffset)));
            }
        }

        private string jacketFileName;
        public string JacketFileName
        {
            get => jacketFileName;
            set
            {
                if (value == jacketFileName) return;
                jacketFileName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(JacketFileName)));
            }
        }

        private string additionalData;
        public string AdditionalData
        {
            get => additionalData;
            set
            {
                if (value == additionalData) return;
                additionalData = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AdditionalData)));
            }
        }

        public Action<string> SetSoundFileNameAction => path => SoundFileName = System.IO.Path.GetFileName(path);
        public Action<string> SetJacketFileNameAction => path => JacketFileName = System.IO.Path.GetFileName(path);

        public SusExportWindowViewModel()
        {
        }

        public SusExportWindowViewModel(ScoreBook book, SusArgs susArgs)
        {
            ScoreBook = book;
            SusArgs = susArgs;
        }

        public void BeginEdit()
        {
            Difficulty = SusArgs.PlayDifficulty;
            Level = SusArgs.PlayLevel;
            SongId = SusArgs.SongId;
            SoundFileName = SusArgs.SoundFileName;
            SoundOffset = (double)SusArgs.SoundOffset;
            JacketFileName = SusArgs.JacketFilePath;
            AdditionalData = SusArgs.AdditionalData;
        }

        public void CommitEdit()
        {
            SusArgs.PlayDifficulty = Difficulty;
            SusArgs.PlayLevel = Level;
            SusArgs.SongId = SongId;
            SusArgs.SoundFileName = SoundFileName;
            SusArgs.SoundOffset = (decimal)SoundOffset;
            SusArgs.JacketFilePath = JacketFileName;
            SusArgs.AdditionalData = AdditionalData;
        }
    }

    public class SusDifficultySourceProvider : EnumSourceProvider<SusArgs.Difficulty>
    {
    }
}
