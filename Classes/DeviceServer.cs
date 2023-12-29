using System.Collections.Concurrent;
using WASimCommander.CLI.Enums;

namespace CockpitHardwareHUB_v2.Classes
{
    internal static class DeviceServer
    {
        internal delegate void UIAddDevice_Handler(COMDevice device);
        internal static event UIAddDevice_Handler UIAddDevice;
        internal delegate void UIRemoveDevice_Handler(COMDevice device);
        internal static event UIRemoveDevice_Handler UIRemoveDevice;

        private static int IsStarted = 0;

        private static readonly SerialPortManager _SerialPortManager = new();

        private static readonly List<COMDevice> _devices = new();

        public static COMDevice FindDeviceBasedOnPNPDeviceID(string pnpDeviceID)
        {
            lock(_devices)
                return _devices.FirstOrDefault(device => device.PNPDeviceID == pnpDeviceID);
        }

        // SerialPortManager Events

        // This is an event handler that is called when USB devices are already connected
        private static void OnPortFoundEvent(object sender, SerialPortEventArgs spea)
        {
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"DeviceServer.OnPortFoundEvent: {spea.PortName}\\{spea.PNPDeviceID} found");
            Task.Run(() => AddDevice(spea.PNPDeviceID, spea.PortName));
        }

        // This is an event handler that is called when USB devices are added
        private static void OnPortAddedEvent(object sender, SerialPortEventArgs spea)
        {
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"DeviceServer.OnPortAddedEvent: {spea.PortName}\\{spea.PNPDeviceID} added");
            Task.Run(() => AddDevice(spea.PNPDeviceID, spea.PortName));
        }

        // This is an event handler that is called when USB devices are removed
        private static void OnPortRemovedEvent(object sender, SerialPortEventArgs spea)
        {
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"DeviceServer.OnPortRemovedEvent: {spea.PortName}\\{spea.PNPDeviceID} removed");
            Task.Run(() => RemoveDevice(spea.PNPDeviceID));
        }

        public static void Init() // MainForm mainForm)
        {
            //_MainForm = mainForm;

            // Setup event handlers for scanning serial ports
            _SerialPortManager.OnPortFoundEvent += OnPortFoundEvent;
            _SerialPortManager.OnPortAddedEvent += OnPortAddedEvent;
            _SerialPortManager.OnPortRemovedEvent += OnPortRemovedEvent;

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, "DeviceServer.Init: DeviceServer Initialized");
        }

        public static void Start()
        {
            // Be suspicious, and assume that the call needs to be re-entrant, hence make it threadsafe
            // If IsStarted == 0, then make it 1, and return the previous value 0 --> execute Start
            // If IsStarted == 1, then don't change it, and return the previous value 1 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 1, 0) == 1)
                return; // Already started

            // start scanning for serial ports
            // - already connected USB devices will be "found"
            // - new connected USB devices will be "added"
            _SerialPortManager.scanPorts(true);

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, "DeviceServer.Start: DeviceServer Started");
        }

        public static async Task Stop()
        {
            // Be suspicious, and assume that the call needs to be re-entrant, hence make it threadsafe
            // If IsStarted == 1, then make it 0, and return the previous value 1 --> execute Start
            // If IsStarted == 0, then don't change it, and return the previous value 0 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 0, 1) == 0)
                return; // Already stopped

            // start scanning for serial ports
            _SerialPortManager.stop();

            var removalTasks = new List<Task>();
            lock (_devices)
            {
                foreach (COMDevice device in _devices)
                {
                    var task = Task.Run(() => RemoveDevice(device));
                    removalTasks.Add(task);
                }
            }

            // Asynchronously wait for all tasks to complete
            await Task.WhenAll(removalTasks);

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, "DeviceServer.Stop: DeviceServer Stopped");
        }

        internal static void AddDevice(string PNPDeviceID, string DeviceID)
        {
            if (DeviceID != "VIRTUAL")
            {
                // If we have a real PNPDeviceID, do some parsing on PNPDeviceID to be sure we have correct type of device - be very restrictive, we can still adapt in the future
                string[] parts = PNPDeviceID.Split(new string[] { "\\" }, StringSplitOptions.None);
                if (parts.Length != 3)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"DeviceServer.AddDevice: PNPDeviceID \"{PNPDeviceID}\" is not correct");
                    return;
                }
                else if (parts[0] != "USB")
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"DeviceServer.AddDevice: PNPDeviceID \"{PNPDeviceID}\" is not USB");
                    return;
                }
                else if (parts[2] == "")
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"DeviceServer.AddDevice: PNPDeviceID \"{PNPDeviceID}\" has no serial number");
                    return;
                }
            }

            COMDevice device = new(PNPDeviceID, DeviceID);

            if (DeviceID != "VIRTUAL")
            {
                if (!device.Open())
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"DeviceServer.AddDevice: {device} unable to open");
                    return; // Unable to open COMDevice
                }

                // Try a maximum of 5 times to connect with COMDevice
                for (int i = 0; i < 5; i++)
                {
                    Logging.LogLine(LogLevel.Debug, LoggingSource.APP, $"DeviceServer.AddDevice: {device} GetProperties try {i + 1}");
                    if (device.GetProperties())
                    {
                        lock (_devices)
                            _devices.Add(device); // keep list of all successfully connected devices
                        UIAddDevice?.Invoke(device);
                        Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"DeviceServer.AddDevice: {device} successfully added");
                        return;
                    }
                    Thread.Sleep(1000); // just let it cool down, and try again later
                }

                // if we fail after 5 times, close the device
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"DeviceServer.AddDevice: {device} GetProperties failed after 5 times");
                device.Close();
            }
            else
            {
                lock (_devices)
                    _devices.Add(device); // keep list of all successfully connected devices
                UIAddDevice?.Invoke(device);
                Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"DeviceServer.AddDevice: {device} successfully added");
                return;
            }
        }

        internal static void RemoveDevice(COMDevice device)
        {
            lock (_devices)
                _devices.Remove(device);
            UIRemoveDevice?.Invoke(device);

            device.Close();
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"DeviceServer.RemoveDevice: {device} successfully removed");
        }

        internal static void RemoveDevice(string PNPDeviceID)
        {
            COMDevice device = FindDeviceBasedOnPNPDeviceID(PNPDeviceID);
            if (device == null)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"DeviceServer.RemoveDevice: {PNPDeviceID} not found in _devices");
                return;
            }
            RemoveDevice(device);
        }

        internal static void ResetStatistics()
        {
            lock (_devices)
                foreach (COMDevice device in _devices)
                    device.ResetStatistics();
        }
    }
}
