using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Data;
using System;

namespace Sales_user.Controllers
{
    public class ReceiptVoucherController
    {
        public DataTable GetAllReceiptVouchers()
        {
            // 💡 確保 DataGridView 對應的實體查詢欄位完全為 cusomerID
            string sql = @"SELECT receiptVoucherID AS 'ID',
                                  receiptVoucherCode AS 'Voucher Code',
                                  cusomerID AS 'Customer ID',
                                  staffID AS 'Staff ID',
                                  paymentAmount AS 'Amount',
                                  paymentMethod AS 'Method',
                                  paymentMethodRef AS 'Reference',
                                  status AS 'Status',
                                  createDate AS 'Date'
                           FROM receiptvoucher
                           ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(ReceiptVoucher rv)
        {
            string sql = @"INSERT INTO receiptvoucher
                (receiptVoucherCode, cusomerID, staffID, paymentAmount, paymentMethod, paymentMethodRef,
                 remark, status, currencyID, paymentReceivedDate, createDate)
                VALUES (@code, @cusomerID, @staffID, @amount, @method, @ref, @remark, @status, @currencyID, @receivedDate, NOW())";

            long id = DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", rv.ReceiptVoucherCode ?? "RV-TEMP"),
                new MySqlParameter("@cusomerID", rv.CusomerID),
                new MySqlParameter("@staffID", rv.StaffID),
                new MySqlParameter("@amount", rv.PaymentAmount),
                new MySqlParameter("@method", rv.PaymentMethod.ToString()),
                new MySqlParameter("@ref", rv.PaymentMethodRef ?? string.Empty),
                new MySqlParameter("@remark", rv.Remark ?? (object)DBNull.Value),
                new MySqlParameter("@status", rv.Status),
                new MySqlParameter("@currencyID", rv.CurrencyID == 0 ? 1 : rv.CurrencyID),
                new MySqlParameter("@receivedDate", DateTime.Today)
            });

            if (id > 0)
            {
                DatabaseConnect.ExecuteNonQuery(
                    "UPDATE receiptvoucher SET receiptVoucherCode = @code WHERE receiptVoucherID = @id",
                    new[] { new MySqlParameter("@code", "RV-" + id), new MySqlParameter("@id", id) });
            }
            return id;
        }

        public bool Update(ReceiptVoucher rv)
        {
            // 💡 同步修復 Update 的參數為 @cusomerID
            string sql = @"UPDATE receiptvoucher
                           SET cusomerID=@cusomerID, staffID=@staffID, paymentAmount=@amount, 
                               paymentMethod=@method, paymentMethodRef=@ref, remark=@remark, 
                               status=@status, lastModifyDate=NOW()
                           WHERE receiptVoucherID=@id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@cusomerID", rv.CusomerID), // 💡 移除參數裡的 t
                new MySqlParameter("@staffID", rv.StaffID),
                new MySqlParameter("@amount", rv.PaymentAmount),
                new MySqlParameter("@method", rv.PaymentMethod.ToString()),
                new MySqlParameter("@ref", rv.PaymentMethodRef ?? string.Empty),
                new MySqlParameter("@remark", rv.Remark ?? (object)DBNull.Value),
                new MySqlParameter("@status", rv.Status),
                new MySqlParameter("@id", rv.ReceiptVoucherID)
            }) > 0;
        }

        public ReceiptVoucher GetById(long id)
        {
            string sql = @"SELECT receiptVoucherID, receiptVoucherCode, cusomerID, staffID, 
                                  paymentAmount, paymentMethod, paymentMethodRef, remark, status, currencyID 
                           FROM receiptvoucher 
                           WHERE receiptVoucherID = @id";

            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new ReceiptVoucher
            {
                ReceiptVoucherID = Convert.ToInt64(row["receiptVoucherID"]),
                ReceiptVoucherCode = row["receiptVoucherCode"].ToString(),
                CusomerID = Convert.ToInt64(row["cusomerID"]),
                StaffID = Convert.ToInt64(row["staffID"]),
                PaymentAmount = Convert.ToDecimal(row["paymentAmount"]),
                PaymentMethod = int.TryParse(row["paymentMethod"].ToString(), out int m) ? m : 0,
                PaymentMethodRef = row["paymentMethodRef"].ToString(),
                Remark = row["remark"].ToString(),
                Status = Convert.ToInt32(row["status"]),
                CurrencyID = Convert.ToInt64(row["currencyID"])
            };
        }

        public DataTable GetIncomeTrend()
        {
            // 查詢近 6 個月已確認(status != 2)的收款總額趨勢
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
            string sql = @"SELECT invoiceID AS 'Invoice ID', receivedAmount AS 'Amount', type AS 'Type'
                           FROM ReceiptVoucherInvoice
                           WHERE receiptVoucherID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", receiptVoucherId) });
        }
    }
}