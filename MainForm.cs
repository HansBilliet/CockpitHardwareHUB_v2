using CockpitHardwareHUB_v2.Classes;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using WASimCommander.CLI.Enums;
using Timer = System.Windows.Forms.Timer;

namespace CockpitHardwareHUB_v2
{
    public partial class MainForm : Form
    {
        // Version
        private const string sVersion = "v2.1.1 - 09FEB2024";

        // Store the silent mode option
        private volatile bool _bSilentMode = false;

        // ListView controller objects for Variables and Logging
        private ListViewControllerVariables _ListViewControllerVariables;
        private ListViewControllerLogging _ListViewControllerLogging;
        private int maxLogLines = 1000;

        // Logfile setting
        private bool _bLogToFile = false;

        // Connection status Virtual Device
        private bool _bVirtualDeviceConnected = false;

        // Timer for statistics updates
        private Timer _Timer;

        // Current device selected in ComboBox
        private string _CurrentSelectedDevice = "";
        private readonly ConcurrentDictionary<string, COMDevice> _Devices = new();

        public MainForm()
        {
            InitializeComponent();

            // initialize event handlers
            Logging.UIUpdateLogging += UIOnUpdateLogging;
            SimClient.UIUpdateConnectionStatus += UIOnUpdateConnectStatus;
            DeviceServer.UIAddDevice += UIOnAddDevice;
            DeviceServer.UIRemoveDevice += UIOnRemoveDevice;
            PropertyPool.UIUpdateVariable += UIOnUpdateVariable;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Text = "Cockpit Hardware HUB v2 - " + sVersion;

            _ListViewControllerVariables = new(lvVariables);
            _ListViewControllerLogging = new(lvLogging, maxLogLines);

            DeviceServer.Init();
            SimClient.Init();

            UpdateVirtualDeviceUIElements();

            cbSilentMode.Checked = _bSilentMode;
            cbLogLevel.Text = Logging.sLogLevel;
            cbLogToFile.Checked = _bLogToFile;
            txtLogFileName.Text = FileLogger.sFileName;
            cbRW.SelectedIndex = 0;

            // Load the filter values
            RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\CockpitHardwareHUB");
            txtVID.Text = (string)key.GetValue("FilterVID", "");
            txtPID.Text = (string)key.GetValue("FilterPID", "");
            txtSerial.Text = (string)key.GetValue("FilterSerial", "");

            // Initialize timer
            _Timer = new Timer();
            _Timer.Interval = 50;
            _Timer.Tick += (sender, e) => UI_UpdateStatistics();
            _Timer.Start();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _Timer.Stop();

            // de-initialize event handlers
            Logging.UIUpdateLogging -= UIOnUpdateLogging;
            SimClient.UIUpdateConnectionStatus -= UIOnUpdateConnectStatus;
            DeviceServer.UIAddDevice -= UIOnAddDevice;
            DeviceServer.UIRemoveDevice -= UIOnRemoveDevice;
            PropertyPool.UIUpdateVariable -= UIOnUpdateVariable;
        }

        internal void UIOnUpdateConnectStatus(bool bIsConnected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UIOnUpdateConnectStatus(bIsConnected)));
                return;
            }

            if (!bIsConnected)
                _bVirtualDeviceConnected = false;

            cbSilentMode.Enabled = !bIsConnected; ;
            btnConnectMSFS.Text = (bIsConnected) ? "Disconnect" : "Connect";
            grpConnect.Text = $"MSFS2020 : {(bIsConnected ? "CONNECTED" : "DISCONNECTED")}";

            UpdateVirtualDeviceUIElements();
        }

        internal void UI_UpdateStatistics()
        {
            if (_bSilentMode || cbDevices.SelectedIndex == -1)
                return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UI_UpdateStatistics()));
                return;
            }

            COMDevice device = (COMDevice)cbDevices.SelectedItem;

            lblCmdRxCntValue.Text = device.cmdRxCnt.ToString();
            lblCmdTxCntValue.Text = device.cmdTxCnt.ToString();
            lblNackCntValue.Text = device.nackCnt.ToString();
        }

        internal void UI_ResetStatistics()
        {
            if (_bSilentMode)
                return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UI_ResetStatistics()));
                return;
            }

            lblCmdRxCntValue.Text = "0";
            lblCmdTxCntValue.Text = "0";
            lblNackCntValue.Text = "0";
        }

        internal void UIOnUpdateLogging(LogLevel logLevel, LoggingSource loggingSource, string sLoggingMsg, UInt64 timestamp = 0)
        {
            if (_bSilentMode)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UIOnUpdateLogging(logLevel, loggingSource, sLoggingMsg, timestamp)));
                return;
            }

            _ListViewControllerLogging.LogLine(logLevel, loggingSource, sLoggingMsg, timestamp);
        }

        private void UpdateVirtualDeviceUIElements()
        {
            if (SimClient.IsConnected)
            {
                txtExecCalcCode.Enabled = !_bSilentMode;
                btnSendExecCalcCode.Enabled = !_bSilentMode;

                txtProperty.Enabled = _bVirtualDeviceConnected;
                if (!_bVirtualDeviceConnected)
                    txtProperty.Text = "";
                btnConnectVD.Enabled = !_bSilentMode;
                btnConnectVD.Text = $"{(_bVirtualDeviceConnected ? "Disconnect" : "Connect")} Virtual Device";
                grpFilterConnect.Enabled = false;
                btnAddProperty.Enabled = _bVirtualDeviceConnected;
                btnSaveVirtualProperties.Enabled = _bVirtualDeviceConnected;
                btnLoadVirtualProperties.Enabled = _bVirtualDeviceConnected;
                btnSendToDevices.Enabled = !_bSilentMode;
                btnSendToMSFS.Enabled = !_bSilentMode;
            }
            else
            {
                grpFilterConnect.Enabled = true;

                txtExecCalcCode.Enabled = false;
                txtExecCalcCode.Text = "";
                txtExecCalcCodeResult.Text = "";
                btnSendExecCalcCode.Enabled = false;

                txtProperty.Text = "";
                txtProperty.Enabled = false;
                btnConnectVD.Text = "Connect Virtual Device";
                btnConnectVD.Enabled = false;
                btnAddProperty.Enabled = false;
                btnSaveVirtualProperties.Enabled = false;
                btnLoadVirtualProperties.Enabled = false;
                btnSendToDevices.Enabled = false;
                btnSendToMSFS.Enabled = false;
            }
        }

        internal void UIOnAddDevice(COMDevice device)
        {
            if (_bSilentMode)
                return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UIOnAddDevice(device)));
                return;
            }

            cbDevices.Items.Add(device);
            if (cbDevices.SelectedIndex == -1 && cbDevices.Items.Count > 0)
                cbDevices.SelectedIndex = 0;

            UI_UpdateUSBDevices();
        }

        internal void UIOnRemoveDevice(COMDevice device)
        {
            if (_bSilentMode)
                return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UIOnRemoveDevice(device)));
                return;
            }

            cbDevices.Items.Remove(device);
            if (cbDevices.SelectedIndex == -1 && cbDevices.Items.Count > 0)
                cbDevices.SelectedIndex = 0;

            UI_UpdateUSBDevices();
        }

        private void UI_UpdateUSBDevices(bool bForceUpdatePropertyList = false)
        {
            if (_bSilentMode)
                return;

            if (cbDevices.Items.Count == 0 && _CurrentSelectedDevice != "")
            {
                lblDeviceNameValue.Text = "";
                lblProcessorTypeValue.Text = "";
                lblDevicePathValue.Text = "";
                txtProperties.Text = "";
                UI_ResetStatistics();
                _CurrentSelectedDevice = "";
                return;
            }

            COMDevice device = (COMDevice)cbDevices.SelectedItem;

            if (_CurrentSelectedDevice != device.ToString() || bForceUpdatePropertyList)
            {
                lock (device)
                {
                    lblDeviceNameValue.Text = device.DeviceName;
                    lblProcessorTypeValue.Text = device.ProcessorType;
                    lblDevicePathValue.Text = device.PNPDeviceID;

                    txtProperties.Text = "";
                    int index = 1;
                    txtProperties.BeginUpdate();
                    foreach (Property property in device.Properties)
                    {
                        if (txtProperties.Text != "")
                            txtProperties.AppendText(Environment.NewLine);
                        txtProperties.AppendText($"{index++:D03}/{(property.iVarId == -1 ? "FAIL" : property.iVarId.ToString("D04"))}: {property.sPropStr}");
                    }
                    txtProperties.Select(0, 0);
                    txtProperties.ScrollToCaret();
                    txtProperties.EndUpdate();

                    _CurrentSelectedDevice = device.ToString();
                }
            }
        }

        internal void UIOnUpdateVariable(UpdateVariable uv, SimVar simVar)
        {
            if (_bSilentMode || simVar.iVarId == -1)
                return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UIOnUpdateVariable(uv, simVar)));
                return;
            }

            switch (uv)
            {
                case UpdateVariable.Add:
                    _ListViewControllerVariables.AddSimVar(simVar);
                    break;

                case UpdateVariable.Remove:
                    _ListViewControllerVariables.RemoveSimVar(simVar);
                    break;

                case UpdateVariable.Usage:
                    _ListViewControllerVariables.ChangeSimVar(simVar);
                    break;

                case UpdateVariable.Value:
                    _ListViewControllerVariables.ChangeSimVar(simVar);
                    break;
            }
        }

        // GroupBox Connect/Disconnect

        private static void MessageBoxFilter(string s)
        {
            MessageBox.Show($"{s} is not a correct value. Possible formats are:" + Environment.NewLine +
                             "- 0xHHHH where [HHHH] is a hex number" + Environment.NewLine +
                             "- NNNNN where [NNNNN] is a decimal number" + Environment.NewLine +
                             "- Only values between 1 (0x0001) and 65535 (0xFFFF) are allowed.",
                             "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static bool IsValidFilterValue(string input, out int value)
        {
            value = -1;
            if (string.IsNullOrEmpty(input))
                return true;

            bool isHex = input.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            bool validParse = isHex ? int.TryParse(input[2..], System.Globalization.NumberStyles.HexNumber, null, out value)
                                    : int.TryParse(input, out value);

            return validParse && value >= 1 && value <= 0xFFFF;
        }

        private bool CheckFilter()
        {
            // check VID
            if (!IsValidFilterValue(txtVID.Text, out int vidValue))
            {
                MessageBoxFilter("VID");
                return false;
            }
            DeviceServer._FilterVID = txtVID.Text == "" ? 0 : (uint)vidValue;

            // check PID
            if (!IsValidFilterValue(txtPID.Text, out int pidValue))
            {
                MessageBoxFilter("PID");
                return false;
            }
            DeviceServer._FilterPID = txtPID.Text == "" ? 0 : (uint)pidValue;

            DeviceServer._FilterSerialNumber = txtSerial.Text.ToUpper();

            RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\CockpitHardwareHUB");
            key.SetValue("FilterVID", txtVID.Text);
            key.SetValue("FilterPID", txtPID.Text);
            key.SetValue("FilterSerial", txtSerial.Text);

            return true;
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (!SimClient.IsConnected)
            {
                if (CheckFilter())
                    SimClient.Connect();
            }
            else
            {
                await SimClient.Disconnect();
            }
            UIOnUpdateConnectStatus(SimClient.IsConnected);
        }

        private void cbSilentMode_CheckedChanged(object sender, EventArgs e)
        {
            MessageBox.Show("This option can only be changed when disconnected.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _bSilentMode = cbSilentMode.Checked;
        }

        private void btnResetFilter_Click(object sender, EventArgs e)
        {
            if (!SimClient.IsConnected)
            {
                txtVID.Text = "";
                txtPID.Text = "";
                txtSerial.Text = "";

                RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\CockpitHardwareHUB");
                key.SetValue("FilterVID", txtVID.Text);
                key.SetValue("FilterPID", txtPID.Text);
                key.SetValue("FilterSerial", txtSerial.Text);
            }
        }

        // GroupBox Devices
        private void cbDevices_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (_bSilentMode)
                return;

            _Devices.TryGetValue(cbDevices.SelectedItem.ToString(), out var _SelectedDevice);
            UI_UpdateUSBDevices();
        }

        private void btnResetStatistics_Click(object sender, EventArgs e)
        {
            if (_bSilentMode)
                return;

            DeviceServer.ResetStatistics();

            UI_ResetStatistics();
        }

        // GroupBox Execute Calculator Code
        private void btnSendExecCalcCode_Click(object sender, EventArgs e)
        {
            if (txtExecCalcCode.Text == "")
            {
                MessageBox.Show("Enter an Calculator Code in the input field.", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Execute the calculator code and show the result
            HR hr = SimClient.ExecuteCalculatorCode(txtExecCalcCode.Text, out string s);
            if (hr != HR.OK)
            {
                txtExecCalcCodeResult.Text = "";
                MessageBox.Show($"execute_calculator_code failed with \"{hr}\"", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txtExecCalcCodeResult.Text = s;
        }

        // GroupBox Virtual Device
        private void btnConnectVD_Click(object sender, EventArgs e)
        {
            if (!_bVirtualDeviceConnected)
            {
                DeviceServer.AddDevice("VIRTUAL", "VIRTUAL");
                _bVirtualDeviceConnected = true;
            }
            else
            {
                DeviceServer.RemoveDevice("VIRTUAL");
                _bVirtualDeviceConnected = false;
            }
            UpdateVirtualDeviceUIElements();
        }

        private void btnAddProperty_Click(object sender, EventArgs e)
        {
            if (txtProperty.Text == "")
            {
                MessageBox.Show("Enter a Property String in the input field.", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            COMDevice device = DeviceServer.FindDeviceBasedOnPNPDeviceID("VIRTUAL");
            if (device == null)
            {
                MessageBox.Show("VIRTUAL device seems not to exist. Something went wrong", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            PR parseResult = device.AddProperty(txtProperty.Text);
            if (parseResult != PR.Ok)
            {
                MessageBox.Show($"Property String parse error: {parseResult}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UI_UpdateUSBDevices(true);
        }

        private void btnSaveVirtualProperties_Click(object sender, EventArgs e)
        {
            COMDevice device = DeviceServer.FindDeviceBasedOnPNPDeviceID("VIRTUAL");
            if (device == null)
            {
                MessageBox.Show("VIRTUAL device seems not to exist. Something went wrong", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (device.Properties.Count == 0)
            {
                MessageBox.Show("VIRTUAL device has no properties to save.", "Nothing to save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //string defaultFileName = "VirtualDeviceProperties.txt";
            string defaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            saveFileDialog.Title = "Save Properties";
            //saveFileDialog.FileName = defaultFileName;

            // Retrieve the last used directory from the registry
            RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\CockpitHardwareHUB");
            string lastUsedDirectory = (string)key.GetValue("VirtualDeviceSaveFolder", defaultDirectory);

            // Check if the directory exists
            if (Directory.Exists(lastUsedDirectory))
                saveFileDialog.InitialDirectory = lastUsedDirectory;
            else
                saveFileDialog.InitialDirectory = defaultDirectory;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
                    {
                        foreach (Property property in device.Properties)
                            sw.WriteLine(property.sPropStr);
                    }

                    // Save the directory back to the registry
                    key.SetValue("VirtualDeviceSaveFolder", Path.GetDirectoryName(saveFileDialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while saving the file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Close the registry key
            key.Close();
        }

        private async void btnLoadVirtualProperties_Click(object sender, EventArgs e)
        {
            COMDevice device = DeviceServer.FindDeviceBasedOnPNPDeviceID("VIRTUAL");
            if (device == null)
            {
                MessageBox.Show("VIRTUAL device seems not to exist. Something went wrong", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string defaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog.Title = "Load Properties";

            // Retrieve the last used directory from the registry
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\CockpitHardwareHUB");
            string lastUsedDirectory = (string)key.GetValue("VirtualDeviceSaveFolder", defaultDirectory);

            // Check if the directory exists
            if (Directory.Exists(lastUsedDirectory))
                openFileDialog.InitialDirectory = lastUsedDirectory;
            else
                openFileDialog.InitialDirectory = defaultDirectory;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(openFileDialog.FileName))
                    {
                        string line;
                        while ((line = await sr.ReadLineAsync()) != null)
                        {
                            await Task.Run(() => device.AddProperty(line));
                        }
                        UI_UpdateUSBDevices(true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while loading the file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SendSimVar(bool bRead)
        {
            if (txtCommand.Text == "")
            {
                MessageBox.Show("Enter an existing Variable number with optional data in the input field." + Environment.NewLine + Environment.NewLine +
                                "Accepted formats are:" + Environment.NewLine +
                                " NNN" + Environment.NewLine +
                                " NNN=" + Environment.NewLine +
                                " NNN=Data", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Split the txtSimVar input by '='
            string[] parts = txtCommand.Text.Split(new char[] { '=' }, 2);
            string sCmd = parts[0];
            string sData = parts.Length > 1 ? parts[1] : "";

            // Check if first part is a number
            if (!int.TryParse(sCmd, out int iVarId))
            {
                MessageBox.Show($"[{sCmd}] is not a valid number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Find the SimVar
            SimVar simVar = SimVar.GetSimVarById(iVarId);
            if (simVar == null)
            {
                MessageBox.Show($"SimVar [{iVarId}] does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (bRead ? !simVar.bRead : !simVar.bWrite)
            {
                MessageBox.Show($"SimVar with Id [{iVarId}] is not a \"{(bRead ? "Read" : "Write")}\" variable.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // If there is data available, check if it matches with the ValType of the SimVar
            if (!simVar.SetValueOfSimVar(sData))
            {
                MessageBox.Show($"SimVar [{iVarId}] requires data of type \"{simVar.ValType}\".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (bRead)
                // Dispatch SimVar to all devices that are listening
                simVar.DispatchSimVar();
            else
                // Send SimVar to MSFS
                SimClient.TriggerSimVar(simVar);
        }

        private void btnSendToMSFS_Click(object sender, EventArgs e)
        {
            // Send SimVar to MSFS
            SendSimVar(false);
        }

        private void btnSendToDevices_Click(object sender, EventArgs e)
        {
            // Dispatch SimVar to all devices that are listening
            SendSimVar(true);
        }

        // GroupBox Variables
        private void txtVariablesFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                _ListViewControllerVariables.FilterName = txtVariablesFilter.Text;
        }

        private void cbRW_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbRW.SelectedIndex <= 0)
                _ListViewControllerVariables.FilterRW = "";
            else
                _ListViewControllerVariables.FilterRW = cbRW.SelectedItem.ToString();
        }

        // GroupBox Logging
        private void txtLoggingFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                _ListViewControllerLogging.FilterLogLine = txtLoggingFilter.Text;
        }

        private void cbLogLevel_SelectionChangeCommitted(object sender, EventArgs e)
        {
            Logging.sLogLevel = cbLogLevel.Text;
            SimClient.SetLogLevel(Logging.SetLogLevel);
        }

        private void btnLoggingClear_Click(object sender, EventArgs e)
        {
            _ListViewControllerLogging.ClearLogging();
        }

        private void cbLogToFile_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLogToFile.Checked)
            {
                if (!FileLogger.OpenFile())
                    cbLogToFile.Checked = false;
            }
            else
                FileLogger.CloseFile();

            txtLogFileName.Text = FileLogger.sFileName;

            _bLogToFile = cbLogToFile.Checked;
        }
    }
}

internal class PropertyTextBox : TextBox
{
    private const int WM_SETREDRAW = 0x000B;
    private const int WM_PAINT = 0x000F;
    private bool _updating = false;

    internal void BeginUpdate()
    {
        _updating = true;
        SendMessage(this.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
    }

    internal void EndUpdate()
    {
        _updating = false;
        SendMessage(this.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
        this.Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        // If we're updating, suppress WM_PAINT messages
        if (_updating && m.Msg == WM_PAINT)
            return;

        base.WndProc(ref m);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
}

internal enum UpdateVariable
{
    Add,
    Remove,
    Value,
    Usage
}
