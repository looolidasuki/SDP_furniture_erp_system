using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    /// <summary>
    /// Filters a ComboBox as the user types. Selection is committed on DropDownClosed.
    /// Uses plain Items while typing to avoid WinForms DataSource auto-select behaviour.
    /// </summary>
    public sealed class FilteredComboBinder
    {
        public const int DefaultMinTypeAheadChars = 2;
        private const int MaxSuggestItems = 25;
        private const int FilterDebounceMs = 60;

        private readonly ComboBox _combo;
        private readonly string _valueMember;
        private readonly string _displayMember;
        private readonly Timer _filterTimer;
        private DataTable _fullSource;
        private readonly List<long> _filteredIds = new List<long>();
        private Func<string, DataTable> _serverSearch;
        private bool _suppressEvents;
        private bool _itemsMode;
        private string _lastTypedText = "";
        private long _pendingSelectId;
        private bool _freezeSuggestions;
        private int _bindGeneration;

        public int MinTypeAheadChars { get; set; } = DefaultMinTypeAheadChars;
        public bool AutoOpenOnType { get; set; } = true;

        public FilteredComboBinder(ComboBox combo, string valueMember, string displayMember)
        {
            _combo = combo ?? throw new ArgumentNullException(nameof(combo));
            _valueMember = valueMember;
            _displayMember = displayMember;
            _combo.DropDownStyle = ComboBoxStyle.DropDown;
            _combo.AutoCompleteMode = AutoCompleteMode.None;
            _combo.MaxDropDownItems = 16;
            _combo.TextUpdate += Combo_TextUpdate;
            _combo.DropDown += Combo_DropDown;
            _combo.DropDownClosed += Combo_DropDownClosed;
            _combo.KeyDown += Combo_KeyDown;
            _combo.Enter += Combo_Enter;
            _combo.MouseDown += Combo_MouseDown;
            _combo.Disposed += Combo_Disposed;
            _combo.HandleCreated += Combo_HandleCreated;

            _filterTimer = new Timer { Interval = FilterDebounceMs };
            _filterTimer.Tick += FilterTimer_Tick;
        }

        private void Combo_HandleCreated(object sender, EventArgs e)
        {
            ApplyPendingSelection();
        }

        private void Combo_Disposed(object sender, EventArgs e)
        {
            _filterTimer.Stop();
            _filterTimer.Dispose();
        }

        public void SetServerSearch(Func<string, DataTable> search)
        {
            _serverSearch = search;
        }

        private void Combo_Enter(object sender, EventArgs e)
        {
            if (_suppressEvents || !IsComboInteractive()) return;
            if (_pendingSelectId > 0 && string.IsNullOrWhiteSpace(SafeGetText()))
                ApplyPendingSelection();
            if (!string.IsNullOrWhiteSpace(SafeGetText()))
                return;
            if (_fullSource != null && _fullSource.Rows.Count > 0)
                PopulateBrowseItems(openDropdown: false);
        }

        private void Combo_MouseDown(object sender, MouseEventArgs e)
        {
            if (_suppressEvents || !IsComboInteractive() || e.Button != MouseButtons.Left) return;
            if (_combo.DroppedDown) return;
            GuardSelectedIndex();
            _combo.BeginInvoke(new Action(HandleMouseDownDeferred));
        }

        private void HandleMouseDownDeferred()
        {
            if (_suppressEvents || !IsComboInteractive() || _freezeSuggestions) return;

            string text = SafeGetText();
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < MinTypeAheadChars)
            {
                if (_fullSource != null && _fullSource.Rows.Count > 0)
                    PopulateBrowseItems(openDropdown: true);
                return;
            }

            if (HasDropDownContent() && string.Equals(text, _lastTypedText, StringComparison.Ordinal))
            {
                SafeResetSelectedIndex();
                if (!_combo.DroppedDown)
                    TryOpenDropDownDeferred(false);
                return;
            }

            ApplyFilterImmediately();
            SafeResetSelectedIndex();

            if (!HasDropDownContent() && _fullSource != null && _fullSource.Rows.Count > 0)
                PopulateBrowseItems(openDropdown: true);
            else if (HasDropDownContent() && !_combo.DroppedDown)
                TryOpenDropDownDeferred(false);
        }

        private void Combo_DropDown(object sender, EventArgs e)
        {
            if (_suppressEvents || !IsComboInteractive()) return;
            _freezeSuggestions = true;
            _filterTimer.Stop();
            GuardSelectedIndex();
            RestoreCaret(SafeGetText(), toEnd: true);
        }

        private void FilterTimer_Tick(object sender, EventArgs e)
        {
            _filterTimer.Stop();
            if (_freezeSuggestions || _combo.DroppedDown) return;
            ApplyFilterFromComboText();
        }

        private void ScheduleFilterApply()
        {
            if (!IsComboInteractive()) return;
            _filterTimer.Stop();
            _filterTimer.Start();
        }

        private void ApplyFilterImmediately()
        {
            _filterTimer.Stop();
            ApplyFilterFromComboText();
        }

        private void HandleBelowMinChars(string text)
        {
            _lastTypedText = text ?? "";

            if (_combo.DroppedDown)
                _combo.DroppedDown = false;

            // Do not Items.Clear() while typing — it resets caret to position 0 in WinForms.
            if (string.IsNullOrWhiteSpace(text) && _itemsMode && _combo.Items.Count > 0)
                ClearDropdownItems(keepText: true);

            RestoreCaret(text ?? "", toEnd: true);
        }

        private void RefreshSuggestionsForCurrentText()
        {
            if (!IsComboInteractive()) return;
            string text = SafeGetText();
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < MinTypeAheadChars)
            {
                HandleBelowMinChars(text);
                return;
            }

            BindFilteredItems(text, openDropdown: true);
        }

        private void TryOpenDropDownDeferred(bool wasOpen)
        {
            if (!IsComboInteractive() || !HasDropDownContent()) return;
            int generation = _bindGeneration;
            _combo.BeginInvoke(new Action(() =>
            {
                if (!IsComboInteractive() || !HasDropDownContent() || generation != _bindGeneration) return;
                if (_freezeSuggestions && _combo.DroppedDown) return;
                try
                {
                    SafeResetSelectedIndex();
                    string latest = SafeGetText();
                    if (!wasOpen || !_combo.DroppedDown)
                        _combo.DroppedDown = true;
                    RestoreCaret(latest, toEnd: true);
                    Cursor.Current = Cursors.IBeam;
                }
                catch { }
            }));
        }

        private void Combo_KeyDown(object sender, KeyEventArgs e)
        {
            if (!IsComboInteractive()) return;

            // Let WinForms handle arrow navigation when the list is already open.
            if (_combo.DroppedDown && (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up))
                return;

            if (e.KeyCode == Keys.Down)
            {
                string text = SafeGetText();
                if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < MinTypeAheadChars)
                {
                    if (_fullSource != null && _fullSource.Rows.Count > 0)
                        PopulateBrowseItems(openDropdown: true);
                }
                else
                {
                    ApplyFilterImmediately();
                    SafeResetSelectedIndex();
                    if (HasDropDownContent() && !_combo.DroppedDown)
                        TryOpenDropDownDeferred(false);
                }
                if (!_combo.DroppedDown)
                    e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && _combo.DroppedDown)
            {
                _combo.DroppedDown = false;
                e.Handled = true;
            }
        }

        private void Combo_DropDownClosed(object sender, EventArgs e)
        {
            if (_suppressEvents || !IsComboAlive()) return;

            _freezeSuggestions = false;
            SafeResetSelectedIndex();
            long id = GetSelectedId();
            if (id > 0)
            {
                _lastTypedText = SafeGetText();
                OnSelectionCommitted();
            }
        }

        private void Combo_TextUpdate(object sender, EventArgs e)
        {
            if (_suppressEvents || !IsComboAlive()) return;
            // Arrow-key navigation updates Text while DroppedDown; rebinding here desyncs SelectedIndex.
            if (_combo.DroppedDown) return;
            _lastTypedText = SafeGetText();
            ScheduleFilterApply();
        }

        private void ApplyFilterFromComboText()
        {
            if (_suppressEvents || !IsComboAlive()) return;
            if (_freezeSuggestions || _combo.DroppedDown) return;
            string text = SafeGetText();
            _lastTypedText = text;

            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < MinTypeAheadChars)
            {
                HandleBelowMinChars(text);
                return;
            }

            BindFilteredItems(text, openDropdown: AutoOpenOnType);
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
            _filterTimer.Stop();
            _fullSource = source?.Copy();
            _pendingSelectId = selectedId > 0 ? selectedId : 0;
            if (selectedId > 0)
                BindFullList(selectedId);
            else
                ClearComboDisplay();
        }

        public long GetSelectedId()
        {
            if (!IsComboAlive()) return 0;

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

            long fromText = ResolveIdFromDisplayText(_fullSource, SafeGetText());
            if (fromText > 0) return fromText;

            if (_combo.DataSource is DataTable bound)
            {
                fromText = ResolveIdFromDisplayText(bound, SafeGetText());
                if (fromText > 0) return fromText;
            }

            if (_pendingSelectId > 0)
            {
                string display = LookupDisplayText(_pendingSelectId);
                string text = SafeGetText();
                if (!string.IsNullOrWhiteSpace(text)
                    && string.Equals(text, display, StringComparison.OrdinalIgnoreCase))
                    return _pendingSelectId;
            }

            return 0;
        }

        public void SelectById(long id)
        {
            _pendingSelectId = id > 0 ? id : 0;
            if (id > 0)
                BindFullList(id);
            else
                ClearComboDisplay();
        }

        public void ClearSelection()
        {
            ClearComboDisplay();
        }

        /// <summary>Shows every row in the dropdown so the user can pick without typing.</summary>
        public void ShowFullList()
        {
            if (_fullSource != null && _fullSource.Rows.Count > 0)
                BindFullList(0);
        }

        private void ApplyPendingSelection()
        {
            if (_pendingSelectId <= 0 || _fullSource == null || !IsComboAlive()) return;
            if (!string.IsNullOrWhiteSpace(TryReadComboText())) return;
            BindFullList(_pendingSelectId);
        }

        private bool IsComboAlive()
        {
            return _combo != null && !_combo.IsDisposed;
        }

        private bool IsComboInteractive()
        {
            return IsComboAlive() && _combo.IsHandleCreated;
        }

        private string TryReadComboText()
        {
            if (!IsComboInteractive()) return _lastTypedText ?? "";
            try
            {
                return _combo.Text ?? "";
            }
            catch (Exception)
            {
                return _lastTypedText ?? "";
            }
        }

        private string SafeGetText()
        {
            return TryReadComboText();
        }

        private string LookupDisplayText(long id)
        {
            if (_fullSource == null || id <= 0) return null;
            if (!_fullSource.Columns.Contains(_valueMember) || !_fullSource.Columns.Contains(_displayMember))
                return null;

            foreach (DataRow row in _fullSource.Rows)
            {
                if (row[_valueMember] == DBNull.Value) continue;
                if (Convert.ToInt64(row[_valueMember]) == id)
                    return row[_displayMember]?.ToString();
            }
            return null;
        }

        private long ResolveIdFromDisplayText(DataTable source, string text)
        {
            if (source == null || string.IsNullOrWhiteSpace(text)) return 0;
            if (!source.Columns.Contains(_valueMember) || !source.Columns.Contains(_displayMember))
                return 0;

            foreach (DataRow row in source.Rows)
            {
                string display = row[_displayMember]?.ToString();
                if (string.Equals(display, text.Trim(), StringComparison.OrdinalIgnoreCase)
                    && row[_valueMember] != DBNull.Value)
                    return Convert.ToInt64(row[_valueMember]);
            }
            return 0;
        }

        private void EnsureDisplayTextForId(long id)
        {
            string display = LookupDisplayText(id);
            if (string.IsNullOrWhiteSpace(display)) return;

            _lastTypedText = display;
            if (!IsComboInteractive()) return;

            _suppressEvents = true;
            try
            {
                if (!string.Equals(_combo.Text, display, StringComparison.Ordinal))
                    _combo.Text = display;
                RestoreCaret(display, toEnd: true);
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void ClearComboDisplay()
        {
            if (!IsComboAlive()) return;
            _filterTimer.Stop();
            _pendingSelectId = 0;
            _suppressEvents = true;
            try
            {
                _itemsMode = false;
                _filteredIds.Clear();
                if (_combo.DroppedDown) _combo.DroppedDown = false;
                _combo.DataSource = null;
                _combo.Items.Clear();
                try { _combo.SelectedIndex = -1; } catch { }
                _combo.Text = "";
                _lastTypedText = "";
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void ClearDropdownItems(bool keepText = false)
        {
            if (!IsComboAlive()) return;
            _suppressEvents = true;
            try
            {
                _itemsMode = false;
                _filteredIds.Clear();
                if (_combo.DroppedDown) _combo.DroppedDown = false;
                DetachDataSource();
                if (!keepText)
                {
                    try { _combo.Text = ""; } catch { }
                    _lastTypedText = "";
                }
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void BindFullList(long selectId)
        {
            if (!IsComboAlive()) return;
            _filterTimer.Stop();
            _suppressEvents = true;
            try
            {
                _itemsMode = false;
                _filteredIds.Clear();
                if (IsComboInteractive())
                {
                    try
                    {
                        if (_combo.DroppedDown) _combo.DroppedDown = false;
                    }
                    catch { }
                    _combo.DataSource = null;
                    _combo.Items.Clear();
                    try { _combo.SelectedIndex = -1; } catch { }
                }

                DataTable source = _fullSource?.Copy();
                if (source == null)
                {
                    if (IsComboInteractive())
                    {
                        _combo.Text = "";
                    }
                    _lastTypedText = "";
                    return;
                }

                if (selectId > 0)
                    _pendingSelectId = selectId;

                if (!IsComboInteractive())
                {
                    EnsureDisplayTextForId(selectId);
                    return;
                }

                _combo.DataSource = source;
                _combo.DisplayMember = _displayMember;
                _combo.ValueMember = _valueMember;

                if (selectId > 0)
                {
                    SetComboLongValue(selectId);
                    EnsureDisplayTextForId(selectId);
                }
                else
                {
                    try { _combo.SelectedIndex = -1; } catch { }
                    try { _combo.Text = ""; } catch { }
                    _lastTypedText = "";
                }
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void BindFilteredItems(string filterText, bool openDropdown)
        {
            if (!IsComboInteractive() || _freezeSuggestions) return;
            _bindGeneration++;
            _suppressEvents = true;
            try
            {
                filterText = filterText ?? "";
                string workingText = filterText;
                if (workingText.Trim().Length < MinTypeAheadChars)
                {
                    HandleBelowMinChars(workingText);
                    return;
                }

                bool wasDroppedDown = _combo.DroppedDown;
                if (wasDroppedDown)
                    _combo.DroppedDown = false;

                DataTable filtered = ResolveFilteredTable(workingText.Trim());
                BindSuggestionDataSource(filtered, workingText);

                if (openDropdown && HasDropDownContent())
                    TryOpenDropDownDeferred(wasDroppedDown);
                else if (_combo.DroppedDown && !HasDropDownContent())
                    _combo.DroppedDown = false;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void RestoreCaret(string text, bool toEnd)
        {
            try
            {
                int pos = toEnd ? text.Length : Math.Min(_combo.SelectionStart, text.Length);
                _combo.SelectionStart = pos;
                _combo.SelectionLength = 0;
            }
            catch
            {
                try
                {
                    _combo.SelectionStart = text.Length;
                    _combo.SelectionLength = 0;
                }
                catch { }
            }
        }

        private DataTable ResolveFilteredTable(string filterText)
        {
            if (_serverSearch != null)
            {
                try
                {
                    var server = _serverSearch(filterText);
                    if (server != null && server.Rows.Count > 0)
                        return server;
                }
                catch { }
            }

            return FilterDataTable(_fullSource, filterText, _displayMember);
        }

        private void SetComboLongValue(long value)
        {
            if (value <= 0 || !IsComboAlive()) return;
            if (!IsComboInteractive())
            {
                EnsureDisplayTextForId(value);
                return;
            }
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
                        EnsureDisplayTextForId(value);
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
                return source.Clone();

            var result = source.Clone();
            int count = 0;
            foreach (DataRow row in source.Rows)
            {
                if (count >= MaxSuggestItems) break;
                if (!source.Columns.Contains(displayColumn)) continue;
                string display = row[displayColumn]?.ToString() ?? "";
                if (display.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.ImportRow(row);
                    count++;
                }
            }
            return result;
        }

        private void PopulateBrowseItems(bool openDropdown)
        {
            if (!IsComboInteractive() || _fullSource == null || _fullSource.Rows.Count == 0 || _freezeSuggestions) return;

            _bindGeneration++;

            _suppressEvents = true;
            try
            {
                string preserveText = SafeGetText();
                bool wasDroppedDown = _combo.DroppedDown;
                if (wasDroppedDown)
                    _combo.DroppedDown = false;

                var browse = _fullSource.Clone();
                while (browse.Rows.Count > MaxSuggestItems)
                    browse.Rows.RemoveAt(browse.Rows.Count - 1);

                BindSuggestionDataSource(browse, preserveText);

                if (openDropdown && HasDropDownContent() && !_combo.DroppedDown)
                    TryOpenDropDownDeferred(wasDroppedDown);
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void BindSuggestionDataSource(DataTable source, string workingText)
        {
            // Use Items while typing — DataSource rebinding during dropdown causes SelectedIndex desync.
            _itemsMode = true;
            _filteredIds.Clear();
            DetachDataSource();

            workingText = workingText ?? "";
            _lastTypedText = workingText;

            if (source == null || source.Rows.Count == 0)
            {
                try
                {
                    if (!string.Equals(_combo.Text, workingText, StringComparison.Ordinal))
                        _combo.Text = workingText;
                }
                catch { }
                RestoreCaret(workingText, toEnd: true);
                return;
            }

            if (!source.Columns.Contains(_valueMember) || !source.Columns.Contains(_displayMember))
                return;

            var seenIds = new HashSet<long>();
            var seenDisplay = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in source.Rows)
            {
                long id = row[_valueMember] == DBNull.Value ? 0 : Convert.ToInt64(row[_valueMember]);
                if (id > 0 && !seenIds.Add(id)) continue;

                string display = row[_displayMember]?.ToString() ?? "";
                if (!seenDisplay.Add(display)) continue;

                _filteredIds.Add(id);
                _combo.Items.Add(display);
            }

            GuardSelectedIndex();
            try
            {
                if (!string.Equals(_combo.Text, workingText, StringComparison.Ordinal))
                    _combo.Text = workingText;
            }
            catch { }
            RestoreCaret(workingText, toEnd: true);
        }

        private void DetachDataSource()
        {
            if (!IsComboInteractive()) return;
            string preserveText = SafeGetText();
            _combo.DataSource = null;
            _combo.Items.Clear();
            try { _combo.SelectedIndex = -1; } catch { }
            try
            {
                if (!string.Equals(_combo.Text, preserveText, StringComparison.Ordinal))
                    _combo.Text = preserveText;
            }
            catch { }
        }

        private bool HasDropDownContent()
        {
            if (_itemsMode)
                return _combo.Items.Count > 0;
            if (_combo.Items.Count > 0) return true;
            return _combo.DataSource is DataTable dt && dt.Rows.Count > 0;
        }

        /// <summary>
        /// WinForms ComboBox can keep SelectedIndex while Items.Count is smaller after rebind; guard before dropdown reads SelectedItem.
        /// </summary>
        private void GuardSelectedIndex()
        {
            ComboSelectionGuard.Clamp(_combo);
        }

        private void SafeResetSelectedIndex()
        {
            GuardSelectedIndex();
        }

        private void OnSelectionCommitted() => SelectionCommitted?.Invoke(_combo, EventArgs.Empty);
    }
}
