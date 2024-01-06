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
            grpFilterConnect = new GroupBox();
            btnResetFilter = new Button();
            txtSerial = new TextBox();
            lblSerial = new Label();
            txtPID = new TextBox();
            lblPID = new Label();
            txtVID = new TextBox();
            lblVID = new Label();
            cbSilentMode = new CheckBox();
            btnConnectMSFS = new Button();
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
            txtCommand = new TextBox();
            lvVariables = new ListView();
            cbRW = new ComboBox();
            btnSendToDevices = new Button();
            btnSendToMSFS = new Button();
            lblRW = new Label();
            txtVariablesFilter = new TextBox();
            lblVariablesFilter = new Label();
            grpVirtualDevice = new GroupBox();
            btnLoadVirtualProperties = new Button();
            btnSaveVirtualProperties = new Button();
            btnAddProperty = new Button();
            btnConnectVD = new Button();
            txtProperty = new TextBox();
            grpExecCalcode = new GroupBox();
            txtExecCalcCodeResult = new TextBox();
            lblExecCalcCodeResult = new Label();
            btnSendExecCalcCode = new Button();
            txtExecCalcCode = new TextBox();
            grpConnect.SuspendLayout();
            grpFilterConnect.SuspendLayout();
            grpLogging.SuspendLayout();
            grpDevices.SuspendLayout();
            grpVariables.SuspendLayout();
            grpVirtualDevice.SuspendLayout();
            grpExecCalcode.SuspendLayout();
            SuspendLayout();
            // 
            // grpConnect
            // 
            grpConnect.Controls.Add(grpFilterConnect);
            grpConnect.Controls.Add(cbSilentMode);
            grpConnect.Controls.Add(btnConnectMSFS);
            grpConnect.Location = new Point(13, 10);
            grpConnect.Margin = new Padding(4, 3, 4, 3);
            grpConnect.Name = "grpConnect";
            grpConnect.Padding = new Padding(4, 3, 4, 3);
            grpConnect.Size = new Size(285, 132);
            grpConnect.TabIndex = 0;
            grpConnect.TabStop = false;
            grpConnect.Text = "MSFS2020 : DISCONNECTED";
            // 
            // grpFilterConnect
            // 
            grpFilterConnect.Controls.Add(btnResetFilter);
            grpFilterConnect.Controls.Add(txtSerial);
            grpFilterConnect.Controls.Add(lblSerial);
            grpFilterConnect.Controls.Add(txtPID);
            grpFilterConnect.Controls.Add(lblPID);
            grpFilterConnect.Controls.Add(txtVID);
            grpFilterConnect.Controls.Add(lblVID);
            grpFilterConnect.Location = new Point(7, 47);
            grpFilterConnect.Name = "grpFilterConnect";
            grpFilterConnect.Size = new Size(271, 79);
            grpFilterConnect.TabIndex = 2;
            grpFilterConnect.TabStop = false;
            grpFilterConnect.Text = "Filter";
            // 
            // btnResetFilter
            // 
            btnResetFilter.Location = new Point(216, 16);
            btnResetFilter.Name = "btnResetFilter";
            btnResetFilter.Size = new Size(48, 52);
            btnResetFilter.TabIndex = 6;
            btnResetFilter.Text = "Reset";
            btnResetFilter.UseVisualStyleBackColor = true;
            btnResetFilter.Click += btnResetFilter_Click;
            // 
            // txtSerial
            // 
            txtSerial.Location = new Point(50, 45);
            txtSerial.MaxLength = 8;
            txtSerial.Name = "txtSerial";
            txtSerial.Size = new Size(146, 23);
            txtSerial.TabIndex = 5;
            // 
            // lblSerial
            // 
            lblSerial.AutoSize = true;
            lblSerial.Location = new Point(6, 48);
            lblSerial.Name = "lblSerial";
            lblSerial.Size = new Size(38, 15);
            lblSerial.TabIndex = 4;
            lblSerial.Text = "Serial:";
            // 
            // txtPID
            // 
            txtPID.Location = new Point(143, 16);
            txtPID.MaxLength = 6;
            txtPID.Name = "txtPID";
            txtPID.Size = new Size(53, 23);
            txtPID.TabIndex = 3;
            // 
            // lblPID
            // 
            lblPID.AutoSize = true;
            lblPID.Location = new Point(109, 19);
            lblPID.Name = "lblPID";
            lblPID.Size = new Size(28, 15);
            lblPID.TabIndex = 2;
            lblPID.Text = "PID:";
            // 
            // txtVID
            // 
            txtVID.Location = new Point(50, 16);
            txtVID.MaxLength = 6;
            txtVID.Name = "txtVID";
            txtVID.Size = new Size(53, 23);
            txtVID.TabIndex = 1;
            // 
            // lblVID
            // 
            lblVID.AutoSize = true;
            lblVID.Location = new Point(16, 19);
            lblVID.Name = "lblVID";
            lblVID.Size = new Size(28, 15);
            lblVID.TabIndex = 0;
            lblVID.Text = "VID:";
            // 
            // cbSilentMode
            // 
            cbSilentMode.AutoSize = true;
            cbSilentMode.Location = new Point(189, 21);
            cbSilentMode.Name = "cbSilentMode";
            cbSilentMode.Size = new Size(89, 19);
            cbSilentMode.TabIndex = 1;
            cbSilentMode.Text = "Silent mode";
            cbSilentMode.UseVisualStyleBackColor = true;
            cbSilentMode.CheckedChanged += cbSilentMode_CheckedChanged;
            // 
            // btnConnectMSFS
            // 
            btnConnectMSFS.Location = new Point(6, 18);
            btnConnectMSFS.Margin = new Padding(4, 3, 4, 3);
            btnConnectMSFS.Name = "btnConnectMSFS";
            btnConnectMSFS.Size = new Size(159, 23);
            btnConnectMSFS.TabIndex = 0;
            btnConnectMSFS.Text = "Connect";
            btnConnectMSFS.UseVisualStyleBackColor = true;
            btnConnectMSFS.Click += btnConnect_Click;
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
            grpLogging.Location = new Point(305, 408);
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
            lvLogging.Location = new Point(7, 72);
            lvLogging.Name = "lvLogging";
            lvLogging.Size = new Size(708, 165);
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
            txtLogFileName.Size = new Size(620, 23);
            txtLogFileName.TabIndex = 8;
            // 
            // cbLogToFile
            // 
            cbLogToFile.AutoSize = true;
            cbLogToFile.Location = new Point(6, 47);
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
            btnLoggingClear.Location = new Point(639, 17);
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
            cbLogLevel.Location = new Point(292, 17);
            cbLogLevel.Margin = new Padding(4, 3, 4, 3);
            cbLogLevel.Name = "cbLogLevel";
            cbLogLevel.Size = new Size(73, 23);
            cbLogLevel.TabIndex = 3;
            cbLogLevel.SelectionChangeCommitted += cbLogLevel_SelectionChangeCommitted;
            // 
            // lblLoggingLevel
            // 
            lblLoggingLevel.AutoSize = true;
            lblLoggingLevel.Location = new Point(226, 20);
            lblLoggingLevel.Margin = new Padding(4, 0, 4, 0);
            lblLoggingLevel.Name = "lblLoggingLevel";
            lblLoggingLevel.Size = new Size(57, 15);
            lblLoggingLevel.TabIndex = 2;
            lblLoggingLevel.Text = "Log level:";
            // 
            // txtLoggingFilter
            // 
            txtLoggingFilter.Location = new Point(48, 17);
            txtLoggingFilter.Margin = new Padding(4, 3, 4, 3);
            txtLoggingFilter.Name = "txtLoggingFilter";
            txtLoggingFilter.Size = new Size(170, 23);
            txtLoggingFilter.TabIndex = 1;
            txtLoggingFilter.KeyDown += txtLoggingFilter_KeyDown;
            // 
            // lblLoggingFilter
            // 
            lblLoggingFilter.AutoSize = true;
            lblLoggingFilter.Location = new Point(6, 20);
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
            grpDevices.Location = new Point(13, 148);
            grpDevices.Margin = new Padding(4, 3, 4, 3);
            grpDevices.Name = "grpDevices";
            grpDevices.Padding = new Padding(4, 3, 4, 3);
            grpDevices.Size = new Size(285, 506);
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
            txtProperties.Location = new Point(6, 222);
            txtProperties.Margin = new Padding(4, 3, 4, 3);
            txtProperties.Multiline = true;
            txtProperties.Name = "txtProperties";
            txtProperties.ReadOnly = true;
            txtProperties.ScrollBars = ScrollBars.Both;
            txtProperties.Size = new Size(272, 275);
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
            grpVariables.Controls.Add(txtCommand);
            grpVariables.Controls.Add(lvVariables);
            grpVariables.Controls.Add(cbRW);
            grpVariables.Controls.Add(btnSendToDevices);
            grpVariables.Controls.Add(btnSendToMSFS);
            grpVariables.Controls.Add(lblRW);
            grpVariables.Controls.Add(txtVariablesFilter);
            grpVariables.Controls.Add(lblVariablesFilter);
            grpVariables.Location = new Point(305, 185);
            grpVariables.Name = "grpVariables";
            grpVariables.Size = new Size(720, 217);
            grpVariables.TabIndex = 3;
            grpVariables.TabStop = false;
            grpVariables.Text = "Variables";
            // 
            // txtCommand
            // 
            txtCommand.Location = new Point(383, 16);
            txtCommand.Name = "txtCommand";
            txtCommand.Size = new Size(169, 23);
            txtCommand.TabIndex = 6;
            // 
            // lvVariables
            // 
            lvVariables.GridLines = true;
            lvVariables.Location = new Point(6, 45);
            lvVariables.Name = "lvVariables";
            lvVariables.Size = new Size(708, 166);
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
            // btnSendToDevices
            // 
            btnSendToDevices.Location = new Point(639, 15);
            btnSendToDevices.Name = "btnSendToDevices";
            btnSendToDevices.Size = new Size(75, 23);
            btnSendToDevices.TabIndex = 5;
            btnSendToDevices.Text = ">Devices";
            btnSendToDevices.UseVisualStyleBackColor = true;
            btnSendToDevices.Click += btnSendToDevices_Click;
            // 
            // btnSendToMSFS
            // 
            btnSendToMSFS.Location = new Point(558, 15);
            btnSendToMSFS.Name = "btnSendToMSFS";
            btnSendToMSFS.Size = new Size(75, 23);
            btnSendToMSFS.TabIndex = 4;
            btnSendToMSFS.Text = ">MSFS";
            btnSendToMSFS.UseVisualStyleBackColor = true;
            btnSendToMSFS.Click += btnSendToMSFS_Click;
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
            // grpVirtualDevice
            // 
            grpVirtualDevice.Controls.Add(btnLoadVirtualProperties);
            grpVirtualDevice.Controls.Add(btnSaveVirtualProperties);
            grpVirtualDevice.Controls.Add(btnAddProperty);
            grpVirtualDevice.Controls.Add(btnConnectVD);
            grpVirtualDevice.Controls.Add(txtProperty);
            grpVirtualDevice.Location = new Point(305, 97);
            grpVirtualDevice.Name = "grpVirtualDevice";
            grpVirtualDevice.Size = new Size(720, 82);
            grpVirtualDevice.TabIndex = 4;
            grpVirtualDevice.TabStop = false;
            grpVirtualDevice.Text = "Virtual Device";
            // 
            // btnLoadVirtualProperties
            // 
            btnLoadVirtualProperties.Location = new Point(639, 51);
            btnLoadVirtualProperties.Name = "btnLoadVirtualProperties";
            btnLoadVirtualProperties.Size = new Size(75, 23);
            btnLoadVirtualProperties.TabIndex = 7;
            btnLoadVirtualProperties.Text = "Load";
            btnLoadVirtualProperties.UseVisualStyleBackColor = true;
            btnLoadVirtualProperties.Click += btnLoadVirtualProperties_Click;
            // 
            // btnSaveVirtualProperties
            // 
            btnSaveVirtualProperties.Location = new Point(558, 51);
            btnSaveVirtualProperties.Name = "btnSaveVirtualProperties";
            btnSaveVirtualProperties.Size = new Size(75, 23);
            btnSaveVirtualProperties.TabIndex = 6;
            btnSaveVirtualProperties.Text = "Save";
            btnSaveVirtualProperties.UseVisualStyleBackColor = true;
            btnSaveVirtualProperties.Click += btnSaveVirtualProperties_Click;
            // 
            // btnAddProperty
            // 
            btnAddProperty.Location = new Point(162, 51);
            btnAddProperty.Name = "btnAddProperty";
            btnAddProperty.Size = new Size(110, 23);
            btnAddProperty.TabIndex = 2;
            btnAddProperty.Text = "Add Property";
            btnAddProperty.UseVisualStyleBackColor = true;
            btnAddProperty.Click += btnAddProperty_Click;
            // 
            // btnConnectVD
            // 
            btnConnectVD.Location = new Point(6, 51);
            btnConnectVD.Name = "btnConnectVD";
            btnConnectVD.Size = new Size(150, 23);
            btnConnectVD.TabIndex = 1;
            btnConnectVD.Text = "Connect Virtual Device";
            btnConnectVD.UseVisualStyleBackColor = true;
            btnConnectVD.Click += btnConnectVD_Click;
            // 
            // txtProperty
            // 
            txtProperty.Location = new Point(6, 22);
            txtProperty.Name = "txtProperty";
            txtProperty.Size = new Size(708, 23);
            txtProperty.TabIndex = 0;
            // 
            // grpExecCalcode
            // 
            grpExecCalcode.Controls.Add(txtExecCalcCodeResult);
            grpExecCalcode.Controls.Add(lblExecCalcCodeResult);
            grpExecCalcode.Controls.Add(btnSendExecCalcCode);
            grpExecCalcode.Controls.Add(txtExecCalcCode);
            grpExecCalcode.Location = new Point(305, 10);
            grpExecCalcode.Name = "grpExecCalcode";
            grpExecCalcode.Size = new Size(720, 81);
            grpExecCalcode.TabIndex = 5;
            grpExecCalcode.TabStop = false;
            grpExecCalcode.Text = "execute_calculator_code";
            // 
            // txtExecCalcCodeResult
            // 
            txtExecCalcCodeResult.Location = new Point(55, 49);
            txtExecCalcCodeResult.Name = "txtExecCalcCodeResult";
            txtExecCalcCodeResult.ReadOnly = true;
            txtExecCalcCodeResult.Size = new Size(659, 23);
            txtExecCalcCodeResult.TabIndex = 7;
            // 
            // lblExecCalcCodeResult
            // 
            lblExecCalcCodeResult.AutoSize = true;
            lblExecCalcCodeResult.Location = new Point(7, 52);
            lblExecCalcCodeResult.Name = "lblExecCalcCodeResult";
            lblExecCalcCodeResult.Size = new Size(42, 15);
            lblExecCalcCodeResult.TabIndex = 6;
            lblExecCalcCodeResult.Text = "Result:";
            // 
            // btnSendExecCalcCode
            // 
            btnSendExecCalcCode.Location = new Point(664, 21);
            btnSendExecCalcCode.Name = "btnSendExecCalcCode";
            btnSendExecCalcCode.Size = new Size(50, 23);
            btnSendExecCalcCode.TabIndex = 1;
            btnSendExecCalcCode.Text = "Send";
            btnSendExecCalcCode.UseVisualStyleBackColor = true;
            btnSendExecCalcCode.Click += btnSendExecCalcCode_Click;
            // 
            // txtExecCalcCode
            // 
            txtExecCalcCode.Location = new Point(6, 22);
            txtExecCalcCode.Name = "txtExecCalcCode";
            txtExecCalcCode.Size = new Size(652, 23);
            txtExecCalcCode.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1039, 666);
            Controls.Add(grpExecCalcode);
            Controls.Add(grpVirtualDevice);
            Controls.Add(grpVariables);
            Controls.Add(grpDevices);
            Controls.Add(grpLogging);
            Controls.Add(grpConnect);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 3, 4, 3);
            MaximumSize = new Size(1055, 705);
            MinimumSize = new Size(1055, 705);
            Name = "MainForm";
            StartPosition = FormStartPosition.Manual;
            Text = "Cockpit Hardware HUB v2 - ";
            FormClosed += MainForm_FormClosed;
            Load += MainForm_Load;
            grpConnect.ResumeLayout(false);
            grpConnect.PerformLayout();
            grpFilterConnect.ResumeLayout(false);
            grpFilterConnect.PerformLayout();
            grpLogging.ResumeLayout(false);
            grpLogging.PerformLayout();
            grpDevices.ResumeLayout(false);
            grpDevices.PerformLayout();
            grpVariables.ResumeLayout(false);
            grpVariables.PerformLayout();
            grpVirtualDevice.ResumeLayout(false);
            grpVirtualDevice.PerformLayout();
            grpExecCalcode.ResumeLayout(false);
            grpExecCalcode.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox grpConnect;
        private Button btnConnectMSFS;
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
        private GroupBox grpVirtualDevice;
        private TextBox txtProperty;
        private Button btnConnectVD;
        private Button btnAddProperty;
        private Button btnSendToDevices;
        private Button btnSendToMSFS;
        private GroupBox grpExecCalcode;
        private Button btnSendExecCalcCode;
        private TextBox txtExecCalcCode;
        private TextBox txtExecCalcCodeResult;
        private Label lblExecCalcCodeResult;
        private Button btnLoadVirtualProperties;
        private Button btnSaveVirtualProperties;
        private CheckBox cbSilentMode;
        private TextBox txtCommand;
        private GroupBox grpFilterConnect;
        private TextBox txtPID;
        private Label lblPID;
        private TextBox txtVID;
        private Label lblVID;
        private TextBox txtSerial;
        private Label lblSerial;
        private Button btnResetFilter;
    }
}