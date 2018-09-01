namespace Ched.UI
{
    partial class ExportForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.browseButton = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.soundOffsetBox = new System.Windows.Forms.NumericUpDown();
            this.titleBox = new System.Windows.Forms.TextBox();
            this.artistBox = new System.Windows.Forms.TextBox();
            this.notesDesignerBox = new System.Windows.Forms.TextBox();
            this.difficultyDropDown = new System.Windows.Forms.ComboBox();
            this.levelDropDown = new System.Windows.Forms.ComboBox();
            this.songIdBox = new System.Windows.Forms.TextBox();
            this.soundFileBox = new System.Windows.Forms.TextBox();
            this.jacketFileBox = new System.Windows.Forms.TextBox();
            this.outputBox = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.exportButton = new System.Windows.Forms.Button();
            this.hasPaddingBarBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.soundOffsetBox)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "タイトル";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "アーティスト";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 66);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "ノーツデザイナー";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 91);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "難易度";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 117);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(34, 12);
            this.label5.TabIndex = 4;
            this.label5.Text = "レベル";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 143);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(47, 12);
            this.label6.TabIndex = 5;
            this.label6.Text = "SONGID";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(14, 168);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 12);
            this.label7.TabIndex = 6;
            this.label7.Text = "サウンドファイル名";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(14, 192);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(71, 12);
            this.label8.TabIndex = 7;
            this.label8.Text = "再生オフセット";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(14, 218);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(93, 12);
            this.label9.TabIndex = 8;
            this.label9.Text = "ジャケットファイル名";
            // 
            // browseButton
            // 
            this.browseButton.Location = new System.Drawing.Point(305, 287);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(75, 23);
            this.browseButton.TabIndex = 25;
            this.browseButton.Text = "参照";
            this.browseButton.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(14, 268);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(41, 12);
            this.label11.TabIndex = 11;
            this.label11.Text = "出力先";
            // 
            // soundOffsetBox
            // 
            this.soundOffsetBox.DecimalPlaces = 3;
            this.soundOffsetBox.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.soundOffsetBox.Location = new System.Drawing.Point(230, 190);
            this.soundOffsetBox.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.soundOffsetBox.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.soundOffsetBox.Name = "soundOffsetBox";
            this.soundOffsetBox.Size = new System.Drawing.Size(67, 19);
            this.soundOffsetBox.TabIndex = 21;
            this.soundOffsetBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // titleBox
            // 
            this.titleBox.Location = new System.Drawing.Point(119, 13);
            this.titleBox.Name = "titleBox";
            this.titleBox.Size = new System.Drawing.Size(178, 19);
            this.titleBox.TabIndex = 14;
            // 
            // artistBox
            // 
            this.artistBox.Location = new System.Drawing.Point(119, 38);
            this.artistBox.Name = "artistBox";
            this.artistBox.Size = new System.Drawing.Size(178, 19);
            this.artistBox.TabIndex = 15;
            // 
            // notesDesignerBox
            // 
            this.notesDesignerBox.Location = new System.Drawing.Point(119, 63);
            this.notesDesignerBox.Name = "notesDesignerBox";
            this.notesDesignerBox.Size = new System.Drawing.Size(178, 19);
            this.notesDesignerBox.TabIndex = 16;
            // 
            // difficultyDropDown
            // 
            this.difficultyDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.difficultyDropDown.FormattingEnabled = true;
            this.difficultyDropDown.Location = new System.Drawing.Point(178, 88);
            this.difficultyDropDown.Name = "difficultyDropDown";
            this.difficultyDropDown.Size = new System.Drawing.Size(119, 20);
            this.difficultyDropDown.TabIndex = 17;
            // 
            // levelDropDown
            // 
            this.levelDropDown.FormattingEnabled = true;
            this.levelDropDown.Location = new System.Drawing.Point(178, 114);
            this.levelDropDown.Name = "levelDropDown";
            this.levelDropDown.Size = new System.Drawing.Size(119, 20);
            this.levelDropDown.TabIndex = 18;
            // 
            // songIdBox
            // 
            this.songIdBox.Location = new System.Drawing.Point(119, 140);
            this.songIdBox.Name = "songIdBox";
            this.songIdBox.Size = new System.Drawing.Size(178, 19);
            this.songIdBox.TabIndex = 19;
            // 
            // soundFileBox
            // 
            this.soundFileBox.Location = new System.Drawing.Point(119, 165);
            this.soundFileBox.Name = "soundFileBox";
            this.soundFileBox.Size = new System.Drawing.Size(178, 19);
            this.soundFileBox.TabIndex = 20;
            // 
            // jacketFileBox
            // 
            this.jacketFileBox.Location = new System.Drawing.Point(119, 215);
            this.jacketFileBox.Name = "jacketFileBox";
            this.jacketFileBox.Size = new System.Drawing.Size(178, 19);
            this.jacketFileBox.TabIndex = 22;
            // 
            // outputBox
            // 
            this.outputBox.Location = new System.Drawing.Point(16, 289);
            this.outputBox.Name = "outputBox";
            this.outputBox.Size = new System.Drawing.Size(281, 19);
            this.outputBox.TabIndex = 24;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(303, 192);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(19, 12);
            this.label12.TabIndex = 23;
            this.label12.Text = "[s]";
            // 
            // exportButton
            // 
            this.exportButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.exportButton.Location = new System.Drawing.Point(16, 321);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(364, 23);
            this.exportButton.TabIndex = 26;
            this.exportButton.Text = "エクスポート";
            this.exportButton.UseVisualStyleBackColor = true;
            // 
            // hasPaddingBarBox
            // 
            this.hasPaddingBarBox.AutoSize = true;
            this.hasPaddingBarBox.Location = new System.Drawing.Point(124, 243);
            this.hasPaddingBarBox.Name = "hasPaddingBarBox";
            this.hasPaddingBarBox.Size = new System.Drawing.Size(173, 16);
            this.hasPaddingBarBox.TabIndex = 27;
            this.hasPaddingBarBox.Text = "先頭に1小節の空白を挿入する";
            this.hasPaddingBarBox.UseVisualStyleBackColor = true;
            // 
            // ExportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(398, 356);
            this.Controls.Add(this.hasPaddingBarBox);
            this.Controls.Add(this.exportButton);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.outputBox);
            this.Controls.Add(this.jacketFileBox);
            this.Controls.Add(this.soundFileBox);
            this.Controls.Add(this.songIdBox);
            this.Controls.Add(this.levelDropDown);
            this.Controls.Add(this.difficultyDropDown);
            this.Controls.Add(this.notesDesignerBox);
            this.Controls.Add(this.artistBox);
            this.Controls.Add(this.titleBox);
            this.Controls.Add(this.soundOffsetBox);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExportForm";
            this.Text = "エクスポート";
            ((System.ComponentModel.ISupportInitialize)(this.soundOffsetBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown soundOffsetBox;
        private System.Windows.Forms.TextBox titleBox;
        private System.Windows.Forms.TextBox artistBox;
        private System.Windows.Forms.TextBox notesDesignerBox;
        private System.Windows.Forms.ComboBox difficultyDropDown;
        private System.Windows.Forms.ComboBox levelDropDown;
        private System.Windows.Forms.TextBox songIdBox;
        private System.Windows.Forms.TextBox soundFileBox;
        private System.Windows.Forms.TextBox jacketFileBox;
        private System.Windows.Forms.TextBox outputBox;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.CheckBox hasPaddingBarBox;
    }
}