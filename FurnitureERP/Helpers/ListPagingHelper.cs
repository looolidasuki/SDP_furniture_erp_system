using System;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class ListPagingHelper
    {
        public static PagedDataTable Execute(string baseSql, string countSql, MySqlParameter[] parameters, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = SqlGuard.ClampLimit(pageSize, 500);
            int offset = (page - 1) * pageSize;

            object countObj = DatabaseConnect.ExecuteScalar(countSql, parameters);
            int total = countObj == null || countObj == DBNull.Value ? 0 : Convert.ToInt32(countObj);

            var pageParams = CloneParameters(parameters);
            Array.Resize(ref pageParams, pageParams.Length + 2);
            pageParams[pageParams.Length - 2] = new MySqlParameter("@pageSize", pageSize);
            pageParams[pageParams.Length - 1] = new MySqlParameter("@offset", offset);

            string dataSql = baseSql + " LIMIT @pageSize OFFSET @offset";
            DataTable rows = DatabaseConnect.ExecuteQuery(dataSql, pageParams);

            return new PagedDataTable
            {
                Rows = rows,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        private static MySqlParameter[] CloneParameters(MySqlParameter[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return new MySqlParameter[0];
            var clone = new MySqlParameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                clone[i] = (MySqlParameter)((ICloneable)parameters[i]).Clone();
            return clone;
        }
    }
}
