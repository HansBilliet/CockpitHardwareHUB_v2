using System.Management;

//  SerialPort manager for C# WPF using Windows Management Instrumentation (WMI)
//  This monitor will produce "Port added", "Port Removed" and "Port Found" events
//  and include the DeviceID, VendorID and ProductID in the EventArgs when an event is raised.
//
//  Make sure to install System.Management in your projects references.
//
//  Start the SerialPortManager with SerialPortManager.ScanPorts()
//  Call SerialPortManager.ScanPorts(false) if you don't want Added or Removed events
//  after the initial scan.
//
//  You can set the VendorID and / or ProductID to filter for matching USB Virtual com ports.
//
//  The reason for this class is to obtain an accurate report on what serial ports are
//  available. The standard method: System.IO.Ports.Serialport.getportnames() just
//  reads the Registry and suffers from caching lag.
//
//  By Paul van Dinther
//  Adapted by Hans Billiet to allow passing the filtered VID, PID and SerialNumber with the call the ScanPorts

namespace CockpitHardwareHUB_v2.Classes
{
    internal class SerialPortEventArgs : EventArgs
    {
        public SerialPortEventArgs(string portName, int vendorID, int productID, string serialNumber, string pnpDeviceID)
        {
            PortName = portName; //  This is the port eg. "COM1"
            VendorID = vendorID;
            ProductID = productID;
            SerialNumber = serialNumber;
            PNPDeviceID = pnpDeviceID;
        }
        public string PortName;
        public int VendorID;
        public int ProductID;
        public string SerialNumber;
        public string PNPDeviceID;
    }

    internal class SerialPortManager
    {
        public event EventHandler<SerialPortEventArgs> OnPortFoundEvent;
        public event EventHandler<SerialPortEventArgs> OnPortAddedEvent;
        public event EventHandler<SerialPortEventArgs> OnPortRemovedEvent;
        private static ManagementEventWatcher _watchingAddedObject = null;
        private static ManagementEventWatcher _watchingRemovedObject = null;
        private static WqlEventQuery _watcherQuery;
        private static ManagementScope _scope;
        private uint _vendorID;
        private uint _productID;
        private string _serialNumber;

        public SerialPortManager()
        {
            _scope = new ManagementScope("root/CIMV2");
            _scope.Options.EnablePrivileges = true;
            AddInsertUSBHandler();
            AddRemoveUSBHandler();
        }

        internal void ScanPorts(bool watchForChanges, uint VendorID, uint ProductID, string SerialNumber)
        {
            _vendorID = VendorID;
            _productID = ProductID;
            _serialNumber = SerialNumber;

            try
            {
                bool checkID = _vendorID + _productID != 0 || !string.IsNullOrEmpty(SerialNumber);
                // Suggested by 'CrossWinnd' on FS forum
                // Apparantly, the original SELECT statement seems not to work with some FTDI chips
                // and by extesion ay RS232 COM type devices.
                // A better SELECT statement that should solve that issue is below.
                // “SELECT DeviceID, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE ‘%(COM[0-9]%’”
                // This should find all devices with names including '(COMn', meaning (COM1), (COM2), ... (COM10), ...
                string queryString = "SELECT DeviceID, PNPDeviceID FROM Win32_SerialPort";

                if (checkID)
                {
                    List<string> conditions = new List<string>();
                    if (_vendorID != 0)
                        conditions.Add("PNPDeviceID Like '%VID_" + _vendorID.ToString("X4") + "%'");
                    if (_productID != 0)
                        conditions.Add("PNPDeviceID Like '%PID_" + _productID.ToString("X4") + "%'");
                    if (!string.IsNullOrEmpty(SerialNumber))
                        conditions.Add("PNPDeviceID Like '%\\\\" + SerialNumber + "'");

                    queryString += " WHERE " + string.Join(" AND ", conditions);
                }

                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", queryString);
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    OnPortFoundEvent?.Invoke(this, CreatePortArgs(queryObj));
                }

                if (watchForChanges)
                {
                    _watchingAddedObject.Start();
                    _watchingRemovedObject.Start();
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }
        }

        internal void Stop()
        {
            _watchingAddedObject.Stop();
            _watchingRemovedObject.Stop();
        }

        private void AddInsertUSBHandler()
        {
            try
            {
                _watchingAddedObject = USBWatcherSetUp("__InstanceCreationEvent");
                _watchingAddedObject.EventArrived += new EventArrivedEventHandler(HandlePortAdded);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (_watchingAddedObject != null)
                    _watchingAddedObject.Stop();
            }
        }

        private void AddRemoveUSBHandler()
        {
            try
            {
                _watchingRemovedObject = USBWatcherSetUp("__InstanceDeletionEvent");
                _watchingRemovedObject.EventArrived += new EventArrivedEventHandler(HandlePortRemoved);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (_watchingRemovedObject != null)
                    _watchingRemovedObject.Stop();
            }
        }

        private ManagementEventWatcher USBWatcherSetUp(string eventType)
        {
            _watcherQuery = new WqlEventQuery();
            _watcherQuery.EventClassName = eventType;
            _watcherQuery.WithinInterval = new TimeSpan(0, 0, 2);
            _watcherQuery.Condition = @"TargetInstance ISA 'Win32_SerialPort'";
            return new ManagementEventWatcher(_scope, _watcherQuery);
        }

        private SerialPortEventArgs CreatePortArgs(ManagementBaseObject queryObj)
        {
            string PNPDeviceID = ((string)queryObj.GetPropertyValue("PNPDeviceID")).ToUpper();
            int vid = 0;
            int pid = 0;
            string serialNumber = "";

            int index = PNPDeviceID.IndexOf("VID_");
            if (index > -1 && PNPDeviceID.Length >= index + 8)
            {
                string id = PNPDeviceID.Substring(index + 4, 4);
                vid = Convert.ToInt32(id, 16);
            }

            index = PNPDeviceID.IndexOf("PID_");
            if (index > -1 && PNPDeviceID.Length >= index + 8)
            {
                string id = PNPDeviceID.Substring(index + 4, 4);
                pid = Convert.ToInt32(id, 16);
            }

            index += 9;
            if (PNPDeviceID.Length > index)
                serialNumber = PNPDeviceID.Substring(index);

            return new SerialPortEventArgs((string)queryObj["DeviceID"], vid, pid, serialNumber, PNPDeviceID);
        }

        private bool CheckIDMatch(SerialPortEventArgs serialPortEventArgs)
        {
            if (_vendorID + _productID != 0 || !string.IsNullOrEmpty(_serialNumber))
            {
                return (_vendorID == 0 || serialPortEventArgs.VendorID == _vendorID) &&
                       (_productID == 0 || serialPortEventArgs.ProductID == _productID) &&
                       (string.IsNullOrEmpty(_serialNumber) || serialPortEventArgs.SerialNumber == _serialNumber);
            }
            return true;
        }

        private void HandlePortAdded(object sender, EventArrivedEventArgs e)
        {
            var instance = e.NewEvent.GetPropertyValue("TargetInstance") as ManagementBaseObject;
            SerialPortEventArgs serialPortEventArgs = CreatePortArgs(instance);
            if (CheckIDMatch(serialPortEventArgs))
            {
                OnPortAddedEvent?.Invoke(this, serialPortEventArgs);
            }
        }

        private void HandlePortRemoved(object sender, EventArrivedEventArgs e)
        {
            var instance = e.NewEvent.GetPropertyValue("TargetInstance") as ManagementBaseObject;
            SerialPortEventArgs serialPortEventArgs = CreatePortArgs(instance);
            if (CheckIDMatch(serialPortEventArgs))
            {
                OnPortRemovedEvent?.Invoke(this, serialPortEventArgs);
            }
        }
    }
}
