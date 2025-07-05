using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using WASimCommander.CLI.Enums;

namespace CockpitHardwareHUB_v2.Classes
{
    // This Property Class maps a HW device Property against a registered SimVar in the SimClient
    internal class Property
    {
        private readonly string _sPropStr; // This is the full string of the Property (ToUpper) as sent by the HW device
        private int _iVarId = -1; // Once registered as a SimVar in the SimClient, a VarId is given

        internal string sPropStr => _sPropStr;
        internal int iVarId { get => _iVarId; set => _iVarId = value; }

        internal Property(string sPropStr) => _sPropStr = sPropStr.ToUpper();
    }

    internal class COMDevice
    {
        // creates unique DeviceId
        static private int _iNewDeviceId = 0;
        private readonly int _iDeviceId;

        private readonly SerialPort _serialPort = new();

        private readonly byte[] LF = { (byte)'\n' };

        private readonly bool _bVirtualDevice = false;

        internal string PortName { get; }

        internal string PNPDeviceID { get; }

        private string _DeviceName;
        internal string DeviceName => _DeviceName;

        private string _ProcessorType;
        internal string ProcessorType => _ProcessorType;

        internal string UniqueName => $"{_iDeviceId:D02}\\{PortName}\\{(_DeviceName == "" ? "UNKNOWN" : _DeviceName)}";

        // List of property strings. Be aware that the 'Property ID' (ID used in device) = [index + 1]
        private readonly List<Property> _Properties = new();
        internal IReadOnlyCollection<Property> Properties => _Properties;

        // A ConcurrentQueue that gets commands from SimClient.DataSubscriptionHandler (via SimVar.DispatchSimVar) and processed in TxPump.
        private readonly BlockingCollection<string> _TxPumpQueue = new();

        // CancellationTokenSource to Stop the pumps
        private CancellationTokenSource _ctPumps;
        
        // Transmit and Receive pumps
        private Task _RxPump;
        private Task _TxPump;
        private int IsStarted = 0;

        // ManualResetEvent to block the Transmit pump until an Ack is received or timeout occurs
        private readonly ManualResetEvent _mreAck = new(false);

        // Statistics
        private class Statistics
        {
            internal ulong _cmdRxCnt = 0;
            internal ulong _cmdTxCnt = 0;
            internal ulong _nackCnt = 0;
        }
        private Statistics stats = new Statistics();
        internal ulong cmdRxCnt => Interlocked.Read(ref stats._cmdRxCnt);
        internal ulong cmdTxCnt => Interlocked.Read(ref stats._cmdTxCnt);
        internal ulong nackCnt => Interlocked.Read(ref stats._nackCnt);
        internal void ResetStatistics()
        { 
            Interlocked.Exchange(ref stats._cmdRxCnt, 0); 
            Interlocked.Exchange(ref stats._cmdTxCnt, 0); 
            Interlocked.Exchange(ref stats._nackCnt, 0); 
        }

        // constructor
        internal COMDevice(string pnpDeviceID, string portName, Int32 baudRate = 500000)
        {
            _iDeviceId = Interlocked.Increment(ref _iNewDeviceId);

            _bVirtualDevice = (portName == "VIRTUAL");
            PortName = portName;

            if (!_bVirtualDevice)
            {
                _serialPort.PortName = portName;
                _serialPort.BaudRate = baudRate;

                _serialPort.Handshake = Handshake.None;

                // format 8-N-1
                _serialPort.DataBits = 8;
                _serialPort.Parity = Parity.None;
                _serialPort.StopBits = StopBits.One;

                // the below forces Arduino's to reboot
                _serialPort.DtrEnable = true;
                _serialPort.RtsEnable = true;

                _serialPort.NewLine = "\n";

                // initial ReadTimeout of 2 seconds is to guarantee that Arduino's left the boot-cycle
                // this is unlikely to happen anyway, because the initial "Ack-sequence" should have occured earlier
                _serialPort.ReadTimeout = 2000;
                _serialPort.WriteTimeout = 100;
            }
            else
            {
                _DeviceName = "VIRTUAL";
                _ProcessorType = "N/A";
            }

            PNPDeviceID = pnpDeviceID.ToUpper();
        }

        public override bool Equals(object obj)
        {
            // If the object is the same instance, return true
            if (ReferenceEquals(this, obj)) return true;

            // If the object is not a COMDevice or is null, return false
            var other = obj as COMDevice;
            if (other == null) return false;

            // Compare the _iDeviceId of both COMDevice instances
            return (this._iDeviceId == other._iDeviceId);
        }

        public static bool operator ==(COMDevice left, COMDevice right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left._iDeviceId == right._iDeviceId;
        }

        public static bool operator !=(COMDevice left, COMDevice right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            // Use the _iDeviceId which is unique for each COMDevice
            return _iDeviceId;
        }

        public override string ToString()
        {
            return UniqueName;
        }

        internal bool Open()
        {
            if (_bVirtualDevice)
                return true;

            bool bSuccess = false;
            int iRetryCount = 0; // Only UnauthorizedAccessAcception will increase iRetryCount - all other attempts will exit

            // Try to open the port maximum 3 times
            while (!bSuccess && iRetryCount < 3)
            {
                try
                {
                    _serialPort.Open();

                    // Wait for Ack-Sequence. This is a way to be sure that an Arduino has left its boot cycle.
                    // Devices not sending a Ack-Sequence will result in a TimeoutException after 2 seconds.
                    _serialPort.ReadLine();
                    _serialPort.ReadTimeout = 200; // reduce ReadTimeout

                    bSuccess = true;
                }
                catch (UnauthorizedAccessException)
                {
                    // This could happen if a port is disconnected and reconnected shortly after - seems that the below method works to recover from that
                    _serialPort.Close();
                    Thread.Sleep(500);
                    Logging.Log(LogLevel.Info, LoggingSource.DEV, () => $"COMDevice.Open {PortName}: UnauthorizedAccessException attempt {++iRetryCount}/3");
                }
                catch (TimeoutException ex)
                {
                    // This can only be caused by the 'ReadLine()'.
                    // It means that we deal with a device that is not sending an Ack-sequence on startup, which is fine.
                    Logging.Log(LogLevel.Info, LoggingSource.DEV, () => $"COMDevice.Open {PortName}: TimeoutException (missing Ack-sequence) {ex.Message}");
                    _serialPort.ReadTimeout = 200; // reduce ReadTimeout
                    return true;
                }
                catch (Exception ex)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.Open {PortName}: Exception {ex.Message}");
                    return false;
                }
            }

            return bSuccess;
        }

        internal bool Close()
        {
            if (!_bVirtualDevice)
                StopPumps();

            foreach (Property property in _Properties)
                PropertyPool.RemovePropertyFromPool(this, property.iVarId);

            if (_bVirtualDevice)
                return true;

            try
            {
                _serialPort.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.Close {PortName}: Exception {ex.Message}");
                return false;
            }
        }

        private void ClearInputBuffer()
        {
            if (_bVirtualDevice)
                return;

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
        internal bool GetProperties()
        {
            if (_bVirtualDevice)
                return false;

            try
            {
                // remove all earlier received properties in case we call GetProperties again after a possible failure
                _Properties.Clear();

                // clean receive buffer (at least, try...)
                _serialPort.Write("\n");
                ClearInputBuffer();

                // make sure that device is in non-registered mode
                _serialPort.Write("RESET\n");

                // TODO: Improvement in HW - after RESET, a device should not send acknowledge - but now it does, so wait 200msec and clear the inputbuffer
                Thread.Sleep(200);

                ClearInputBuffer(); // We should not get anything back, so make sure that all is clean

                // get identification
                _serialPort.Write("IDENT\n");
                _DeviceName = _serialPort.ReadLine();
                _ProcessorType = _serialPort.ReadLine();
                Logging.Log(LogLevel.Info, LoggingSource.DEV, () => $"COMDevice.GetProperties: {this} IDENT = \"{_DeviceName}\" - \"{_ProcessorType}\"");

                // get properties to register
                _serialPort.Write("REGISTER\n");
                int iPropId = 1; // only for logging purposes
                string sPropStr;
                while ((sPropStr = _serialPort.ReadLine()) != "")
                {
                    // Add each property in the property list of the COMDevice - the PropertyId is the index + 1
                    _Properties.Add(new Property(sPropStr));
                    Logging.Log(LogLevel.Debug, LoggingSource.DEV, () => $"COMDevice.GetProperties: {this} REGISTER {iPropId} = \"{sPropStr}\"");
                    iPropId++;
                }

                // During 'ReadLine()', exceptions can be thrown which aborts the 'GetProperties()'.
                // Because of that, 'AddPropertyInPool()' is only called after all Properties are loaded successfully
                // We use For-loop, because we need the iPropId in the call to 'AddPropertyInPool()' - be aware that the index is [iPropId - 1]
                for (iPropId = 1; iPropId <= _Properties.Count; iPropId++)
                {
                    Property property = _Properties[iPropId - 1];
                    // Add the Property to the Pool. If successfully parsed, we will get a SimId not equal to -1.
                    property.iVarId = PropertyPool.AddPropertyInPool(this, iPropId, property.sPropStr, out _);
                }

                // Start the TxPump and RxPump
                StartPumps();

                return true;
            }
            catch (TimeoutException ex)
            {
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.GetProperties: {this} TimeoutException {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.GetProperties: {this} Exception {ex.Message}");
                return false;
            }
        }

        // AddProperty is only used for the VIRTUAL COMDevice, and allows manually adding properties one by one
        internal PR AddProperty(string sPropStr)
        {
            int iPropId = _Properties.Count; // Take next Property Id
            Property property = new Property(sPropStr);
            property.iVarId = PropertyPool.AddPropertyInPool(this, iPropId, property.sPropStr, out PR parseResult);

            //if (property.iVarId != -1)
                _Properties.Add(property);

            return parseResult;
        }

        internal void StartPumps()
        {
            if (_bVirtualDevice)
            {
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.StartPumps: Operation not allowed for Virtual Device");
                return;
            }

            // Be suspicious, and assume that the call needs to be re-entrant, hence make it threadsafe
            // If IsStarted == 0, then make it 1, and return the previous value 0 --> execute Start
            // If IsStarted == 1, then don't change it, and return the previous value 1 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 1, 0) == 1)
            {
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.StartPumps: {this} Pumps already started");
                return; // Already started
            }

            // create the CancellationTokenSource
            _ctPumps = new();

            // Start Transmit Pump
            _TxPump = Task.Run(() => TxPump(_ctPumps.Token), _ctPumps.Token);

            // Start Receive Pump
            _RxPump = Task.Run(() => RxPump(_ctPumps.Token), _ctPumps.Token);

            Logging.Log(LogLevel.Info, LoggingSource.DEV, () => $"COMDevice.StartPumps: {this} Pumps started");
        }

        internal void StopPumps()
        {
            if (_bVirtualDevice)
            {
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.StopPumps: Operation not allowed for Virtual Device");
                return;
            }

            // Be suspicious, and assume that the call needs to be re-entrant, hence make it threadsafe
            // If IsStarted == 1, then make it 0, and return the previous value 1 --> execute Start
            // If IsStarted == 0, then don't change it, and return the previous value 0 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 0, 1) == 0)
            {
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.StopPumps: {this} Pumps already stopped");
                return; // Already stopped
            }

            // cancel the CancellationTokenSource
            _ctPumps.Cancel(false);

            // wait for the pumps to Stop running
            if (!Task.WaitAll(new Task[] { _TxPump, _RxPump }, 600))
                // One task seems not to have stopped in time
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.StopPumps: {this} One of the pumps didn't Stop in time");
            else
                Logging.Log(LogLevel.Debug, LoggingSource.DEV, () => $"COMDevice.StopPumps: {this} All pumps stopped in time");

            // cleanup
            _ctPumps.Dispose();
            _TxPump = null;
            _RxPump = null;

            Logging.Log(LogLevel.Info, LoggingSource.DEV, () => $"COMDevice.StopPumps: {this} Pumps stopped");
        }

        private void RxPump(CancellationToken ct)
        {
            bool bPumpStarted = false;
            var buffer = new byte[1024];
            StringBuilder sbCmd = new ("", 256); // More efficient than string when adding characters

            if (_bVirtualDevice)
            {
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.RxPump: Operation not allowed for Virtual Device");
                return;
            }

            while (_serialPort.IsOpen && !ct.IsCancellationRequested)
            {
                try
                {
                    // Only for logging purposes
                    if (!bPumpStarted)
                        Logging.Log(LogLevel.Debug, LoggingSource.DEV, () => $"COMDevice.RxPump: {this} RxPump started");
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
                            else if ((sbCmd.Length >= 4) && int.TryParse(sbCmd.ToString().AsSpan(0, 3), out int iPropId))
                            {
                                // Command received with format 'NNN=...' or 'NNN?'
                                Logging.Log(LogLevel.Debug, LoggingSource.DEV, () => $"COMDevice.RxPump: {this} Command = \"{sbCmd}\"");
                                if (!ct.IsCancellationRequested)
                                {
                                    switch (sbCmd[3])
                                    {
                                        case '=': // Command received with format 'NNN=...'.
                                            PropertyPool.TriggerProperty(_Properties[iPropId - 1].iVarId, sbCmd.ToString().AsSpan(4).ToString());
                                            break;

                                        case '?': // Command received with format 'NNN?'.

                                            PropertyPool.FetchProperty(_Properties[iPropId - 1].iVarId);
                                            break;

                                        default:
                                            Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.RxPump: {this} Unknown command format \"{sbCmd}\"");
                                            break;
                                    }
                                    stats._cmdRxCnt = Interlocked.Increment(ref stats._cmdRxCnt);
                                }
                            }

                            sbCmd.Clear();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging.Log(LogLevel.Trace, LoggingSource.DEV, () => $"COMDevice.RxPump: {this} OperationCanceledException");
                }
                catch (TimeoutException)
                {
                    Logging.Log(LogLevel.Trace, LoggingSource.DEV, () => $"COMDevice.RxPump: {this} TimeoutException");
                }
                catch (Exception ex)
                {
                    Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.RxPump: {this} Exception {ex.Message}");
                }
            }
            Logging.Log(LogLevel.Info, LoggingSource.DEV, () => $"COMDevice.RxPump: {this} RxPump stopped");
        }

        private void TxPump(CancellationToken ct)
        {
            bool bPumpStarted = false;

            if (_bVirtualDevice)
            {
                Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.TxPump: Operation not allowed for Virtual Device");
                return;
            }

            while (_serialPort.IsOpen && !ct.IsCancellationRequested)
            {
                try
                {
                    // Only for logging purposes
                    if (!bPumpStarted)
                        Logging.Log(LogLevel.Info, LoggingSource.DEV, () => $"COMDevice.TxPump: {this} TxPump started");
                    bPumpStarted = true;

                    // blocking take
                    string sCmd = _TxPumpQueue.Take(ct);
                    Logging.Log(LogLevel.Debug, LoggingSource.DEV, () => $"COMDevice.TxPump: {this} Command = \"{sCmd}\"");

                    byte[] buffer = Encoding.ASCII.GetBytes($"{sCmd}\n");

                    int attempts = 2;

                    while (attempts-- != 0)
                    {
                        _serialPort.BaseStream.Write(buffer, 0, sCmd.Length + 1);

                        // Reset the ManualResetEvent and wait for ACK for 150 msec
                        _mreAck.Reset();
                        if (_mreAck.WaitOne(150))
                        {
                            stats._cmdTxCnt = Interlocked.Increment(ref stats._cmdTxCnt);
                            break;
                        }
                        else
                        {
                            stats._nackCnt = Interlocked.Increment(ref stats._nackCnt);
                            Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.TxPump: {this} Not Ack for attempt {2 - attempts} for \"{sCmd}\"");
                            // Send linefeed to be sure we are in sync
                            _serialPort.BaseStream.Write(LF, 0, 1);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging.Log(LogLevel.Trace, LoggingSource.DEV, () => $"COMDevice.TxPump: {this} OperationCanceledException");
                }
                catch (TimeoutException)
                {
                    Logging.Log(LogLevel.Trace, LoggingSource.DEV, () => $"COMDevice.TxPump: {this} TimeoutException");
                }
                catch (Exception ex)
                {
                    Logging.Log(LogLevel.Error, LoggingSource.DEV, () => $"COMDevice.TxPump: {this} Exception {ex.Message}");
                }
            }
            Logging.Log(LogLevel.Info, LoggingSource.DEV, () => $"COMDevice.TxPump: {this} TxPump stopped");
        }

        internal void AddCmdToTxPumpQueue(int iPropId, string sData)
        {
            string sCmd = $"{iPropId:D03}={sData}";
            Logging.Log(LogLevel.Debug, LoggingSource.DEV, () => $"COMDevice.AddCmdToTxPumpQueue: {this} \"{sCmd}\"");
            _TxPumpQueue.Add(sCmd);
        }
    }
}
