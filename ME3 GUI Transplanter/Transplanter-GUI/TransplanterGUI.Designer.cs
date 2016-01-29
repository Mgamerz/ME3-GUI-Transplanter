namespace Transplanter_GUI
{
    partial class TransplanterGUI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TransplanterGUI));
            this.transplantButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.srcfileBrowseButton = new System.Windows.Forms.Button();
            this.srcTextField = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.destTextField = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.statusLabel = new System.Windows.Forms.Label();
            this.srcFileChooser = new System.Windows.Forms.OpenFileDialog();
            this.destFileChooser = new System.Windows.Forms.OpenFileDialog();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // transplantButton
            // 
            this.transplantButton.Location = new System.Drawing.Point(396, 118);
            this.transplantButton.Name = "transplantButton";
            this.transplantButton.Size = new System.Drawing.Size(85, 23);
            this.transplantButton.TabIndex = 2;
            this.transplantButton.Text = "Transplant";
            this.transplantButton.UseVisualStyleBackColor = true;
            this.transplantButton.Click += new System.EventHandler(this.transplantButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.srcfileBrowseButton);
            this.groupBox1.Controls.Add(this.srcTextField);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(469, 47);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Source File";
            // 
            // srcfileBrowseButton
            // 
            this.srcfileBrowseButton.Location = new System.Drawing.Point(384, 17);
            this.srcfileBrowseButton.Name = "srcfileBrowseButton";
            this.srcfileBrowseButton.Size = new System.Drawing.Size(75, 23);
            this.srcfileBrowseButton.TabIndex = 1;
            this.srcfileBrowseButton.Text = "Browse...";
            this.srcfileBrowseButton.UseVisualStyleBackColor = true;
            this.srcfileBrowseButton.Click += new System.EventHandler(this.srcfileBrowseButton_Click);
            // 
            // srcTextField
            // 
            this.srcTextField.Location = new System.Drawing.Point(6, 19);
            this.srcTextField.Name = "srcTextField";
            this.srcTextField.Size = new System.Drawing.Size(372, 20);
            this.srcTextField.TabIndex = 3;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.destTextField);
            this.groupBox2.Location = new System.Drawing.Point(12, 65);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(469, 47);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Destination File";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(384, 17);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Browse...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // destTextField
            // 
            this.destTextField.Location = new System.Drawing.Point(6, 19);
            this.destTextField.Name = "destTextField";
            this.destTextField.Size = new System.Drawing.Size(372, 20);
            this.destTextField.TabIndex = 3;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(183, 118);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(207, 23);
            this.progressBar1.TabIndex = 9;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(12, 123);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(168, 13);
            this.statusLabel.TabIndex = 10;
            this.statusLabel.Text = "Select source and destination files";
            // 
            // srcFileChooser
            // 
            this.srcFileChooser.Filter = "Mass Effect 3 PCC Files|*.pcc";
            this.srcFileChooser.InitialDirectory = "Desktop";
            this.srcFileChooser.Title = "Select source file to extract from";
            // 
            // destFileChooser
            // 
            this.destFileChooser.Filter = "Mass Effect 3 PCC Files|*.pcc";
            this.destFileChooser.InitialDirectory = "Desktop";
            this.destFileChooser.Title = "Select file to inject into";
            // 
            // TransplanterGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(501, 155);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.transplantButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TransplanterGUI";
            this.Text = "ME3 GFX Transplanter by FemShep";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button transplantButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button srcfileBrowseButton;
        private System.Windows.Forms.TextBox srcTextField;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox destTextField;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.OpenFileDialog srcFileChooser;
        private System.Windows.Forms.OpenFileDialog destFileChooser;
    }
}

