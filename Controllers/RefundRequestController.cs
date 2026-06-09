using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class RefundRequestController
    {
        public DataTable GetAllRefundRequests()
        {
            string sql = @"SELECT rr.refundRequestCode AS 'Request Code',
                                  rv.receiptVoucherCode AS 'Receipt Voucher',
                                  i.invoiceCode AS 'Invoice',
                                  rr.createDate AS 'Request Date',
                                  rr.refundAmount AS 'Amount',
                                  rr.refundRef AS 'Refund Ref',
                                  rr.refundMethod AS 'Refund Method',
                                  rr.refundReason AS 'Refund Reason',
                                  rr.status AS 'Status',
                                  rr.remark AS 'Remark'
                           FROM RefundRequest rr
                           LEFT JOIN Invoice i ON rr.InvoiceID = i.invoiceID
                           LEFT JOIN receiptvoucher rv ON rr.ReceiptVoucherID = rv.receiptVoucherID
                           ORDER BY rr.createDate DESC";
            var dt = DatabaseConnect.ExecuteQuery(sql);
            return DecorateRefundGrid(dt);
        }

        public static DataTable DecorateRefundGrid(DataTable dt)
        {
            if (dt == null) return dt;

            if (!dt.Columns.Contains("Method Label"))
                dt.Columns.Add("Method Label", typeof(string));
            if (!dt.Columns.Contains("Reason Label"))
                dt.Columns.Add("Reason Label", typeof(string));
            if (!dt.Columns.Contains("Status Label"))
                dt.Columns.Add("Status Label", typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                if (row["Refund Method"] != DBNull.Value)
                {
                    int method = Convert.ToInt32(row["Refund Method"]);
                    row["Method Label"] = DictionaryService.GetDisplayName(DictionaryService.Categories.RefundMethod, method);
                }
                row["Reason Label"] = DictionaryService.GetRefundReasonDisplay(row["Refund Reason"]?.ToString());
                if (row["Status"] != DBNull.Value)
                {
                    int status = Convert.ToInt32(row["Status"]);
                    row["Status Label"] = DictionaryService.GetDisplayName(DictionaryService.Categories.RefundStatus, status);
                }
            }
            return dt;
        }

        public DataTable GetHeaderDetail(string requestCode)
        {
            string sql = @"SELECT rr.refundRequestCode AS 'Request Code',
                                  CONCAT('CU-', LPAD(c.customerID, 9, '0')) AS 'Customer Code',
                                  c.customerName AS 'Customer',
                                  (SELECT cp.contactPerson FROM contactperson cp
                                    WHERE cp.customerID = c.customerID
                                    ORDER BY cp.contactPersonID LIMIT 1) AS 'Contact Person',
                                  (SELECT cp.phone FROM contactperson cp
                                    WHERE cp.customerID = c.customerID
                                    ORDER BY cp.contactPersonID LIMIT 1) AS 'Phone Number',
                                  c.billingAddress AS 'Address',
                                  rv.receiptVoucherCode AS 'Receipt Voucher',
                                  i.invoiceCode AS 'Invoice',
                                  CONCAT(COALESCE(st.firstName, ''), ' ', COALESCE(st.lastName, '')) AS 'Staff',
                                  rr.createDate AS 'Request Date',
                                  rr.lastModifyDate AS 'Last Modified',
                                  rr.refundAmount AS 'Refund Amount',
                                  rr.refundRef AS 'Refund Transaction Ref',
                                  rr.refundMethod AS 'Refund Method',
                                  rr.refundReason AS 'Refund Reason',
                                  rr.status AS 'Status',
                                  rr.remark AS 'Remark'
                           FROM RefundRequest rr
                           LEFT JOIN Invoice i ON rr.InvoiceID = i.invoiceID
                           LEFT JOIN receiptvoucher rv ON rr.ReceiptVoucherID = rv.receiptVoucherID
                           LEFT JOIN Customer c ON c.customerID = COALESCE(i.customerID, rv.cusomerID)
                           LEFT JOIN Staff st ON rr.staffID = st.staffID
                           WHERE rr.refundRequestCode = @code";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@code", requestCode) });
            if (dt == null || dt.Rows.Count == 0) return dt;

            foreach (DataRow row in dt.Rows)
            {
                if (dt.Columns.Contains("Refund Reason"))
                    row["Refund Reason"] = DictionaryService.GetRefundReasonDisplay(row["Refund Reason"]?.ToString());
            }

            return DetailViewHelper.MapIntColumnsToString(dt, new Dictionary<string, Func<int, string>>
            {
                ["Refund Method"] = v => DictionaryService.GetDisplayName(DictionaryService.Categories.RefundMethod, v),
                ["Status"] = v => DictionaryService.GetDisplayName(DictionaryService.Categories.RefundStatus, v)
            });
        }

        public long ResolveCustomerId(string requestCode)
        {
            string sql = @"SELECT COALESCE(i.customerID, rv.cusomerID) AS customerID
                           FROM RefundRequest rr
                           LEFT JOIN Invoice i ON rr.InvoiceID = i.invoiceID
                           LEFT JOIN receiptvoucher rv ON rr.ReceiptVoucherID = rv.receiptVoucherID
                           WHERE rr.refundRequestCode = @code";
            object value = DatabaseConnect.ExecuteScalar(sql, new[] { new MySqlParameter("@code", requestCode) });
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt64(value);
        }

        public long CreateRefundRequest(RefundRequest refund)
        {
            string sql = @"INSERT INTO RefundRequest
                (refundRequestID, refundRequestCode, staffID, ReceiptVoucherID, InvoiceID,
                 refundAmount, refundMethod, refundRef, refundReason, status, remark)
                VALUES (@id, @code, @staffID, @receiptID, @invoiceID,
                        @amount, @method, @ref, @reason, @status, @remark)";

            long id = DatabaseConnect.InsertWithAllocatedId("refundrequest", "refundRequestID", sql, new[] {
                new MySqlParameter("@code", refund.RefundRequestCode),
                new MySqlParameter("@staffID", refund.StaffID),
                new MySqlParameter("@receiptID", refund.ReceiptVoucherID ?? (object)DBNull.Value),
                new MySqlParameter("@invoiceID", refund.InvoiceID ?? (object)DBNull.Value),
                new MySqlParameter("@amount", refund.RefundAmount),
                new MySqlParameter("@method", refund.RefundMethod),
                new MySqlParameter("@ref", string.IsNullOrWhiteSpace(refund.RefundRef) ? (object)DBNull.Value : refund.RefundRef.Trim()),
                new MySqlParameter("@reason", refund.RefundReason),
                new MySqlParameter("@status", refund.Status),
                new MySqlParameter("@remark", refund.Remark ?? (object)DBNull.Value)
            });
            if (id > 0)
            {
                DatabaseConnect.ExecuteNonQuery(
                    "UPDATE RefundRequest SET refundRequestCode = @code WHERE refundRequestID = @id",
                    new[] {
                        new MySqlParameter("@code", "RF-" + id),
                        new MySqlParameter("@id", id)
                    });
                refund.RefundRequestCode = "RF-" + id;
                refund.RefundRequestID = id;
            }
            return id;
        }

        public bool UpdateStatus(string requestCode, int newStatus, long staffID)
        {
            string sql = @"UPDATE RefundRequest
                           SET status = @status, staffID = @staffID, lastModifyDate = NOW()
                           WHERE refundRequestCode = @code";

            MySqlParameter[] parameters = {
                new MySqlParameter("@status", newStatus),
                new MySqlParameter("@staffID", staffID),
                new MySqlParameter("@code", requestCode)
            };

            return DatabaseConnect.ExecuteNonQuery(sql, parameters) > 0;
        }

        public RefundRequest GetByCode(string requestCode)
        {
            string sql = @"SELECT refundRequestID, refundRequestCode, staffID, ReceiptVoucherID, InvoiceID,
                                  refundAmount, refundMethod, refundRef, refundReason, status, remark
                           FROM RefundRequest
                           WHERE refundRequestCode = @code";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@code", requestCode) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return MapRow(row);
        }

        public bool Update(RefundRequest refund)
        {
            string sql = @"UPDATE RefundRequest
                           SET staffID=@staffID, ReceiptVoucherID=@receiptID, InvoiceID=@invoiceID,
                               refundAmount=@amount, refundMethod=@method, refundRef=@ref, refundReason=@reason,
                               status=@status, remark=@remark, lastModifyDate=NOW()
                           WHERE refundRequestID=@id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@staffID", refund.StaffID),
                new MySqlParameter("@receiptID", refund.ReceiptVoucherID ?? (object)DBNull.Value),
                new MySqlParameter("@invoiceID", refund.InvoiceID ?? (object)DBNull.Value),
                new MySqlParameter("@amount", refund.RefundAmount),
                new MySqlParameter("@method", refund.RefundMethod),
                new MySqlParameter("@ref", string.IsNullOrWhiteSpace(refund.RefundRef) ? (object)DBNull.Value : refund.RefundRef.Trim()),
                new MySqlParameter("@reason", refund.RefundReason ?? ""),
                new MySqlParameter("@status", refund.Status),
                new MySqlParameter("@remark", refund.Remark ?? (object)DBNull.Value),
                new MySqlParameter("@id", refund.RefundRequestID)
            }) > 0;
        }

        private static RefundRequest MapRow(DataRow row)
        {
            return new RefundRequest
            {
                RefundRequestID = Convert.ToInt64(row["refundRequestID"]),
                RefundRequestCode = row["refundRequestCode"]?.ToString(),
                StaffID = Convert.ToInt64(row["staffID"]),
                ReceiptVoucherID = row["ReceiptVoucherID"] == DBNull.Value ? (long?)null : Convert.ToInt64(row["ReceiptVoucherID"]),
                InvoiceID = row["InvoiceID"] == DBNull.Value ? (long?)null : Convert.ToInt64(row["InvoiceID"]),
                RefundAmount = Convert.ToDecimal(row["refundAmount"]),
                RefundMethod = Convert.ToInt32(row["refundMethod"]),
                RefundRef = row["refundRef"] == DBNull.Value ? null : row["refundRef"].ToString(),
                RefundReason = row["refundReason"]?.ToString(),
                Status = Convert.ToInt32(row["status"]),
                Remark = row["remark"] == DBNull.Value ? null : row["remark"].ToString()
            };
        }
    }
}
