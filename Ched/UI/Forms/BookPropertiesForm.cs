using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Core;

namespace Ched.UI.Forms
{
    public partial class BookPropertiesForm : Form
    {
        public SoundSource MusicSource { get { return musicSourceSelector.Value; } }

        public BookPropertiesForm(ScoreBook book, SoundSource musicSource)
        {
            InitializeComponent();
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            titleBox.Text = book.Title;
            artistBox.Text = book.ArtistName;
            notesDesignerBox.Text = book.NotesDesignerName;
            if (musicSource != null) musicSourceSelector.Value = musicSource;

            buttonOK.Click += (s, e) =>
            {
                book.Title = titleBox.Text;
                book.ArtistName = artistBox.Text;
                book.NotesDesignerName = notesDesignerBox.Text;
                Close();
            };
        }
    }
}
