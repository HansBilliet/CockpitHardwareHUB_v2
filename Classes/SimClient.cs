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
            _WASimClient.setLogLevel(logLevel == LogLevel.Trace ? LogLevel.Debug : logLevel, LogFacility.Remote, LogSource.Server);
            _WASimClient.setLogLevel(LogLevel.None, LogFacility.Console | LogFacility.File, LogSource.Server);

            Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"LogLevel set to {logLevel}");
        }

        // WASimCommander Event handlers

        // This is an event handler for printing Client and Server log messages
        private static void LogHandler(LogRecord lr, LogSource src)
        {
            //Logging.Log(lr.level, src == LogSource.Client ? LoggingSource.CLT : LoggingSource.SRV, () => lr.message.op_Implicit(), lr.timestamp);
            Logging.Log(lr.level, src == LogSource.Client ? LoggingSource.CLT : LoggingSource.SRV, () => lr.message.op_Implicit(), lr.timestamp);
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

            Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"Simclient.ClientStatusHandler: Client event {ev.eventType} - \"{ev.message}\" - Client status: {ev.status}");
        }

        // Event handler for showing listing results (eg. local vars list)
        private static void ListResultsHandler(ListResult lr)
        {
            Logging.Log(LogLevel.Info, LoggingSource.APP, () => lr.ToString());  // just use the ToString() override
        }

        // Event handler to process data value subscription updates.
        private static void DataSubscriptionHandler(DataRequestRecord dr)
        {
            SimVar simVar = SimVar.GetSimVarById((int)dr.requestId);
            if (simVar == null)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.DataSubscriptionHandler: {dr.requestId} \"{dr.nameOrCode}\" - Couldn't find SimVar");
                return;
            }

            if (!simVar.ConvertDataRequestRecordToString(dr))
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.DataSubscriptionHandler: {dr.requestId} \"{dr.nameOrCode}\" - Could not convert to string");
                return;
            }

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

            Logging.Log(LogLevel.Info, LoggingSource.APP, () => "SimClient.Init: SimClient Initialized");
        }

        internal static void Connect()
        {
            HR hr;

            if (!_WASimClient.isInitialized())
            {
                // Not yet connected to the Simulator, try to connect
                if ((hr = _WASimClient.connectSimulator()) != HR.OK)
                {
                    Logging.Log(LogLevel.Error, LoggingSource.APP, () => "SimClient.Connect: Cannot connect to Simulator, quitting. Error: " + hr.ToString());
                    return; // There is nothing more we can do
                }
            }

            // Ping the WASimCommander server to make sure it's running and get the server version number (returns zero if no response).
            uint version = _WASimClient.pingServer();
            if (version == 0)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => "SimClient.Connect: Server did not respond to ping, quitting.");
                _WASimClient.disconnectSimulator();
                return;
            }

            // Decode version number to dotted format and print it
            Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.Connect: Found WASimModule Server v{version >> 24}.{version >> 16 & 0xFF}.{version >> 8 & 0xFF}.{version & 0xFF}");

            if (!_WASimClient.isConnected())
            {
                // Not yet connected to the Server, try to connect
                if ((hr = _WASimClient.connectServer()) != HR.OK)
                {
                    Logging.Log(LogLevel.Error, LoggingSource.APP, () => "SimClient.Connect: Server connection failed, quitting. Error: " + hr.ToString());
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
            if (!IsConnected)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: \"{simVar.sVarName}\" failed because Simulator is not connected");
                return false;
            }

            if (simVar.bIsRegistered)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: \"{simVar.sVarName}\" failed because SimVar is already registered");
                return false;
            }

            HR hr; // Variable to keep the result of WASimClient calls

            switch (simVar.cVarType)
            {
                case 'A':
                    {
                        // First lookup the variable and store the ExternalId in the SimVar - if it fails, then the ExternalId will remain 0
                        // The id's are only used for reading 'A'-Vars. For writing, the full names are used, as there is no Gauge API function to write.
                        if ((hr = _WASimClient.lookup(LookupItemType.SimulatorVariable, simVar.sVarName, out int externalId)) != HR.OK)
                        {
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                                $"'A'-var \"{simVar.sVarName}\" lookup failed with {hr}");
                            return false;
                        }

                        if (simVar.sUnit != "")
                        {
                            if ((hr = _WASimClient.lookup(LookupItemType.UnitType, simVar.sUnit, out int unitId)) != HR.OK)
                            {
                                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                                    $"'A'-var \"{simVar.sVarName}\" lookup for unit failed with {hr}");
                                return false;
                            }
                            simVar.iUnitId = unitId;
                        }
                        else
                            simVar.iUnitId = -1;

                        // If it is a Read variable, also save a DataRequest
                        if (simVar.bRead)
                        {
                            DataRequest dr = new((uint)simVar.iVarId, simVar.sVarName, simVar.sUnit, simVar.Index, (uint)simVar.ValType);
                            if ((hr = _WASimClient.saveDataRequestAsync(dr)) != HR.OK)
                            {
                                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                                    $"'A'-var \"{simVar.sVarName}\" saveDataRequestAsync failed with {hr}");
                                return false;
                            }
                        }

                        Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                            $"'A'-var \"{simVar.sVarName}\" with Id's {simVar.iVarId}/{simVar.ExternalId} successfully registered for \"{simVar.sRW}\"");
                        simVar.ExternalId = (uint)externalId;
                        simVar.bIsRegistered = true;
                        break;
                    }

                case 'L':
                    {
                        // first lookup to get the ID
                        if ((hr = _WASimClient.lookup(LookupItemType.LocalVariable, simVar.sVarName, out int externalId)) != HR.OK)
                        {
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                                $"'L'-var \"{simVar.sVarName}\" failed with {hr}");
                            return false;
                        }
                        // If it is a Read variable, also save a DataRequest
                        if (simVar.bRead)
                        {
                            DataRequest dr = new((uint)simVar.iVarId, 'L', simVar.sVarName, (uint)simVar.ValType);
                            if ((hr = _WASimClient.saveDataRequestAsync(dr)) != HR.OK)
                            {
                                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                                    $"'L'-var \"{simVar.sVarName}\" saveDataRequestAsync failed with {hr}");
                                return false;
                            }
                        }
                        Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                            $"'L'-var \"{simVar.sVarName}\" with Id's {simVar.iVarId}/{simVar.ExternalId} successfully registered for \"{simVar.sRW}\"");
                        simVar.ExternalId = (uint)externalId;
                        simVar.bIsRegistered = true;
                        break;
                    }

                case 'K':
                    {
                        if (!simVar.bWrite)
                            break;

                        if (simVar.bCustomEvent)
                        {
                            // Custom Events
                            if ((hr = _WASimClient.registerCustomEvent(simVar.sVarName, out uint externalId)) != HR.OK)
                            {
                                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                                    $"Custom 'K'-var \"{simVar.sVarName}\" registerCustomEvent failed with {hr}");
                                return false;
                            }
                            simVar.ExternalId = externalId;
                        }
                        else
                        {
                            // Simulator Events
                            // first lookup to get the ID
                            if ((hr = _WASimClient.lookup(LookupItemType.KeyEventId, simVar.sVarName, out int externalId)) != HR.OK)
                            {
                                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                                    $"'K'-var \"{simVar.sVarName}\" lookup failed with {hr}");
                                return false;
                            }
                            simVar.ExternalId = (uint)externalId;
                        }
                        Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.RegisterSimVar: {(simVar.bCustomEvent ? "Custom" : "")} " +
                            $"'K'-var \"{simVar.sVarName}\" with Id's {simVar.iVarId}/{simVar.ExternalId} successfully registered for \"{simVar.sRW}\"");
                        simVar.bIsRegistered = true;
                        break;
                    }

                case 'X':
                    {
                        if (simVar.bWrite && !simVar.bFormatedXVar)
                        {
                            // Register X-type as calculator code events, except if it has format specifiers, which will be then treated as normal exec_calculator_code
                            RegisteredEvent ev = new RegisteredEvent((uint)simVar.iVarId, simVar.sVarName);
                            if ((hr = _WASimClient.registerEvent(ev)) != HR.OK)
                            {
                                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                                    $"'X'-var \"{simVar.sVarName}\" registerEvent failed with {hr}");
                                return false;
                            }
                        }
                        if (simVar.bRead)
                        {
                            DataRequest dr = new((uint)simVar.iVarId, simVar.CRType, simVar.sVarName, (uint)simVar.ValType);
                            if ((hr = _WASimClient.saveDataRequestAsync(dr)) != HR.OK)
                            {
                                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                                    $"'X'-var \"{simVar.sVarName}\" saveDataRequestAsync failed with {hr}");
                                return false;
                            }
                        }
                        Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.RegisterSimVar: " +
                            $"'X'-var \"{simVar.sVarName}\" with Id {simVar.iVarId} successfully registered for \"{simVar.sRW}\"");
                        simVar.ExternalId = 0; // Just to be sure, as ExternalId is not used
                        simVar.bIsRegistered = true;
                        break;
                    }
            }
            return true;
        }

        internal static void UnregisterSimVar(SimVar simVar)
        {
            if (!IsConnected)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: \"{simVar.sVarName}\" failed because Simulator is not connected");
                return;
            }

            if (!simVar.bIsRegistered)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: \"{simVar.sVarName}\" failed because SimVar is already registered");
                return;
            }

            HR hr; // Variable to keep the result of WASimClient calls

            switch (simVar.cVarType)
            {
                case 'A':
                    if (simVar.bRead)
                    {
                        if ((hr = _WASimClient.removeDataRequest((uint)simVar.iVarId)) != HR.OK)
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: " +
                                $"'A'-var \"{simVar.sVarName}\" removeDataRequest failed with {hr}");
                        else
                            Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: " +
                                $"'A'-var \"{simVar.sVarName}\" with Id's {simVar.iVarId}/{simVar.ExternalId} successfully unregistered");
                    }
                    break;

                case 'L':
                    if (simVar.bRead)
                    {
                        if ((hr = _WASimClient.removeDataRequest((uint)simVar.iVarId)) != HR.OK)
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: " +
                                $"'L'-var \"{simVar.sVarName}\" removeDataRequest failed with {hr}");
                        else
                            Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: " +
                                $"'L'-var \"{simVar.sVarName}\" with Id's {simVar.iVarId}/{simVar.ExternalId} successfully unregistered");
                    }
                    break;

                case 'K':
                    if (simVar.bWrite && simVar.bCustomEvent)
                    {
                        // Only Custom Events can be unregistered
                        if ((hr = _WASimClient.removeCustomEvent((uint)simVar.ExternalId)) != HR.OK)
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: " +
                                $"Custom 'K'-var \"{simVar.sVarName}\" removeCustomEvent failed with {hr}");
                        else
                            Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: " +
                                $"Custom 'K'-var \"{simVar.sVarName}\" with Id's {simVar.iVarId}/{simVar.ExternalId} successfully unregistered");
                    }
                    break;

                case 'X':
                    if (simVar.bWrite && !simVar.bFormatedXVar)
                    {
                        if ((hr = _WASimClient.removeEvent((uint)simVar.iVarId))  != HR.OK)
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.UnregisterSimVar:" +
                                $"'X'-var \"{simVar.sVarName}\" removeEvent failed with {hr}");
                        else
                            Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: " +
                                $"'X'-var \"{simVar.sVarName}\" with Id {simVar.iVarId} successfully unregistered for W");
                    }
                    if (simVar.bRead)
                    {
                        if ((hr = _WASimClient.removeDataRequest((uint)simVar.iVarId)) != HR.OK)
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: " +
                                $"'X'-var \"{simVar.sVarName}\" removeDataRequest failed with {hr}");
                        else
                            Logging.Log(LogLevel.Info, LoggingSource.APP, () => $"SimClient.UnregisterSimVar: " +
                                $"'X'-var \"{simVar.sVarName}\" with Id's {simVar.iVarId} successfully unregistered for R");
                    }
                    break;
            }

            // In any case, we remove the External Id and mark as unregistered
            simVar.ExternalId = 0;
            simVar.bIsRegistered = false;
        }

        internal static void TriggerSimVar(SimVar simVar)
        {
            if (!IsConnected)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.TriggerSimVar: \"{simVar.sVarName}\" failed because Simulator is not connected");
                return;
            }

            if (!simVar.bIsRegistered)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.TriggerSimVar: \"{simVar.sVarName}\" failed because SimVar is already registered");
                return;
            }

            if (!simVar.bWrite)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.TriggerSimVar: \"{simVar.sVarName}\" failed because SimVar is not Write");
                return;
            }

            HR hr; // Variable to keep the result of WASimClient calls

            switch (simVar.cVarType)
            {
                case 'A':
                    {
                        if ((hr = _WASimClient.setVariable(new(simVar.sVarName, simVar.sUnit, simVar.Index), simVar.dValue[0])) != HR.OK)
                        {
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.TriggerSimVar: \"{simVar.sVarName}\" failed with {hr}");
                            return;
                        }
                        break;
                    }

                case 'L':
                    {
                        if ((hr = _WASimClient.setVariable(new((int)simVar.ExternalId), simVar.dValue[0])) != HR.OK)
                        {
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.TriggerSimVar: \"{simVar.sVarName}\" failed with {hr}");
                            return;
                        }
                        break;
                    }

                case 'K':
                    {
                        hr = _WASimClient.sendKeyEvent(simVar.ExternalId, (uint)simVar.dValue[0], (uint)simVar.dValue[1], (uint)simVar.dValue[2], (uint)simVar.dValue[3], (uint)simVar.dValue[4]);
                        if (hr != HR.OK)
                        {
                            Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.TriggerSimVar: \"{simVar.sVarName}\" failed with {hr}");
                            return;
                        }
                        break;
                    }

                case 'X':
                    {
                        if (simVar.bFormatedXVar)
                        {
                            string sFormated = string.Format(simVar.sVarName, simVar.dValue[0], simVar.dValue[1], simVar.dValue[2], simVar.dValue[3], simVar.dValue[4]);
                            if ((hr = _WASimClient.executeCalculatorCode(sFormated)) != HR.OK)
                            {
                                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.TriggerSimVar: \"{simVar.sVarName}\" failed with {hr}");
                                return;
                            }
                        }
                        else
                        {
                            if ((hr = _WASimClient.transmitEvent((uint)simVar.iVarId)) != HR.OK)
                            {
                                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"SimClient.TriggerSimVar: \"{simVar.sVarName}\" failed with {hr}");
                                return;
                            }
                        }
                        break;
                    }
            }

            Logging.Log(LogLevel.Debug, LoggingSource.APP, () => $"SimClient.TriggerSimVar: \"{simVar.sVarName}\" = {simVar.dValue} succeeded");
        }

        internal static HR ExecuteCalculatorCode(string sCode, out string s)
        {
            HR hr = _WASimClient.executeCalculatorCode(sCode, CalcResultType.String, out _, out s);
            return hr;
        }
    }
}
