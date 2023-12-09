using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using CockpitHardwareHUB_v2.Classes;
using WASimCommander.CLI.Enums;
using Timer = System.Windows.Forms.Timer;

namespace CockpitHardwareHUB_v2
{
    public partial class MainForm : Form
    {
        // Version
        private const string sVersion = "v0.01 - 01NOV2023";

        // Column width in DataGridViews
        private const int wTimeStamp = 80;
        private const int wLevel = 50;
        private const int wSource = 50;
        private const int wLogLine = 5000;

        // Datasources for DataGridView
        private DataTable dtLogLines = new DataTable();
        //private ConcurrentQueue<string> _LogData = new();
        private const int MaxLogLines = 500;

        // Timer for UI updates
        private Timer _Timer;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Text += sVersion;

            InitializeDataGridViews();

            DeviceServer.Init();
            SimClient.Init(OnConnectStatus);

            cbLogLevel.Text = Logging.sLogLevel;

            // Initialize timer
            _Timer = new Timer();
            _Timer.Interval = 10;
            _Timer.Tick += new EventHandler(timer_Tick);
            _Timer.Start();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _Timer.Stop();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            UpdateLogger();
        }

        private void OnConnectStatus(bool bIsConnected)
        {
            Invoke((MethodInvoker)(() =>
            {
                btnConnect.Text = (bIsConnected) ? "Disconnect" : "Connect";
                grpConnect.Text = $"MSFS2020 : {(bIsConnected ? "CONNECTED" : "DISCONNECTED")}";
            }));
        }

        private void UpdateLogger()
        {
            try
            {
                var count = Logging.LogData.Count;
                if (count == 0)
                    return;

                count = Math.Min(50, count); // update 10 loglines in one go

                for (int i = 0; i < count; i++)
                {
                    Logging.LogData.TryDequeue(out LogData logData);
                    dtLogLines.Rows.Add(new string[] { logData.sTimeStamp, logData.sLogLevel, logData.sLoggingSource, logData.sLogLine });
                }

                if (dtLogLines.Rows.Count > MaxLogLines)
                    dtLogLines.Rows.RemoveAt(0);

                // if logging is enabled, move cursor at the end
                // if (cbLoggingEnabled.Checked && dgvLogging.RowCount != 0)
                if (dgvLogging.RowCount != 0)
                {
                    dgvLogging.ClearSelection();
                    dgvLogging.CurrentCell = null;
                    dgvLogging.FirstDisplayedScrollingRowIndex = dgvLogging.RowCount - 1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CockpitHardwareForm.timer_Tick() Exception: {ex}");
            }
        }

        private void InitializeDataGridViewAppearance(DataGridView dgv)
        {
            dgv.RowHeadersVisible = false;
            dgv.Columns.Cast<DataGridViewColumn>().ToList().ForEach(f => f.SortMode = DataGridViewColumnSortMode.NotSortable);
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToResizeColumns = false;
            // Disable standard Windows colors
            dgv.EnableHeadersVisualStyles = false;
            // set color of column headers
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            // avoid highlighting selection in grid
            dgv.DefaultCellStyle.SelectionBackColor = dgv.DefaultCellStyle.BackColor;
            dgv.DefaultCellStyle.SelectionForeColor = dgv.DefaultCellStyle.ForeColor;
        }

        private void InitializeDataGridViews()
        {
            //// DataGridView Variables

            //dtVariables.Columns.Add("ID", typeof(string));
            //dtVariables.Columns.Add("Variable", typeof(string));
            //dtVariables.Columns.Add("Value", typeof(string));

            //dgvVariables.DataSource = dtVariables;

            //dgvVariables.Columns["ID"].Width = wID;
            //dgvVariables.Columns["Value"].Width = wVariable;
            //dgvVariables.Columns["Variable"].Width = dgvVariables.Width - wID - wVariable - SystemInformation.VerticalScrollBarWidth - 3;

            //InitializeDataGridViewAppearance(dgvVariables);

            // DataGridView Logging
            dtLogLines.Columns.Add("TimeStamp", typeof(string));
            dtLogLines.Columns.Add("Level", typeof(string));
            dtLogLines.Columns.Add("Source", typeof(string));
            dtLogLines.Columns.Add("LogLine", typeof(string));

            dgvLogging.DataSource = dtLogLines;

            dgvLogging.Columns["TimeStamp"].Width = wTimeStamp;
            dgvLogging.Columns["Level"].Width = wLevel;
            dgvLogging.Columns["Source"].Width = wSource;
            dgvLogging.Columns["LogLine"].Width = wLogLine;

            InitializeDataGridViewAppearance(dgvLogging);
        }

        private void UpdateUI_Connection(bool bConnected)
        {
            //btnConnect.Text = (bConnected) ? "Disconnect" : "Connect";
            grpConnect.Text = $"MSFS2020 : {(bConnected ? "CONNECTED" : "DISCONNECTED")}";
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!SimClient.IsConnected)
            {
                SimClient.Connect();
            }
            else
            {
                SimClient.Disconnect();
            }
            UpdateUI_Connection(SimClient.IsConnected);
        }

        private void btnLoggingClear_Click(object sender, EventArgs e)
        {
            dtLogLines.Clear();
        }

        private void cbLogLevel_SelectionChangeCommitted(object sender, EventArgs e)
        {
            Logging.sLogLevel = cbLogLevel.Text;
            SimClient.SetLogLevel(Logging.SetLogLevel);
        }

        private void txtLoggingFilter_TextChanged(object sender, EventArgs e)
        {
            dtLogLines.DefaultView.RowFilter = $"[LogLine] LIKE '%{txtLoggingFilter.Text}%'";
        }
    }
}