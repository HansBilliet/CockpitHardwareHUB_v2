using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WASimCommander.CLI.Enums;


namespace CockpitHardwareHUB_v2.Classes
{
    public enum LoggingSource : byte
    {
        CLT = LogSource.Client, // Re-use WASimCommander enum value LogSource.Client
        SRV = LogSource.Server, // Re-use WASimCommander enum value LogSource.Server
        APP,
        DEV
    }

    public struct LogData
    {
        private DateTime _dtTimeStamp;
        private LogLevel _LogLevel; // re-use of same LogLevel enum as defined in WASimCommander
        private LoggingSource _LoggingSource;
        private string _sLogLine;

        public string sTimeStamp { get { return _dtTimeStamp.ToString("HH:mm:ss:fff"); } }
        public string sLogLevel {  get { return _LogLevel.ToString(); } }
        public string sLoggingSource { get { return _LoggingSource.ToString(); } }
        public string sLogLine { get { return _sLogLine; } }

        public LogData(LogLevel logLevel, LoggingSource loggingSource, string sLoggingMsg, UInt64 timestamp = 0)
        {
            _LogLevel = logLevel;
            _LoggingSource = loggingSource;
            _sLogLine = sLoggingMsg;
            if (timestamp == 0)
                _dtTimeStamp = DateTime.Now;
            else
                _dtTimeStamp = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp).LocalDateTime;
        }
    }

    internal static class Logging
    {
        private static ConcurrentQueue<LogData> _LogData = new();
        public static ConcurrentQueue<LogData> LogData { get { return _LogData; } }

        private static LogLevel _SetLogLevel = LogLevel.Info;
        public static string sLogLevel { get => _SetLogLevel.ToString(); set => Enum.TryParse(value, out _SetLogLevel); }
        public static LogLevel SetLogLevel { get => _SetLogLevel; set => _SetLogLevel = value; }    

        public static void LogLine(LogLevel logLevel, LoggingSource loggingSource, string sLoggingMsg, UInt64 timestamp = 0)
        {
            if (logLevel <= _SetLogLevel)
                _LogData.Enqueue(new LogData(logLevel, loggingSource, sLoggingMsg, timestamp));
        }
    }
}
