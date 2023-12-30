using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using WASimCommander.CLI;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;
using static CockpitHardwareHUB_v2.Classes.PropertyPool;

namespace CockpitHardwareHUB_v2.Classes
{
    internal enum PR
    {
        Ok,
        UnsupportedFormat,
        MissingVarName,
        MissingUnit,
        UnsupportedValType,
        KVarUnsupportedValType,
        KVarCanOnlyBeWrite,
        LVarUnsupportedValType,
        XVarUnsupportedValType,
        XVarCanNotBeBothReadAndWrite
    }

    internal enum SIMCONNECT_DATATYPE
    {
        NONE,
        INT32,
        INT64,
        FLOAT32,
        FLOAT64,
        STRING8,
        STRING32,
        STRING64,
        STRING128,
        STRING256,
        STRING260,
        STRINGV,
        INITPOSITION,
        MARKERSTATE,
        WAYPOINT,
        LATLONALT,
        XYZ,
        MAX
    }

    //public struct ValueTypes
    //{
    //    public const uint DATA_TYPE_INT8 = uint.MaxValue;
    //    public const uint DATA_TYPE_INT16 = 4294967294u;
    //    public const uint DATA_TYPE_INT32 = 4294967293u;
    //    public const uint DATA_TYPE_INT64 = 4294967292u;
    //    public const uint DATA_TYPE_FLOAT = 4294967291u;
    //    public const uint DATA_TYPE_DOUBLE = 4294967290u;
    //}

    internal class SimVar
    {
        private readonly UIUpdateVariable_Handler UIUpdateVariable;

        // creates unique VarId
        static private int _iNewVarId = 0;
        private readonly int _iVarId;
        internal int iVarId => _iVarId;

        // Example: "FLOAT64_RW_A:AUTOPILOT ALTITUDE LOCK VAR:3, feet"
        internal string sPropStr { get; init; } = "";

        internal string sVarId => $"{iVarId:D04}";

        internal uint ExternalId { get; set; } = 0;
        internal bool bIsRegistered { get; set; } = false;

        // This is only the variable name "AUTOPILOT ALTITUDE LOCK VAR:3"
        private string _sVarName;
        internal string sVarName => _sVarName;

        private byte _bIndex;
        internal byte bIndex => _bIndex;

        // True if we deal with a Custom Event such as A32NX.FCU_SPD_INC (has a '.')
        private bool _bCustomEvent = false;
        internal bool bCustomEvent => _bCustomEvent;

        // Is true in case of R or RW
        private bool _bRead = false;
        internal bool bRead => _bRead;

        // Is true in case of W or RW or VOID
        private bool _bWrite = false;
        internal bool bWrite => _bWrite;

        internal string sRW => $"{(_bRead ? "R" : "")}{(_bWrite ? "W" : "")}";

        // ValType can be one of "INT32", "INT64", "FLOAT32", "FLOAT64", "STRING8", "STRING32", "STRING64", "STRING128", "STRING256" or "STRING260"
        private SIMCONNECT_DATATYPE _scValType;
        internal SIMCONNECT_DATATYPE scValType => _scValType;

        private uint _ValType;
        internal uint ValType => _ValType;

        public string sValType { get {
            switch (_ValType)
            {
                case ValueTypes.DATA_TYPE_INT8:
                    return "int8";
                case ValueTypes.DATA_TYPE_INT16:
                    return "int16";
                case ValueTypes.DATA_TYPE_INT32:
                    return "int32";
                case ValueTypes.DATA_TYPE_INT64:
                    return "int64";
                case ValueTypes.DATA_TYPE_FLOAT:
                    return "float";
                case ValueTypes.DATA_TYPE_DOUBLE:
                    return "double";
                default:
                    return "unknown";
            }}}

        // VarType can be one of 'A', 'L', 'X' or 'K'
        private char _cVarType;
        internal char cVarType => _cVarType;

        // Any unit that is appended. Example "feet"
        private string _sUnit;
        internal string sUnit => _sUnit;

        private PR _ParseResult;
        internal PR ParseResult => _ParseResult;

        internal string sValue { get; set; } = "";

        // keep list of COMDevices that are interested in listening to a Read variable - value is translated iPropId
        private readonly Dictionary<COMDevice, int> _Listeners = new();

        private int _iUsageCnt = 0;
        internal int iUsageCnt => _iUsageCnt;
        internal string sUsageCnt => $"{_iUsageCnt}";

        public override bool Equals(object obj)
        {
            // If the object is the same instance, return true
            if (ReferenceEquals(this, obj)) return true;

            // If the object is not a SimVar or is null, return false
            var other = obj as SimVar;
            if (other == null) return false;

            // Compare the iVarId of both SimVar instances
            return (this.iVarId == other.iVarId);
        }

        public static bool operator ==(SimVar left, SimVar right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.iVarId == right.iVarId;
        }

        public static bool operator !=(SimVar left, SimVar right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            // Use the iVarId which is unique for each SimVar
            return iVarId;
        }

        public override string ToString()
        {
            return $"'{cVarType}' {scValType} {(bRead ? "R" : "")}{(bWrite ? "W" : "")} sUnit = \"{sUnit}\" \"{sVarName}\"";
        }

        internal void IncUsageCnt(COMDevice device, int iPropId)
        {
            lock(_Listeners)
            {
                try
                {
                    // Increase the usage of the simVar
                    _iUsageCnt++;
                    if (_bRead)
                        _Listeners.Add(device, iPropId);
                }
                catch (ArgumentException)
                {
                    // This should never happen. If it does, it means that a device has registered the same read variable more than once.
                    Logging.LogLine(LogLevel.Error, LoggingSource.PRP, $"SimVar.IncUsageCnt: Listener {device} already exists for sPropStr {sPropStr}.");
                }
            }
            // Update the Usage Cnt and Value in the UI
            UIUpdateVariable?.Invoke(UpdateVariable.Usage, this);
        }

        internal int DecUsageCnt(COMDevice device)
        {
            lock (_Listeners)
            {
                if (_iUsageCnt == 0)
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.PRP, $"SimVar.DecUsageCnt: _iUsageCnt was already 0.");
                    return -1;
                }

                // Decrease the usage of the SimVar
                _iUsageCnt--;

                if (_bRead && !_Listeners.Remove(device))
                    // This should never happen. If it does, it means that we try to remove a listener that isn't registered.
                    Logging.LogLine(LogLevel.Error, LoggingSource.PRP, $"SimVar.DecUsageCnt: Listener {device} doesn't exist for sPropStr {sPropStr}.");
            }

            // Update the Usage Cnt in the UI
            UIUpdateVariable?.Invoke(UpdateVariable.Usage, this);

            return _iUsageCnt;
        }

        //private char cValTypeLX
        //{
        //    get
        //    {
        //        switch (_scValType)
        //        {
        //            case SIMCONNECT_DATATYPE.INT32:
        //            case SIMCONNECT_DATATYPE.INT64:
        //                return 'I';
        //            case SIMCONNECT_DATATYPE.FLOAT32:
        //            case SIMCONNECT_DATATYPE.FLOAT64:
        //                return 'F';
        //            case SIMCONNECT_DATATYPE.STRING8:
        //            case SIMCONNECT_DATATYPE.STRING32:
        //            case SIMCONNECT_DATATYPE.STRING64:
        //            case SIMCONNECT_DATATYPE.STRING128:
        //            case SIMCONNECT_DATATYPE.STRING256:
        //            case SIMCONNECT_DATATYPE.STRING260:
        //                return 'S';
        //            default:
        //                return 'V';
        //        }
        //    }
        //}

        private char cValAccessLX
        {
            get
            {
                if (_bRead && _bWrite)
                    return 'B';
                else if (_bRead)
                    return 'R';
                else if (_bWrite)
                    return 'W';
                else
                    return 'N';
            }
        }

        // Dictionaries to keep SimVars by name and by id
        private static readonly ConcurrentDictionary<string, SimVar> _SimVarsByName = new();
        private static readonly ConcurrentDictionary<int, SimVar> _SimVarsById = new();
        internal static ConcurrentDictionary<int, SimVar> SimVarsById => _SimVarsById;
        // Object to make writing and reading to both dictionaries atomic
        internal static readonly object VarLock = new();

        internal bool AddSimVar()
        {
            lock (VarLock)
            {
                if (_SimVarsByName.ContainsKey(sPropStr) || _SimVarsById.ContainsKey(iVarId))
                {
                    Logging.LogLine(LogLevel.Error, LoggingSource.PRP, $"PropertyPool.AddSimVar: simVar {sPropStr} with iVarId {iVarId} already exists");
                    return false;
                }

                // Add to concurrent dictionary using using sPropStr as key
                _SimVarsByName.TryAdd(sPropStr, this);
                // Add to concurrent dictionary using using iVarId as key
                _SimVarsById.TryAdd(iVarId, this);
                // Add the SimVar in the UI
                UIUpdateVariable?.Invoke(UpdateVariable.Add, this);

                return true;
            }
        }

        internal void RemoveSimVar()
        {
            lock (VarLock)
            {
                // Remove from concurrent dictionary using using sPropStr as key
                _SimVarsByName.TryRemove(sPropStr, out SimVar _); // if key not found, just ignore and proceed
                // Remove from concurrent dictionary using using iVarId as key
                _SimVarsById.TryRemove(iVarId, out SimVar _);  // if key not found, just ignore and proceed
                // Remove the SimVar in the UI
                UIUpdateVariable?.Invoke(UpdateVariable.Remove, this);
            }
        }

        internal static SimVar GetSimVarByName(string sVarName)
        {
            // Retrieve SimVar by sPropStr
            _SimVarsByName.TryGetValue(sVarName, out var simVar);
            return simVar; // If not found, result will be default(SimVar), which is null
        }

        internal static SimVar GetSimVarById(int iVarId)
        {
            // Retrieve SimVar by iVarId
            _SimVarsById.TryGetValue(iVarId, out var simVar);
            return simVar; // If not found, result will be default(SimVar), which is null
        }

        internal SimVar(string sPropName, UIUpdateVariable_Handler UIUpdateVariable)
        {
            // Keep the original property name for comparison reasons
            // Example: FLOAT64_RW_A:AUTOPILOT ALTITUDE LOCK VAR:3,feet
            this.sPropStr = sPropName;
            this.UIUpdateVariable = UIUpdateVariable;

            if (new Regex(@"^VOID_[ALKX]:.+$", RegexOptions.IgnoreCase).IsMatch(sPropName))
            {
                // VOID_A:Command[,Unit]
                _scValType = SIMCONNECT_DATATYPE.NONE;
                _bRead = false;
                _bWrite = true;
            }
            else if (new Regex(@"^([A-Z0-9]+)_[RW]((?<!W)W){0,1}_[ALKX]:.+$", RegexOptions.IgnoreCase).IsMatch(sPropName))
            {
                // VALTYPE_RW_A:Command[,Unit]
                int iUnderscore = sPropName.IndexOf('_');
                if (!GetValType(sPropName.Substring(0, iUnderscore)))
                {
                    _ParseResult = PR.UnsupportedValType;
                    return;
                }
                _bRead = (char.ToUpper(sPropName[iUnderscore + 1]) == 'R');
                _bWrite = (char.ToUpper(sPropName[iUnderscore + 1]) == 'W' || char.ToUpper(sPropName[iUnderscore + 2]) == 'W');
            }
            else
            {
                _ParseResult = PR.UnsupportedFormat;
                return;
            }

            // Extract VarType
            int iColon = sPropName.IndexOf(':');
            _cVarType = char.ToUpper(sPropName[iColon - 1]);

            // Extract the trailing Unit if it exists
            int iComma = sPropName.LastIndexOf(',');
            if ((iComma != -1) && (sPropName.Length > iComma + 1))
            {
                // Extract the Unit
                _sUnit = sPropName.Substring(iComma + 1).Trim().ToUpper();
                // Avoid that we don't take a part of the variable name that contains a comma
                // Example: INT32_X:4 (>L:A32NX_EFIS_L_OPTION,enum) (L:A32NX_EFIS_L_OPTION,enum)
                // The above would return ",enum)", but the ')' character indicates that it's not a Unit, but part of the variable name
                if (new Regex(@"^[A-Z0-9]+$").IsMatch(_sUnit))
                    sPropName = sPropName.Substring(0, iComma).Trim();
                else
                    _sUnit = "";
            }
            else
                _sUnit = "";

            // Extract VarName
            if (sPropName.Length < iColon + 2)
            {
                _ParseResult = PR.MissingVarName;
                return;
            }
            _sVarName = sPropName.Substring(iColon + 1).Trim();

            // check if variable has an index
            iColon = _sVarName.LastIndexOf(":");
            if (iColon != -1)
            {
                if (!byte.TryParse(_sVarName.Substring(iColon + 1), out _bIndex))
                    _bIndex = 0;
                else
                    _sVarName = _sVarName.Remove(iColon);
            }

            // check if variable is Custom Event
            _bCustomEvent = _sVarName.IndexOf('.') != -1;

            // A and L variables require a Unit
            if (((_cVarType == 'A') || (_cVarType == 'L')) && (_sUnit == ""))
            {
                _ParseResult = PR.MissingUnit;
                return;
            }

            // K variables can only be INT32 or VOID
            SIMCONNECT_DATATYPE[] KTypes = {
                SIMCONNECT_DATATYPE.INT32,
                SIMCONNECT_DATATYPE.NONE
            };
            if (_cVarType == 'K' && !Array.Exists(KTypes, x => x == _scValType))
            {
                _ParseResult = PR.KVarUnsupportedValType;
                return;
            }

            // K variables can only be write
            if (_cVarType == 'K' && cValAccessLX != 'W')
            {
                _ParseResult = PR.KVarCanOnlyBeWrite;
                return;
            }

            // L variables can only be INT32, FLOAT64 or VOID
            SIMCONNECT_DATATYPE[] LTypes = {
                SIMCONNECT_DATATYPE.INT32,
                SIMCONNECT_DATATYPE.FLOAT64,
                SIMCONNECT_DATATYPE.NONE
            };
            if (_cVarType == 'L' && !Array.Exists(LTypes, x => x == _scValType))
            {
                _ParseResult = PR.LVarUnsupportedValType;
                return;
            }

            // X variables can only be INT32, FLOAT64, STRING256 or VOID
            SIMCONNECT_DATATYPE[] XTypes = {
                SIMCONNECT_DATATYPE.INT32,
                SIMCONNECT_DATATYPE.FLOAT64,
                SIMCONNECT_DATATYPE.STRING256,
                SIMCONNECT_DATATYPE.NONE
            };
            if (_cVarType == 'X' && !Array.Exists(XTypes, x => x == _scValType))
            {
                _ParseResult = PR.XVarUnsupportedValType;
                return;
            }

            // X variables can never be both Read and Write
            if (_cVarType == 'X' && cValAccessLX == 'B')
            {
                _ParseResult = PR.XVarCanNotBeBothReadAndWrite;
                return;
            }

            _iVarId = Interlocked.Increment(ref _iNewVarId); ;
            _ParseResult = PR.Ok;
        }

        private bool GetValType(string sValType)
        {
            switch (sValType.ToUpper())
            {
                case "INT32":
                    _scValType = SIMCONNECT_DATATYPE.INT32;
                    _ValType = ValueTypes.DATA_TYPE_INT32;
                    return true;
                case "INT64":
                    _scValType = SIMCONNECT_DATATYPE.INT64;
                    _ValType = ValueTypes.DATA_TYPE_INT64;
                    return true;
                case "FLOAT32":
                    _scValType = SIMCONNECT_DATATYPE.FLOAT32;
                    _ValType = ValueTypes.DATA_TYPE_FLOAT;
                    return true;
                case "FLOAT64":
                    _scValType = SIMCONNECT_DATATYPE.FLOAT64;
                    _ValType = ValueTypes.DATA_TYPE_DOUBLE;
                    return true;
                case "STRING8":
                    _scValType = SIMCONNECT_DATATYPE.STRING8;
                    _ValType = 0;
                    return true;
                case "STRING32":
                    _scValType = SIMCONNECT_DATATYPE.STRING32;
                    _ValType = 0;
                    return true;
                case "STRING64":
                    _scValType = SIMCONNECT_DATATYPE.STRING64;
                    _ValType = 0;
                    return true;
                case "STRING128":
                    _scValType = SIMCONNECT_DATATYPE.STRING128;
                    _ValType = 0;
                    return true;
                case "STRING256":
                    _scValType = SIMCONNECT_DATATYPE.STRING256;
                    _ValType = 0;
                    return true;
                case "STRING260":
                    _scValType = SIMCONNECT_DATATYPE.STRING260;
                    _ValType = 0;
                    return true;
                default:
                    _scValType = SIMCONNECT_DATATYPE.NONE;
                    _ValType = 0;
                    return false;
            }
        }

        internal void DispatchSimVar()
        {
            lock (_Listeners)
            {
                foreach (var listener in _Listeners)
                {
                    if (listener.Key?.PNPDeviceID != "VIRTUAL")
                        listener.Key?.AddCmdToTxPumpQueue(listener.Value, sValue);
                }
            }
            // Update the value in the UI
            UIUpdateVariable?.Invoke(UpdateVariable.Value, this);
        }

        internal bool ConvertDataForSimVar(DataRequestRecord dr)
        {
            bool bConversionSucceeded;

            switch (ValType)
            {
                case ValueTypes.DATA_TYPE_INT8:
                    if (bConversionSucceeded = dr.tryConvert(out byte i8Val))
                        sValue = i8Val.ToString();
                    break;
                case ValueTypes.DATA_TYPE_INT32:
                    if (bConversionSucceeded = dr.tryConvert(out int i32Val))
                        sValue = i32Val.ToString();
                    break;
                case ValueTypes.DATA_TYPE_INT64:
                    if (bConversionSucceeded = dr.tryConvert(out long i64Val))
                        sValue = i64Val.ToString();
                    break;
                case ValueTypes.DATA_TYPE_FLOAT:
                    if (bConversionSucceeded = dr.tryConvert(out float fVal))
                        sValue = fVal.ToString("0.000", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                    break;
                case ValueTypes.DATA_TYPE_DOUBLE:
                    if (bConversionSucceeded = dr.tryConvert(out double dVal))
                        sValue = dVal.ToString("0.000", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                    break;
                default:
                    if (bConversionSucceeded = dr.tryConvert(out string sVal))
                        sValue = sVal;
                    break;
            }

            if (bConversionSucceeded)
                Logging.LogLine(LogLevel.Debug, LoggingSource.APP, $"SimVar.ConvertDataForSimVar: {dr.requestId} \"{dr.nameOrCode}\" has value \"{sValue}\"");
            else
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimVar.ConvertDataForSimVar: {dr.requestId} \"{dr.nameOrCode}\" - Conversion failed");

            return bConversionSucceeded;
        }

        internal bool CheckDataForSimVar(string sData)
        {
            bool bConversionSucceeded;

            switch (ValType)
            {
                case ValueTypes.DATA_TYPE_INT8:
                    if (bConversionSucceeded = byte.TryParse(sData, out byte _))
                        sValue = sData;
                    break;
                case ValueTypes.DATA_TYPE_INT32:
                    if (bConversionSucceeded = int.TryParse(sData, out int _))
                        sValue = sData;
                    break;
                case ValueTypes.DATA_TYPE_INT64:
                    if (bConversionSucceeded = long.TryParse(sData, out long _))
                        sValue = sData;
                    break;
                case ValueTypes.DATA_TYPE_FLOAT:
                    if (bConversionSucceeded = float.TryParse(sData, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out float fVal))
                        sValue = fVal.ToString("0.000", CultureInfo.GetCultureInfo("en-US"));
                        break;
                case ValueTypes.DATA_TYPE_DOUBLE:
                    if (bConversionSucceeded = double.TryParse(sData, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out double dVal))
                        sValue = dVal.ToString("0.000", CultureInfo.GetCultureInfo("en-US"));
                    break;
                default:
                    bConversionSucceeded = true;
                    break;
            }

            if (bConversionSucceeded)
                Logging.LogLine(LogLevel.Debug, LoggingSource.APP, $"SimVar.CheckDataForSimVar: {sData} is a valid {ValType}");
            else
                Logging.LogLine(LogLevel.Error, LoggingSource.APP, $"SimVar.CheckDataForSimVar: Value {sData} is not a valid {ValType}");

            return bConversionSucceeded;
        }
    }
}
