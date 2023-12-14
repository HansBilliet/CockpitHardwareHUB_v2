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
        // TEST ONLY
        private static uint _iSPD_INC = 0;
        private static uint _iSPD_DEC = 0;
        // TEST ONLY END

        private static readonly WASimClient _WASimClient = new(1965);
        public static bool IsConnected { get { return _WASimClient.isConnected(); } }
        private static int IsStarted = 0;

        // CancellationTokenSource to stop the pumps
        private static CancellationTokenSource _ctPumps;

        // Processing the Properties of Added and/or Removed Devices
        private static BlockingCollection<ProcessProperties> _ProcessPropertiesQueue = new();
        private static Task _ProcessPropertiesPump;
        public static ProcessProperties AddDeviceToProcessProperties { set => _ProcessPropertiesQueue.Add(value); }

        // Process the Commands coming from HW Devices - they have format [iVarID]=[optional data]
        private static BlockingCollection<string> _TxPumpQueue = new();
        private static Task _TxPump;
        public static string AddCmdToTxPumpQueue { set => _TxPumpQueue.Add(value); }    

        private static Action<bool> _ConnectionStatus = null;

        private static HR hr;  // store method invocation results for logging

        public static void SetLogLevel(LogLevel logLevel)
        {
            // Set Client's callback logging level to display messages in the console.
            _WASimClient.setLogLevel(logLevel, LogFacility.All, LogSource.Client);
            // Lets also see some log messages from the server
            _WASimClient.setLogLevel(logLevel, LogFacility.All, LogSource.Server);

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"LogLevel set to {logLevel}");
        }

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
                case ClientEventType.ServerConnected: // We only consider connection to both Simulator and WASM Module (server), hence only listen to ServerConnected
                    Start();
                    break;
                case ClientEventType.ServerDisconnected: // Whatever disconnect that happens is considered as a disconnection, hence we listen to all of them
                case ClientEventType.SimDisconnecting:
                case ClientEventType.SimDisconnected:
                    Stop();
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

            // just register 2 commands for fun
            _WASimClient.registerCustomEvent("A32NX.FCU_SPD_INC", out _iSPD_INC);
            _WASimClient.registerCustomEvent("A32NX.FCU_SPD_DEC", out _iSPD_DEC);

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
            // Assumed that ClientStatusHandler is re-entrant, make sure that Start() is only executed once
            // If IsStarted == 0, then make it 1, and return the previous value 0 --> execute Start
            // If IsStarted == 1, then don't change it, and return the previous value 1 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 1, 0) == 1)
                return; // Already started

            _ConnectionStatus?.Invoke(true);

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.StartPumps.");

            // create the CancellationTokenSource
            _ctPumps = new();

            // Start processing properties of added or removed devices
            _ProcessPropertiesPump = Task.Run(() => ProcessPropertiesPump(_ctPumps.Token), _ctPumps.Token);
            
            // Start sending variables and/or commands to the simulator
            _TxPump = Task.Run(() => TxPump(_ctPumps.Token), _ctPumps.Token);
            
            DeviceServer.Start();
        }

        private static void Stop()
        {
            // Assumed that ClientStatusHandler is re-entrant, make sure that Stop() is only executed once
            // If IsStarted == 1, then make it 0, and return the previous value 1 --> execute Start
            // If IsStarted == 0, then don't change it, and return the previous value 0 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 0, 1) == 0)
                return; // Already stopped

            _ConnectionStatus?.Invoke(false);

            DeviceServer.Stop();

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.StopPumps.");

            // cancel the CancellationTokenSource
            _ctPumps.Cancel(false);

            // wait for the pumps to stop running
            if (!Task.WaitAll(new Task[] { _ProcessPropertiesPump, _TxPump }, 250))
                // One task seems not to have stopped in time
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.Stop: One of the pumps didn't stop in time.");
            else
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.Stop: All pumps stopped in time.");

            // cleanup
            _ctPumps.Dispose();
            _ProcessPropertiesPump = null;
            _TxPump = null;
        }

        private static void ProcessPropertiesPump(CancellationToken ct)
        {
            bool bPumpStarted = false;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Only for logging purposes
                    if (!bPumpStarted)
                        Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.ProcessPropertiesPump started.");
                    bPumpStarted = true;

                    ProcessProperties pp = _ProcessPropertiesQueue.Take(ct);
                    Logging.LogLine(LogLevel.Trace, LoggingSource.APP, $"SimClient.ProcessPropertiesPump: {pp.ProcessAction} for {pp.Device.PortName}");

                    // simulate putting SimId's
                    int iSimId = 1;
                    foreach (Property prop in pp.Device.Properties)
                    {
                        prop.iSimId = iSimId++;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.APP, $"SimClient.ProcessPropertiesPump throws OperationCanceledException.");
                }
                catch (Exception ex)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.ProcessPropertiesPump throws {ex}");
                }
            }
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.ProcessPropertiesPump stopped.");
        }

        private static void TxPump(CancellationToken ct)
        {
            bool bPumpStarted = false;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Only for logging purposes
                    if (!bPumpStarted)
                        Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.TxPump started.");
                    bPumpStarted = true;

                    string sCmd = _TxPumpQueue.Take(ct);
                    Logging.LogLine(LogLevel.Trace, LoggingSource.APP, $"SimClient.TxPump: {sCmd}");
                    if (sCmd.Substring(0, 4) == "0001")
                        _WASimClient.sendKeyEvent(_iSPD_INC);
                    if (sCmd.Substring(0, 4) == "0002")
                        _WASimClient.sendKeyEvent(_iSPD_DEC);
                }
                catch (OperationCanceledException)
                {
                    Logging.LogLine(LogLevel.Trace, LoggingSource.APP, $"SimClient.TxPump throws OperationCanceledException.");
                }
                catch (Exception ex)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.TxPump throws {ex}");
                }
            }
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.TxPump stopped.");
        }

    }
}
