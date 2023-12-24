using Microsoft.Win32;
using System.Collections.Concurrent;
using WASimCommander.CLI.Enums;


namespace CockpitHardwareHUB_v2.Classes
{
    public enum LoggingSource : byte
    {
        CLT = LogSource.Client, // Re-use WASimCommander enum value LogSource.Client
        SRV = LogSource.Server, // Re-use WASimCommander enum value LogSource.Server
        APP,
        DEV,
        PRP
    }

    public struct LogData
    {
        private DateTime _dtTimeStamp;
        private static DateTime _PreviousTimeStamp = default;
        private int _Delta;
        private LogLevel _LogLevel; // re-use of same LogLevel enum as defined in WASimCommander
        private LoggingSource _LoggingSource;
        private string _sLogLine;

        public string sTimeStamp { get { return $"{_dtTimeStamp.ToString("HH:mm:ss:fff")}[{sDelta}]"; } }
        public string sLogLevel {  get { return _LogLevel.ToString(); } }
        public string sLoggingSource { get { return _LoggingSource.ToString(); } }
        public string sLogLine { get { return _sLogLine; } }
        private string sDelta => _Delta < 0 ? $"{_Delta:D03}" : $"{_Delta:D04}";

        public LogData(LogLevel logLevel, LoggingSource loggingSource, string sLoggingMsg, UInt64 timestamp = 0)
        {
            _LogLevel = logLevel;
            _LoggingSource = loggingSource;
            _sLogLine = sLoggingMsg;
            if (timestamp == 0)
                _dtTimeStamp = DateTime.Now;
            else
                _dtTimeStamp = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp).LocalDateTime;
            if (_PreviousTimeStamp == default)
                _Delta = 0;
            else
            {
                double d = (_dtTimeStamp - _PreviousTimeStamp).TotalMilliseconds;
                _Delta = (int)Math.Max(Math.Min(d, 9999), -999);
            }
            _PreviousTimeStamp = _dtTimeStamp;
        }
    }

    internal static class Logging
    {
        private static ConcurrentQueue<LogData> _LogData = new();
        public static ConcurrentQueue<LogData> LogData { get { return _LogData; } }

        private static LogLevel _SetLogLevel = LogLevel.Info;
        internal static string sLogLevel { get => _SetLogLevel.ToString(); set => Enum.TryParse(value, out _SetLogLevel); }
        internal static LogLevel SetLogLevel { get => _SetLogLevel; set => _SetLogLevel = value; }    

        internal static void LogLine(LogLevel logLevel, LoggingSource loggingSource, string sLoggingMsg, UInt64 timestamp = 0)
        {
            if (logLevel <= _SetLogLevel)
                _LogData.Enqueue(new LogData(logLevel, loggingSource, sLoggingMsg, timestamp));
        }
    }
    internal static class FileLogger
    {
        internal static string sFileName { get => _bIsOpen ? _sFileName : "No log file active"; }

        private static StreamWriter LogFile;
        private static string _sFileName = "";
        private static bool _bIsOpen = false;

        internal static bool OpenFile()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\CockpitHardwareHUB");
            _sFileName = (string)key.GetValue("LogFileName");

            if (string.IsNullOrEmpty(_sFileName))
                _sFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\log.txt";

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                dialog.FileName = Path.GetFileName(_sFileName);
                dialog.InitialDirectory = Path.GetDirectoryName(_sFileName);
                dialog.Title = "Select Log File";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _sFileName = dialog.FileName;
                    key.SetValue("LogFileName", _sFileName);
                    LogFile = new StreamWriter(_sFileName, false);
                    _bIsOpen = true;
                    LogFile.WriteLine($"{DateTime.Now}: Logfile created");
                    LogFile.WriteLine("-------------------------------------------------");
                    return true;
                }
                else
                    _bIsOpen = false;
            }

            key.Close();

            return _bIsOpen;
        }

        internal static void CloseFile()
        {
            if (_bIsOpen)
            {
                LogFile.Close();
                _bIsOpen = false;
            }
        }

        internal static void FlushFile()
        {
            if (_bIsOpen)
            {
                LogFile.Flush();
            }
        }

        internal static void LogLine(string sLogLine)
        {
             if (_bIsOpen)
                LogFile.WriteLine(sLogLine);
        }
    }
}
