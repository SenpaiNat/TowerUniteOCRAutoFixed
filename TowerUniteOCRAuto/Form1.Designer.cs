namespace TowerUniteOCRAuto
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.delayTimeTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.randomTimeTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.StartedStoppedLabel = new System.Windows.Forms.Label();
            this.keybindTextBox = new System.Windows.Forms.TextBox();
            this.bypassCounterLabel = new System.Windows.Forms.Label();
            this.previewButton = new System.Windows.Forms.Button();
            // NEW: Monitor selection controls
            this.label5 = new System.Windows.Forms.Label();
            this.monitorComboBox = new System.Windows.Forms.ComboBox();
            this.labelMonitorInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1 (unchanged)
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(34, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(182, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Currently Automatically Set To Space";
            // 
            // delayTimeTextBox
            // 
            this.delayTimeTextBox.Location = new System.Drawing.Point(118, 35);
            this.delayTimeTextBox.Name = "delayTimeTextBox";
            this.delayTimeTextBox.Size = new System.Drawing.Size(116, 20);
            this.delayTimeTextBox.TabIndex = 2;
            this.delayTimeTextBox.Text = "350";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Delay Time (ms)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(95, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Random Time (ms)";
            // 
            // randomTimeTextBox
            // 
            this.randomTimeTextBox.Location = new System.Drawing.Point(118, 61);
            this.randomTimeTextBox.Name = "randomTimeTextBox";
            this.randomTimeTextBox.Size = new System.Drawing.Size(116, 20);
            this.randomTimeTextBox.TabIndex = 4;
            this.randomTimeTextBox.Text = "150";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(17, 92);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(103, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Keybind (Start/Stop)";
            // 
            // StartedStoppedLabel
            // 
            this.StartedStoppedLabel.AutoSize = true;
            this.StartedStoppedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StartedStoppedLabel.Location = new System.Drawing.Point(79, 124);
            this.StartedStoppedLabel.Name = "StartedStoppedLabel";
            this.StartedStoppedLabel.Size = new System.Drawing.Size(81, 24);
            this.StartedStoppedLabel.TabIndex = 8;
            this.StartedStoppedLabel.Text = "Stopped";
            // 
            // keybindTextBox
            // 
            this.keybindTextBox.Location = new System.Drawing.Point(118, 89);
            this.keybindTextBox.Name = "keybindTextBox";
            this.keybindTextBox.Size = new System.Drawing.Size(116, 20);
            this.keybindTextBox.TabIndex = 9;
            this.keybindTextBox.Text = "None";
            this.keybindTextBox.TextChanged += new System.EventHandler(this.keybindTextBox_TextChanged);
            this.keybindTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.keybindTextBox_KeyDown);
            // 
            // bypassCounterLabel
            // 
            this.bypassCounterLabel.AutoSize = true;
            this.bypassCounterLabel.Location = new System.Drawing.Point(79, 155);
            this.bypassCounterLabel.Name = "bypassCounterLabel";
            this.bypassCounterLabel.Size = new System.Drawing.Size(65, 13);
            this.bypassCounterLabel.TabIndex = 10;
            this.bypassCounterLabel.Text = "Bypasses: 0";
            // 
            // previewButton
            // 
            this.previewButton.Location = new System.Drawing.Point(79, 175);
            this.previewButton.Name = "previewButton";
            this.previewButton.Size = new System.Drawing.Size(100, 23);
            this.previewButton.TabIndex = 11;
            this.previewButton.Text = "Open Binary Preview";
            this.previewButton.UseVisualStyleBackColor = true;
            this.previewButton.Click += new System.EventHandler(this.previewButton_Click);
            // 
            // label5 (NEW)
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 204);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Monitor Index:";
            // 
            // monitorComboBox (NEW, replaces NumericUpDown)
            // 
            this.monitorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.monitorComboBox.FormattingEnabled = true;
            this.monitorComboBox.Location = new System.Drawing.Point(118, 201);
            this.monitorComboBox.Name = "monitorComboBox";
            this.monitorComboBox.Size = new System.Drawing.Size(45, 21);
            this.monitorComboBox.TabIndex = 13;
            // 
            // labelMonitorInfo (NEW)
            // 
            this.labelMonitorInfo.AutoSize = true;
            this.labelMonitorInfo.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular);
            this.labelMonitorInfo.Location = new System.Drawing.Point(115, 225);
            this.labelMonitorInfo.Name = "labelMonitorInfo";
            this.labelMonitorInfo.Size = new System.Drawing.Size(135, 12);
            this.labelMonitorInfo.TabIndex = 14;
            this.labelMonitorInfo.Text = "0 = Primary, 1 = Second, etc.";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(252, 250);  // increased height to 250
            this.Controls.Add(this.labelMonitorInfo);
            this.Controls.Add(this.monitorComboBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.previewButton);
            this.Controls.Add(this.bypassCounterLabel);
            this.Controls.Add(this.keybindTextBox);
            this.Controls.Add(this.StartedStoppedLabel);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.randomTimeTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.delayTimeTextBox);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "Form1";
            this.Text = "TowerUniteOCRAuto";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // Designer fields (all controls)
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox delayTimeTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox randomTimeTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label StartedStoppedLabel;
        private System.Windows.Forms.TextBox keybindTextBox;
        private System.Windows.Forms.Label bypassCounterLabel;
        private System.Windows.Forms.Button previewButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox monitorComboBox;
        private System.Windows.Forms.Label labelMonitorInfo;
    }
}