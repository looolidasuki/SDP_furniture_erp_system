using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Sales_user.Controllers
{
    public static class SearchQueryHelper
    {
        public static void AddLike(List<string> conditions, List<MySqlParameter> parameters, string column, string value, string paramName)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                conditions.Add($"{column} LIKE {paramName} ESCAPE '\\\\'");
                parameters.Add(new MySqlParameter(paramName, "%" + SqlGuard.EscapeLikeValue(value.Trim()) + "%"));
            }
        }

        public static void AddEquals(List<string> conditions, List<MySqlParameter> parameters, string column, object value, string paramName)
        {
            if (value != null && value.ToString() != "")
            {
                conditions.Add($"{column} = {paramName}");
                parameters.Add(new MySqlParameter(paramName, value));
            }
        }

        public static void AddDateFrom(List<string> conditions, List<MySqlParameter> parameters, string column, DateTime? fromDate)
        {
            if (fromDate.HasValue)
            {
                conditions.Add($"{column} >= @dateFrom_{column}");
                parameters.Add(new MySqlParameter($"@dateFrom_{column}", fromDate.Value.Date));
            }
        }

        public static void AddDateTo(List<string> conditions, List<MySqlParameter> parameters, string column, DateTime? toDate)
        {
            if (toDate.HasValue)
            {
                conditions.Add($"{column} <= @dateTo_{column}");
                parameters.Add(new MySqlParameter($"@dateTo_{column}", toDate.Value.Date.AddDays(1).AddSeconds(-1)));
            }
        }

        public static void AddStatus(List<string> conditions, List<MySqlParameter> parameters, string column, int? status)
        {
            if (status.HasValue && status.Value >= 0)
            {
                conditions.Add($"{column} = @filterStatus");
                parameters.Add(new MySqlParameter("@filterStatus", status.Value));
            }
        }
    }
}
