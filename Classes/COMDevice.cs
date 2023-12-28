﻿using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using WASimCommander.CLI.Enums;

namespace CockpitHardwareHUB_v2.Classes
{
    // This Property Class maps a HW device Property against a registered SimVar in the SimClient
    internal class Property
    {
        private readonly string _sPropStr; // This is the full string of the Property (ToUpper) as sent by the HW device
        private int _iSimId = -1; // Once registered as a SimVar in the SimClient, a VarId is given

        internal string sPropStr => _sPropStr;
        internal int iSimId { get => _iSimId; set => _iSimId = value; }

        internal Property(string sPropStr) => _sPropStr = sPropStr.ToUpper();
    }

    internal class COMDevice
    {
        // creates unique DeviceId
        static private int _iNewDeviceId = 0;

        private readonly int _iDeviceId;

        private readonly byte[] LF = { (byte)'\n' };

        private readonly SerialPort _serialPort = new();
        internal string PortName => _serialPort.PortName;

        private readonly string _PNPDeviceID;
        internal string PNPDeviceID => _PNPDeviceID;

        private string _DeviceName;
        internal string DeviceName => _DeviceName;

        private string _ProcessorType;
        internal string ProcessorType => _ProcessorType;

        internal string UniqueName => $"{_iDeviceId:D02}\\{_serialPort.PortName}\\{(_DeviceName == "" ? "UNKNOWN" : _DeviceName)}";

        // List of property strings - Property ID = [index + 1]
        private readonly List<Property> _Properties = new();
        internal IReadOnlyCollection<Property> Properties => _Properties;

        // A ConcurrentQueue that gets commands from SimClient.DataSubscriptionHandler (via SimVar.DispatchSimVar) and processed in TxPump.
        private readonly BlockingCollection<string> _TxPumpQueue = new();

        // CancellationTokenSource to stop the pumps
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
            return $"{UniqueName}";
        }

        internal bool Open()
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

        internal bool Close()
        {
            StopPumps();

            foreach (Property property in _Properties)
                PropertyPool.RemovePropertyFromPool(this, property.iSimId);

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
        internal bool GetProperties()
        {
            try
            {
                // remove all earlier received properties in both dictionaries (although, at this point in time, _PropertiesBySimVar will be empty)
                _Properties.Clear();

                // clean receive buffer (at least, try...)
                _serialPort.Write("\n");
                ClearInputBuffer();

                // make sure that device is in non-registered mode
                _serialPort.Write("RESET\n");

                // TODO: Improvement in HW - after RESET we should not send acknowledge - now, let's wait 200 msec, and then clear the input buffer
                Thread.Sleep(200);

                ClearInputBuffer(); // We should not get anything back, so make sure that all is clean

                // get identification
                _serialPort.Write("IDENT\n");
                _DeviceName = _serialPort.ReadLine();
                _ProcessorType = _serialPort.ReadLine();
                Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.GetProperties: {this} IDENT = \"{_DeviceName}\" - \"{_ProcessorType}\"");

                // get properties to register
                _serialPort.Write("REGISTER\n");

                int iPropId = 1; // only for logging purposes
                string sPropStr;
                while ((sPropStr = _serialPort.ReadLine()) != "")
                {
                    // Add each property in the property list of the COMDevice - the PropertyId is the index + 1
                    _Properties.Add(new Property(sPropStr));
                    Logging.LogLine(LogLevel.Debug, LoggingSource.DEV, $"COMDevice.GetProperties: {this} REGISTER {iPropId++} = \"{sPropStr}\"");
                }

                // During 'ReadLine()', exceptions can be thrown which aborts the 'GetProperties()'.
                // Because of that, 'AddPropertyInPool()' is only called when all Properties are loaded successfully, otherwise multiple usage might occur.
                // We use For-loop, because we need it in the call to 'AddPropertyInPool()' - be aware that the index is [iPropId - 1]
                for (iPropId = 1; iPropId <= _Properties.Count; iPropId++)
                {
                    Property property = _Properties[iPropId - 1];
                    // Add the Property to the Pool. If successfully parsed, we will get a SimId not equal to -1.
                    property.iSimId = PropertyPool.AddPropertyInPool(this, iPropId, property.sPropStr);
                }

                // Start the TxPump and RxPump
                StartPumps();

                return true;
            }
            catch (TimeoutException ex)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.GetProperties: {this} TimeoutException {ex}");
                return false;
            }
            catch (Exception ex)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.GetProperties: {this} Exception {ex}");
                return false;
            }
        }

        internal void StartPumps()
        {
            // Be suspicious, and assume that the call needs to be re-entrant, hence make it threadsafe
            // If IsStarted == 0, then make it 1, and return the previous value 0 --> execute Start
            // If IsStarted == 1, then don't change it, and return the previous value 1 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 1, 0) == 1)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.StartPumps: {this} Pumps already started");
                return; // Already started
            }

            // create the CancellationTokenSource
            _ctPumps = new();

            // Start Transmit Pump
            _TxPump = Task.Run(() => TxPump(_ctPumps.Token), _ctPumps.Token);

            // Start Receive Pump
            _RxPump = Task.Run(() => RxPump(_ctPumps.Token), _ctPumps.Token);

            Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.StartPumps: {this} Pumps started");
        }

        internal void StopPumps()
        {
            // Be suspicious, and assume that the call needs to be re-entrant, hence make it threadsafe
            // If IsStarted == 1, then make it 0, and return the previous value 1 --> execute Start
            // If IsStarted == 0, then don't change it, and return the previous value 0 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 0, 1) == 0)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.StopPumps: {this} Pumps already stopped");
                return; // Already stopped
            }

            // cancel the CancellationTokenSource
            _ctPumps.Cancel(false);

            // wait for the pumps to stop running
            if (!Task.WaitAll(new Task[] { _TxPump, _RxPump }, 600))
                // One task seems not to have stopped in time
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.StopPumps: {this} One of the pumps didn't stop in time");
            else
                Logging.LogLine(LogLevel.Debug, LoggingSource.DEV, $"COMDevice.StopPumps: {this} All pumps stopped in time");

            // cleanup
            _ctPumps.Dispose();
            _TxPump = null;
            _RxPump = null;

            Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.StopPumps: {this} Pumps stopped");
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
                        Logging.LogLine(LogLevel.Debug, LoggingSource.DEV, $"COMDevice.RxPump: {this} RxPump started");
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
                                // Command received with format 'NNN=...'. Check if it is a valid Property, and if it has its matching iVarId
                                Logging.LogLine(LogLevel.Debug, LoggingSource.DEV, $"COMDevice.RxPump: {this} Command \"{sbCmd}\"");
                                if (!ct.IsCancellationRequested)
                                {
                                    PropertyPool.TriggerProperty(_Properties[iPropId-1].iSimId, sbCmd.ToString().AsSpan(4).ToString());
                                    stats._cmdRxCnt = Interlocked.Increment(ref stats._cmdRxCnt);
                                }
                            }
                            sbCmd.Clear();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.RxPump: {this} OperationCanceledException");
                }
                catch (TimeoutException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.RxPump: {this} TimeoutException");
                }
                catch (Exception ex)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.RxPump: {this} Exception {ex}");
                }
            }
            Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.RxPump: {this} RxPump stopped");
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
                        Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.TxPump: {this} TxPump started");
                    bPumpStarted = true;

                    // blocking take
                    string sCmd = _TxPumpQueue.Take(ct);
                    Logging.LogLine(LogLevel.Debug, LoggingSource.DEV, $"COMDevice.TxPump: {this} Command \"{sCmd}\"");

                    byte[] buffer = Encoding.ASCII.GetBytes($"{sCmd}\n");

                    int attempts = 2;

                    while (attempts-- != 0)
                    {
                        _serialPort.BaseStream.Write(buffer, 0, sCmd.Length + 1);

                        // Reset the ManualResetEvent and wait for ACK for 50 msec
                        _mreAck.Reset();
                        if (_mreAck.WaitOne(150))
                        {
                            stats._cmdTxCnt = Interlocked.Increment(ref stats._cmdTxCnt);
                            break;
                        }
                        else
                        {
                            stats._nackCnt = Interlocked.Increment(ref stats._nackCnt);
                            Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.TxPump: {this} Not Ack for attempt {2 - attempts} for \"{sCmd}\"");
                            // Send linefeed to be sure we are in sync
                            _serialPort.BaseStream.Write(LF, 0, 1);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.TxPump: {this} OperationCanceledException");
                }
                catch (TimeoutException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.DEV, $"COMDevice.TxPump: {this} TimeoutException");
                }
                catch (Exception ex)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.TxPump: {this} Exception {ex}");
                }
            }
            Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.TxPump: {this} TxPump stopped");
        }

        internal void AddCmdToTxPumpQueue(int iPropId, string sData)
        {
            string sCmd = $"{iPropId:D03}={sData}";
            _TxPumpQueue.Add(sCmd);
            Logging.LogLine(LogLevel.Debug, LoggingSource.DEV, $"COMDevice.AddCmdToTxPumpQueue: {this} \"{sCmd}\""); // Is this thread safe, and does it need to be?
        }
    }
}
