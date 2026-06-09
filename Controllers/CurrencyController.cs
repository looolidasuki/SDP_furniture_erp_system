using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class CurrencyController
    {
        public DataTable GetAllForCombo()
        {
            string sql = @"SELECT currencyID AS 'Currency ID',
                                  currencyCode AS 'Code',
                                  currencySymbol AS 'Symbol',
                                  rateToBase AS 'Rate To Base'
                           FROM Currency
                           ORDER BY currencyID";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public List<Currency> GetAll()
        {
            var dt = GetAllForCombo();
            var list = new List<Currency>();
            if (dt == null) return list;
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Currency
                {
                    CurrencyID = System.Convert.ToInt64(row["Currency ID"]),
                    CurrencyCode = row["Code"]?.ToString(),
                    CurrencySymbol = row["Symbol"]?.ToString(),
                    RateToBase = System.Convert.ToDecimal(row["Rate To Base"])
                });
            }
            return list;
        }

        public Currency GetById(long currencyId)
        {
            string sql = @"SELECT currencyID, currencyCode, currencySymbol, rateToBase
                           FROM Currency WHERE currencyID = @id";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", currencyId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Currency
            {
                CurrencyID = System.Convert.ToInt64(row["currencyID"]),
                CurrencyCode = row["currencyCode"]?.ToString(),
                CurrencySymbol = row["currencySymbol"]?.ToString(),
                RateToBase = System.Convert.ToDecimal(row["rateToBase"])
            };
        }

        public decimal GetRateToBase(long currencyId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT rateToBase FROM Currency WHERE currencyID = @id",
                new[] { new MySqlParameter("@id", currencyId) });
            if (value == null || value == System.DBNull.Value) return 1m;
            return System.Convert.ToDecimal(value);
        }
    }
}
