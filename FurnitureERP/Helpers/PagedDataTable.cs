using System;
using System.Data;

namespace FurnitureERP.Helpers
{
    public class PagedDataTable
    {
        public DataTable Rows { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => PageSize <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    }
}
