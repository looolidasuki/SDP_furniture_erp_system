using FurnitureERP.Helpers;
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
                           WHERE COALESCE(isEnabled, 1) = 1
                           ORDER BY currencyID";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetAllForAdmin()
        {
            string sql = @"SELECT currencyID AS 'Currency ID',
                                  currencyCode AS 'Code',
                                  currencySymbol AS 'Symbol',
                                  rateToBase AS 'Rate To Base',
                                  isBaseCurrency AS 'Base',
                                  decimalPlaces AS 'Decimals',
                                  isEnabled AS 'Enabled'
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
                list.Add(MapCurrencyRow(row, useAdminAliases: false));
            }
            return list;
        }

        public Currency GetById(long currencyId)
        {
            string sql = @"SELECT currencyID, currencyCode, currencySymbol, rateToBase,
                                  isBaseCurrency, decimalPlaces, isEnabled
                           FROM Currency WHERE currencyID = @id";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", currencyId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            return MapCurrencyRow(dt.Rows[0], useAdminAliases: false);
        }

        public decimal GetRateToBase(long currencyId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT rateToBase FROM Currency WHERE currencyID = @id",
                new[] { new MySqlParameter("@id", currencyId) });
            if (value == null || value == System.DBNull.Value) return 1m;
            return System.Convert.ToDecimal(value);
        }

        public decimal LockRateForCurrency(long currencyId)
        {
            return CurrencyConversionService.LockRate(GetRateToBase(currencyId));
        }

        public bool UpdateRate(long currencyId, decimal newRate)
        {
            if (currencyId <= 0 || newRate <= 0m) return false;
            var currency = GetById(currencyId);
            if (currency == null) return false;
            if (currency.IsBaseCurrency) return false;

            return DatabaseConnect.ExecuteNonQuery(
                "UPDATE Currency SET rateToBase = @rate WHERE currencyID = @id",
                new[]
                {
                    new MySqlParameter("@rate", CurrencyConversionService.LockRate(newRate)),
                    new MySqlParameter("@id", currencyId)
                }) > 0;
        }

        public bool SetEnabled(long currencyId, bool enabled)
        {
            var currency = GetById(currencyId);
            if (currency == null || currency.IsBaseCurrency) return false;

            return DatabaseConnect.ExecuteNonQuery(
                "UPDATE Currency SET isEnabled = @enabled WHERE currencyID = @id",
                new[]
                {
                    new MySqlParameter("@enabled", enabled ? 1 : 0),
                    new MySqlParameter("@id", currencyId)
                }) > 0;
        }

        private static Currency MapCurrencyRow(DataRow row, bool useAdminAliases)
        {
            string idCol = useAdminAliases ? "Currency ID" : (row.Table.Columns.Contains("currencyID") ? "currencyID" : "Currency ID");
            string codeCol = useAdminAliases ? "Code" : (row.Table.Columns.Contains("currencyCode") ? "currencyCode" : "Code");
            string symCol = useAdminAliases ? "Symbol" : (row.Table.Columns.Contains("currencySymbol") ? "currencySymbol" : "Symbol");
            string rateCol = useAdminAliases ? "Rate To Base" : (row.Table.Columns.Contains("rateToBase") ? "rateToBase" : "Rate To Base");

            var currency = new Currency
            {
                CurrencyID = System.Convert.ToInt64(row[idCol]),
                CurrencyCode = row[codeCol]?.ToString(),
                CurrencySymbol = row[symCol]?.ToString(),
                RateToBase = System.Convert.ToDecimal(row[rateCol])
            };

            if (row.Table.Columns.Contains("isBaseCurrency"))
                currency.IsBaseCurrency = System.Convert.ToInt32(row["isBaseCurrency"]) == 1;
            else if (row.Table.Columns.Contains("Base"))
                currency.IsBaseCurrency = System.Convert.ToInt32(row["Base"]) == 1;

            if (row.Table.Columns.Contains("decimalPlaces"))
                currency.DecimalPlaces = System.Convert.ToInt32(row["decimalPlaces"]);
            else if (row.Table.Columns.Contains("Decimals"))
                currency.DecimalPlaces = System.Convert.ToInt32(row["Decimals"]);

            if (row.Table.Columns.Contains("isEnabled"))
                currency.IsEnabled = System.Convert.ToInt32(row["isEnabled"]) == 1;
            else if (row.Table.Columns.Contains("Enabled"))
                currency.IsEnabled = System.Convert.ToInt32(row["Enabled"]) == 1;

            return currency;
        }
    }
}
