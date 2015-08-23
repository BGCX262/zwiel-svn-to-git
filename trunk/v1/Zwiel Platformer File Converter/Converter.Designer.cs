namespace Zwiel_Platformer_File_Converter
{
    partial class Converter
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Converter));
            this.textBoxToConvertPath = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.textBoxWorldName = new System.Windows.Forms.TextBox();
            this.textBoxLevelName = new System.Windows.Forms.TextBox();
            this.buttonConvert = new System.Windows.Forms.Button();
            this.textBoxTimeAllotted = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBoxToConvertPath
            // 
            this.textBoxToConvertPath.Enabled = false;
            this.textBoxToConvertPath.Location = new System.Drawing.Point(12, 75);
            this.textBoxToConvertPath.Name = "textBoxToConvertPath";
            this.textBoxToConvertPath.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBoxToConvertPath.Size = new System.Drawing.Size(409, 20);
            this.textBoxToConvertPath.TabIndex = 0;
            this.textBoxToConvertPath.Text = "File Path";
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(427, 75);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(145, 20);
            this.buttonBrowse.TabIndex = 1;
            this.buttonBrowse.Text = "Browse";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // textBoxWorldName
            // 
            this.textBoxWorldName.Location = new System.Drawing.Point(12, 101);
            this.textBoxWorldName.MaxLength = 32;
            this.textBoxWorldName.Name = "textBoxWorldName";
            this.textBoxWorldName.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBoxWorldName.Size = new System.Drawing.Size(282, 20);
            this.textBoxWorldName.TabIndex = 2;
            this.textBoxWorldName.Text = "World Name";
            // 
            // textBoxLevelName
            // 
            this.textBoxLevelName.Location = new System.Drawing.Point(12, 127);
            this.textBoxLevelName.MaxLength = 32;
            this.textBoxLevelName.Name = "textBoxLevelName";
            this.textBoxLevelName.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.textBoxLevelName.Size = new System.Drawing.Size(282, 20);
            this.textBoxLevelName.TabIndex = 3;
            this.textBoxLevelName.Text = "Level Name";
            // 
            // buttonConvert
            // 
            this.buttonConvert.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonConvert.Location = new System.Drawing.Point(300, 101);
            this.buttonConvert.Name = "buttonConvert";
            this.buttonConvert.Size = new System.Drawing.Size(272, 46);
            this.buttonConvert.TabIndex = 4;
            this.buttonConvert.Text = "Convert";
            this.buttonConvert.UseVisualStyleBackColor = true;
            this.buttonConvert.Click += new System.EventHandler(this.buttonConvert_Click);
            // 
            // textBoxTimeAllotted
            // 
            this.textBoxTimeAllotted.Location = new System.Drawing.Point(12, 153);
            this.textBoxTimeAllotted.MaxLength = 3;
            this.textBoxTimeAllotted.Name = "textBoxTimeAllotted";
            this.textBoxTimeAllotted.Size = new System.Drawing.Size(282, 20);
            this.textBoxTimeAllotted.TabIndex = 5;
            this.textBoxTimeAllotted.Text = "Time to Complete Level (sec)";
            // 
            // Converter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Silver;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ClientSize = new System.Drawing.Size(584, 332);
            this.Controls.Add(this.textBoxTimeAllotted);
            this.Controls.Add(this.buttonConvert);
            this.Controls.Add(this.textBoxLevelName);
            this.Controls.Add(this.textBoxWorldName);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.textBoxToConvertPath);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Converter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Zwiel Platformer File Converter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxToConvertPath;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.TextBox textBoxWorldName;
        private System.Windows.Forms.TextBox textBoxLevelName;
        private System.Windows.Forms.Button buttonConvert;
        private System.Windows.Forms.TextBox textBoxTimeAllotted;
    }
}

