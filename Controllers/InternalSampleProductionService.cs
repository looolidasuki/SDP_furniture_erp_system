using System;
using MySql.Data.MySqlClient;
using Sales_user.Models;

namespace Sales_user.Controllers
{
    /// <summary>
    /// Internal sample / trial production (Case Study: design approval before catalog sales).
    /// Uses a system sales-order placeholder to satisfy FK without linking to a customer order.
    /// </summary>
    public static class InternalSampleProductionService
    {
        public const string InternalSalesOrderCode = "SO-INTERNAL-SAMPLE";
        public const string InternalCustomerName = "Internal (Sample Production)";
        public const string SampleRemarkPrefix = "[Sample]";

        public static bool IsInternalSampleSalesOrder(long salesOrderId)
        {
            if (salesOrderId <= 0) return false;
            object code = DatabaseConnect.ExecuteScalar(
                "SELECT salesOrderCode FROM SalesOrder WHERE salesOrderID = @id LIMIT 1",
                new[] { new MySqlParameter("@id", salesOrderId) });
            return code != null
                && code != DBNull.Value
                && string.Equals(code.ToString(), InternalSalesOrderCode, StringComparison.OrdinalIgnoreCase);
        }

        public static string EnsureSampleRemark(string remark)
        {
            if (string.IsNullOrWhiteSpace(remark))
                return SampleRemarkPrefix + " Trial production";
            string trimmed = remark.Trim();
            if (trimmed.StartsWith(SampleRemarkPrefix, StringComparison.OrdinalIgnoreCase))
                return trimmed;
            return SampleRemarkPrefix + " " + trimmed;
        }

        public static long GetOrCreateInternalSampleSalesOrderId(long staffId)
        {
            object existing = DatabaseConnect.ExecuteScalar(
                "SELECT salesOrderID FROM SalesOrder WHERE salesOrderCode = @code LIMIT 1",
                new[] { new MySqlParameter("@code", InternalSalesOrderCode) });
            if (existing != null && existing != DBNull.Value)
                return Convert.ToInt64(existing);

            long customerId = GetOrCreateInternalCustomer();
            long validStaffId = staffId > 0 ? staffId : 1;
            var salesOrderCtrl = new SalesOrderController();

            long salesOrderId = salesOrderCtrl.Insert(new SalesOrder
            {
                SalesOrderCode = "SO-TEMP",
                CustomerID = customerId,
                StaffID = validStaffId,
                CurrencyCurrencyID = 1,
                DeliveryAddress = "Internal sample production — not for customer delivery",
                Status = 5,
                Remark = "System placeholder for internal sample/trial production orders."
            });

            DatabaseConnect.ExecuteNonQuery(
                "UPDATE SalesOrder SET salesOrderCode = @code WHERE salesOrderID = @id",
                new[]
                {
                    new MySqlParameter("@code", InternalSalesOrderCode),
                    new MySqlParameter("@id", salesOrderId)
                });

            return salesOrderId;
        }

        private static long GetOrCreateInternalCustomer()
        {
            object existing = DatabaseConnect.ExecuteScalar(
                "SELECT customerID FROM Customer WHERE customerName = @name LIMIT 1",
                new[] { new MySqlParameter("@name", InternalCustomerName) });
            if (existing != null && existing != DBNull.Value)
                return Convert.ToInt64(existing);

            var customerCtrl = new CustomerController();
            long customerId = customerCtrl.Insert(new Customer
            {
                CustomerName = InternalCustomerName,
                BillingAddress = "Premium Living — internal R&D / sample production",
                PaymentTerm = "N/A"
            });
            customerCtrl.UpdateCodeAfterInsert(customerId);
            customerCtrl.UpdateRefNumberAfterInsert(customerId);
            return customerId;
        }
    }
}
