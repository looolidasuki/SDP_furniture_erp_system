using System;

using System.Collections.Generic;



namespace FurnitureERP.Helpers

{

    public class DocumentFilterCondition

    {

        public string Column { get; set; }

        public string Operator { get; set; }

        public string TextValue { get; set; }

        public decimal? NumericValue { get; set; }

        public decimal? NumericValueTo { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        public int? StatusCode { get; set; }

    }



    public class DocumentListFilter

    {

        public string Keyword { get; set; }

        public int? Status { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 100;

        public List<DocumentFilterCondition> Conditions { get; set; } = new List<DocumentFilterCondition>();



        public DocumentListFilter Clone()

        {

            return new DocumentListFilter

            {

                Keyword = Keyword,

                Status = Status,

                Page = Page,

                PageSize = PageSize,

                Conditions = Conditions != null

                    ? new List<DocumentFilterCondition>(Conditions)

                    : new List<DocumentFilterCondition>()

            };

        }

    }

}

