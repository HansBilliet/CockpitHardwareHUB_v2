using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WASimCommander.CLI.Enums;

namespace CockpitHardwareHUB_v2.Classes
{
    internal class Property
    {
        private readonly int _iPropId = -1;
        private readonly string _sPropStr;
        private int _iSimId = -1;

        public int iPropId { get => _iPropId; }
        public string sPropStr { get => _sPropStr; }
        public int iSimId { get => _iSimId; set => _iSimId = value; }

        public Property(int iPropId, string sPropStr)
        {
            _iPropId = iPropId;
            _sPropStr = sPropStr;
        }
    }

    internal class COMDevice
    {
        private readonly byte[] LF = { (byte)'\n' };

        private SerialPort _serialPort = new SerialPort();

        private string _PNPDeviceID;
        private string _DeviceName;
        private string _ProcessorType;

        public List<Property> _Properties = new List<Property>();

        public string PNPDeviceID
        {
            get => _PNPDeviceID;
            set
            {
                _PNPDeviceID = value.ToUpper();
            }
        }

        public string PortName { get => _serialPort.PortName; }

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

            _serialPort.ReadTimeout = 1000;
            _serialPort.WriteTimeout = 100;

            PNPDeviceID = pnpDeviceID;
        }

        public bool Open()
        {
            bool bSuccess = false;
            int iRetryCount = 0;

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
                    Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.Open {_serialPort.PortName}: UnauthorizedAccessException - wait 500 msec");
                    _serialPort.Close();
                    Thread.Sleep(500);
                    Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.Open {_serialPort.PortName}: UnauthorizedAccessException - retry");
                    iRetryCount++;
                }
                catch (TimeoutException ex)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.Open {_serialPort.PortName}: TimeoutException {ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.Open {_serialPort.PortName}: Exception {ex}");
                    return false;
                }
            }

            return bSuccess;
        }

        public bool Close()
        {
            if (!_serialPort.IsOpen)
                return false;

            try
            {
                _serialPort.Write("RESET\n");
                _serialPort.Close();

                //Task[] tasks = { _TxPump, _RxPump };
                //_src.Cancel(false);
                //Task.WaitAll(tasks, 100);
                //_TxQueue.Dispose();
                //_TxQueue = null;
                return true;
            }
            catch (Exception ex)
            {
                Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.Close {_serialPort.PortName}: Exception  {ex}");
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

        public bool GetProperties()
        {
            try
            {
                // clean receive buffer
                _serialPort.Write("\n");
                ClearInputBuffer();

                // make sure that device is in non-registered mode
                _serialPort.Write("RESET\n");

                // get identification
                _serialPort.Write("IDENT\n");
                _DeviceName = _serialPort.ReadLine();
                _ProcessorType = _serialPort.ReadLine();
                Logging.LogLine(LogLevel.Info, LoggingSource.DEV, $"COMDevice.GetProperties {_serialPort.PortName}: New Device found: IDENT = \"{_DeviceName}\" - \"{_ProcessorType}\"");

                // counter for each Variable
                int iPropId = 1;

                // get properties to register
                _serialPort.Write("REGISTER\n");

                string sPropStr;
                while ((sPropStr = _serialPort.ReadLine()) != "")
                {
                    Logging.LogLine(LogLevel.Debug, LoggingSource.DEV, $"COMDevice.GetProperties {_serialPort.PortName}: PropId: {iPropId} - PropStr: \"{sPropStr}\"");
                    _Properties.Add(new Property(iPropId++, sPropStr));
                }

                return true;
            }
            catch (TimeoutException ex)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.GetProperties {_serialPort.PortName}: TimeoutException {ex}");
                return false;
            }
            catch (Exception ex)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.DEV, $"COMDevice.GetProperties {_serialPort.PortName}: Exception {ex}");
                return false;
            }
        }
    }
}
