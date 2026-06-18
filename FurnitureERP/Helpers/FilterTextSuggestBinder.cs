using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    /// <summary>
    /// Type-to-filter ComboBox for filter value fields (plain text suggestions).
    /// </summary>
    public sealed class FilterTextSuggestBinder
    {
        public const int MinTypeAheadChars = 1;
        private const int MaxSuggestItems = 25;
        private readonly ComboBox _combo;
        private List<string> _fullSource = new List<string>();
        private Func<string, IEnumerable<string>> _serverSuggest;
        private bool _suppressEvents;
        private string _lastTypedText = "";

        public FilterTextSuggestBinder(ComboBox combo)
        {
            _combo = combo ?? throw new ArgumentNullException(nameof(combo));
            _combo.DropDownStyle = ComboBoxStyle.DropDown;
            _combo.AutoCompleteMode = AutoCompleteMode.None;
            _combo.MaxDropDownItems = 20;
            _combo.Font = new Font("Segoe UI", 9f);
            _combo.TextUpdate += Combo_TextUpdate;
            _combo.KeyDown += Combo_KeyDown;
        }

        public void SetServerSuggest(Func<string, IEnumerable<string>> provider)
        {
            _serverSuggest = provider;
        }

        public void SetLocalSource(IEnumerable<string> values)
        {
            _fullSource = values?
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
            if (!string.IsNullOrWhiteSpace(SafeGetText()))
                BindFilteredItems(SafeGetText(), restoreCaretToEnd: true);
        }

        public string GetText()
        {
            return SafeGetText().Trim();
        }

        public void Clear()
        {
            if (_combo.IsDisposed) return;
            _suppressEvents = true;
            try
            {
                if (_combo.DroppedDown) _combo.DroppedDown = false;
                _combo.DataSource = null;
                _combo.Items.Clear();
                _combo.Text = "";
                _lastTypedText = "";
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void Combo_KeyDown(object sender, KeyEventArgs e)
        {
            if (_combo.IsDisposed) return;
            if (e.KeyCode == Keys.Escape && _combo.DroppedDown)
            {
                _combo.DroppedDown = false;
                e.Handled = true;
            }
        }

        private void Combo_TextUpdate(object sender, EventArgs e)
        {
            if (_suppressEvents || _combo.IsDisposed) return;
            _combo.BeginInvoke(new Action(ApplyFilterFromComboText));
        }

        private void ApplyFilterFromComboText()
        {
            if (_suppressEvents || _combo.IsDisposed) return;
            string text = SafeGetText();
            _lastTypedText = text;

            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < MinTypeAheadChars)
            {
                ClearDropdownItems(keepText: true);
                return;
            }

            BindFilteredItems(text, restoreCaretToEnd: true);
        }

        private void ClearDropdownItems(bool keepText = false)
        {
            if (_combo.IsDisposed) return;
            _suppressEvents = true;
            try
            {
                if (_combo.DroppedDown) _combo.DroppedDown = false;
                _combo.DataSource = null;
                _combo.Items.Clear();
                if (!keepText)
                {
                    _combo.Text = "";
                    _lastTypedText = "";
                }
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void BindFilteredItems(string filterText, bool restoreCaretToEnd)
        {
            if (_combo.IsDisposed) return;
            _suppressEvents = true;
            try
            {
                filterText = filterText ?? "";
                string workingText = filterText;

                IEnumerable<string> matches = GetMatches(workingText);
                bool wasDroppedDown = _combo.DroppedDown;

                _combo.DataSource = null;
                _combo.Items.Clear();
                foreach (string item in matches)
                    _combo.Items.Add(item);

                _combo.SelectedIndex = -1;

                // Avoid resetting Text when unchanged — reduces caret jumps during TextUpdate.
                if (!string.Equals(_combo.Text, workingText, StringComparison.Ordinal))
                    _combo.Text = workingText;

                _lastTypedText = workingText;
                RestoreCaret(workingText, restoreCaretToEnd);

                if (_combo.Items.Count > 0)
                    OpenDropDownDeferred(workingText, wasDroppedDown);
                else if (_combo.DroppedDown)
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

        private void OpenDropDownDeferred(string text, bool wasOpen)
        {
            _combo.BeginInvoke(new Action(() =>
            {
                if (_combo.IsDisposed || _combo.Items.Count == 0) return;
                try
                {
                    if (!wasOpen || !_combo.DroppedDown)
                        _combo.DroppedDown = true;
                    RestoreCaret(text, toEnd: true);
                    Cursor.Current = Cursors.IBeam;
                }
                catch { }
            }));
        }

        private IEnumerable<string> GetMatches(string filterText)
        {
            string trimmed = (filterText ?? "").Trim();
            if (trimmed.Length < MinTypeAheadChars)
                return Array.Empty<string>();

            if (_serverSuggest != null)
            {
                try
                {
                    var server = _serverSuggest(trimmed)?
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Select(v => v.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(MaxSuggestItems)
                        .ToList();
                    if (server != null && server.Count > 0)
                        return server;
                }
                catch { }
            }

            string matchNeedle = filterText ?? "";
            if (string.IsNullOrEmpty(matchNeedle))
                return _fullSource.Take(MaxSuggestItems);

            return _fullSource
                .Where(v => v.IndexOf(matchNeedle, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(MaxSuggestItems);
        }

        private string SafeGetText()
        {
            if (_combo.IsDisposed) return _lastTypedText ?? "";
            try
            {
                return _combo.Text ?? "";
            }
            catch (ArgumentOutOfRangeException)
            {
                return _lastTypedText ?? "";
            }
        }
    }
}
