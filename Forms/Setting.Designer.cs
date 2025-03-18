namespace Convert_to_dcm
{
    partial class Setting
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
            components = new System.ComponentModel.Container();
            ipservertxt = new IPTextBox();
            label1 = new Label();
            btnSaveSetings = new Button();
            panel1 = new Panel();
            label10 = new Label();
            label9 = new Label();
            comboModality = new ComboBox();
            tlschk = new CheckBox();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            portserver = new NumericText();
            txtAET = new TextBox();
            titletxt = new TextBox();
            destxt = new TextBox();
            panel2 = new Panel();
            label8 = new Label();
            txtCatalog = new TextBox();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            txtPassword = new TextBox();
            txtUsername = new TextBox();
            txtInstance = new TextBox();
            toolTips = new ToolTip(components);
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // ipservertxt
            // 
            ipservertxt.Location = new Point(4, 12);
            ipservertxt.Margin = new Padding(4, 3, 4, 3);
            ipservertxt.MaxLength = 15;
            ipservertxt.Name = "ipservertxt";
            ipservertxt.Size = new Size(115, 23);
            ipservertxt.TabIndex = 0;
            ipservertxt.Text = "000.000.000.000";
            toolTips.SetToolTip(ipservertxt, "آدرس سرور پکس");
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(129, 15);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(61, 15);
            label1.TabIndex = 1;
            label1.Text = "آدرس سرور";
            // 
            // btnSaveSetings
            // 
            btnSaveSetings.Dock = DockStyle.Bottom;
            btnSaveSetings.Location = new Point(0, 359);
            btnSaveSetings.Margin = new Padding(4, 3, 4, 3);
            btnSaveSetings.Name = "btnSaveSetings";
            btnSaveSetings.Size = new Size(211, 33);
            btnSaveSetings.TabIndex = 1;
            btnSaveSetings.Text = "ذخیره";
            toolTips.SetToolTip(btnSaveSetings, "ذخیره سازی");
            btnSaveSetings.UseVisualStyleBackColor = true;
            btnSaveSetings.Click += btnSaveSetings_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(label10);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(comboModality);
            panel1.Controls.Add(tlschk);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(portserver);
            panel1.Controls.Add(txtAET);
            panel1.Controls.Add(titletxt);
            panel1.Controls.Add(destxt);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 161);
            panel1.Name = "panel1";
            panel1.Size = new Size(211, 198);
            panel1.TabIndex = 6;
            toolTips.SetToolTip(panel1, "تنظیمات مربوط به سرور پکس");
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(127, 68);
            label10.Margin = new Padding(4, 0, 4, 0);
            label10.Name = "label10";
            label10.Size = new Size(59, 15);
            label10.TabIndex = 15;
            label10.Text = "ServerAET";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(127, 97);
            label9.Margin = new Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new Size(54, 15);
            label9.TabIndex = 15;
            label9.Text = "Modality";
            // 
            // comboModality
            // 
            comboModality.FormattingEnabled = true;
            comboModality.Location = new Point(4, 93);
            comboModality.Name = "comboModality";
            comboModality.Size = new Size(116, 23);
            comboModality.TabIndex = 3;
            // 
            // tlschk
            // 
            tlschk.AutoSize = true;
            tlschk.Location = new Point(22, 173);
            tlschk.Margin = new Padding(4, 3, 4, 3);
            tlschk.Name = "tlschk";
            tlschk.Size = new Size(62, 19);
            tlschk.TabIndex = 5;
            tlschk.Text = "Use Tls";
            tlschk.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(123, 137);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(54, 15);
            label4.TabIndex = 12;
            label4.Text = "توضیحات";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(128, 39);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(29, 15);
            label3.TabIndex = 13;
            label3.Text = "Title";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(128, 10);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(32, 15);
            label2.TabIndex = 11;
            label2.Text = "پورت";
            // 
            // portserver
            // 
            portserver.Location = new Point(4, 6);
            portserver.Margin = new Padding(4, 3, 4, 3);
            portserver.Name = "portserver";
            portserver.Size = new Size(116, 23);
            portserver.TabIndex = 0;
            // 
            // txtAET
            // 
            txtAET.Location = new Point(3, 64);
            txtAET.Margin = new Padding(4, 3, 4, 3);
            txtAET.Name = "txtAET";
            txtAET.Size = new Size(116, 23);
            txtAET.TabIndex = 2;
            // 
            // titletxt
            // 
            titletxt.Location = new Point(4, 35);
            titletxt.Margin = new Padding(4, 3, 4, 3);
            titletxt.Name = "titletxt";
            titletxt.Size = new Size(116, 23);
            titletxt.TabIndex = 1;
            // 
            // destxt
            // 
            destxt.Location = new Point(4, 122);
            destxt.Margin = new Padding(4, 3, 4, 3);
            destxt.Multiline = true;
            destxt.Name = "destxt";
            destxt.Size = new Size(116, 45);
            destxt.TabIndex = 4;
            // 
            // panel2
            // 
            panel2.Controls.Add(label8);
            panel2.Controls.Add(txtCatalog);
            panel2.Controls.Add(label7);
            panel2.Controls.Add(label6);
            panel2.Controls.Add(label5);
            panel2.Controls.Add(txtPassword);
            panel2.Controls.Add(txtUsername);
            panel2.Controls.Add(txtInstance);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 46);
            panel2.Name = "panel2";
            panel2.Size = new Size(211, 115);
            panel2.TabIndex = 7;
            toolTips.SetToolTip(panel2, "تنظیمات مربوط به دیتابیس پکس");
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(129, 94);
            label8.Name = "label8";
            label8.Size = new Size(48, 15);
            label8.TabIndex = 20;
            label8.Text = "Catalog";
            // 
            // txtCatalog
            // 
            txtCatalog.ForeColor = SystemColors.WindowText;
            txtCatalog.Location = new Point(3, 90);
            txtCatalog.Name = "txtCatalog";
            txtCatalog.Size = new Size(116, 23);
            txtCatalog.TabIndex = 3;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(129, 65);
            label7.Name = "label7";
            label7.Size = new Size(37, 15);
            label7.TabIndex = 18;
            label7.Text = "پسورد";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(129, 36);
            label6.Name = "label6";
            label6.Size = new Size(56, 15);
            label6.TabIndex = 17;
            label6.Text = "نام کاروری";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(125, 7);
            label5.Name = "label5";
            label5.Size = new Size(51, 15);
            label5.TabIndex = 16;
            label5.Text = "Instance";
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(3, 61);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(116, 23);
            txtPassword.TabIndex = 2;
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(3, 32);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(116, 23);
            txtUsername.TabIndex = 1;
            // 
            // txtInstance
            // 
            txtInstance.Location = new Point(3, 3);
            txtInstance.Name = "txtInstance";
            txtInstance.Size = new Size(116, 23);
            txtInstance.TabIndex = 0;
            // 
            // Setting
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(211, 392);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(btnSaveSetings);
            Controls.Add(label1);
            Controls.Add(ipservertxt);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Setting";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Seting";
            Load += Setting_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private IPTextBox ipservertxt;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSaveSetings;
        private Panel panel1;
        private CheckBox tlschk;
        private Label label4;
        private Label label3;
        private Label label2;
        private NumericText portserver;
        private TextBox titletxt;
        private TextBox destxt;
        private Panel panel2;
        private Label label7;
        private Label label6;
        private Label label5;
        private TextBox txtPassword;
        private TextBox txtUsername;
        private TextBox txtInstance;
        private ToolTip toolTips;
        private Label label8;
        private TextBox txtCatalog;
        private Label label9;
        private ComboBox comboModality;
        private Label label10;
        private TextBox txtAET;
    }
}