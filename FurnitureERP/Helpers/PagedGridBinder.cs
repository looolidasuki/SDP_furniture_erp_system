using System;
using System.Drawing;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public sealed class PagedGridBinder
    {
        private readonly DataGridView _grid;
        private readonly Func<DocumentListFilter, PagedDataTable> _loader;
        private readonly Action _afterBind;
        private readonly Panel _footer;
        private readonly Label _lblInfo;
        private readonly Button _btnPrev;
        private readonly Button _btnNext;
        private DocumentListFilter _filter = new DocumentListFilter();
        private PagedDataTable _current;

        public DocumentListFilter CurrentFilter => _filter;

        public PagedGridBinder(DataGridView grid, Control host, Func<DocumentListFilter, PagedDataTable> loader, Action afterBind = null)
        {
            _grid = grid;
            _loader = loader;
            _afterBind = afterBind;

            _footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(248, 250, 254),
                Padding = new Padding(8, 4, 8, 4)
            };
            _lblInfo = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UITheme.TextGray,
                Font = new Font("Segoe UI", 8.5f)
            };
            _btnNext = UITheme.CreateSecondaryButton("Next ▶");
            _btnNext.Width = 72;
            _btnNext.Height = 26;
            _btnNext.Dock = DockStyle.Right;
            _btnPrev = UITheme.CreateSecondaryButton("◀ Prev");
            _btnPrev.Width = 72;
            _btnPrev.Height = 26;
            _btnPrev.Dock = DockStyle.Right;

            _btnPrev.Click += (s, e) => LoadPage(_filter.Page - 1);
            _btnNext.Click += (s, e) => LoadPage(_filter.Page + 1);

            _footer.Controls.Add(_lblInfo);
            _footer.Controls.Add(_btnNext);
            _footer.Controls.Add(_btnPrev);
            host.Controls.Add(_footer);
            _grid.Tag = this;
        }

        public static PagedGridBinder TryGet(DataGridView grid)
        {
            return grid?.Tag as PagedGridBinder;
        }

        public void ApplyFilter(DocumentListFilter filter)
        {
            _filter = filter?.Clone() ?? new DocumentListFilter();
            _filter.Page = Math.Max(1, _filter.Page);
            LoadPage(1);
        }

        public void Reload() => LoadPage(_filter.Page);

        public void LoadPage(int page)
        {
            try
            {
                _filter.Page = Math.Max(1, page);
                _current = _loader(_filter);
                _grid.DataSource = _current?.Rows;
                _afterBind?.Invoke();
                UpdateFooter();
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Failed to load page: " + ex.Message);
            }
        }

        private void UpdateFooter()
        {
            if (_current == null)
            {
                _lblInfo.Text = "No data";
                _btnPrev.Enabled = false;
                _btnNext.Enabled = false;
                return;
            }

            _lblInfo.Text = $"Page {_current.Page} of {_current.TotalPages}  |  {_current.TotalCount} record(s)  |  {_current.PageSize} per page";
            _btnPrev.Enabled = _current.Page > 1;
            _btnNext.Enabled = _current.Page < _current.TotalPages;
        }
    }
}
