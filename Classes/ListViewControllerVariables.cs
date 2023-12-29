using System.ComponentModel;

namespace CockpitHardwareHUB_v2.Classes
{
    internal class ListViewControllerVariables
    {
        // ListView and BindingList
        private ListView _ListView;
        private readonly SortedBindingListVariables _BindingList = new();

        // ListView column widths (width of Variable is calculated remaining width)
        private const int wID = 40;
        private const int wUsage = 40;
        private const int wRW = 30;
        private const int wValue = 80;

        internal string FilterRW { set { _BindingList.FilterRW = value; } }
        internal string FilterName { set { _BindingList.FilterName = value; } }

        private class ListItemVariables
        {
            private readonly int _iVarId;
            private int _iUsageCnt;
            private readonly string _sRW;
            private readonly string _sVarName;
            private string _sValue;

            internal int iVarId => _iVarId;
            internal int iUsageCnt { get { return _iUsageCnt; } set { _iUsageCnt = value; } }
            internal string sUsageCnt => _iUsageCnt.ToString();
            internal string sRW => _sRW;
            internal string sVarName => _sVarName;
            internal string sValue { get { return _sValue; } set { _sValue = value; } }

            internal ListItemVariables(SimVar simVar)
            {
                _iVarId = simVar.iVarId;
                _iUsageCnt = simVar.iUsageCnt;
                _sRW = simVar.sRW;
                _sVarName = simVar.sPropStr;
                _sValue = simVar.sValue;
            }
            internal ListViewItem listViewItem => new ListViewItem(new[] {
                $"{_iVarId:D04}",
                _iUsageCnt.ToString(),
                _sRW,
                _sVarName,
                _sValue
        });
        }

        private class SortedBindingListVariables : BindingList<ListItemVariables>
        {
            // Filter Properties
            private string _FilterRW = "";
            private string _FilterName = "";

            internal string FilterRW
            {
                set
                {
                    _FilterRW = value;
                    Refresh();
                }
            }

            internal string FilterName
            {
                set
                {
                    _FilterName = value;
                    Refresh();
                }
            }

            protected override void InsertItem(int index, ListItemVariables listItem)
            {
                if (!Filter(listItem))
                    return; // Do not add the item if it does not pass the filter

                int insertionIndex = FindInsertionIndex(listItem);
                base.InsertItem(insertionIndex, listItem);
            }

            internal new void Remove(ListItemVariables listItem)
            {

                var item = this.FirstOrDefault(li => li.iVarId == listItem.iVarId);
                if (item != null)
                    base.Remove(item);
            }

            internal void Change(ListItemVariables listItem)
            {
                int index = IndexOf(this.FirstOrDefault(li => li.iVarId == listItem.iVarId));
                if (index >= 0)
                {
                    // Update the properties of the found item
                    this[index].iUsageCnt = listItem.iUsageCnt;
                    this[index].sValue = listItem.sValue;

                    // Manually raise the ListChanged event
                    this.OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
                }
            }

            internal void Refresh()
            {
                RaiseListChangedEvents = false; // Suppress individual list changed events

                ClearItems();
                lock (SimVar.VarLock)
                {
                    foreach (KeyValuePair<int, SimVar> kvp in SimVar.SimVarsById)
                        Add(new ListItemVariables(kvp.Value));
                }

                RaiseListChangedEvents = true; // Re-enable list changed events

                // Raise a Reset event to indicate to bound controls that they should refresh
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }

            private int FindInsertionIndex(ListItemVariables listItem)
            {
                int low = 0;
                int high = Count - 1;

                while (low <= high)
                {
                    int mid = low + (high - low) / 2;
                    if (this[mid].iVarId < listItem.iVarId)
                        low = mid + 1;
                    else if (this[mid].iVarId > listItem.iVarId)
                        high = mid - 1;
                    else
                        throw new InvalidOperationException($"An item with iVarId {listItem.iVarId} already exists.");
                }

                return low;
            }

            protected virtual bool Filter(ListItemVariables listItem)
            {
                bool bFilterName = listItem.sVarName.Contains(_FilterName, StringComparison.OrdinalIgnoreCase);
                bool bFilterRW = listItem.sRW.Contains(_FilterRW, StringComparison.OrdinalIgnoreCase);

                // Return true to include the item, or false to exclude it
                return (bFilterName && bFilterRW);
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
                sf.Alignment = StringAlignment.Center;
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
                case ListChangedType.ItemChanged:
                    BindingList_UpdateListViewItem(e.NewIndex);
                    break;
                case ListChangedType.Reset:
                    BindingList_ResetListViewItems();
                    break;
            }
        }

        private void BindingList_AddListViewItem(int index)
        {
            ListItemVariables listItem = _BindingList[index];
            _ListView.Items.Insert(index, listItem.listViewItem);
        }

        private void BindingList_RemoveListViewItem(int index)
        {
            _ListView.Items.RemoveAt(index);
        }

        private void BindingList_UpdateListViewItem(int index)
        {
            ListItemVariables listItem = _BindingList[index];
            ListViewItem listViewItem = _ListView.Items[index];
            listViewItem.SubItems[1].Text = listItem.sUsageCnt;
            listViewItem.SubItems[4].Text = listItem.sValue;
        }

        private void BindingList_ResetListViewItems()
        {
            _ListView.Items.Clear();

            foreach (ListItemVariables listItem in _BindingList)
                _ListView.Items.Add(listItem.listViewItem);
        }

        // Constructor and Interface
        internal ListViewControllerVariables(ListView listView)
        {
            _ListView = listView;

            // Override drawing
            _ListView.OwnerDraw = true;

            _ListView.ColumnWidthChanging += new ColumnWidthChangingEventHandler(ListView_ColumnWidthChanging);
            _ListView.DrawColumnHeader += new DrawListViewColumnHeaderEventHandler(ListView_DrawColumnHeader);
            _ListView.DrawItem += new DrawListViewItemEventHandler(ListView_DrawItem);
            _ListView.DrawSubItem += new DrawListViewSubItemEventHandler(ListView_DrawSubItem);

            _ListView.Columns.Add("ID", wID, HorizontalAlignment.Left);
            _ListView.Columns.Add("Use", wUsage, HorizontalAlignment.Left);
            _ListView.Columns.Add("RW", wRW, HorizontalAlignment.Left);
            _ListView.Columns.Add("Variable name", _ListView.Width - wID - wUsage - wRW - wValue - SystemInformation.VerticalScrollBarWidth - 4, HorizontalAlignment.Left);
            _ListView.Columns.Add("Value", wValue, HorizontalAlignment.Left);

            _BindingList.ListChanged += BindingList_ListChanged;
        }

        internal void AddSimVar(SimVar simVar)
        {
            _BindingList.Add(new ListItemVariables(simVar));
        }

        internal void RemoveSimVar(SimVar simVar)
        {
            _BindingList.Remove(new ListItemVariables(simVar));
        }

        internal void ChangeSimVar(SimVar simVar)
        {
            _BindingList.Change(new ListItemVariables(simVar));
        }

        internal void RefreshSimVars()
        {
            _BindingList.Refresh();
        }
    }
}
