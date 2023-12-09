using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using WASimCommander.CLI;
using WASimCommander.CLI.Client;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;

namespace CockpitHardwareHUB_v2.Classes
{
    internal static class SimClient
    {
        private static readonly WASimClient _WASimClient = new(1965);
        public static bool IsConnected { get { return _WASimClient.isConnected(); } }

        // Processing the Properties of Added and/or Removed Devices
        private static BlockingCollection<ProcessProperties> _ProcessPropertiesQueue = new();
        private static CancellationTokenSource _ctProcessPropertiesPump;
        private static Task _ProcessPropertiesPump;
        public static ProcessProperties AddDeviceToProcessProperties { set => _ProcessPropertiesQueue.Add(value); }

        private static Action<bool> _ConnectionStatus = null;

        private static HR hr;  // store method invocation results for logging

        // WASimCommander Event handlers

        // This is an event handler for printing Client and Server log messages
        private static void LogHandler(LogRecord lr, LogSource src)
        {
            Logging.LogLine(lr.level, src == LogSource.Client ? LoggingSource.CLT : LoggingSource.SRV, lr.message.op_Implicit(), lr.timestamp);
        }

        // Event handler to print the current Client status.
        private static void ClientStatusHandler(ClientEvent ev)
        {
            switch (ev.eventType)
            {
                case ClientEventType.None:
                    break;
                case ClientEventType.SimConnecting:
                    break;
                case ClientEventType.SimConnected:
                    break;
                case ClientEventType.ServerConnecting:
                    break;
                case ClientEventType.ServerConnected: // We only consider connection to both Simulator and WASM Module (server)
                    _ConnectionStatus?.Invoke(true);
                    _ctProcessPropertiesPump = new(); // create a CancellationTokenSource
                    _ProcessPropertiesPump = Task.Run(() => ProcessPropertiesPump(_ctProcessPropertiesPump.Token), _ctProcessPropertiesPump.Token); // Start processing properties of added or removed devices
                    Start(); // Start the DeviceServer
                    break;
                case ClientEventType.ServerDisconnected: // When Server is disconnected, we consider this as full disconnection
                    _ConnectionStatus?.Invoke(false);
                    _ctProcessPropertiesPump.Cancel(false); // cancel the CancellationTokenSource to stop the ProcessPropertiesPump
                    _ProcessPropertiesPump.Wait(100);
                    _ProcessPropertiesPump.Dispose();
                    _ProcessPropertiesPump = null;
                    Stop(); // Stop the DeviceServer
                    break;
                case ClientEventType.SimDisconnecting:
                    break;
                case ClientEventType.SimDisconnected:
                    break;
            }

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"Client event {ev.eventType} - \"{ev.message}\"; Client status: {ev.status}");
        }

        // Event handler for showing listing results (eg. local vars list)
        private static void ListResultsHandler(ListResult lr)
        {
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, lr.ToString());  // just use the ToString() override
        }

        // Event handler to process data value subscription updates.
        private static void DataSubscriptionHandler(DataRequestRecord dr)
        {
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"<< Got Data for request {dr.requestId} \"{dr.nameOrCode}\" with Value: ");
            // Convert the received data into a value using DataRequestRecord's tryConvert() methods.
            // This could be more efficient in a "real" application, but it's good enough for our tests with only 2 value types.
            if (dr.tryConvert(out float fVal))
                Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"(float) {fVal}");
            else if (dr.tryConvert(out double dVal))
                Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"(double) {dVal}");
            else if (dr.tryConvert(out int iVal))
                Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"(int) {iVal}");
            else if (dr.tryConvert(out string sVal))
            {
                Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"(string) \"{sVal}\"");
            }
            else
                Logging.LogLine(LogLevel.Info, LoggingSource.APP, "Could not convert result data to value!");
        }

        private static void ProcessPropertiesPump(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ProcessProperties pp = _ProcessPropertiesQueue.Take(cancellationToken);
                    Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.ProcessPropertiesPump: {pp.ProcessAction} for {pp.Device.PortName}");

                    // just for fun, let's do a registration of a Custom Event
                    uint uEventId;
                    _WASimClient.registerCustomEvent("A32NX.FCU_SPD_INC", out uEventId);
                    Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"A32NX.FCU_SPD_INC gives {uEventId}");
                }
            }
            catch (Exception ex)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.ProcessPropertiesPump: Exception {ex}");
            }
        }

        public static void Init(Action<bool> ConnectionStatus)
        {
            _ConnectionStatus = ConnectionStatus;

            // Set all LogLevels in _WASimClient
            SetLogLevel(Logging.SetLogLevel);

            // Setup event handlers for WASimCommander
            _WASimClient.OnClientEvent += ClientStatusHandler;
            _WASimClient.OnLogRecordReceived += LogHandler;
            _WASimClient.OnDataReceived += DataSubscriptionHandler;
            _WASimClient.OnListResults += ListResultsHandler;

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, "SimClient Initialized");
        }

        public static void Connect()
        {
            if (!_WASimClient.isInitialized())
            {
                // Not yet connected to the Simulator, try to connect
                if ((hr = _WASimClient.connectSimulator()) != HR.OK)
                {
                    Logging.LogLine(LogLevel.Info, LoggingSource.APP, "Cannot connect to Simulator, quitting. Error: " + hr.ToString());
                    return; // There is nothing more we can do
                }
            }

            // Ping the WASimCommander server to make sure it's running and get the server version number (returns zero if no response).
            uint version = _WASimClient.pingServer();
            if (version == 0)
            {
                Logging.LogLine(LogLevel.Info, LoggingSource.APP, "Server did not respond to ping, quitting.");
                _WASimClient.disconnectSimulator();
                return;
            }

            // Decode version number to dotted format and print it
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"Found WASimModule Server v{version >> 24}.{version >> 16 & 0xFF}.{version >> 8 & 0xFF}.{version & 0xFF}");

            if (!_WASimClient.isConnected())
            {
                // Not yet connected to the Server, try to connect
                if ((hr = _WASimClient.connectServer()) != HR.OK)
                {
                    Logging.LogLine(LogLevel.Info, LoggingSource.APP, "Server connection failed, quitting. Error: " + hr.ToString());
                    _WASimClient.disconnectSimulator();
                    return;
                }
            }

            SetLogLevel(Logging.SetLogLevel);

            return;
        }

        public static void Disconnect()
        {
            if (_WASimClient.isConnected())
                _WASimClient.disconnectServer();

            if (_WASimClient.isInitialized())
                _WASimClient.disconnectSimulator();
        }

        private static void Start()
        {
            DeviceServer.Start();
        }

        private static void Stop()
        {
            DeviceServer.Stop();
        }

        public static void SetLogLevel(LogLevel logLevel)
        {
            // Set Client's callback logging level to display messages in the console.
            _WASimClient.setLogLevel(logLevel, LogFacility.All, LogSource.Client);
            // Lets also see some log messages from the server
            _WASimClient.setLogLevel(logLevel, LogFacility.All, LogSource.Server);

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"LogLevel set to {logLevel}");
        }
    }
}
