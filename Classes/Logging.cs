using Microsoft.Win32;
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

    internal static class Logging
    {
        internal delegate void UIUpdateLogging_Handler(LogLevel logLevel, LoggingSource loggingSource, string sLoggingMsg, UInt64 timestamp = 0);
        internal static event UIUpdateLogging_Handler UIUpdateLogging;

        private static LogLevel _SetLogLevel = LogLevel.Info;
        internal static string sLogLevel { get => _SetLogLevel.ToString(); set => Enum.TryParse(value, out _SetLogLevel); }
        internal static LogLevel SetLogLevel { get => _SetLogLevel; set => _SetLogLevel = value; }    

        internal static void LogLine(LogLevel logLevel, LoggingSource loggingSource, string sLoggingMsg, UInt64 timestamp = 0)
        {
            if (logLevel <= _SetLogLevel)
                UIUpdateLogging?.Invoke(logLevel, loggingSource, sLoggingMsg, timestamp);
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
                LogFile.Flush();
        }

        internal static void LogLine(string sLogLine)
        {
             if (_bIsOpen)
                LogFile.WriteLine(sLogLine);
        }
    }
}
