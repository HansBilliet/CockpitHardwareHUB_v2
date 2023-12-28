using System.ComponentModel;
using WASimCommander.CLI.Enums;

namespace CockpitHardwareHUB_v2.Classes
{
    internal class ListViewControllerLogging
    {
        // ListView and BindingList
        private ListView _ListView;
        private readonly BindingListLogging _BindingList = new();

        // ListView column widths
        private const int wTimeStamp = 105;
        private const int wLevel = 50;
        private const int wSource = 50;
        private const int wLogLine = 1000;

        internal string FilterLogLine { set { _BindingList.FilterLogLine = value; } }

        private class ListItemLogging
        {
            private LogLevel _LogLevel; // re-use of same LogLevel enum as defined in WASimCommander
            private LoggingSource _LoggingSource;
            private string _sLogLine;
            private DateTime _dtTimeStamp;
            private static DateTime _PreviousTimeStamp = default;
            private int _Delta;

            internal string sLogLevel { get { return _LogLevel.ToString(); } }
            internal string sLoggingSource { get { return _LoggingSource.ToString(); } }
            internal string sLogLine { get { return _sLogLine; } }
            private string sDelta => _Delta < 0 ? $"{_Delta:D03}" : $"{_Delta:D04}";
            internal string sTimeStamp { get { return $"{_dtTimeStamp.ToString("HH:mm:ss:fff")}[{sDelta}]"; } }

            internal ListItemLogging(LogLevel logLevel, LoggingSource loggingSource, string sLogLine, UInt64 timestamp = 0)
            {
                _LogLevel = logLevel;
                _LoggingSource = loggingSource;
                _sLogLine = sLogLine;
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

            internal ListViewItem listViewItem => new ListViewItem(new[] {
                sTimeStamp,
                sLogLevel,
                sLoggingSource,
                sLogLine
            });
        }

        private class BindingListLogging : BindingList<ListItemLogging>
        {
            // List of all LoggingLines
            private readonly List<ListItemLogging> _loggingLines = new();
            private int _MaxLogLines = 1000;

            private string _FilterLogLine = "";
            //private int _MaxLogLines = 1000;

            internal string FilterLogLine
            {
                set
                {
                    _FilterLogLine = value;
                    Refresh();
                }
            }

            internal int MaxLogLines { set { _MaxLogLines = value; } }

            internal void AddLoggingLine(ListItemLogging listItem)
            {
                _loggingLines.Add(listItem);
                if (_loggingLines.Count > _MaxLogLines)
                    _loggingLines.RemoveAt(0);
            }

            internal void ClearLogging()
            {
                _loggingLines.Clear();
                Refresh();
            }

            protected override void InsertItem(int index, ListItemLogging listItem)
            {
                if (!Filter(listItem))
                    return; // Do not add the item if it does not pass the filter

                base.InsertItem(index, listItem);

                if (Count > _MaxLogLines)
                    RemoveAt(0);
            }

            internal void Refresh()
            {
                RaiseListChangedEvents = false; // Suppress individual list changed events

                ClearItems();
                foreach (ListItemLogging listItem in _loggingLines)
                    Add(listItem);

                RaiseListChangedEvents = true; // Re-enable list changed events

                // Raise a Reset event to indicate to bound controls that they should refresh
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }

            protected virtual bool Filter(ListItemLogging listItem)
            {
                bool bFilterLogLine = listItem.sLogLine.Contains(_FilterLogLine, StringComparison.OrdinalIgnoreCase);

                // Return true to include the item, or false to exclude it
                return (bFilterLogLine);
            }
        }

        // ListView drawing control
        private void ListView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = _ListView.Columns[e.ColumnIndex].Width;
        }

        private void ListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush headerBrush = new SolidBrush(Color.Black))
            {
                e.Graphics.FillRectangle(headerBrush, e.Bounds);
            }

            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = (e.Header.Text == "  LogLine" ? StringAlignment.Near : StringAlignment.Center);
                sf.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(e.Header.Text, e.Font, textBrush, e.Bounds, sf);
            }

            // Draw the vertical lines between header items
            using (Pen whitePen = new Pen(Color.White))
            {
                e.Graphics.DrawLine(whitePen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
            }
        }

        private void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void ListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        // BindingList Change methods
        private void BindingList_ListChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    BindingList_AddListViewItem(e.NewIndex);
                    break;
                case ListChangedType.ItemDeleted:
                    BindingList_RemoveListViewItem(e.NewIndex);
                    break;
                case ListChangedType.Reset:
                    BindingList_ResetListViewItems();
                    break;
            }
        }

        private void BindingList_AddListViewItem(int index)
        {
            ListItemLogging listItem = _BindingList[index];
            _ListView.Items.Insert(index, listItem.listViewItem);
        }

        private void BindingList_RemoveListViewItem(int index)
        {
            _ListView.Items.RemoveAt(index);
        }

        private void BindingList_ResetListViewItems()
        {
            _ListView.Items.Clear();

            foreach (ListItemLogging listItem in _BindingList)
                _ListView.Items.Add(listItem.listViewItem);
        }


        // Constructor and Interface
        internal ListViewControllerLogging(ListView listView, int MaxLogLines)
        {
            _ListView = listView;
            _BindingList.MaxLogLines = MaxLogLines;

            // Override drawing
            _ListView.OwnerDraw = true;

            _ListView.ColumnWidthChanging += new ColumnWidthChangingEventHandler(ListView_ColumnWidthChanging);
            _ListView.DrawColumnHeader += new DrawListViewColumnHeaderEventHandler(ListView_DrawColumnHeader);
            _ListView.DrawItem += new DrawListViewItemEventHandler(ListView_DrawItem);
            _ListView.DrawSubItem += new DrawListViewSubItemEventHandler(ListView_DrawSubItem);

            _ListView.Columns.Add("TimeStamp", wTimeStamp, HorizontalAlignment.Left);
            _ListView.Columns.Add("Level", wLevel, HorizontalAlignment.Left);
            _ListView.Columns.Add("Source", wSource, HorizontalAlignment.Left);
            _ListView.Columns.Add("  LogLine", wLogLine, HorizontalAlignment.Left);

            _BindingList.ListChanged += BindingList_ListChanged;
        }

        internal void LogLine(LogLevel logLevel, LoggingSource loggingSource, string sLogLine, UInt64 timestamp = 0)
        {
            ListItemLogging listItem = new ListItemLogging(logLevel, loggingSource, sLogLine, timestamp);

            _BindingList.AddLoggingLine(listItem);
            _BindingList.Add(listItem);

            FileLogger.LogLine($"{listItem.sTimeStamp} - {listItem.sLogLevel} - {listItem.sLoggingSource} - {listItem.sLogLine}");
            FileLogger.FlushFile();

            if (_ListView.Items.Count > 0 )
                _ListView.EnsureVisible(_ListView.Items.Count - 1);
        }
        internal void RefreshLogging()
        {
            _BindingList.Refresh();
        }

        internal void ClearLogging()
        {
            _BindingList.ClearLogging();
        }
    }
}
