﻿using WASimCommander.CLI.Enums;

namespace CockpitHardwareHUB_v2.Classes
{
    internal static class PropertyPool
    {
        internal delegate void UIUpdateVariable_Handler(UpdateVariable uv, SimVar simVar);
        internal static event UIUpdateVariable_Handler UIUpdateVariable;

        internal static int AddPropertyInPool(COMDevice device, int iPropId, string sPropStr)
        {
            // Check if variable already exists
            SimVar simVar = SimVar.GetSimVarByName(sPropStr);
            if (simVar != null)
            {
                // If simVar exists, increase its usage and return the iVarId
                simVar.IncUsageCnt(device, iPropId);

                // Send value immediately to device
                device.AddCmdToTxPumpQueue(iPropId, simVar.sValue); // Pumps aren't running yet, so this will only be queued

                return simVar.iVarId;
            }

            // If variable doesn't exist yet, create a new variable
            simVar = new SimVar(sPropStr, UIUpdateVariable);
            if (simVar.ParseResult != PR.Ok)
            {
                // Parsing of sPropStr failed
                Logging.LogLine(LogLevel.Error, LoggingSource.PRP, $"PropertyPool.AddPropertyInPool: {device} - {simVar.ParseResult} for {sPropStr}");
                return -1;
            }
            Logging.LogLine(LogLevel.Debug, LoggingSource.PRP, $"PropertyPool.AddPropertyInPool: {device} - {simVar}");

            // Increase usage of the SimVar
            simVar.IncUsageCnt(device, iPropId);

            // Add the variable in the pool
            simVar.AddSimVar();

            // Register the simVar
            SimClient.RegisterSimVar(simVar);

            return simVar.iVarId;
        }

        internal static void RemovePropertyFromPool(COMDevice device, int iSimId)
        {
            // Check if variable exists
            SimVar simVar = SimVar.GetSimVarById(iSimId);
            if (simVar == null)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.PRP, $"PropertyPool.RemovePropertyFromPool: {device} - {simVar}");
                return;
            }

            Logging.LogLine(LogLevel.Debug, LoggingSource.PRP, $"PropertyPool.RemovePropertyFromPool: {device} - {simVar}");

            // Decrease the usage of the SimVar - if UsageCnt becomes 0, we can unregister and remove the variable
            if (simVar.DecUsageCnt(device) != 0)
                return;

            // Unregister the variable - no need to check for failure, as we remove the variable anyway
            SimClient.UnregisterSimVar(simVar);

            // Remove the variable from the list
            simVar.RemoveSimVar();
        }

        internal static void TriggerProperty(SimVar simVar, string sData, bool bCheckDataForSimVar = false)
        {
            if (!simVar.bWrite)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"PropertyPool.TriggerProperty: SimVar with ID {simVar.iVarId} is not a Write property");
                return;
            }

            if (bCheckDataForSimVar && !simVar.CheckDataForSimVar(sData))
                return;

            SimClient.TriggerSimVar(simVar, sData);
        }

        internal static void TriggerProperty(int iSimId, string sData)
        {
            // Check if variable exists
            SimVar simVar = SimVar.GetSimVarById(iSimId);
            if (simVar == null)
            {
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"PropertyPool.TriggerProperty: SimVar with ID {iSimId} not found");
                return;
            }
            TriggerProperty(simVar, sData);
        }
    }
}
