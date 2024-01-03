using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
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
        XVarCanNotBeBothReadAndWrite,
        FormatSpecifiersNotAllowed
    }

    internal enum ValueTypes : uint
    {
        INT8 = WASimCommander.CLI.ValueTypes.DATA_TYPE_INT8,       // -1
        INT16 = WASimCommander.CLI.ValueTypes.DATA_TYPE_INT16,     // -2
        INT32 = WASimCommander.CLI.ValueTypes.DATA_TYPE_INT32,     // -3
        INT64 = WASimCommander.CLI.ValueTypes.DATA_TYPE_INT64,     // -4
        FLOAT32 = WASimCommander.CLI.ValueTypes.DATA_TYPE_FLOAT,   // -5
        FLOAT64 = WASimCommander.CLI.ValueTypes.DATA_TYPE_DOUBLE,  // -6
        STRING16 = 16,
        STRING32 = 32,
        STRING64 = 64,
        STRING128 = 128,
        STRING256 = 256,
        VOID = 257,
        INVALID = 512
    }

    internal class SimVar
    {
        private readonly UIUpdateVariable_Handler UIUpdateVariable;

        private static readonly Dictionary<string, ValueTypes> _valTypeMap = new Dictionary<string, ValueTypes>
        {
            { "INT8", ValueTypes.INT8 },
            { "INT16", ValueTypes.INT16 },
            { "INT32", ValueTypes.INT32 },
            { "INT64", ValueTypes.INT64 },
            { "FLOAT32", ValueTypes.FLOAT32 },
            { "FLOAT64", ValueTypes.FLOAT64 },
            { "STRING16", ValueTypes.STRING16 },
            { "STRING32", ValueTypes.STRING32 },
            { "STRING64", ValueTypes.STRING64 },
            { "STRING128", ValueTypes.STRING128 },
            { "STRING256", ValueTypes.STRING256 },
            { "VOID", ValueTypes.VOID },
            { "INVALID", ValueTypes.INVALID }
        };

        private bool GetValType(string sValType)
        {
            sValType = sValType.ToUpper();

            if (!_valTypeMap.TryGetValue(sValType, out ValueTypes valType))
                return false;

            _ValType = valType;
            return true;
        }

        internal CalcResultType CRType
        {
            get
            {
                switch (_ValType)
                {
                    case ValueTypes.INT8:
                    case ValueTypes.INT16:
                    case ValueTypes.INT32:
                        return CalcResultType.Integer;
                    case ValueTypes.INT64:
                    case ValueTypes.FLOAT32:
                    case ValueTypes.FLOAT64:
                        return CalcResultType.Double;
                    case ValueTypes.STRING16:
                    case ValueTypes.STRING32:
                    case ValueTypes.STRING64:
                    case ValueTypes.STRING128:
                    case ValueTypes.STRING256:
                        return CalcResultType.String;
                    default:
                        return CalcResultType.None;
                }
            }
        }

        // creates unique VarId
        static private int _iNewVarId = 0;
        private readonly int _iVarId;
        internal int iVarId => _iVarId;

        // Example: "FLOAT64_RW_A:AUTOPILOT ALTITUDE LOCK VAR:3, feet"
        internal string sPropStr { get; init; } = "";

        internal string sVarId => $"{iVarId:D04}";

        internal uint ExternalId { get; set; } = 0;
        internal bool bIsRegistered { get; set; } = false;

        // This is only the variable name "AUTOPILOT ALTITUDE LOCK VAR" (without eventual index)
        private string _sVarName;
        internal string sVarName => _sVarName;

        private byte _Index;
        internal byte Index => _Index;

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

        // ValType can be INT8, INT16, INT32, INT64, FLOAT, DOUBLE and VOID (=0)
        private ValueTypes _ValType;
        internal ValueTypes ValType => _ValType;

        // VarType can be one of 'A', 'L', 'X' or 'K'
        private char _cVarType;
        internal char cVarType => _cVarType;

        // Any unit that is appended. Example "feet"
        private string _sUnit;
        internal string sUnit => _sUnit;

        internal int iUnitId = -1; // storage for unit id obtained by lookup during registration

        // True if an X-Var contains one of the format specifiers '{0}, {1}, {2}, {3} and {4}' in sequence
        private bool _bFormatedXVar = false;
        internal bool bFormatedXVar => _bFormatedXVar;


        private PR _ParseResult;
        internal PR ParseResult => _ParseResult;

        internal string sValue { get; set; } = "";
        internal double[] dValue = new double[5];

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
            return sVarName;
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
                    Logging.Log(LogLevel.Error, LoggingSource.VAR, () => $"SimVar.IncUsageCnt: {device} already listens for sPropStr \"{sPropStr}\"");
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
                    Logging.Log(LogLevel.Error, LoggingSource.VAR, () => $"SimVar.DecUsageCnt: _iUsageCnt was already 0 for \"{sPropStr}\"");
                    return -1;
                }

                // Decrease the usage of the SimVar
                _iUsageCnt--;

                if (_bRead && !_Listeners.Remove(device))
                    // This should never happen. If it does, it means that we try to remove a listener that isn't registered.
                    Logging.Log(LogLevel.Error, LoggingSource.VAR, () => $"SimVar.DecUsageCnt: {device} doesn't listen for sPropStr \"{sPropStr}\"");
            }

            // Update the Usage Cnt in the UI
            UIUpdateVariable?.Invoke(UpdateVariable.Usage, this);

            return _iUsageCnt;
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
                    Logging.Log(LogLevel.Error, LoggingSource.VAR, () => $"PropertyPool.AddSimVar: SimVar \"{sPropStr}\" with iVarId {iVarId} already exists");
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

        public bool IsValidFormatSequence()
        {
            string[] patterns = { "{0}", "{1}", "{2}", "{3}", "{4}" };
            bool previousFound = true;

            foreach (string pattern in patterns)
            {
                bool currentFound = sVarName.Contains(pattern);
                if (currentFound && !previousFound)
                {
                    return false; // Found a higher pattern without finding all lower ones
                }
                previousFound = currentFound;
            }

            return sVarName.Contains(patterns[0]); // The string must contain at least {0} to be valid
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
                _ValType = ValueTypes.VOID;
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
                if (!byte.TryParse(_sVarName.Substring(iColon + 1), out _Index))
                    _Index = 0;
                else
                    _sVarName = _sVarName.Remove(iColon);
            }

            // check if variable is Custom Event
            _bCustomEvent = _sVarName.Contains('.');

            // A and L variables require a Unit
            if (_cVarType == 'A' && _sUnit == "")
            {
                _ParseResult = PR.MissingUnit;
                return;
            }

            // A and L can only be INT8, INT16, INT32, INT64, FLOAT32 and FLOAT64
            ValueTypes[] ATypes = {
                ValueTypes.INT8,
                ValueTypes.INT16,
                ValueTypes.INT32,
                ValueTypes.INT64,
                ValueTypes.FLOAT32,
                ValueTypes.FLOAT64
            };
            if ((_cVarType == 'A' || _cVarType == 'L') && !Array.Exists(ATypes, x => x == _ValType))
            {
                _ParseResult = PR.KVarUnsupportedValType;
                return;
            }

            // K variables can only be INT8, INT16, INT32 or VOID
            ValueTypes[] KTypes = {
                ValueTypes.INT8,
                ValueTypes.INT16,
                ValueTypes.INT32,
                ValueTypes.VOID
            };
            if (_cVarType == 'K' && !Array.Exists(KTypes, x => x == _ValType))
            {
                _ParseResult = PR.KVarUnsupportedValType;
                return;
            }

            // K variables can only be write
            if (_cVarType == 'K' && !_bWrite)
            {
                _ParseResult = PR.KVarCanOnlyBeWrite;
                return;
            }

            // X variables can never be both Read and Write
            if (_cVarType == 'X' && _bRead && _bWrite)
            {
                _ParseResult = PR.XVarCanNotBeBothReadAndWrite;
                return;
            }

            // Check if X variable contains the format specifiers {0}, {1}, {2}, {3} or {4} in sequence
            ValueTypes[] XTypes = {
                ValueTypes.INT8,
                ValueTypes.INT16,
                ValueTypes.INT32,
                ValueTypes.FLOAT32,
                ValueTypes.FLOAT64
            };
            _bFormatedXVar = IsValidFormatSequence();
            if (_bFormatedXVar && !(_cVarType == 'X' && _bWrite && Array.Exists(XTypes, x => x == _ValType)))
            {
                _ParseResult = PR.FormatSpecifiersNotAllowed;
                return;
            }

            _iVarId = Interlocked.Increment(ref _iNewVarId); ;
            _ParseResult = PR.Ok;
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

        internal bool ConvertDataRequestRecordToString(DataRequestRecord dr)
        {
            bool bConversionSucceeded = false;

            switch (ValType)
            {
                case ValueTypes.INT8:
                    if (bConversionSucceeded = dr.tryConvert(out byte i8Val))
                        sValue = i8Val.ToString();
                    break;
                case ValueTypes.INT16:
                    if (bConversionSucceeded = dr.tryConvert(out short i16Val))
                        sValue = i16Val.ToString();
                    break;
                case ValueTypes.INT32:
                    if (bConversionSucceeded = dr.tryConvert(out int i32Val))
                        sValue = i32Val.ToString();
                    break;
                case ValueTypes.INT64:
                    if (bConversionSucceeded = dr.tryConvert(out long i64Val))
                        sValue = i64Val.ToString();
                    break;
                case ValueTypes.FLOAT32:
                    if (bConversionSucceeded = dr.tryConvert(out float fVal))
                        sValue = fVal.ToString("0.000", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                    break;
                case ValueTypes.FLOAT64:
                    if (bConversionSucceeded = dr.tryConvert(out double dVal))
                        sValue = dVal.ToString("0.000", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                    break;
                case ValueTypes.STRING16:
                case ValueTypes.STRING32:
                case ValueTypes.STRING64:
                case ValueTypes.STRING128:
                case ValueTypes.STRING256:
                    if (bConversionSucceeded = dr.tryConvert(out string sVal))
                        sValue = sVal;
                    break;
            }

            Array.Clear(dValue, 0, dValue.Length);
            if (bConversionSucceeded)
                dValue[0] = dr.tryConvert(out double d) ? d : 0.0;

            if (bConversionSucceeded)
                Logging.Log(LogLevel.Debug, LoggingSource.VAR, () => $"SimVar.ConvertDataRequestRecordToString: {dr.requestId} \"{dr.nameOrCode}\" has value \"{sValue}\"");
            else
                Logging.Log(LogLevel.Error, LoggingSource.VAR, () => $"SimVar.ConvertDataRequestRecordToString: {dr.requestId} \"{dr.nameOrCode}\" - Conversion failed");

            return bConversionSucceeded;
        }

        internal bool SetValueOfSimVar(string sData)
        {
            Array.Clear(dValue, 0, dValue.Length);
            sValue = sData;
            if (sData == "")
                return true;

            // split the ';'-separated data in maximum 5 individual elements
            string[] sEachData = sData.Split(';',5);
            bool bConversionSucceeded = false;

            for (int i = 0; i < sEachData.Length; i++)
            {
                switch (ValType)
                {
                    case ValueTypes.INT8:
                        if (bConversionSucceeded = byte.TryParse(sEachData[i], out byte i8))
                            dValue[i] = i8;
                        break;
                    case ValueTypes.INT16:
                        if (bConversionSucceeded = short.TryParse(sEachData[i], out short i16))
                            dValue[i] = i16;
                        break;
                    case ValueTypes.INT32:
                        if (bConversionSucceeded = int.TryParse(sEachData[i], out int i32))
                            dValue[i] = i32;
                        break;
                    case ValueTypes.INT64:
                        if (bConversionSucceeded = long.TryParse(sEachData[i], out long i64))
                            dValue[i] = i64;
                        break;
                    case ValueTypes.FLOAT32:
                        if (bConversionSucceeded = float.TryParse(sEachData[i], NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out float f))
                            dValue[i] = f;
                        break;
                    case ValueTypes.FLOAT64:
                        if (bConversionSucceeded = double.TryParse(sEachData[i], NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out double d))
                            dValue[i] = d;
                        break;
                    case ValueTypes.VOID:
                        if (bConversionSucceeded = int.TryParse(sEachData[i], out int i32void))
                            dValue[i] = i32void;
                        break;
                    case ValueTypes.STRING256:
                        bConversionSucceeded = false;
                        break;
                }
            }

            if (bConversionSucceeded)
                Logging.Log(LogLevel.Debug, LoggingSource.VAR, () => $"SimVar.SetValueOfSimVar: \"{sData}\" contains all valid [{ValType}] types");
            else
            {
                Array.Clear(dValue, 0, dValue.Length);
                sValue = "0";
                Logging.Log(LogLevel.Error, LoggingSource.VAR, () => $"SimVar.SetValueOfSimVar: \"{sData}\" contains not all valid [{ValType}] types");
            }

            return bConversionSucceeded;
        }
    }
}
