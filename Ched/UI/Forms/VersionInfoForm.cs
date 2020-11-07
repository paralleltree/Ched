using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using Ched.Properties;

namespace Ched.UI.Forms
{
    public partial class VersionInfoForm : Form
    {
        public VersionInfoForm()
        {
            InitializeComponent();

            var asm = Assembly.GetEntryAssembly();

            labelTitle.Text = string.Format("{0} - {1}", asm.GetCustomAttribute<AssemblyTitleAttribute>().Title, asm.GetCustomAttribute<AssemblyDescriptionAttribute>().Description);
            labelVersion.Text = string.Format("Version {0}", asm.GetName().Version.ToString());
            labelProduct.Text = asm.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;

            pictureBox1.Image = Bitmap.FromHicon(Resources.MainIcon.Handle);

            buttonClose.Click += (s, e) => Close();
        }
    }
}
