using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Ched.UI
{
    // https://arstechnica.com/civis/viewtopic.php?f=20&t=311656
    public class CheckableToolStripSplitButton : ToolStripSplitButton
    {
        private bool _Checked = false;
        private VisualStyleRenderer renderer = null;
        private System.ComponentModel.IContainer components;
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

        private void InitializeComponent()
        {

        }
    }
}
