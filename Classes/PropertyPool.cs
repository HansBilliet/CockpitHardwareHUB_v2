using WASimCommander.CLI.Enums;

namespace CockpitHardwareHUB_v2.Classes
{
    internal static class PropertyPool
    {
        internal delegate void UIUpdateVariable_Handler(UpdateVariable uv, SimVar simVar);
        internal static event UIUpdateVariable_Handler UIUpdateVariable;
        private static readonly object lockObject = new();

        internal static int AddPropertyInPool(COMDevice device, int iPropId, string sPropStr, out PR parseResult)
        {
            lock (lockObject)
            {
                // Check if variable already exists
                SimVar simVar = SimVar.GetSimVarByName(sPropStr);
                if (simVar != null)
                {
                    // If simVar exists, increase its usage and return the iVarId
                    simVar.IncUsageCnt(device, iPropId);

                    // Send value immediately to device, but only if it has already received data from the sim
                    if (simVar.bRead && simVar.bHasValue)
                        device.AddCmdToTxPumpQueue(iPropId, simVar.sValue); // Pumps aren't running yet, so this will only be queued

                    parseResult = PR.Ok;
                    return simVar.iVarId;
                }

                // If variable doesn't exist yet, create a new variable
                simVar = new SimVar(sPropStr, UIUpdateVariable);
                parseResult = simVar.ParseResult;

                if (simVar.ParseResult != PR.Ok)
                {
                    // Parsing of sPropStr failed
                    Logging.Log(LogLevel.Error, LoggingSource.PPL, () => $"PropertyPool.AddPropertyInPool: Parsing of \"{sPropStr}\" for {device} failed with {simVar.ParseResult}");
                    return -1;
                }

                // Increase usage of the SimVar
                simVar.IncUsageCnt(device, iPropId);

                // Add the variable in the pool
                simVar.AddSimVar();

                // Register the simVar
                SimClient.RegisterSimVar(simVar);

                Logging.Log(LogLevel.Debug, LoggingSource.PPL, () => $"PropertyPool.AddPropertyInPool: \"{sPropStr}\" for {device} added");

                return simVar.iVarId;
            }
        }

        internal static void RemovePropertyFromPool(COMDevice device, int iVarId)
        {
            lock (lockObject)
            {
                if (iVarId == -1)
                    return;

                // Check if variable exists
                SimVar simVar = SimVar.GetSimVarById(iVarId);
                if (simVar == null)
                {
                    Logging.Log(LogLevel.Error, LoggingSource.PPL, () => $"PropertyPool.RemovePropertyFromPool: SimVar {iVarId} for {device} not found");
                    return;
                }

                // Decrease the usage of the SimVar - if UsageCnt becomes 0, we can unregister and remove the variable
                if (simVar.DecUsageCnt(device) != 0)
                    return;

                // Unregister the variable - no need to check for failure, as we remove the variable anyway
                SimClient.UnregisterSimVar(simVar);

                // Remove the variable from the list
                simVar.RemoveSimVar();
                Logging.Log(LogLevel.Debug, LoggingSource.PPL, () => $"PropertyPool.RemovePropertyFromPool: \"{simVar.sPropStr}\" for {device} removed");
            }
        }

        internal static void TriggerProperty(int iVarId, string sData)
        {
            // Check if variable exists
            SimVar simVar = SimVar.GetSimVarById(iVarId);
            if (simVar == null)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"PropertyPool.TriggerProperty: SimVar {iVarId} not found");
                return;
            }

            // Check if variable is writeable
            if (!simVar.bWrite)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"PropertyPool.TriggerProperty: \"{simVar.sPropStr}\" is not a Write property");
                return;
            }

            if (!simVar.SetValueOfSimVar(sData))
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"PropertyPool.TriggerProperty: \"{simVar.sPropStr}\" requires data of type [{simVar.ValType}].");
                return;
            }

            SimClient.TriggerSimVar(simVar);
        }

        internal static void FetchProperty(int iVarId)
        {
            // Check if variable exists
            SimVar simVar = SimVar.GetSimVarById(iVarId);
            if (simVar == null)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"PropertyPool.FetchProperty: SimVar {iVarId} not found");
                return;
            }

            // Check if variable is readable
            if (!simVar.bRead)
            {
                Logging.Log(LogLevel.Error, LoggingSource.APP, () => $"PropertyPool.FetchProperty: \"{simVar.sPropStr}\" is not a Read property");
                return;
            }

            simVar.DispatchSimVar();
        }
    }
}
