using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    /// <summary>
    /// Filters a ComboBox as the user types. Selection is committed on DropDownClosed.
    /// Uses plain Items while typing to avoid WinForms DataSource auto-select behaviour.
    /// </summary>
    public sealed class FilteredComboBinder
    {
        private readonly ComboBox _combo;
        private readonly string _valueMember;
        private readonly string _displayMember;
        private DataTable _fullSource;
        private readonly List<long> _filteredIds = new List<long>();
        private bool _suppressEvents;
        private bool _itemsMode;
        private string _lastTypedText = "";

        public FilteredComboBinder(ComboBox combo, string valueMember, string displayMember)
        {
            _combo = combo ?? throw new ArgumentNullException(nameof(combo));
            _valueMember = valueMember;
            _displayMember = displayMember;
            _combo.DropDownStyle = ComboBoxStyle.DropDown;
            _combo.AutoCompleteMode = AutoCompleteMode.None;
            _combo.MaxDropDownItems = 16;
            _combo.TextUpdate += Combo_TextUpdate;
            _combo.DropDownClosed += Combo_DropDownClosed;
            _combo.KeyDown += Combo_KeyDown;
            _combo.Enter += Combo_Enter;
        }

        private void Combo_Enter(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            if (!string.IsNullOrWhiteSpace(SafeGetText())) return;
            BindFullList(0);
            if (_combo.Items.Count > 0)
                _combo.DroppedDown = true;
        }

        private void Combo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _combo.Items.Count > 0 && !_combo.DroppedDown)
                _combo.DroppedDown = true;
            else if (e.KeyCode == Keys.Escape && _combo.DroppedDown)
            {
                _combo.DroppedDown = false;
                e.Handled = true;
            }
        }

        private void Combo_DropDownClosed(object sender, EventArgs e)
        {
            if (_suppressEvents) return;

            if (GetSelectedId() > 0)
            {
                _lastTypedText = SafeGetText();
                OnSelectionCommitted();
            }
        }

        private void Combo_TextUpdate(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            _combo.BeginInvoke(new Action(ApplyFilterFromComboText));
        }

        private void ApplyFilterFromComboText()
        {
            if (_suppressEvents || _combo.IsDisposed) return;
            string text = SafeGetText();
            _lastTypedText = text;
            if (string.IsNullOrEmpty(text))
                BindFullList(0);
            else
                BindFilteredItems(text);
        }

        public bool SuppressEvents
        {
            get => _suppressEvents;
            set => _suppressEvents = value;
        }

        public event EventHandler SelectionCommitted;

        public void SetSource(DataTable source, long selectedId = 0)
        {
            LoadSource(source, selectedId);
        }

        public void RefreshSource(DataTable source, long selectedId = 0)
        {
            LoadSource(source, selectedId);
        }

        private void LoadSource(DataTable source, long selectedId)
        {
            _fullSource = source?.Copy();
            if (selectedId > 0)
                BindFullList(selectedId);
            else
                BindFullList(0);
        }

        public long GetSelectedId()
        {
            if (_itemsMode)
            {
                if (_combo.SelectedIndex >= 0 && _combo.SelectedIndex < _filteredIds.Count)
                    return _filteredIds[_combo.SelectedIndex];

                string text = SafeGetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    for (int i = 0; i < _combo.Items.Count; i++)
                    {
                        if (string.Equals(_combo.Items[i]?.ToString(), text, StringComparison.OrdinalIgnoreCase)
                            && i < _filteredIds.Count)
                            return _filteredIds[i];
                    }
                }
                return 0;
            }

            try
            {
                if (_combo.SelectedValue != null && _combo.SelectedValue != DBNull.Value
                    && long.TryParse(_combo.SelectedValue.ToString(), out long id) && id > 0)
                    return id;
            }
            catch { }

            if (_combo.DataSource is DataTable && _combo.SelectedIndex >= 0 && _combo.SelectedIndex < _combo.Items.Count)
            {
                if (_combo.Items[_combo.SelectedIndex] is DataRowView drv
                    && drv.Row.Table.Columns.Contains(_valueMember)
                    && drv[_valueMember] != DBNull.Value
                    && long.TryParse(drv[_valueMember].ToString(), out long rowId) && rowId > 0)
                    return rowId;
            }
            return 0;
        }

        public void SelectById(long id)
        {
            BindFullList(id);
        }

        public void ClearSelection()
        {
            BindFullList(0);
        }

        private string SafeGetText()
        {
            try
            {
                return _combo.Text ?? "";
            }
            catch (ArgumentOutOfRangeException)
            {
                return _lastTypedText ?? "";
            }
        }

        private void BindFullList(long selectId)
        {
            _suppressEvents = true;
            try
            {
                _itemsMode = false;
                _filteredIds.Clear();
                _combo.DataSource = null;
                _combo.Items.Clear();

                DataTable source = _fullSource?.Copy();
                if (source == null)
                {
                    _combo.Text = "";
                    _lastTypedText = "";
                    return;
                }

                _combo.DataSource = source;
                _combo.DisplayMember = _displayMember;
                _combo.ValueMember = _valueMember;

                if (selectId > 0)
                {
                    SetComboLongValue(selectId);
                    _lastTypedText = SafeGetText();
                }
                else
                {
                    _combo.SelectedIndex = -1;
                    _combo.Text = "";
                    _lastTypedText = "";
                }
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void BindFilteredItems(string filterText)
        {
            _suppressEvents = true;
            try
            {
                filterText = filterText ?? "";
                int caret = 0;
                try { caret = _combo.SelectionStart; } catch { caret = filterText.Length; }

                DataTable filtered = FilterDataTable(_fullSource, filterText, _displayMember);
                _itemsMode = true;
                _filteredIds.Clear();
                _combo.DataSource = null;
                _combo.Items.Clear();

                foreach (DataRow row in filtered.Rows)
                {
                    if (!filtered.Columns.Contains(_displayMember) || !filtered.Columns.Contains(_valueMember))
                        continue;
                    if (row[_valueMember] == DBNull.Value) continue;
                    _combo.Items.Add(row[_displayMember]?.ToString() ?? "");
                    _filteredIds.Add(Convert.ToInt64(row[_valueMember]));
                }

                _combo.SelectedIndex = -1;
                _combo.Text = filterText;
                _lastTypedText = filterText;
                try
                {
                    _combo.SelectionStart = Math.Min(caret, filterText.Length);
                    _combo.SelectionLength = 0;
                }
                catch { }

                _combo.DroppedDown = filtered.Rows.Count > 0;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void SetComboLongValue(long value)
        {
            if (value <= 0) return;
            try
            {
                _combo.SelectedValue = value;
                if (_combo.SelectedValue != null && _combo.SelectedValue != DBNull.Value
                    && Convert.ToInt64(_combo.SelectedValue) == value)
                    return;
            }
            catch { }

            if (_combo.DataSource is DataTable)
            {
                for (int i = 0; i < _combo.Items.Count; i++)
                {
                    if (!(_combo.Items[i] is DataRowView drv)) continue;
                    if (drv.Row.Table.Columns.Contains(_valueMember)
                        && drv[_valueMember] != DBNull.Value
                        && Convert.ToInt64(drv[_valueMember]) == value)
                    {
                        _combo.SelectedIndex = i;
                        _lastTypedText = drv[_displayMember]?.ToString() ?? "";
                        return;
                    }
                }
            }
        }

        public static DataTable FilterDataTable(DataTable source, string filter, string displayColumn)
        {
            if (source == null)
            {
                var empty = new DataTable();
                empty.Columns.Add(displayColumn, typeof(string));
                return empty;
            }

            filter = (filter ?? "").Trim();
            if (string.IsNullOrEmpty(filter))
                return source.Copy();

            var result = source.Clone();
            foreach (DataRow row in source.Rows)
            {
                if (!source.Columns.Contains(displayColumn)) continue;
                string display = row[displayColumn]?.ToString() ?? "";
                if (display.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.ImportRow(row);
            }
            return result;
        }

        protected void OnSelectionCommitted() => SelectionCommitted?.Invoke(_combo, EventArgs.Empty);
    }
}
