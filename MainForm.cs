using CockpitHardwareHUB_v2.Classes;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using WASimCommander.CLI.Enums;
using Timer = System.Windows.Forms.Timer;

namespace CockpitHardwareHUB_v2
{
    public partial class MainForm : Form
    {
        // Version
        private const string sVersion = "v0.01 - 01NOV2023";

        // ListView controller objects for Variables and Logging
        private ListViewControllerVariables _ListViewControllerVariables;
        private ListViewControllerLogging _ListViewControllerLogging;
        private int maxLogLines = 1000;

        // Logfile setting
        private bool _bLogToFile = false;

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
            Text += sVersion;

            _ListViewControllerVariables = new(lvVariables);
            _ListViewControllerLogging = new(lvLogging, maxLogLines);

            DeviceServer.Init();
            SimClient.Init();

            cbLogLevel.Text = Logging.sLogLevel;
            cbLogToFile.Checked = _bLogToFile;
            txtLogFileName.Text = FileLogger.sFileName;
            cbRW.SelectedIndex = 0;

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

        internal void UI_UpdateStatistics()
        {
            if (cbDevices.SelectedIndex == -1)
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
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UIOnUpdateLogging(logLevel, loggingSource, sLoggingMsg, timestamp)));
                return;
            }

            _ListViewControllerLogging.LogLine(logLevel, loggingSource, sLoggingMsg, timestamp);
        }

        internal void UIOnUpdateConnectStatus(bool bIsConnected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UIOnUpdateConnectStatus(bIsConnected)));
                return;
            }

            btnConnect.Text = (bIsConnected) ? "Disconnect" : "Connect";
            grpConnect.Text = $"MSFS2020 : {(bIsConnected ? "CONNECTED" : "DISCONNECTED")}";
        }

        internal void UIOnAddDevice(COMDevice device)
        {
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

        private void UI_UpdateUSBDevices()
        {
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

            if (_CurrentSelectedDevice != device.ToString())
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
                        txtProperties.AppendText($"{index++:D03}: {property.sPropStr}");
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
            if (simVar.iVarId == -1)
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

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (!SimClient.IsConnected)
            {
                SimClient.Connect();
            }
            else
            {
                await SimClient.Disconnect();
            }
            UIOnUpdateConnectStatus(SimClient.IsConnected);
        }

        private void cbDevices_SelectionChangeCommitted(object sender, EventArgs e)
        {
            _Devices.TryGetValue(cbDevices.SelectedItem.ToString(), out var _SelectedDevice);
            UI_UpdateUSBDevices();
        }

        private void btnResetStatistics_Click(object sender, EventArgs e)
        {
            DeviceServer.ResetStatistics();

            UI_ResetStatistics();
        }

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
}