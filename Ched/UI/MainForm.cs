using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

using Ched.Components;

namespace Ched.UI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            ToolStripManager.RenderMode = ToolStripManagerRenderMode.System;

            // コンテキストメニュー
            var fileMenuItems = new MenuItem[]
            {
                new MenuItem("新規作成(&N)") { Shortcut = Shortcut.CtrlN },
                new MenuItem("開く(&O)") { Shortcut = Shortcut.CtrlO },
                new MenuItem("上書き保存(&S)") { Shortcut = Shortcut.CtrlS },
                new MenuItem("名前を付けて保存(&A)") { Shortcut = Shortcut.CtrlShiftS },
                new MenuItem("-"),
                new MenuItem("最近使ったファイル", new MenuItem[] { new MenuItem("なし") { Enabled = false } }),
                new MenuItem("-"),
                new MenuItem("終了(&X)", (s, e) => this.Close())
            };

            var fileMenu = new MenuItem("ファイル(&F)", fileMenuItems) { Shortcut = Shortcut.CtrlF };

            var menuStrip = new MainMenu(new MenuItem[] { fileMenu });


            // 編集ツールバー
            var newFileButton = new ToolStripButton("新規作成", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\document.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var openFileButton = new ToolStripSplitButton("開く", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\folder-horizontal-open.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var saveFileButton = new ToolStripButton("上書き保存", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\disk-return-black.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var exportButton = new ToolStripButton("エクスポート", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\blue-document-export.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var undoButton = new ToolStripButton("元に戻す", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\arrow-curve-180-left.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var redoButton = new ToolStripButton("やり直す", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\arrow-curve.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var cutButton = new ToolStripButton("切り取り", Image.FromFile(@"C:\Users\paltee\Downloads\famfamfam_silk_icons_v013\icons\cut.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var copyButton = new ToolStripButton("コピー", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\document-copy.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var pasteButton = new ToolStripButton("貼り付け", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\clipboard-paste.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var penButton = new ToolStripButton("ペン", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\pencil.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var selectionButton = new ToolStripButton("範囲選択", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\selection.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var eraserButton = new ToolStripButton("消しゴム", Image.FromFile(@"C:\Users\paltee\Downloads\fugue-icons-3.5.6\icons\eraser.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var editStrip = new ToolStrip(new ToolStripItem[] { newFileButton, openFileButton, saveFileButton, exportButton, new ToolStripSeparator(), undoButton, redoButton, new ToolStripSeparator(), cutButton, copyButton, pasteButton, new ToolStripSeparator(), penButton, selectionButton, eraserButton });

            // ノーツツールバー

            var tapButton = new ToolStripButton("TAP", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\tap.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Checked = true
            };
            var exTapButton = new ToolStripButton("ExTAP", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\extap.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var airActionButton = new ToolStripButton("AIR-ACTION", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\airaction.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var holdButton = new ToolStripButton("HOLD", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\hold.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var slideButton = new ToolStripButton("SLIDE", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\slide.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var flickButton = new ToolStripButton("FLICK", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\flick.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var damageButton = new ToolStripButton("DAMAGE", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\damage.png"))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var airKind = new CheckableToolStripSplitButton() { Checked = false, DisplayStyle = ToolStripItemDisplayStyle.Image };
            airKind.Text = "AIR";
            airKind.Click += (s, e) => airKind.Checked = !airKind.Checked;
            airKind.DropDown.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("振り上げAIR", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\airup.png")),
                new ToolStripMenuItem("振り上げ左AIR", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\airleftup.png")),
                new ToolStripMenuItem("振り上げ右AIR", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\airrightup.png")),
                new ToolStripMenuItem("振り下げAIR", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\airdown.png")),
                new ToolStripMenuItem("振り下げ左AIR", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\airleftdown.png")),
                new ToolStripMenuItem("振り下げ右AIR", Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\airrightdown.png"))
            });
            airKind.Image = Image.FromFile(@"C:\Users\paltee\Documents\Visual Studio 2015\Projects\Ched\airup.png");

            var toolStrip = new ToolStrip(new ToolStripItem[] { tapButton, exTapButton, holdButton, slideButton, airKind, airActionButton, flickButton, damageButton });

            var picBox = new PictureBox() { BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill, BackColor = Color.Black };
            using (var manager = this.WorkWithLayout())
            {
                this.Menu = menuStrip;
                //this.Controls.Add(picBox);
                this.Controls.Add(new NoteView() { Dock = DockStyle.Fill });
                this.Controls.AddRange(new Control[] { toolStrip, editStrip });
            }

            var bitmap = new Bitmap(picBox.Width, picBox.Height);
            picBox.Image = bitmap;

            var tap = new Tap();
            var hold = new Hold();
            using (var g = Graphics.FromImage(bitmap))
            {
                //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                tap.Draw(g, new RectangleF(10, 30, 16, 4));
                hold.DrawBackground(g, new RectangleF(100, 10, 40, 120));
                //new Air(tap) { HorizontalDirection = HorizontalAirDirection.Right }.Draw(g, new RectangleF(10, 60, 60, 5));
                tap.Draw(g, new RectangleF(10, 60, 60, 5));
                tap.Draw(g, new RectangleF(10, 80, 80, 20));
            }
        }

        private ToolStrip GenerateNoteTypesToolStrip(bool airTypeSelectable)
        {
            throw new NotImplementedException();
        }
    }

    internal class LayoutManager : IDisposable
    {
        protected Control _control;

        public LayoutManager(Control control)
        {
            control.SuspendLayout();
            _control = control;
        }

        public void Dispose()
        {
            _control.ResumeLayout(false);
            _control.PerformLayout();
        }
    }

    internal static class WinFormsEx
    {
        public static LayoutManager WorkWithLayout(this Control control)
        {
            return new LayoutManager(control);
        }
    }


    // https://arstechnica.com/civis/viewtopic.php?f=20&t=311656
    public class CheckableToolStripSplitButton : ToolStripSplitButton
    {
        private bool _Checked = false;
        private VisualStyleRenderer renderer = null;
        private readonly VisualStyleElement element = VisualStyleElement.ToolBar.Button.Checked;

        public CheckableToolStripSplitButton()
        {
            if (Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(element))
            {
                renderer = new VisualStyleRenderer(element);
            }
        }

        public bool Checked
        {
            get
            {
                return _Checked;
            }
            set
            {
                _Checked = value;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_Checked)
            {
                if (renderer != null)
                {
                    System.Drawing.Rectangle cr = base.ContentRectangle;
                    System.Drawing.Image img = this.Image;

                    // Compute the center of the item's ContentRectangle.
                    int centerY = (cr.Height - img.Height) / 2;
                    System.Drawing.Rectangle fullRect = new System.Drawing.Rectangle(0, 0, this.Width, this.Height);

                    System.Drawing.Rectangle imageRect = new System.Drawing.Rectangle(
                        base.ContentRectangle.Left,
                        centerY,
                        base.Image.Width,
                        base.Image.Height);

                    System.Drawing.Rectangle textRect = new System.Drawing.Rectangle(
                        imageRect.Width,
                        base.ContentRectangle.Top,
                        base.ContentRectangle.Width - (imageRect.Width + 10),
                        base.ContentRectangle.Height);

                    renderer.DrawBackground(e.Graphics, fullRect);
                    //renderer.DrawText(e.Graphics, textRect, this.Text);
                    //renderer.DrawImage(e.Graphics, imageRect, this.Image);
                    base.OnPaint(e);
                }
                else
                {
                    e.Graphics.FillRectangle(System.Drawing.SystemBrushes.Control, 0, 0, this.Width, this.Height);
                    e.Graphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.SystemColors.Highlight), 0, 0, this.Width - 1, this.Height - 1);
                    base.OnPaint(e);
                }
            }
            else
            {
                base.OnPaint(e);
            }
        }
    }

    /// <summary>
    /// ToolStripSplitCheckButton adds a Check property to a ToolStripSplitButton.
    /// https://stackoverflow.com/questions/35570331/can-i-have-a-checked-state-on-a-toolstripsplitbutton-in-winforms
    /// </summary>
    public partial class ToolStripSplitCheckButton : ToolStripSplitButton
    {
        //==============================================================================
        // Inner class: ToolBarButonSplitCheckButtonEventArgs
        //==============================================================================

        /// <summary>
        /// The event args for the check button click event. To be able to use the OnCheckedChanged
        /// event, we must also record a dummy button as well as this one (hack).
        /// </summary>
        public class ToolBarButonSplitCheckButtonEventArgs : ToolBarButtonClickEventArgs
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="split_button">The sender split check button</param>
            public ToolBarButonSplitCheckButtonEventArgs(ToolStripSplitCheckButton split_button)
                : base(new ToolBarButton("Dummy Button"))       // Hack - Dummy Button is not used
            {
                SplitCheckButton = split_button;
            }

            /// <summary>
            /// The ToolStripSplitCheckButton to be sent as an argument.
            /// </summary>
            public ToolStripSplitCheckButton SplitCheckButton { get; set; }
        }


        //==========================================================================
        // Construction

        public ToolStripSplitCheckButton()
        {
            m_checked = false;
            m_mouse_over = false;
        }


        //==========================================================================
        // Properties

        /// <summary>
        /// Indicates whether the button should toggle its Checked state on click.
        /// </summary>
        [Category("Behavior"),
        Description("Indicates whether the item should toggle its selected state when clicked."),
        DefaultValue(true)]
        public bool CheckOnClick { get; set; }

        /// <summary>
        /// Indictates the Checked state of the button.
        /// </summary>
        [Category("Behavior"),
        Description("Indicates whether the ToolStripSplitCheckButton is pressed in or not pressed in."),
        DefaultValue(false)]
        public bool Checked { get { return m_checked; } set { m_checked = value; } }


        //==========================================================================
        // Methods

        /// <summary>
        /// Toggle the click state on button click.
        /// </summary>
        protected override void OnButtonClick(EventArgs e)
        {
            if (CheckOnClick)
            {
                m_checked = !m_checked;
                // Raise the OnCheckStateChanged event when the button is clicked
                if (OnCheckChanged != null)
                {
                    ToolBarButonSplitCheckButtonEventArgs args = new ToolBarButonSplitCheckButtonEventArgs(this);
                    OnCheckChanged(this, args);
                }
            }
            base.OnButtonClick(e);
        }

        /// <summary>
        /// On mouse enter, record that we are over the button.
        /// </summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            m_mouse_over = true;
            base.OnMouseEnter(e);
            this.Invalidate();
        }

        /// <summary>
        /// On mouse leave, record that we are no longer over the button.
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            m_mouse_over = false;
            base.OnMouseLeave(e);
            this.Invalidate();
        }

        /// <summary>
        /// Paint the check highlight when required.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (m_checked)
            {
                // I can't get the check + mouse over to render properly, so just give the button a colour fill - Hack
                if (m_mouse_over)
                {
                    using (Brush brush = new SolidBrush(Color.FromArgb(64, SystemColors.MenuHighlight)))
                    {
                        e.Graphics.FillRectangle(brush, ButtonBounds);
                    }
                }
                ControlPaint.DrawBorder(
                    e.Graphics,
                    e.ClipRectangle,            // To draw around the button + drop-down
                                                //this.ButtonBounds,        // To draw only around the button
                    SystemColors.MenuHighlight,
                    ButtonBorderStyle.Solid);
            }
        }


        //==========================================================================
        // Member Variables

        // The delegate that acts as a signature for the function that is ultimately called
        // when the OnCheckChanged event is triggered.
        public delegate void SplitCheckButtonEventHandler(object source, EventArgs e);
        public event SplitCheckButtonEventHandler OnCheckChanged;

        private bool m_checked;
        private bool m_mouse_over;
    }
}
