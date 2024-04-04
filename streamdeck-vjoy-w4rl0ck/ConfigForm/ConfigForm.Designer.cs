namespace streamdeck_vjoy_w4rl0ck.ConfigForm
{
    partial class ConfigForm
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
            label1 = new Label();
            vJoySelector = new ComboBox();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            axis1 = new ComboBox();
            axis2 = new ComboBox();
            label5 = new Label();
            axis3 = new ComboBox();
            label6 = new Label();
            axis4 = new ComboBox();
            label7 = new Label();
            axis8 = new ComboBox();
            label8 = new Label();
            axis7 = new ComboBox();
            label9 = new Label();
            axis6 = new ComboBox();
            label10 = new Label();
            axis5 = new ComboBox();
            label11 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 62);
            label1.Name = "label1";
            label1.Size = new Size(104, 25);
            label1.TabIndex = 0;
            label1.Text = "vJoy Device";
            // 
            // vJoySelector
            // 
            vJoySelector.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            vJoySelector.DropDownStyle = ComboBoxStyle.DropDownList;
            vJoySelector.FormattingEnabled = true;
            vJoySelector.Location = new Point(164, 59);
            vJoySelector.Name = "vJoySelector";
            vJoySelector.Size = new Size(300, 33);
            vJoySelector.TabIndex = 1;
            vJoySelector.SelectedIndexChanged += vJoySelector_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label2.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(13, 9);
            label2.Name = "label2";
            label2.Size = new Size(451, 32);
            label2.TabIndex = 2;
            label2.Text = "General Settings";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label3.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(13, 112);
            label3.Name = "label3";
            label3.Size = new Size(451, 32);
            label3.TabIndex = 3;
            label3.Text = "Axis Settings";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 161);
            label4.Name = "label4";
            label4.Size = new Size(23, 25);
            label4.TabIndex = 4;
            label4.Text = "X";
            // 
            // axis1
            // 
            axis1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            axis1.DropDownStyle = ComboBoxStyle.DropDownList;
            axis1.FormattingEnabled = true;
            axis1.Location = new Point(164, 158);
            axis1.Name = "axis1";
            axis1.Size = new Size(300, 33);
            axis1.TabIndex = 5;
            axis1.SelectedIndexChanged += axis_SelectedIndexChanged;
            // 
            // axis2
            // 
            axis2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            axis2.DropDownStyle = ComboBoxStyle.DropDownList;
            axis2.FormattingEnabled = true;
            axis2.Location = new Point(164, 197);
            axis2.Name = "axis2";
            axis2.Size = new Size(300, 33);
            axis2.TabIndex = 7;
            axis2.SelectedIndexChanged += axis_SelectedIndexChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 200);
            label5.Name = "label5";
            label5.Size = new Size(22, 25);
            label5.TabIndex = 6;
            label5.Text = "Y";
            // 
            // axis3
            // 
            axis3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            axis3.DropDownStyle = ComboBoxStyle.DropDownList;
            axis3.FormattingEnabled = true;
            axis3.Location = new Point(164, 236);
            axis3.Name = "axis3";
            axis3.Size = new Size(300, 33);
            axis3.TabIndex = 9;
            axis3.SelectedIndexChanged += axis_SelectedIndexChanged;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 239);
            label6.Name = "label6";
            label6.Size = new Size(22, 25);
            label6.TabIndex = 8;
            label6.Text = "Z";
            // 
            // axis4
            // 
            axis4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            axis4.DropDownStyle = ComboBoxStyle.DropDownList;
            axis4.FormattingEnabled = true;
            axis4.Location = new Point(164, 275);
            axis4.Name = "axis4";
            axis4.Size = new Size(300, 33);
            axis4.TabIndex = 11;
            axis4.SelectedIndexChanged += axis_SelectedIndexChanged;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(12, 278);
            label7.Name = "label7";
            label7.Size = new Size(31, 25);
            label7.TabIndex = 10;
            label7.Text = "Rx";
            // 
            // axis8
            // 
            axis8.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            axis8.DropDownStyle = ComboBoxStyle.DropDownList;
            axis8.FormattingEnabled = true;
            axis8.Location = new Point(164, 434);
            axis8.Name = "axis8";
            axis8.Size = new Size(300, 33);
            axis8.TabIndex = 19;
            axis8.SelectedIndexChanged += axis_SelectedIndexChanged;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(12, 437);
            label8.Name = "label8";
            label8.Size = new Size(34, 25);
            label8.TabIndex = 18;
            label8.Text = "sl1";
            // 
            // axis7
            // 
            axis7.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            axis7.DropDownStyle = ComboBoxStyle.DropDownList;
            axis7.FormattingEnabled = true;
            axis7.Location = new Point(164, 395);
            axis7.Name = "axis7";
            axis7.Size = new Size(300, 33);
            axis7.TabIndex = 17;
            axis7.SelectedIndexChanged += axis_SelectedIndexChanged;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(12, 398);
            label9.Name = "label9";
            label9.Size = new Size(34, 25);
            label9.TabIndex = 16;
            label9.Text = "sl0";
            // 
            // axis6
            // 
            axis6.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            axis6.DropDownStyle = ComboBoxStyle.DropDownList;
            axis6.FormattingEnabled = true;
            axis6.Location = new Point(164, 356);
            axis6.Name = "axis6";
            axis6.Size = new Size(300, 33);
            axis6.TabIndex = 15;
            axis6.SelectedIndexChanged += axis_SelectedIndexChanged;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(12, 359);
            label10.Name = "label10";
            label10.Size = new Size(31, 25);
            label10.TabIndex = 14;
            label10.Text = "Rz";
            // 
            // axis5
            // 
            axis5.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            axis5.DropDownStyle = ComboBoxStyle.DropDownList;
            axis5.FormattingEnabled = true;
            axis5.Location = new Point(164, 317);
            axis5.Name = "axis5";
            axis5.Size = new Size(300, 33);
            axis5.TabIndex = 13;
            axis5.SelectedIndexChanged += axis_SelectedIndexChanged;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(12, 320);
            label11.Name = "label11";
            label11.Size = new Size(32, 25);
            label11.TabIndex = 12;
            label11.Text = "Ry";
            // 
            // ConfigForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(476, 522);
            Controls.Add(axis8);
            Controls.Add(label8);
            Controls.Add(axis7);
            Controls.Add(label9);
            Controls.Add(axis6);
            Controls.Add(label10);
            Controls.Add(axis5);
            Controls.Add(label11);
            Controls.Add(axis4);
            Controls.Add(label7);
            Controls.Add(axis3);
            Controls.Add(label6);
            Controls.Add(axis2);
            Controls.Add(label5);
            Controls.Add(axis1);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(vJoySelector);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConfigForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Stream Deck vJoy Configuration";
            TopMost = true;
            FormClosed += ConfigForm_FormClosed;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox vJoySelector;
        private Label label2;
        private Label label3;
        private Label label4;
        private ComboBox axis1;
        private ComboBox axis2;
        private Label label5;
        private ComboBox axis3;
        private Label label6;
        private ComboBox axis4;
        private Label label7;
        private ComboBox axis8;
        private Label label8;
        private ComboBox axis7;
        private Label label9;
        private ComboBox axis6;
        private Label label10;
        private ComboBox axis5;
        private Label label11;
    }
}