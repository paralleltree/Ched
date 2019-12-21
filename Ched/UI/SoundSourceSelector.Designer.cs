namespace Ched.UI
{
    partial class SoundSourceSelector
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SoundSourceSelector));
            this.filePathBox = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.latencyBox = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.latencyBox)).BeginInit();
            this.SuspendLayout();
            // 
            // filePathBox
            // 
            resources.ApplyResources(this.filePathBox, "filePathBox");
            this.filePathBox.Name = "filePathBox";
            // 
            // buttonBrowse
            // 
            resources.ApplyResources(this.buttonBrowse, "buttonBrowse");
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // latencyBox
            // 
            resources.ApplyResources(this.latencyBox, "latencyBox");
            this.latencyBox.DecimalPlaces = 3;
            this.latencyBox.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.latencyBox.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.latencyBox.Minimum = new decimal(new int[] {
            60,
            0,
            0,
            -2147483648});
            this.latencyBox.Name = "latencyBox";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // SoundSourceSelector
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.latencyBox);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.filePathBox);
            this.Name = "SoundSourceSelector";
            ((System.ComponentModel.ISupportInitialize)(this.latencyBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox filePathBox;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.NumericUpDown latencyBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}
