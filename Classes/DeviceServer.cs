using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WASimCommander.CLI.Client;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;

namespace CockpitHardwareHUB_v2.Classes
{
    internal static class DeviceServer
    {
        private static bool _IsStarted = false;

        private static readonly SerialPortManager _SerialPortManager = new();

        private static readonly ConcurrentDictionary<string, COMDevice> _devices = new();

        // SerialPortManager Events

        // This is an event handler that is called when USB devices are already connected
        private static void OnPortFoundEvent(object sender, SerialPortEventArgs spea)
        {
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"PNP Device ID {spea.PNPDeviceID} found on {spea.DeviceID}");
            Task.Run(() => AddDevice(spea));
        }

        // This is an event handler that is called when USB devices are added
        private static void OnPortAddedEvent(object sender, SerialPortEventArgs spea)
        {
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"PNP Device ID {spea.PNPDeviceID} added on {spea.DeviceID}");
            Task.Run(() => AddDevice(spea));
        }

        // This is an event handler that is called when USB devices are removed
        private static void OnPortRemovedEvent(object sender, SerialPortEventArgs spea)
        {
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"PNP Device ID {spea.PNPDeviceID} removed on {spea.DeviceID}");
            RemoveDevice(spea.PNPDeviceID);
        }

        public static void Init()
        {
            // Setup event handlers for scanning serial ports
            _SerialPortManager.OnPortFoundEvent += OnPortFoundEvent;
            _SerialPortManager.OnPortAddedEvent += OnPortAddedEvent;
            _SerialPortManager.OnPortRemovedEvent += OnPortRemovedEvent;

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, "DeviceServer Initialized");
        }

        public static void Start()
        {
            if (_IsStarted)
                return;

            // start scanning for serial ports
            // - already connected USB devices will be "found"
            // - new connected USB devices will be "added"
            _SerialPortManager.scanPorts(true);

            _IsStarted = true;
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, "DeviceServer Started");
        }

        public static void Stop()
        {
            if (!_IsStarted)
                return;

            // start scanning for serial ports
            _SerialPortManager.stop();

            // remove all devices
            foreach (KeyValuePair<string, COMDevice> pair in _devices.ToArray())
                RemoveDevice(pair.Key);

            _IsStarted = false;
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, "DeviceServer Stopped");
        }

        private static void AddDevice(SerialPortEventArgs spea)
        {
            if (_devices.ContainsKey(spea.PNPDeviceID))
                return; // COMDevice already exists

            COMDevice device = new COMDevice(spea.PNPDeviceID, spea.DeviceID); // default baudrate is 500000

            if (!device.Open())
                return; // Unable to open COMDevice

            // Try a maximum of 5 times to connect with COMDevice
            for (int i = 0; i < 5; i++)
            {
                if (device.GetProperties())
                {
                    Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"DeviceServer.AddDevice({device.PNPDeviceID}) for {device.PortName} done");
                    _devices.TryAdd(device.PNPDeviceID, device); // keep list of all successfully connected devices
                    SimClient.AddDeviceToProcessProperties = new ProcessProperties(ProcessAction.Add, device); // add the device in the processing queue of the SimClient to add Properties
                    return;
                }
                else
                    Thread.Sleep(1000);
            }

            // if we fail after 5 times, close the device
            Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"DeviceServer.AddDevice({device.PNPDeviceID}) for {device.PortName}: Failed after 5 times");
            device.Close();
        }

        private static void RemoveDevice(string PNPDeviceID)
        {
            if (_devices.TryRemove(PNPDeviceID, out COMDevice device))
            {
                Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"DeviceServer.RemoveDevice({PNPDeviceID}) for {device.PortName} done");
                SimClient.AddDeviceToProcessProperties = new ProcessProperties(ProcessAction.Remove, device); // add the device in the processing queue of the SimClient to remove Properties
                device.Close();
            }
        }
    }
}
