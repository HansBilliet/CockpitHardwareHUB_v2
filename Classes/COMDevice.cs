using System.Collections.Concurrent;
using System.IO.Ports;
using System.Security.Cryptography;
using System.Text;
using WASimCommander.CLI.Enums;

namespace CockpitHardwareHUB_v2.Classes
{
    // This Property Class maps a HW device Property against a registered SimVar in the SimClient
    internal class Property
    {
        private readonly int _iPropId = -1; // This is the Id in the HW device (order of registration starting with 001)
        private readonly string _sPropStr; // This is the full string of the Property as sent by the HW device
        private int _iVarId = -1; // Once registered as a SimVar in the SimClient, a VarId is given

        public int iPropId => _iPropId;
        public string sPropStr => _sPropStr;
        public int iSimId { get => _iVarId; set => _iVarId = value; }

        public Property(int iPropId, string sPropStr)
        {
            _iPropId = iPropId;
            _sPropStr = sPropStr;
        }
    }

    internal class COMDevice
    {
        private readonly byte[] LF = { (byte)'\n' };

        private readonly SerialPort _serialPort = new();
        public string PortName => _serialPort.PortName;

        private readonly string _PNPDeviceID;
        public string PNPDeviceID => _PNPDeviceID;

        private string _DeviceName;
        public string DeviceName => _DeviceName;

        private string _ProcessorType;
        public string ProcessorType => _ProcessorType;

        // List of all properties of the HW Device
        private List<Property> _Properties = new();
        public List<Property> Properties => _Properties;    

        // A ConcurrentQueue that gets commands sent from the SimClient to the devices. They are processed in the TxPump
        private readonly BlockingCollection<string> _TxPumpQueue = new();
        public string AddCmdToTxPumpQueue { set => _TxPumpQueue.Add(value); }

        // CancellationTokenSource to stop the pumps
        private CancellationTokenSource _ctPumps;
        
        // Transmit and Receive pumps
        private Task _RxPump;
        private Task _TxPump;
        private bool _bPumpsRunning = false;

        // ManualResetEvent to block the Transmit pump until an Ack is received or timeout occurs
        private readonly ManualResetEvent _mreAck = new(false);

        // Statistics
        private ulong _cmdRxCnt = 0;
        private ulong _cmdTxCnt = 0;
        private ulong _nackCnt = 0;

        public ulong cmdRxCnt { get => _cmdRxCnt; private set => _cmdRxCnt = value; }
        public ulong cmdTxCnt { get => _cmdTxCnt; private set => _cmdTxCnt = value; }
        public ulong nackCnt { get => _nackCnt; private set => _nackCnt = value; }

        public void ResetStatistics() { _cmdRxCnt = 0; _cmdTxCnt = 0; _nackCnt = 0; }

        // constructor
        public COMDevice(string pnpDeviceID, string portName, Int32 baudRate = 500000)
        {
            _serialPort.PortName = portName;
            _serialPort.BaudRate = baudRate;

            _serialPort.Handshake = Handshake.None;

            // format 8-N-1
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;

            _serialPort.NewLine = "\n";

            _serialPort.ReadTimeout = 300;
            _serialPort.WriteTimeout = 100;

            _PNPDeviceID = pnpDeviceID.ToUpper();
        }

        public bool Open()
        {
            bool bSuccess = false;
            int iRetryCount = 0; // Only UnauthorizedAccessAcception will increase iRetryCount - all other attempts will exit

            // Try to open the port maximum 3 times
            while (!bSuccess && iRetryCount < 3)
            {
                try
                {
                    _serialPort.Open();
                    bSuccess = true;
                }
                catch (UnauthorizedAccessException)
                {
                    // This could happen if a port is disconnected and reconnected shortly after - seems that the below method works to recover from that
                    _serialPort.Close();
                    Thread.Sleep(500);
                    Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.Open {PortName}: UnauthorizedAccessException attempt {++iRetryCount}/3");
                }
                catch (TimeoutException ex)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.Open {PortName}: TimeoutException {ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.Open {PortName}: Exception {ex}");
                    return false;
                }
            }

            return bSuccess;
        }

        public bool Close()
        {
            StopPumps();

            try
            {
                _serialPort.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.Close {PortName}: Exception  {ex}");
                return false;
            }
        }

        private void ClearInputBuffer()
        {
            while (true)
            {
                int readCount = _serialPort.BytesToRead;
                if (readCount == 0)
                    break;
                byte[] buffer = new byte[readCount];
                _serialPort.Read(buffer, 0, readCount);
            }
        }

        // Get Properties of the connected HW device
        public bool GetProperties()
        {
            try
            {
                // clean receive buffer (at least, try...)
                _serialPort.Write("\n");
                ClearInputBuffer();

                // make sure that device is in non-registered mode
                _serialPort.Write("RESET\n");
                ClearInputBuffer(); // We should not get anything back, so make sure that all is clean

                // get identification
                _serialPort.Write("IDENT\n");
                _DeviceName = _serialPort.ReadLine();
                _ProcessorType = _serialPort.ReadLine();
                Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.GetProperties {PortName}: New Device found: IDENT = \"{_DeviceName}\" - \"{_ProcessorType}\"");

                // counter for each Property
                int iPropId = 1;

                // get properties to register
                _serialPort.Write("REGISTER\n");

                string sPropStr;
                while ((sPropStr = _serialPort.ReadLine()) != "")
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.GetProperties {PortName}: PropId: {iPropId} - PropStr: \"{sPropStr}\"");
                    _Properties.Add(new Property(iPropId++, sPropStr));
                }

                StartPumps();

                return true;
            }
            catch (TimeoutException ex)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.GetProperties {PortName}: TimeoutException {ex}");
                return false;
            }
            catch (Exception ex)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.GetProperties {PortName}: Exception {ex}");
                return false;
            }
        }

        public void StartPumps()
        {
            if (_bPumpsRunning)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.StartPumps for {PortName}: Pumps already started.");
                return;
            }

            Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.StartPumps for {PortName}.");

            // create the CancellationTokenSource
            _ctPumps = new();

            // Start Transmit Pump
            _TxPump = Task.Run(() => TxPump(_ctPumps.Token), _ctPumps.Token);

            // Start Receive Pump
            _RxPump = Task.Run(() => RxPump(_ctPumps.Token), _ctPumps.Token);

            _bPumpsRunning = true;
        }

        public void StopPumps()
        {
            if (!_bPumpsRunning)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.StopPumps for {PortName}: Pumps already stopped.");
                return;
            }

            Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.StopPumps for {PortName}.");

            // cancel the CancellationTokenSource
            _ctPumps.Cancel(false);

            // wait for the pumps to stop running
            if (!Task.WaitAll(new Task[] { _TxPump, _RxPump }, 600))
                // One task seems not to have stopped in time
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.StopPumps for {PortName}: One of the pumps didn't stop in time.");
            else
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.StopPumps for {PortName}: All pumps stopped in time.");

            // cleanup
            _ctPumps.Dispose();
            _TxPump = null;
            _RxPump = null;

            _bPumpsRunning = false;
        }

        private void RxPump(CancellationToken ct)
        {
            bool bPumpStarted = false;
            var buffer = new byte[1024];
            StringBuilder sbCmd = new ("", 256); // More efficient than string when adding characters

            while (_serialPort.IsOpen && !ct.IsCancellationRequested)
            {
                try
                {
                    // Only for logging purposes
                    if (!bPumpStarted)
                        Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.RxPump for {PortName} started.");
                    bPumpStarted = true;

                    // blocking read
                    int cnt = _serialPort.BaseStream.Read(buffer, 0, 1024);

                    // itterate through all received characters
                    for (int i = 0; i < cnt; i++)
                    {
                        if ((char)buffer[i] != '\n')
                            // keep appending characters until '\n' is received
                            sbCmd.Append((char)buffer[i]);
                        else
                        {
                            if ((sbCmd.Length == 1) && (sbCmd[0] == 'A'))
                                // ACK sequence received, release TxPump
                                _mreAck.Set();
                            else if ((sbCmd.Length >= 4) && (sbCmd[3] == '=') && int.TryParse(sbCmd.ToString().AsSpan(0, 3), out int iPropId))
                            {
                                Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.RxPump for {PortName} command {sbCmd}");
                                // Command received with format 'NNN=...'. Check if it is a valid Property, and if it has its matching iVarId
                                Property prop = _Properties.Find(x => x.iPropId == iPropId);
                                if (prop != null && prop.iSimId != -1 && !ct.IsCancellationRequested)
                                {
                                    // Replace the iPropId with the iSimId, and send the command to the SimClient !!! Check what the lenght of a SimId is - here we take 4 characters
                                    string sCmd = $"{prop.iSimId:D04}{sbCmd.ToString().AsSpan(3)}";
                                    SimClient.AddCmdToTxPumpQueue = sCmd;
                                    cmdRxCnt++;
                                }
                            }
                            sbCmd.Clear();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.RxPump for {PortName} throws OperationCanceledException.");
                }
                catch (TimeoutException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.RxPump for {PortName} throws TimeoutException.");
                }
                catch (Exception ex)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.RxPump for {PortName} throws {ex}");
                }
            }
            Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.RxPump for {PortName} stopped.");
        }

        private void TxPump(CancellationToken ct)
        {
            bool bPumpStarted = false;

            while (_serialPort.IsOpen && !ct.IsCancellationRequested)
            {
                try
                {
                    // Only for logging purposes
                    if (!bPumpStarted)
                        Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.TxPump for {PortName} started.");
                    bPumpStarted = true;

                    // blocking take
                    string sCmd = _TxPumpQueue.Take(ct);

                    byte[] buffer = Encoding.ASCII.GetBytes($"{sCmd}\n");

                    int attempts = 2;

                    while (attempts-- != 0)
                    {
                        //_serialPort.BaseStream.Write(buffer, 0, sCmd.Length + 1);
                        _serialPort.BaseStream.Write(buffer, 0, sCmd.Length + 1);

                        // Reset the ManualResetEvent and wait for ACK for 50 msec
                        _mreAck.Reset();
                        if (_mreAck.WaitOne(150))
                        {
                            cmdTxCnt++;
                            break;
                        }
                        else
                        {
                            nackCnt++;
                            Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.TxPump for {PortName} : Not Ack attempt {{2 - attempts}} for \\\"{{sCmd}}\\\"\"");
                            // Send linefeed to be sure we are in sync
                            _serialPort.BaseStream.Write(LF, 0, 1);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.TxPump for {PortName} throws OperationCanceledException.");
                }
                catch (TimeoutException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.TxPump for {PortName} throws TimeoutException.");
                }
                catch (Exception ex)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.TxPump for {PortName} throws {ex}");
                }
            }
            Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.TxPump for {PortName} stopped.");
        }
    }
}
