using MySql.Data.MySqlClient;

using Sales_user.Models;

using System;

using System.Collections.Generic;

using System.Data;

using FurnitureERP.Helpers;



namespace Sales_user.Controllers

{

    public class ReceiptVoucherController

    {

        private static readonly string[] StatusLabels = { "Draft", "Confirmed", "Cancelled" };



        public DataTable GetAllReceiptVouchers()

        {

            string sql = @"SELECT rv.receiptVoucherID AS 'ID',

                                  rv.receiptVoucherCode AS 'Voucher Code',

                                  CONCAT('CU-', LPAD(c.customerID, 9, '0')) AS 'Customer Code',

                                  c.customerName AS 'Customer',

                                  CONCAT(COALESCE(st.firstName, ''), ' ', COALESCE(st.lastName, '')) AS 'Staff',

                                  rv.paymentAmount AS 'Amount',

                                  rv.paymentMethod AS 'Method',

                                  rv.paymentMethodRef AS 'Reference',

                                  rv.paymentReceivedDate AS 'Received Date',

                                  rv.status AS 'Status',

                                  rv.createDate AS 'Date'

                           FROM receiptvoucher rv

                           LEFT JOIN Customer c ON rv.cusomerID = c.customerID

                           LEFT JOIN Staff st ON rv.staffID = st.staffID

                           ORDER BY rv.createDate DESC";

            var dt = DatabaseConnect.ExecuteQuery(sql);

            if (dt != null && !dt.Columns.Contains("Status Label"))

            {

                dt.Columns.Add("Status Label", typeof(string));

                foreach (DataRow row in dt.Rows)

                {

                    int status = Convert.ToInt32(row["Status"]);

                    row["Status Label"] = status >= 0 && status < StatusLabels.Length ? StatusLabels[status] : status.ToString();

                }

            }

            return dt;

        }

        public DataTable GetByCustomer(long customerId)
        {
            string sql = @"SELECT rv.receiptVoucherID AS 'ID',
                                  rv.receiptVoucherCode AS 'Voucher Code',
                                  CONCAT(COALESCE(st.firstName, ''), ' ', COALESCE(st.lastName, '')) AS 'Staff',
                                  rv.paymentAmount AS 'Amount',
                                  rv.paymentMethod AS 'Method',
                                  rv.paymentMethodRef AS 'Reference',
                                  rv.paymentReceivedDate AS 'Received Date',
                                  rv.status AS 'Status',
                                  rv.createDate AS 'Date'
                           FROM receiptvoucher rv
                           LEFT JOIN Staff st ON rv.staffID = st.staffID
                           WHERE rv.cusomerID = @customerId
                           ORDER BY rv.createDate DESC";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@customerId", customerId) });
            if (dt != null && !dt.Columns.Contains("Status Label"))
            {
                dt.Columns.Add("Status Label", typeof(string));
                foreach (DataRow row in dt.Rows)
                {
                    int status = Convert.ToInt32(row["Status"]);
                    row["Status Label"] = status >= 0 && status < StatusLabels.Length ? StatusLabels[status] : status.ToString();
                }
            }
            return dt;
        }



        public long Insert(ReceiptVoucher rv)

        {

            string sql = @"INSERT INTO receiptvoucher

                (receiptVoucherID, receiptVoucherCode, cusomerID, staffID, paymentAmount, paymentMethod, paymentMethodRef,

                 remark, status, currencyID, paymentReceivedDate, createDate)

                VALUES (@id, @code, @cusomerID, @staffID, @amount, @method, @ref, @remark, @status, @currencyID, @receivedDate, NOW())";



            long id = DatabaseConnect.InsertWithAllocatedId("receiptvoucher", "receiptVoucherID", sql, new[] {

                new MySqlParameter("@code", string.IsNullOrWhiteSpace(rv.ReceiptVoucherCode) ? "RV-TEMP" : rv.ReceiptVoucherCode.Trim()),

                new MySqlParameter("@cusomerID", rv.CusomerID),

                new MySqlParameter("@staffID", rv.StaffID),

                new MySqlParameter("@amount", rv.PaymentAmount),

                new MySqlParameter("@method", ResolvePaymentMethod(rv)),

                new MySqlParameter("@ref", rv.PaymentMethodRef ?? string.Empty),

                new MySqlParameter("@remark", rv.Remark ?? (object)DBNull.Value),

                new MySqlParameter("@status", rv.Status),

                new MySqlParameter("@currencyID", rv.CurrencyID == 0 ? 1 : rv.CurrencyID),

                new MySqlParameter("@receivedDate", rv.PaymentReceivedDate == default ? DateTime.Today : rv.PaymentReceivedDate)

            });

            if (id > 0)
                UpdateCodeAfterInsert(id);

            return id;

        }

        public void UpdateCodeAfterInsert(long receiptVoucherId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE receiptvoucher SET receiptVoucherCode = @code WHERE receiptVoucherID = @id",
                new[]
                {
                    new MySqlParameter("@code", DocumentCodeHelper.FormatReceiptVoucherCode(receiptVoucherId)),
                    new MySqlParameter("@id", receiptVoucherId)
                });
        }



        public bool Update(ReceiptVoucher rv)

        {

            string sql = @"UPDATE receiptvoucher

                           SET receiptVoucherCode=@code, cusomerID=@cusomerID, staffID=@staffID, paymentAmount=@amount,

                               paymentMethod=@method, paymentMethodRef=@ref, remark=@remark,

                               status=@status, paymentReceivedDate=@receivedDate, lastModifyDate=NOW()

                           WHERE receiptVoucherID=@id";

            return DatabaseConnect.ExecuteNonQuery(sql, new[] {

                new MySqlParameter("@code", (rv.ReceiptVoucherCode ?? "").Trim()),

                new MySqlParameter("@cusomerID", rv.CusomerID),

                new MySqlParameter("@staffID", rv.StaffID),

                new MySqlParameter("@amount", rv.PaymentAmount),

                new MySqlParameter("@method", ResolvePaymentMethod(rv)),

                new MySqlParameter("@ref", rv.PaymentMethodRef ?? string.Empty),

                new MySqlParameter("@remark", rv.Remark ?? (object)DBNull.Value),

                new MySqlParameter("@status", rv.Status),

                new MySqlParameter("@receivedDate", rv.PaymentReceivedDate == default ? DateTime.Today : rv.PaymentReceivedDate),

                new MySqlParameter("@id", rv.ReceiptVoucherID)

            }) > 0;

        }



        private static string ResolvePaymentMethod(ReceiptVoucher rv)

        {

            if (!string.IsNullOrWhiteSpace(rv.PaymentMethodName)) return rv.PaymentMethodName.Trim();

            return "Cash";

        }



        public DataTable GetReceiptVouchersForPicker()

        {

            string sql = @"SELECT receiptVoucherID AS 'Receipt Voucher ID',

                                  receiptVoucherCode AS 'Voucher Code',

                                  paymentAmount AS 'Amount',

                                  paymentMethodRef AS 'Reference',

                                  status AS 'Status'

                           FROM receiptvoucher

                           ORDER BY createDate DESC";

            var dt = DatabaseConnect.ExecuteQuery(sql);

            if (dt != null && !dt.Columns.Contains("DisplayText"))

            {

                dt.Columns.Add("DisplayText", typeof(string));

                foreach (DataRow row in dt.Rows)

                {

                    string code = row["Voucher Code"]?.ToString();

                    string reference = row["Reference"]?.ToString();

                    row["DisplayText"] = string.IsNullOrWhiteSpace(reference) ? code : $"{code} ({reference})";

                }

            }

            return dt;

        }



        public ReceiptVoucher GetByCode(string voucherCode)

        {

            if (string.IsNullOrWhiteSpace(voucherCode)) return null;

            string sql = @"SELECT receiptVoucherID FROM receiptvoucher WHERE receiptVoucherCode = @code LIMIT 1";

            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@code", voucherCode.Trim()) });

            if (dt.Rows.Count == 0) return null;

            return GetById(Convert.ToInt64(dt.Rows[0]["receiptVoucherID"]));

        }

        public bool ExistsByCode(string voucherCode, long excludeId = 0)
        {
            if (string.IsNullOrWhiteSpace(voucherCode)) return false;
            string sql = @"SELECT COUNT(*) FROM receiptvoucher WHERE receiptVoucherCode = @code";
            if (excludeId > 0) sql += " AND receiptVoucherID <> @excludeId";
            var prms = new System.Collections.Generic.List<MySqlParameter> { new MySqlParameter("@code", voucherCode.Trim()) };
            if (excludeId > 0) prms.Add(new MySqlParameter("@excludeId", excludeId));
            object value = DatabaseConnect.ExecuteScalar(sql, prms.ToArray());
            return value != null && value != DBNull.Value && Convert.ToInt32(value) > 0;
        }



        public ReceiptVoucher GetById(long id)

        {

            string sql = @"SELECT receiptVoucherID, receiptVoucherCode, cusomerID, staffID,

                                  paymentAmount, paymentMethod, paymentMethodRef, remark, status, currencyID,

                                  paymentReceivedDate

                           FROM receiptvoucher

                           WHERE receiptVoucherID = @id";



            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });

            if (dt.Rows.Count == 0) return null;

            return MapReceiptVoucherRow(dt.Rows[0]);

        }



        private static ReceiptVoucher MapReceiptVoucherRow(DataRow row)

        {

            return new ReceiptVoucher

            {

                ReceiptVoucherID = Convert.ToInt64(row["receiptVoucherID"]),

                ReceiptVoucherCode = row["receiptVoucherCode"].ToString(),

                CusomerID = Convert.ToInt64(row["cusomerID"]),

                StaffID = Convert.ToInt64(row["staffID"]),

                PaymentAmount = Convert.ToDecimal(row["paymentAmount"]),

                PaymentMethodName = row["paymentMethod"]?.ToString(),

                PaymentMethodRef = row["paymentMethodRef"].ToString(),

                Remark = row["remark"] == DBNull.Value ? null : row["remark"].ToString(),

                Status = Convert.ToInt32(row["status"]),

                CurrencyID = Convert.ToInt64(row["currencyID"]),

                PaymentReceivedDate = row["paymentReceivedDate"] == DBNull.Value

                    ? DateTime.Today : Convert.ToDateTime(row["paymentReceivedDate"])

            };

        }



        public DataTable GetHeaderDetail(long receiptVoucherId)

        {

            string sql = @"SELECT rv.receiptVoucherCode AS 'Voucher Code',

                                  CONCAT('CU-', LPAD(c.customerID, 9, '0')) AS 'Customer Code',

                                  c.customerName AS 'Customer',

                                  (SELECT cp.contactPerson FROM contactperson cp
                                    WHERE cp.customerID = c.customerID
                                    ORDER BY cp.contactPersonID LIMIT 1) AS 'Contact Person',

                                  (SELECT cp.phone FROM contactperson cp
                                    WHERE cp.customerID = c.customerID
                                    ORDER BY cp.contactPersonID LIMIT 1) AS 'Phone Number',

                                  c.billingAddress AS 'Address',

                                  CONCAT(COALESCE(st.firstName, ''), ' ', COALESCE(st.lastName, '')) AS 'Staff',

                                  rv.paymentAmount AS 'Amount',

                                  rv.paymentMethod AS 'Payment Method',

                                  rv.paymentMethodRef AS 'Method Reference',

                                  rv.paymentReceivedDate AS 'Payment Received Date',

                                  CASE rv.status
                                      WHEN 0 THEN 'Draft'
                                      WHEN 1 THEN 'Confirmed'
                                      WHEN 2 THEN 'Cancelled'
                                      ELSE CAST(rv.status AS CHAR)
                                  END AS 'Status',

                                  rv.createDate AS 'Create Date',

                                  rv.lastModifyDate AS 'Last Modified',

                                  rv.remark AS 'Remark'

                           FROM receiptvoucher rv

                           LEFT JOIN Customer c ON rv.cusomerID = c.customerID

                           LEFT JOIN Staff st ON rv.staffID = st.staffID

                           WHERE rv.receiptVoucherID = @id";

            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", receiptVoucherId) });

        }



        public DataTable GetIncomeTrend()

        {

            string sql = @"SELECT DATE_FORMAT(createDate, '%Y-%m') AS 'Month',

                          SUM(paymentAmount) AS 'Total'

                   FROM receiptvoucher

                   WHERE status != 2

                   GROUP BY DATE_FORMAT(createDate, '%Y-%m')

                   ORDER BY 'Month' ASC

                   LIMIT 6";

            return DatabaseConnect.ExecuteQuery(sql);

        }



        public DataTable GetIncomeByMethod()

        {

            string sql = @"SELECT paymentMethod AS 'Method',

                          SUM(paymentAmount) AS 'Total'

                   FROM receiptvoucher

                   WHERE status != 2

                   GROUP BY paymentMethod";

            return DatabaseConnect.ExecuteQuery(sql);

        }



        public DataTable GetInvoiceAllocations(long receiptVoucherId)

        {

            return GetInvoiceAllocationsDetailed(receiptVoucherId);

        }



        public DataTable GetInvoiceAllocationsDetailed(long receiptVoucherId)
        {
            var dt = QueryInvoiceAllocations(receiptVoucherId);
            if (dt == null || !dt.Columns.Contains("Clearing Type")) return dt;
            return DetailViewHelper.MapIntColumnsToString(dt, new Dictionary<string, Func<int, string>>
            {
                ["Clearing Type"] = type => DictionaryService.GetDisplayName(DictionaryService.Categories.PoPaymentType, type)
            });
        }

        /// <summary>Raw allocation rows for grid editor (Clearing Type remains int).</summary>
        public DataTable GetInvoiceAllocationsForEditor(long receiptVoucherId)
        {
            return QueryInvoiceAllocations(receiptVoucherId);
        }

        private static DataTable QueryInvoiceAllocations(long receiptVoucherId)
        {
            string sql = @"SELECT COALESCE(i.invoiceCode, '(Exchange Loss)') AS 'Invoice Code',
                                  rvi.invoiceID AS 'Invoice ID',
                                  rvi.receivedAmount AS 'Allocated Amount',
                                  rvi.type AS 'Clearing Type'
                           FROM ReceiptVoucherInvoice rvi
                           LEFT JOIN Invoice i ON rvi.invoiceID = i.invoiceID
                           WHERE rvi.receiptVoucherID = @id
                           ORDER BY rvi.lineNo";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", receiptVoucherId) });
        }

        public bool HasInvoiceAllocations(long receiptVoucherId)
        {
            object count = DatabaseConnect.ExecuteScalar(
                @"SELECT COUNT(*) FROM ReceiptVoucherInvoice WHERE receiptVoucherID = @id",
                new[] { new MySqlParameter("@id", receiptVoucherId) });
            return count != null && count != DBNull.Value && Convert.ToInt64(count) > 0;
        }

        public bool TryUpdateStatus(long receiptVoucherId, int newStatus, out string error)
        {
            error = null;
            if (newStatus < 0 || newStatus > 2)
            {
                error = "Invalid status.";
                return false;
            }

            var rv = GetById(receiptVoucherId);
            if (rv == null)
            {
                error = "Receipt voucher not found.";
                return false;
            }

            if (rv.Status == 1 && newStatus == 0)
            {
                error = "Confirmed vouchers cannot revert to Draft.";
                return false;
            }

            if (newStatus == 0 && HasInvoiceAllocations(receiptVoucherId))
            {
                error = "Cannot set Draft while invoice allocations exist.";
                return false;
            }

            bool updated = DatabaseConnect.ExecuteNonQuery(
                @"UPDATE receiptvoucher
                  SET status = @status, lastModifyDate = NOW()
                  WHERE receiptVoucherID = @id",
                new[]
                {
                    new MySqlParameter("@status", newStatus),
                    new MySqlParameter("@id", receiptVoucherId)
                }) > 0;
            if (!updated)
            {
                error = "Update failed.";
                return false;
            }

            return true;
        }
    }
}
