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
            _combo.Enter += Combo_Enter;
            _combo.MouseDown += Combo_MouseDown;
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
            BindFilteredItems(SafeGetText());
        }

        public string GetText()
        {
            return SafeGetText().Trim();
        }

        public void Clear()
        {
            _suppressEvents = true;
            try
            {
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

        private void Combo_Enter(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            OpenDropdownForCurrentText();
        }

        private void Combo_MouseDown(object sender, MouseEventArgs e)
        {
            if (_suppressEvents || e.Button != MouseButtons.Left) return;
            if (!_combo.DroppedDown)
                OpenDropdownForCurrentText();
        }

        private void OpenDropdownForCurrentText()
        {
            BindFilteredItems(SafeGetText());
            if (_combo.Items.Count > 0 && !_combo.DroppedDown)
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

        private void Combo_TextUpdate(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            _combo.BeginInvoke(new Action(ApplyFilterFromComboText));
        }

        private void ApplyFilterFromComboText()
        {
            if (_suppressEvents || _combo.IsDisposed) return;
            BindFilteredItems(SafeGetText());
        }

        private void BindFilteredItems(string filterText)
        {
            _suppressEvents = true;
            try
            {
                filterText = filterText ?? "";
                int caret = 0;
                try { caret = _combo.SelectionStart; } catch { caret = filterText.Length; }

                IEnumerable<string> matches = GetMatches(filterText);
                _combo.DataSource = null;
                _combo.Items.Clear();
                foreach (string item in matches)
                    _combo.Items.Add(item);

                _combo.SelectedIndex = -1;
                _combo.Text = filterText;
                _lastTypedText = filterText;
                try
                {
                    _combo.SelectionStart = Math.Min(caret, filterText.Length);
                    _combo.SelectionLength = 0;
                }
                catch { }

                _combo.DroppedDown = _combo.Items.Count > 0;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private IEnumerable<string> GetMatches(string filterText)
        {
            filterText = (filterText ?? "").Trim();
            if (_serverSuggest != null)
            {
                try
                {
                    var server = _serverSuggest(filterText)?
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Select(v => v.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(25)
                        .ToList();
                    if (server != null && server.Count > 0)
                        return server;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(filterText))
                return _fullSource.Take(25);

            return _fullSource
                .Where(v => v.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(25);
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
    }
}
