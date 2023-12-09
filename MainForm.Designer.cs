namespace CockpitHardwareHUB_v2
{
    partial class MainForm
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
            grpConnect = new GroupBox();
            btnConnect = new Button();
            grpLogging = new GroupBox();
            dgvLogging = new DataGridView();
            btnLoggingClear = new Button();
            cbLogLevel = new ComboBox();
            lblLoggingLevel = new Label();
            txtLoggingFilter = new TextBox();
            lblLoggingFilter = new Label();
            grpDevices = new GroupBox();
            grpConnect.SuspendLayout();
            grpLogging.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLogging).BeginInit();
            SuspendLayout();
            // 
            // grpConnect
            // 
            grpConnect.Controls.Add(btnConnect);
            grpConnect.Location = new Point(13, 13);
            grpConnect.Name = "grpConnect";
            grpConnect.Size = new Size(285, 49);
            grpConnect.TabIndex = 0;
            grpConnect.TabStop = false;
            grpConnect.Text = "MSFS2020 : DISCONNECTED";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(6, 19);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(272, 23);
            btnConnect.TabIndex = 0;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // grpLogging
            // 
            grpLogging.Controls.Add(dgvLogging);
            grpLogging.Controls.Add(btnLoggingClear);
            grpLogging.Controls.Add(cbLogLevel);
            grpLogging.Controls.Add(lblLoggingLevel);
            grpLogging.Controls.Add(txtLoggingFilter);
            grpLogging.Controls.Add(lblLoggingFilter);
            grpLogging.Location = new Point(305, 355);
            grpLogging.Name = "grpLogging";
            grpLogging.Size = new Size(715, 233);
            grpLogging.TabIndex = 1;
            grpLogging.TabStop = false;
            grpLogging.Text = "Logging";
            // 
            // dgvLogging
            // 
            dgvLogging.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLogging.Location = new Point(6, 45);
            dgvLogging.Name = "dgvLogging";
            dgvLogging.RowTemplate.Height = 25;
            dgvLogging.Size = new Size(703, 181);
            dgvLogging.TabIndex = 6;
            // 
            // btnLoggingClear
            // 
            btnLoggingClear.Location = new Point(634, 16);
            btnLoggingClear.Name = "btnLoggingClear";
            btnLoggingClear.Size = new Size(75, 23);
            btnLoggingClear.TabIndex = 4;
            btnLoggingClear.Text = "Clear Log";
            btnLoggingClear.UseVisualStyleBackColor = true;
            btnLoggingClear.Click += btnLoggingClear_Click;
            // 
            // cbLogLevel
            // 
            cbLogLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLogLevel.FormattingEnabled = true;
            cbLogLevel.Items.AddRange(new object[] { "None", "Critical", "Error", "Warning", "Info", "Debug", "Trace" });
            cbLogLevel.Location = new Point(287, 16);
            cbLogLevel.Name = "cbLogLevel";
            cbLogLevel.Size = new Size(73, 23);
            cbLogLevel.TabIndex = 3;
            cbLogLevel.SelectionChangeCommitted += cbLogLevel_SelectionChangeCommitted;
            // 
            // lblLoggingLevel
            // 
            lblLoggingLevel.AutoSize = true;
            lblLoggingLevel.Location = new Point(224, 19);
            lblLoggingLevel.Name = "lblLoggingLevel";
            lblLoggingLevel.Size = new Size(57, 15);
            lblLoggingLevel.TabIndex = 2;
            lblLoggingLevel.Text = "Log level:";
            // 
            // txtLoggingFilter
            // 
            txtLoggingFilter.Location = new Point(48, 16);
            txtLoggingFilter.Name = "txtLoggingFilter";
            txtLoggingFilter.Size = new Size(170, 23);
            txtLoggingFilter.TabIndex = 1;
            txtLoggingFilter.TextChanged += txtLoggingFilter_TextChanged;
            // 
            // lblLoggingFilter
            // 
            lblLoggingFilter.AutoSize = true;
            lblLoggingFilter.Location = new Point(6, 19);
            lblLoggingFilter.Name = "lblLoggingFilter";
            lblLoggingFilter.Size = new Size(36, 15);
            lblLoggingFilter.TabIndex = 0;
            lblLoggingFilter.Text = "Filter:";
            // 
            // grpDevices
            // 
            grpDevices.Location = new Point(13, 68);
            grpDevices.Name = "grpDevices";
            grpDevices.Size = new Size(285, 520);
            grpDevices.TabIndex = 2;
            grpDevices.TabStop = false;
            grpDevices.Text = "USB Devices";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1032, 593);
            Controls.Add(grpDevices);
            Controls.Add(grpLogging);
            Controls.Add(grpConnect);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximumSize = new Size(1048, 632);
            MinimumSize = new Size(1048, 632);
            Name = "MainForm";
            StartPosition = FormStartPosition.Manual;
            Text = "Cockpit Hardware HUB v2 - ";
            FormClosed += MainForm_FormClosed;
            Load += MainForm_Load;
            grpConnect.ResumeLayout(false);
            grpLogging.ResumeLayout(false);
            grpLogging.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLogging).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox grpConnect;
        private Button btnConnect;
        private GroupBox grpLogging;
        private TextBox txtLoggingFilter;
        private Label lblLoggingFilter;
        private ComboBox cbLogLevel;
        private Label lblLoggingLevel;
        private Button btnLoggingClear;
        private DataGridView dgvLogging;
        private GroupBox grpDevices;
    }
}