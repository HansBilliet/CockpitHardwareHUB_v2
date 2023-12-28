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
            lvLogging = new ListView();
            txtLogFileName = new TextBox();
            cbLogToFile = new CheckBox();
            btnLoggingClear = new Button();
            cbLogLevel = new ComboBox();
            lblLoggingLevel = new Label();
            txtLoggingFilter = new TextBox();
            lblLoggingFilter = new Label();
            grpDevices = new GroupBox();
            btnResetStatistics = new Button();
            txtProperties = new PropertyTextBox();
            lblProperties = new Label();
            lblNackCntValue = new Label();
            lblCmdTxCntValue = new Label();
            lblCmdRxCntValue = new Label();
            lblDevicePathValue = new Label();
            lblProcessorTypeValue = new Label();
            lblDeviceNameValue = new Label();
            lblNackCnt = new Label();
            lblCmdTxCnt = new Label();
            lblCmdRxCnt = new Label();
            lblPath = new Label();
            lblProcessorType = new Label();
            lblDeviceName = new Label();
            cbDevices = new ComboBox();
            grpVariables = new GroupBox();
            lvVariables = new ListView();
            cbRW = new ComboBox();
            lblRW = new Label();
            txtVariablesFilter = new TextBox();
            lblVariablesFilter = new Label();
            grpConnect.SuspendLayout();
            grpLogging.SuspendLayout();
            grpDevices.SuspendLayout();
            grpVariables.SuspendLayout();
            SuspendLayout();
            // 
            // grpConnect
            // 
            grpConnect.Controls.Add(btnConnect);
            grpConnect.Location = new Point(13, 13);
            grpConnect.Margin = new Padding(4, 3, 4, 3);
            grpConnect.Name = "grpConnect";
            grpConnect.Padding = new Padding(4, 3, 4, 3);
            grpConnect.Size = new Size(285, 48);
            grpConnect.TabIndex = 0;
            grpConnect.TabStop = false;
            grpConnect.Text = "MSFS2020 : DISCONNECTED";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(6, 18);
            btnConnect.Margin = new Padding(4, 3, 4, 3);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(272, 23);
            btnConnect.TabIndex = 0;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // grpLogging
            // 
            grpLogging.Controls.Add(lvLogging);
            grpLogging.Controls.Add(txtLogFileName);
            grpLogging.Controls.Add(cbLogToFile);
            grpLogging.Controls.Add(btnLoggingClear);
            grpLogging.Controls.Add(cbLogLevel);
            grpLogging.Controls.Add(lblLoggingLevel);
            grpLogging.Controls.Add(txtLoggingFilter);
            grpLogging.Controls.Add(lblLoggingFilter);
            grpLogging.Location = new Point(306, 342);
            grpLogging.Margin = new Padding(4, 3, 4, 3);
            grpLogging.Name = "grpLogging";
            grpLogging.Padding = new Padding(4, 3, 4, 3);
            grpLogging.Size = new Size(720, 246);
            grpLogging.TabIndex = 1;
            grpLogging.TabStop = false;
            grpLogging.Text = "Logging";
            // 
            // lvLogging
            // 
            lvLogging.GridLines = true;
            lvLogging.Location = new Point(10, 74);
            lvLogging.Name = "lvLogging";
            lvLogging.Size = new Size(704, 165);
            lvLogging.TabIndex = 4;
            lvLogging.UseCompatibleStateImageBehavior = false;
            lvLogging.View = View.Details;
            // 
            // txtLogFileName
            // 
            txtLogFileName.Location = new Point(94, 45);
            txtLogFileName.Margin = new Padding(4, 3, 4, 3);
            txtLogFileName.Name = "txtLogFileName";
            txtLogFileName.ReadOnly = true;
            txtLogFileName.Size = new Size(618, 23);
            txtLogFileName.TabIndex = 8;
            // 
            // cbLogToFile
            // 
            cbLogToFile.AutoSize = true;
            cbLogToFile.Location = new Point(8, 47);
            cbLogToFile.Margin = new Padding(4, 3, 4, 3);
            cbLogToFile.Name = "cbLogToFile";
            cbLogToFile.Size = new Size(79, 19);
            cbLogToFile.TabIndex = 7;
            cbLogToFile.Text = "Log to file";
            cbLogToFile.UseVisualStyleBackColor = true;
            cbLogToFile.CheckedChanged += cbLogToFile_CheckedChanged;
            // 
            // btnLoggingClear
            // 
            btnLoggingClear.Location = new Point(637, 16);
            btnLoggingClear.Margin = new Padding(4, 3, 4, 3);
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
            cbLogLevel.Location = new Point(295, 16);
            cbLogLevel.Margin = new Padding(4, 3, 4, 3);
            cbLogLevel.Name = "cbLogLevel";
            cbLogLevel.Size = new Size(73, 23);
            cbLogLevel.TabIndex = 3;
            cbLogLevel.SelectionChangeCommitted += cbLogLevel_SelectionChangeCommitted;
            // 
            // lblLoggingLevel
            // 
            lblLoggingLevel.AutoSize = true;
            lblLoggingLevel.Location = new Point(230, 19);
            lblLoggingLevel.Margin = new Padding(4, 0, 4, 0);
            lblLoggingLevel.Name = "lblLoggingLevel";
            lblLoggingLevel.Size = new Size(57, 15);
            lblLoggingLevel.TabIndex = 2;
            lblLoggingLevel.Text = "Log level:";
            // 
            // txtLoggingFilter
            // 
            txtLoggingFilter.Location = new Point(52, 16);
            txtLoggingFilter.Margin = new Padding(4, 3, 4, 3);
            txtLoggingFilter.Name = "txtLoggingFilter";
            txtLoggingFilter.Size = new Size(170, 23);
            txtLoggingFilter.TabIndex = 1;
            txtLoggingFilter.KeyDown += txtLoggingFilter_KeyDown;
            // 
            // lblLoggingFilter
            // 
            lblLoggingFilter.AutoSize = true;
            lblLoggingFilter.Location = new Point(8, 19);
            lblLoggingFilter.Margin = new Padding(4, 0, 4, 0);
            lblLoggingFilter.Name = "lblLoggingFilter";
            lblLoggingFilter.Size = new Size(36, 15);
            lblLoggingFilter.TabIndex = 0;
            lblLoggingFilter.Text = "Filter:";
            // 
            // grpDevices
            // 
            grpDevices.Controls.Add(btnResetStatistics);
            grpDevices.Controls.Add(txtProperties);
            grpDevices.Controls.Add(lblProperties);
            grpDevices.Controls.Add(lblNackCntValue);
            grpDevices.Controls.Add(lblCmdTxCntValue);
            grpDevices.Controls.Add(lblCmdRxCntValue);
            grpDevices.Controls.Add(lblDevicePathValue);
            grpDevices.Controls.Add(lblProcessorTypeValue);
            grpDevices.Controls.Add(lblDeviceNameValue);
            grpDevices.Controls.Add(lblNackCnt);
            grpDevices.Controls.Add(lblCmdTxCnt);
            grpDevices.Controls.Add(lblCmdRxCnt);
            grpDevices.Controls.Add(lblPath);
            grpDevices.Controls.Add(lblProcessorType);
            grpDevices.Controls.Add(lblDeviceName);
            grpDevices.Controls.Add(cbDevices);
            grpDevices.Location = new Point(13, 68);
            grpDevices.Margin = new Padding(4, 3, 4, 3);
            grpDevices.Name = "grpDevices";
            grpDevices.Padding = new Padding(4, 3, 4, 3);
            grpDevices.Size = new Size(285, 520);
            grpDevices.TabIndex = 2;
            grpDevices.TabStop = false;
            grpDevices.Text = "USB Devices";
            // 
            // btnResetStatistics
            // 
            btnResetStatistics.Location = new Point(223, 110);
            btnResetStatistics.Margin = new Padding(4, 3, 4, 3);
            btnResetStatistics.Name = "btnResetStatistics";
            btnResetStatistics.Size = new Size(56, 47);
            btnResetStatistics.TabIndex = 15;
            btnResetStatistics.Text = "Reset Statistics";
            btnResetStatistics.UseVisualStyleBackColor = true;
            btnResetStatistics.Click += btnResetStatistics_Click;
            // 
            // txtProperties
            // 
            txtProperties.BackColor = SystemColors.Window;
            txtProperties.Location = new Point(6, 219);
            txtProperties.Margin = new Padding(4, 3, 4, 3);
            txtProperties.Multiline = true;
            txtProperties.Name = "txtProperties";
            txtProperties.ReadOnly = true;
            txtProperties.ScrollBars = ScrollBars.Both;
            txtProperties.Size = new Size(272, 294);
            txtProperties.TabIndex = 14;
            txtProperties.WordWrap = false;
            // 
            // lblProperties
            // 
            lblProperties.AutoSize = true;
            lblProperties.Location = new Point(6, 204);
            lblProperties.Margin = new Padding(4, 0, 4, 0);
            lblProperties.Name = "lblProperties";
            lblProperties.Size = new Size(63, 15);
            lblProperties.TabIndex = 13;
            lblProperties.Text = "Properties:";
            // 
            // lblNackCntValue
            // 
            lblNackCntValue.AutoSize = true;
            lblNackCntValue.Location = new Point(97, 144);
            lblNackCntValue.Margin = new Padding(4, 0, 4, 0);
            lblNackCntValue.Name = "lblNackCntValue";
            lblNackCntValue.Size = new Size(13, 15);
            lblNackCntValue.TabIndex = 12;
            lblNackCntValue.Text = "0";
            // 
            // lblCmdTxCntValue
            // 
            lblCmdTxCntValue.AutoSize = true;
            lblCmdTxCntValue.Location = new Point(97, 127);
            lblCmdTxCntValue.Margin = new Padding(4, 0, 4, 0);
            lblCmdTxCntValue.Name = "lblCmdTxCntValue";
            lblCmdTxCntValue.Size = new Size(13, 15);
            lblCmdTxCntValue.TabIndex = 11;
            lblCmdTxCntValue.Text = "0";
            // 
            // lblCmdRxCntValue
            // 
            lblCmdRxCntValue.AutoSize = true;
            lblCmdRxCntValue.Location = new Point(97, 110);
            lblCmdRxCntValue.Margin = new Padding(4, 0, 4, 0);
            lblCmdRxCntValue.Name = "lblCmdRxCntValue";
            lblCmdRxCntValue.Size = new Size(13, 15);
            lblCmdRxCntValue.TabIndex = 10;
            lblCmdRxCntValue.Text = "0";
            // 
            // lblDevicePathValue
            // 
            lblDevicePathValue.AutoSize = true;
            lblDevicePathValue.Location = new Point(97, 85);
            lblDevicePathValue.Margin = new Padding(4, 0, 4, 0);
            lblDevicePathValue.Name = "lblDevicePathValue";
            lblDevicePathValue.Size = new Size(94, 15);
            lblDevicePathValue.TabIndex = 9;
            lblDevicePathValue.Text = "DevicePathValue";
            // 
            // lblProcessorTypeValue
            // 
            lblProcessorTypeValue.AutoSize = true;
            lblProcessorTypeValue.Location = new Point(97, 68);
            lblProcessorTypeValue.Margin = new Padding(4, 0, 4, 0);
            lblProcessorTypeValue.Name = "lblProcessorTypeValue";
            lblProcessorTypeValue.Size = new Size(110, 15);
            lblProcessorTypeValue.TabIndex = 8;
            lblProcessorTypeValue.Text = "ProcessorTypeValue";
            // 
            // lblDeviceNameValue
            // 
            lblDeviceNameValue.AutoSize = true;
            lblDeviceNameValue.Location = new Point(97, 51);
            lblDeviceNameValue.Margin = new Padding(4, 0, 4, 0);
            lblDeviceNameValue.Name = "lblDeviceNameValue";
            lblDeviceNameValue.Size = new Size(102, 15);
            lblDeviceNameValue.TabIndex = 7;
            lblDeviceNameValue.Text = "DeviceNameValue";
            // 
            // lblNackCnt
            // 
            lblNackCnt.AutoSize = true;
            lblNackCnt.Location = new Point(6, 144);
            lblNackCnt.Margin = new Padding(4, 0, 4, 0);
            lblNackCnt.Name = "lblNackCnt";
            lblNackCnt.Size = new Size(56, 15);
            lblNackCnt.TabIndex = 6;
            lblNackCnt.Text = "NackCnt:";
            // 
            // lblCmdTxCnt
            // 
            lblCmdTxCnt.AutoSize = true;
            lblCmdTxCnt.Location = new Point(6, 127);
            lblCmdTxCnt.Margin = new Padding(4, 0, 4, 0);
            lblCmdTxCnt.Name = "lblCmdTxCnt";
            lblCmdTxCnt.Size = new Size(66, 15);
            lblCmdTxCnt.TabIndex = 5;
            lblCmdTxCnt.Text = "CmdTxCnt:";
            // 
            // lblCmdRxCnt
            // 
            lblCmdRxCnt.AutoSize = true;
            lblCmdRxCnt.Location = new Point(6, 110);
            lblCmdRxCnt.Margin = new Padding(4, 0, 4, 0);
            lblCmdRxCnt.Name = "lblCmdRxCnt";
            lblCmdRxCnt.Size = new Size(68, 15);
            lblCmdRxCnt.TabIndex = 4;
            lblCmdRxCnt.Text = "CmdRxCnt:";
            // 
            // lblPath
            // 
            lblPath.AutoSize = true;
            lblPath.Location = new Point(6, 85);
            lblPath.Margin = new Padding(4, 0, 4, 0);
            lblPath.Name = "lblPath";
            lblPath.Size = new Size(34, 15);
            lblPath.TabIndex = 3;
            lblPath.Text = "Path:";
            // 
            // lblProcessorType
            // 
            lblProcessorType.AutoSize = true;
            lblProcessorType.Location = new Point(6, 68);
            lblProcessorType.Margin = new Padding(4, 0, 4, 0);
            lblProcessorType.Name = "lblProcessorType";
            lblProcessorType.Size = new Size(85, 15);
            lblProcessorType.TabIndex = 2;
            lblProcessorType.Text = "ProcessorType:";
            // 
            // lblDeviceName
            // 
            lblDeviceName.AutoSize = true;
            lblDeviceName.Location = new Point(6, 51);
            lblDeviceName.Margin = new Padding(4, 0, 4, 0);
            lblDeviceName.Name = "lblDeviceName";
            lblDeviceName.Size = new Size(77, 15);
            lblDeviceName.TabIndex = 1;
            lblDeviceName.Text = "DeviceName:";
            // 
            // cbDevices
            // 
            cbDevices.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDevices.FormattingEnabled = true;
            cbDevices.Location = new Point(6, 22);
            cbDevices.Margin = new Padding(4, 3, 4, 3);
            cbDevices.Name = "cbDevices";
            cbDevices.Size = new Size(272, 23);
            cbDevices.TabIndex = 0;
            cbDevices.SelectionChangeCommitted += cbDevices_SelectionChangeCommitted;
            // 
            // grpVariables
            // 
            grpVariables.Controls.Add(lvVariables);
            grpVariables.Controls.Add(cbRW);
            grpVariables.Controls.Add(lblRW);
            grpVariables.Controls.Add(txtVariablesFilter);
            grpVariables.Controls.Add(lblVariablesFilter);
            grpVariables.Location = new Point(308, 119);
            grpVariables.Name = "grpVariables";
            grpVariables.Size = new Size(718, 217);
            grpVariables.TabIndex = 3;
            grpVariables.TabStop = false;
            grpVariables.Text = "Variables";
            // 
            // lvVariables
            // 
            lvVariables.GridLines = true;
            lvVariables.Location = new Point(8, 45);
            lvVariables.Name = "lvVariables";
            lvVariables.Size = new Size(704, 166);
            lvVariables.TabIndex = 4;
            lvVariables.UseCompatibleStateImageBehavior = false;
            lvVariables.View = View.Details;
            // 
            // cbRW
            // 
            cbRW.BackColor = SystemColors.Window;
            cbRW.DropDownStyle = ComboBoxStyle.DropDownList;
            cbRW.FormattingEnabled = true;
            cbRW.Items.AddRange(new object[] { "All", "R", "W", "RW" });
            cbRW.Location = new Point(292, 16);
            cbRW.Name = "cbRW";
            cbRW.Size = new Size(50, 23);
            cbRW.TabIndex = 4;
            cbRW.SelectionChangeCommitted += cbRW_SelectionChangeCommitted;
            // 
            // lblRW
            // 
            lblRW.AutoSize = true;
            lblRW.Location = new Point(224, 19);
            lblRW.Name = "lblRW";
            lblRW.Size = new Size(62, 15);
            lblRW.TabIndex = 3;
            lblRW.Text = "Filter R/W:";
            // 
            // txtVariablesFilter
            // 
            txtVariablesFilter.Location = new Point(48, 16);
            txtVariablesFilter.Name = "txtVariablesFilter";
            txtVariablesFilter.Size = new Size(170, 23);
            txtVariablesFilter.TabIndex = 1;
            txtVariablesFilter.KeyDown += txtVariablesFilter_KeyDown;
            // 
            // lblVariablesFilter
            // 
            lblVariablesFilter.AutoSize = true;
            lblVariablesFilter.Location = new Point(6, 19);
            lblVariablesFilter.Name = "lblVariablesFilter";
            lblVariablesFilter.Size = new Size(36, 15);
            lblVariablesFilter.TabIndex = 0;
            lblVariablesFilter.Text = "Filter:";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1036, 593);
            Controls.Add(grpVariables);
            Controls.Add(grpDevices);
            Controls.Add(grpLogging);
            Controls.Add(grpConnect);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 3, 4, 3);
            MaximumSize = new Size(1052, 632);
            MinimumSize = new Size(1048, 632);
            Name = "MainForm";
            StartPosition = FormStartPosition.Manual;
            Text = "Cockpit Hardware HUB v2 - ";
            FormClosed += MainForm_FormClosed;
            Load += MainForm_Load;
            grpConnect.ResumeLayout(false);
            grpLogging.ResumeLayout(false);
            grpLogging.PerformLayout();
            grpDevices.ResumeLayout(false);
            grpDevices.PerformLayout();
            grpVariables.ResumeLayout(false);
            grpVariables.PerformLayout();
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
        private GroupBox grpDevices;
        private CheckBox cbLogToFile;
        private TextBox txtLogFileName;
        private ComboBox cbDevices;
        private Label lblDevicePathValue;
        private Label lblProcessorTypeValue;
        private Label lblDeviceNameValue;
        private Label lblNackCnt;
        private Label lblCmdTxCnt;
        private Label lblCmdRxCnt;
        private Label lblPath;
        private Label lblProcessorType;
        private Label lblDeviceName;
        private PropertyTextBox txtProperties;
        private Label lblProperties;
        private Label lblNackCntValue;
        private Label lblCmdTxCntValue;
        private Label lblCmdRxCntValue;
        private Button btnResetStatistics;
        private GroupBox grpVariables;
        private TextBox txtVariablesFilter;
        private Label lblVariablesFilter;
        private ComboBox cbRW;
        private Label lblRW;
        private ListView lvVariables;
        private ListView lvLogging;
    }
}