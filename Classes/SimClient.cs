using WASimCommander.CLI;
using WASimCommander.CLI.Client;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;

namespace CockpitHardwareHUB_v2.Classes
{
    internal static class SimClient
    {
        internal delegate void UpdateConnectionStatus_Handler(bool bConnected);
        internal static event UpdateConnectionStatus_Handler UIUpdateConnectionStatus;

        //private static MainForm _MainForm;

        private static readonly WASimClient _WASimClient = new(1965);
        public static bool IsConnected { get { return _WASimClient.isConnected(); } }
        private static int IsStarted = 0;

        public static void SetLogLevel(LogLevel logLevel)
        {
            // Only use the Remote LogFacility for Client and Server
            _WASimClient.setLogLevel(logLevel, LogFacility.Remote, LogSource.Client);
            _WASimClient.setLogLevel(LogLevel.None, LogFacility.Console | LogFacility.File, LogSource.Client);
            _WASimClient.setLogLevel(logLevel, LogFacility.Remote, LogSource.Server);
            _WASimClient.setLogLevel(LogLevel.None, LogFacility.Console | LogFacility.File, LogSource.Server);

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
                    Task.Run(() => Stop());
                    break;
            }

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"Simclient.ClientStatusHandler: Client event {ev.eventType} - \"{ev.message}\" - Client status: {ev.status}");
        }

        // Event handler for showing listing results (eg. local vars list)
        private static void ListResultsHandler(ListResult lr)
        {
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, lr.ToString());  // just use the ToString() override
        }

        // Event handler to process data value subscription updates.
        private static void DataSubscriptionHandler(DataRequestRecord dr)
        {
            SimVar simVar = SimVar.GetSimVarById((int)dr.requestId);
            if (simVar == null)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.DataSubscriptionHandler: {dr.requestId} \"{dr.nameOrCode}\" - Couldn't find SimVar");
                return;
            }

            if (!simVar.ConvertDataForSimVar(dr))
                return;

            simVar.DispatchSimVar();
        }

        public static void Init() // MainForm mainForm)
        {
            //_MainForm = mainForm;

            // Set all LogLevels in _WASimClient
            SetLogLevel(Logging.SetLogLevel);

            // Setup event handlers for WASimCommander
            _WASimClient.OnClientEvent += ClientStatusHandler;
            _WASimClient.OnLogRecordReceived += LogHandler;
            _WASimClient.OnDataReceived += DataSubscriptionHandler;
            _WASimClient.OnListResults += ListResultsHandler;

            Logging.LogLine(LogLevel.Info, LoggingSource.APP, "SimClient.Init: SimClient Initialized");
        }

        internal static void Connect()
        {
            HR hr;

            if (!_WASimClient.isInitialized())
            {
                // Not yet connected to the Simulator, try to connect
                if ((hr = _WASimClient.connectSimulator()) != HR.OK)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.APP, "SimClient.Connect: Cannot connect to Simulator, quitting. Error: " + hr.ToString());
                    return; // There is nothing more we can do
                }
            }

            // Ping the WASimCommander server to make sure it's running and get the server version number (returns zero if no response).
            uint version = _WASimClient.pingServer();
            if (version == 0)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, "SimClient.Connect: Server did not respond to ping, quitting.");
                _WASimClient.disconnectSimulator();
                return;
            }

            // Decode version number to dotted format and print it
            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.Connect: Found WASimModule Server v{version >> 24}.{version >> 16 & 0xFF}.{version >> 8 & 0xFF}.{version & 0xFF}");

            if (!_WASimClient.isConnected())
            {
                // Not yet connected to the Server, try to connect
                if ((hr = _WASimClient.connectServer()) != HR.OK)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.APP, "SimClient.Connect: Server connection failed, quitting. Error: " + hr.ToString());
                    _WASimClient.disconnectSimulator();
                    return;
                }
            }

            SetLogLevel(Logging.SetLogLevel);

            return;
        }

        internal static async Task Disconnect()
        {
            await Stop();

            if (_WASimClient.isConnected())
                _WASimClient.disconnectServer();

            if (_WASimClient.isInitialized())
                _WASimClient.disconnectSimulator();
        }

        private static void Start()
        {
            // Be suspicious, and assume that the call needs to be re-entrant, hence make it threadsafe
            // If IsStarted == 0, then make it 1, and return the previous value 0 --> execute Start
            // If IsStarted == 1, then don't change it, and return the previous value 1 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 1, 0) == 1)
                return; // Already started

            UIUpdateConnectionStatus?.Invoke(true);

            DeviceServer.Start();
        }

        private static async Task Stop()
        {
            // Be suspicious, and assume that the call needs to be re-entrant, hence make it threadsafe
            // If IsStarted == 1, then make it 0, and return the previous value 1 --> execute Start
            // If IsStarted == 0, then don't change it, and return the previous value 0 --> don't do anything, we are already started
            if (Interlocked.CompareExchange(ref IsStarted, 0, 1) == 0)
                return; // Already stopped

            UIUpdateConnectionStatus?.Invoke(false);

            await DeviceServer.Stop();
        }

        internal static bool RegisterSimVar(SimVar simVar)
        {
            if (!IsConnected || simVar.bIsRegistered)
                return false;

            switch (simVar.cVarType)
            {
                case 'A':
                    if (simVar.bWrite)
                    {
                        // TODO - might be nothing needed - maybe need a different external Id for Write and Read?
                    }
                    if (simVar.bRead)
                    {
                        int Id;
                        HR hr;
                        hr = _WASimClient.lookup(LookupItemType.SimulatorVariable, simVar.sVarName, out Id);
                        if (hr != HR.OK)
                        {
                            Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.RegisterSimVar: {simVar.sVarName} failed with {hr}");
                            return false;
                        }
                        simVar.ExternalId = (uint)Id;
                        DataRequest dr = new((uint)simVar.iVarId, simVar.sVarName, simVar.sUnit, simVar.bIndex, simVar.ValType);
                        _WASimClient.saveDataRequestAsync(dr);
                        simVar.bIsRegistered = true;
                        Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.RegisterSimVar: {simVar.sVarName} with Id's {simVar.iVarId}/{simVar.ExternalId} success");
                    }
                    break;

                case 'L':
                    if (simVar.bWrite)
                    {
                        // TODO - might be nothing needed - maybe need a different external Id for Write and Read?
                    }
                    if (simVar.bRead)
                    {
                        // first lookup to get the ID
                        int Id;
                        HR hr;
                        hr = _WASimClient.lookup(LookupItemType.LocalVariable, simVar.sVarName, out Id);
                        if (hr != HR.OK)
                        {
                            Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.RegisterSimVar: {simVar.sVarName} failed with {hr}");
                            return false;
                        }
                        simVar.ExternalId = (uint)Id;
                        DataRequest dr = new((uint)simVar.iVarId, 'L', simVar.sVarName, simVar.ValType);
                        _WASimClient.saveDataRequestAsync(dr);
                        simVar.bIsRegistered = true;
                        Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.RegisterSimVar: {simVar.sVarName} with Id's {simVar.iVarId}/{simVar.ExternalId} success");
                    }
                    break;

                case 'K':
                    if (simVar.bWrite)
                    {
                        if (simVar.bCustomEvent)
                        {
                            // Custom Events
                            uint uCustomEventId;
                            HR hr = _WASimClient.registerCustomEvent(simVar.sVarName, out uCustomEventId);
                            if (hr != HR.OK)
                            {
                                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.RegisterSimVar: {simVar.sVarName} failed with {hr}");
                                return false;
                            }
                            simVar.ExternalId = uCustomEventId;
                            simVar.bIsRegistered = true;
                            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.RegisterSimVar: {simVar.sVarName} with Id's {simVar.iVarId}/{uCustomEventId} success");
                        }
                        else
                        {
                            // Simulator Events

                            // TODO
                        }
                    }
                    break;

                case 'X':
                    if (simVar.bWrite)
                    {
                        // register X-type as calculator code events
                        RegisteredEvent ev = new RegisteredEvent((uint)simVar.iVarId, simVar.sVarName);
                        HR hr = _WASimClient.registerEvent(ev);
                        if (hr != HR.OK)
                        {
                            Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.RegisterSimVar: {simVar.sVarName} failed with {hr}");
                            return false;
                        }
                        simVar.bIsRegistered = true;
                        Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.RegisterSimVar: {simVar.sVarName} with Id {simVar.iVarId} success");
                    }
                    if (simVar.bRead)
                    {
                        // TODO
                    }
                    break;
            }
            return true;
        }

        internal static void UnregisterSimVar(SimVar simVar)
        {
            if (!IsConnected || !simVar.bIsRegistered)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.UnregisterSimVar: {simVar.sVarName} failed {IsConnected} {IsStarted} {simVar.bIsRegistered}");
                return;
            }

            switch (simVar.cVarType)
            {
                case 'A':
                    if (simVar.bRead)
                    {
                        HR hr = _WASimClient.removeDataRequest((uint)simVar.iVarId);
                        if (hr != HR.OK)
                            Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.UnregisterSimVar: {simVar.sVarName} failed with {hr}");
                        else
                            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.UnregisterSimVar: {simVar.sVarName} with Id's {simVar.iVarId}/{simVar.ExternalId} success");
                    }
                    break;

                case 'L':
                    if (simVar.bRead)
                    {
                        HR hr = _WASimClient.removeDataRequest((uint)simVar.iVarId);
                        if (hr != HR.OK)
                            Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.UnregisterSimVar: {simVar.sVarName} failed with {hr}");
                        else
                            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.UnregisterSimVar: {simVar.sVarName} with Id's {simVar.iVarId}/{simVar.ExternalId} success");
                    }
                    break;

                case 'K':
                    if (simVar.bCustomEvent)
                    {
                        HR hr = _WASimClient.removeCustomEvent((uint)simVar.ExternalId);
                        if (hr != HR.OK)
                            Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.UnregisterSimVar: {simVar.sVarName} failed with {hr}");
                        else
                            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.UnregisterSimVar: {simVar.sVarName} with Id's {simVar.iVarId}/{simVar.ExternalId} success");
                    }
                    break;

                case 'X':
                    if (simVar.bWrite)
                    {
                        HR hr = _WASimClient.removeEvent((uint)simVar.iVarId);
                        if (hr != HR.OK)
                            Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.UnregisterSimVar: {simVar.sVarName} failed with {hr}");
                        else
                            Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.UnregisterSimVar: {simVar.sVarName} with Id {simVar.iVarId} success");
                    }
                    if (simVar.bRead)
                    {

                    }
                    break;
            }
            simVar.ExternalId = 0;
            simVar.bIsRegistered = false;
        }

        internal static void TriggerSimVar(SimVar simVar, string sData)
        {
            if (!IsConnected || IsStarted == 0 || !simVar.bIsRegistered || !simVar.bWrite)
                return;

            Logging.LogLine(LogLevel.Debug, LoggingSource.APP, $"SimClient.TriggerSimVar: iSimId = {simVar.iVarId} = {sData}");

            switch (simVar.cVarType)
            {
                case 'A':
                    break;

                case 'L':
                    break;

                case 'K':
                    if (simVar.bCustomEvent)
                    {
                        uint.TryParse(sData, out uint uData);
                        _WASimClient.sendKeyEvent(simVar.ExternalId, uData);
                    }
                    break;

                case 'X':
                    _WASimClient.transmitEvent((uint)simVar.iVarId);
                    break;
            }
        }

        internal static bool ExecuteCalculatorCode(string sCode, out double d, out string s)
        {
            HR hr = _WASimClient.executeCalculatorCode(sCode, CalcResultType.String, out d, out s);
            if (hr != HR.OK)
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimClient.ExecuteCalculatorCode: \"{sCode}\" failed with {hr}");
            else
                Logging.LogLine(LogLevel.Info, LoggingSource.APP, $"SimClient.ExecuteCalculatorCode: \"{sCode}\" returns double {d} and string \"{s}\"");

            return hr == HR.OK;
        }
    }
}
