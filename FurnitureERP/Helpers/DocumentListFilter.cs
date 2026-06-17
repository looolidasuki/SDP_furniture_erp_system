namespace FurnitureERP.Helpers
{
    public class DocumentListFilter
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;

        public DocumentListFilter Clone()
        {
            return new DocumentListFilter
            {
                Keyword = Keyword,
                Status = Status,
                Page = Page,
                PageSize = PageSize
            };
        }
    }
}
