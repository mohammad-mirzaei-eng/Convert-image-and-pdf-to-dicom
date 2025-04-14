namespace Convert_to_dcm
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            btnselect = new Button();
            btn = new Button();
            txtpatientfamily = new TextBox();
            txtpatientId = new TextBox();
            label1 = new Label();
            label2 = new Label();
            notifyIcon1 = new NotifyIcon(components);
            contextMenuStrip1 = new ContextMenuStrip(components);
            SettingsToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            panel1 = new Panel();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // btnselect
            // 
            btnselect.Location = new Point(452, 472);
            btnselect.Name = "btnselect";
            btnselect.Size = new Size(75, 23);
            btnselect.TabIndex = 1;
            btnselect.Text = "Brows...";
            btnselect.UseVisualStyleBackColor = true;
            btnselect.Click += btnselect_Click;
            // 
            // btn
            // 
            btn.Location = new Point(216, 530);
            btn.Name = "btn";
            btn.Size = new Size(75, 23);
            btn.TabIndex = 2;
            btn.Text = "Send";
            btn.UseVisualStyleBackColor = true;
            btn.Click += btn_Click;
            // 
            // txtpatientfamily
            // 
            txtpatientfamily.Location = new Point(94, 501);
            txtpatientfamily.Name = "txtpatientfamily";
            txtpatientfamily.Size = new Size(238, 23);
            txtpatientfamily.TabIndex = 3;
            // 
            // txtpatientId
            // 
            txtpatientId.Location = new Point(94, 472);
            txtpatientId.Name = "txtpatientId";
            txtpatientId.Size = new Size(238, 23);
            txtpatientId.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(338, 475);
            label1.Name = "label1";
            label1.RightToLeft = RightToLeft.Yes;
            label1.Size = new Size(80, 15);
            label1.TabIndex = 5;
            label1.Text = "شماره مراجعه :";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(338, 504);
            label2.Name = "label2";
            label2.RightToLeft = RightToLeft.Yes;
            label2.Size = new Size(98, 15);
            label2.TabIndex = 6;
            label2.Text = "نام و نام خانوادگی:";
            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "Convert To Dicom";
            notifyIcon1.Visible = true;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { SettingsToolStripMenuItem, toolStripMenuItem1, aboutToolStripMenuItem, toolStripMenuItem2, exitToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(120, 82);
            // 
            // SettingsToolStripMenuItem
            // 
            SettingsToolStripMenuItem.Name = "SettingsToolStripMenuItem";
            SettingsToolStripMenuItem.Size = new Size(119, 22);
            SettingsToolStripMenuItem.Text = "&Settings ";
            SettingsToolStripMenuItem.Click += SettingsToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(116, 6);
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(119, 22);
            aboutToolStripMenuItem.Text = "&About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(116, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(119, 22);
            exitToolStripMenuItem.Text = "&Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel1.AutoScroll = true;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(574, 454);
            panel1.TabIndex = 7;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(575, 565);
            Controls.Add(panel1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtpatientId);
            Controls.Add(txtpatientfamily);
            Controls.Add(btn);
            Controls.Add(btnselect);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Convert To Dicom";
            Load += Main_Load;
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button btnselect;
        private Button btn;
        private TextBox txtpatientfamily;
        private TextBox txtpatientId;
        private Label label1;
        private Label label2;
        private NotifyIcon notifyIcon1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem SettingsToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private Panel panel1;
    }
}
