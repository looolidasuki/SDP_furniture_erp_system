using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Collections.Generic;
using System.Data;
using FurnitureERP.Helpers;

namespace Sales_user.Controllers
{
    public class PaymentVoucherController
    {
        private static readonly string[] StatusLabels = { "Draft", "Approved", "Paid", "Cancelled" };

        public DataTable GetAllPaymentVouchers()
        {
            string sql = @"SELECT pv.paymentVoucherID AS 'ID',
                                  pv.paymentVoucherCode AS 'Voucher Code',
                                  s.supplierName AS 'Supplier',
                                  CONCAT(COALESCE(st.firstName, ''), ' ', COALESCE(st.lastName, '')) AS 'Staff',
                                  pv.totalAmount AS 'Amount',
                                  pv.paymentMethod AS 'Method',
                                  pv.paymentMethodRef AS 'Reference',
                                  pv.status AS 'Status',
                                  pv.createDate AS 'Date'
                           FROM paymentvoucher pv
                           LEFT JOIN Supplier s ON pv.supplierID = s.supplierID
                           LEFT JOIN Staff st ON pv.staffID = st.staffID
                           ORDER BY pv.createDate DESC";
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

        // 2. 獲取月度總計
        public DataTable GetMonthlyTotals()
        {
            string sql = @"SELECT DATE_FORMAT(createDate, '%Y-%m') AS Month,
                                  SUM(totalAmount) AS Total
                           FROM paymentvoucher
                           WHERE status != 3
                           GROUP BY DATE_FORMAT(createDate, '%Y-%m')
                           ORDER BY Month DESC
                           LIMIT 12";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        // 3. 獲取付款方式佔比
        public DataTable GetMethodBreakdown()
        {
            string sql = @"SELECT paymentMethod AS Method,
                                  SUM(totalAmount) AS Total
                           FROM paymentvoucher
                           WHERE status != 3
                           GROUP BY paymentMethod";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public PaymentVoucher GetById(long id)
        {
            string sql = @"SELECT pv.paymentVoucherID, pv.paymentVoucherCode, pv.supplierID,
                          pv.staffID, pv.totalAmount, pv.paymentMethod, pv.paymentMethodRef,
                          pv.remark, pv.status, pv.createDate
                   FROM paymentvoucher pv
                   WHERE pv.paymentVoucherID = @id";

            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            var pv = MapPaymentVoucherRow(row);
            pv.PurchaseOrderLines = GetPurchaseOrderSettlements(id);
            if (pv.PurchaseOrderLines != null && pv.PurchaseOrderLines.Count > 0)
            {
                var first = pv.PurchaseOrderLines[0];
                pv.PurchaseOrderID = first.PurchaseOrderID;
                pv.PurchaseOrderCode = first.PurchaseOrderCode;
                pv.ClearingType = first.ClearingType;
                pv.VoucherPayAmount = first.PayAmount;
            }
            return pv;
        }

        public PaymentVoucher GetByCode(string voucherCode)
        {
            if (string.IsNullOrWhiteSpace(voucherCode)) return null;
            string sql = @"SELECT paymentVoucherID FROM paymentvoucher WHERE paymentVoucherCode = @code LIMIT 1";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@code", voucherCode.Trim()) });
            if (dt == null || dt.Rows.Count == 0) return null;
            return GetById(Convert.ToInt64(dt.Rows[0]["paymentVoucherID"]));
        }

        public bool ExistsByCode(string voucherCode, long excludeId = 0)
        {
            if (string.IsNullOrWhiteSpace(voucherCode)) return false;
            string sql = @"SELECT COUNT(*) FROM paymentvoucher WHERE paymentVoucherCode = @code";
            if (excludeId > 0) sql += " AND paymentVoucherID <> @excludeId";
            var prms = new List<MySqlParameter> { new MySqlParameter("@code", voucherCode.Trim()) };
            if (excludeId > 0) prms.Add(new MySqlParameter("@excludeId", excludeId));
            object value = DatabaseConnect.ExecuteScalar(sql, prms.ToArray());
            return value != null && value != DBNull.Value && Convert.ToInt32(value) > 0;
        }

        private static PaymentVoucher MapPaymentVoucherRow(DataRow row)
        {
            return new PaymentVoucher
            {
                PaymentVoucherID = Convert.ToInt64(row["paymentVoucherID"]),
                PaymentVoucherCode = row["paymentVoucherCode"]?.ToString(),
                SupplierID = Convert.ToInt64(row["supplierID"]),
                StaffID = Convert.ToInt64(row["staffID"]),
                Amount = Convert.ToDecimal(row["totalAmount"]),
                PaymentMethod = row["paymentMethod"]?.ToString(),
                PaymentRef = row["paymentMethodRef"] == DBNull.Value ? null : row["paymentMethodRef"].ToString(),
                Remark = row["remark"] == DBNull.Value ? null : row["remark"].ToString(),
                Status = Convert.ToInt32(row["status"]),
                CreateDate = Convert.ToDateTime(row["createDate"])
            };
        }

        public List<VoucherPurchaseOrderLine> GetPurchaseOrderSettlements(long paymentVoucherId)
        {
            string sql = @"SELECT pvpo.purchaseOrderID, po.purchaseOrderCode, po.requestDeliveryDate,
                                  pvpo.type AS ClearingType, pvpo.payAmount
                           FROM paymentvoucherpurchaseorder pvpo
                           INNER JOIN purchaseorder po ON pvpo.purchaseOrderID = po.purchaseOrderID
                           WHERE pvpo.paymentVoucherID = @id
                           ORDER BY po.purchaseOrderCode";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", paymentVoucherId) });
            var list = new List<VoucherPurchaseOrderLine>();
            if (dt == null) return list;
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new VoucherPurchaseOrderLine
                {
                    PurchaseOrderID = Convert.ToInt64(row["purchaseOrderID"]),
                    PurchaseOrderCode = row["purchaseOrderCode"]?.ToString(),
                    RequestDeliveryDate = row["requestDeliveryDate"] == DBNull.Value
                        ? (DateTime?)null : Convert.ToDateTime(row["requestDeliveryDate"]),
                    ClearingType = Convert.ToInt32(row["ClearingType"]),
                    PayAmount = Convert.ToDecimal(row["payAmount"])
                });
            }
            return list;
        }

        public DataTable GetPurchaseOrderSettlementsDetailed(long paymentVoucherId)
        {
            string sql = @"SELECT po.purchaseOrderCode AS 'Purchase Order',
                                  po.requestDeliveryDate AS 'Request Delivery Date',
                                  pvpo.payAmount AS 'Pay Amount',
                                  pvpo.type AS 'Clearing Type'
                           FROM paymentvoucherpurchaseorder pvpo
                           INNER JOIN purchaseorder po ON pvpo.purchaseOrderID = po.purchaseOrderID
                           WHERE pvpo.paymentVoucherID = @id
                           ORDER BY po.purchaseOrderCode";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", paymentVoucherId) });
            return MapClearingTypeColumn(dt);
        }

        private static DataTable MapClearingTypeColumn(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains("Clearing Type")) return dt;
            return DetailViewHelper.MapIntColumnsToString(dt, new Dictionary<string, Func<int, string>>
            {
                ["Clearing Type"] = type => DictionaryService.GetDisplayName(DictionaryService.Categories.PoPaymentType, type)
            });
        }

        public DataTable GetHeaderDetail(long paymentVoucherId)
        {
            string sql = @"SELECT pv.paymentVoucherCode AS 'Voucher Code',
                                  s.supplierName AS 'Supplier',
                                  CONCAT(COALESCE(st.firstName, ''), ' ', COALESCE(st.lastName, '')) AS 'Staff',
                                  pv.totalAmount AS 'Amount',
                                  pv.paymentMethod AS 'Payment Method',
                                  pv.paymentMethodRef AS 'Method Reference',
                                  CASE pv.status
                                      WHEN 0 THEN 'Draft'
                                      WHEN 1 THEN 'Approved'
                                      WHEN 2 THEN 'Paid'
                                      WHEN 3 THEN 'Cancelled'
                                      ELSE CAST(pv.status AS CHAR)
                                  END AS 'Status',
                                  pv.createDate AS 'Create Date',
                                  pv.lastModifyDate AS 'Last Modified',
                                  pv.remark AS 'Remark'
                           FROM paymentvoucher pv
                           LEFT JOIN Supplier s ON pv.supplierID = s.supplierID
                           LEFT JOIN Staff st ON pv.staffID = st.staffID
                           WHERE pv.paymentVoucherID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", paymentVoucherId) });
        }

        public decimal GetSettledTotalByPurchaseOrder(long purchaseOrderId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(payAmount), 0) FROM paymentvoucherpurchaseorder WHERE purchaseOrderID = @id",
                new[] { new MySqlParameter("@id", purchaseOrderId) });
            return value == null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        }

        public DataTable GetSettlementsByPurchaseOrder(long purchaseOrderId)
        {
            string sql = @"SELECT pv.paymentVoucherCode AS 'Voucher Code',
                                  pv.createDate AS 'Date',
                                  pvpo.type AS 'Payment Type',
                                  pvpo.payAmount AS 'Settled Amount',
                                  pv.paymentMethod AS 'Method',
                                  pv.status AS 'Status'
                           FROM paymentvoucherpurchaseorder pvpo
                           INNER JOIN paymentvoucher pv ON pv.paymentVoucherID = pvpo.paymentVoucherID
                           WHERE pvpo.purchaseOrderID = @id
                           ORDER BY pv.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", purchaseOrderId) });
        }

        // 5. 新增付款憑證 (解決第3個錯誤)
        public long Insert(PaymentVoucher pv)
        {
            // 使用你最原本內建的預設連接字串
            string connectionString = "Server=localhost;Port=3306;Database=furniture_erp_system;Uid=root;Pwd=;CharSet=utf8mb4;AllowPublicKeyRetrieval=True;SslMode=Disabled;";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 步驟 A: 寫入主表
                        string sqlMain = @"INSERT INTO paymentvoucher
                            (paymentVoucherCode, supplierID, staffID, totalAmount, paymentMethod, paymentMethodRef, remark, status)
                            VALUES (@code, @supplierID, @staffID, @amount, @method, @ref, @remark, @status)";

                        long pvId = 0;
                        using (var cmd = new MySqlCommand(sqlMain, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@code", (pv.PaymentVoucherCode ?? "").Trim());
                            cmd.Parameters.AddWithValue("@supplierID", pv.SupplierID);
                            cmd.Parameters.AddWithValue("@staffID", pv.StaffID);
                            cmd.Parameters.AddWithValue("@amount", pv.Amount);

                            // ✅ 修正錯誤 3：因為 pv.PaymentMethod 現在是 string，
                            // AddWithValue 會自動將其轉為 MySQL 辨識的字串，避免傳入錯誤的型態 (如 sbyte/int)
                            cmd.Parameters.AddWithValue("@method", pv.PaymentMethod ?? "");

                            cmd.Parameters.AddWithValue("@ref", pv.PaymentRef ?? "");
                            cmd.Parameters.AddWithValue("@remark", pv.Remark ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@status", pv.Status);

                            cmd.ExecuteNonQuery();
                            pvId = cmd.LastInsertedId;
                        }

                        if (pvId > 0)
                            WritePurchaseOrderSettlements(conn, trans, pvId, pv);

                        trans.Commit();
                        return pvId;
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        return 0;
                    }
                }
            }
        }

        // 6. 更新資料
        public bool Update(PaymentVoucher pv)
        {
            string connectionString = "Server=localhost;Port=3306;Database=furniture_erp_system;Uid=root;Pwd=;CharSet=utf8mb4;AllowPublicKeyRetrieval=True;SslMode=Disabled;";
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string sql = @"UPDATE paymentvoucher
                           SET paymentVoucherCode=@code, supplierID=@supplierID, staffID=@staffID,
                               totalAmount=@amount, paymentMethod=@method, paymentMethodRef=@ref,
                               remark=@remark, status=@status, lastModifyDate=NOW()
                           WHERE paymentVoucherID=@id";
                        using (var cmd = new MySqlCommand(sql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@code", (pv.PaymentVoucherCode ?? "").Trim());
                            cmd.Parameters.AddWithValue("@supplierID", pv.SupplierID);
                            cmd.Parameters.AddWithValue("@staffID", pv.StaffID);
                            cmd.Parameters.AddWithValue("@amount", pv.Amount);
                            cmd.Parameters.AddWithValue("@method", pv.PaymentMethod ?? "");
                            cmd.Parameters.AddWithValue("@ref", pv.PaymentRef ?? "");
                            cmd.Parameters.AddWithValue("@remark", pv.Remark ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@status", pv.Status);
                            cmd.Parameters.AddWithValue("@id", pv.PaymentVoucherID);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new MySqlCommand(
                            "DELETE FROM paymentvoucherpurchaseorder WHERE paymentVoucherID = @id", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@id", pv.PaymentVoucherID);
                            cmd.ExecuteNonQuery();
                        }

                        WritePurchaseOrderSettlements(conn, trans, pv.PaymentVoucherID, pv);

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }

        public DataTable GetExpenseTrend()
        {
            // 查詢近 6 個月已審批/已付款(status != 3)的支出總額趨勢
            string sql = @"SELECT DATE_FORMAT(createDate, '%Y-%m') AS Month,
                          SUM(totalAmount) AS Total
                   FROM paymentvoucher
                   WHERE status != 3
                   GROUP BY DATE_FORMAT(createDate, '%Y-%m')
                   ORDER BY Month ASC
                   LIMIT 6";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetExpenseByMethod()
        {
            string sql = @"SELECT paymentMethod AS Method,
                          SUM(totalAmount) AS Total
                   FROM paymentvoucher
                   WHERE status != 3
                   GROUP BY paymentMethod";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        private static void WritePurchaseOrderSettlements(MySqlConnection conn, MySqlTransaction trans, long pvId, PaymentVoucher pv)
        {
            var lines = pv.PurchaseOrderLines;
            if (lines == null || lines.Count == 0)
            {
                if (!pv.PurchaseOrderID.HasValue || pv.PurchaseOrderID.Value <= 0) return;
                lines = new List<VoucherPurchaseOrderLine>
                {
                    new VoucherPurchaseOrderLine
                    {
                        PurchaseOrderID = pv.PurchaseOrderID.Value,
                        ClearingType = pv.ClearingType > 0 ? pv.ClearingType : 1,
                        PayAmount = pv.VoucherPayAmount > 0 ? pv.VoucherPayAmount : pv.Amount
                    }
                };
            }

            string sqlRelation = @"INSERT INTO paymentvoucherpurchaseorder
                (paymentVoucherID, purchaseOrderID, type, payAmount)
                VALUES (@pvId, @poId, @type, @payAmount)";
            foreach (var line in lines)
            {
                if (line.PurchaseOrderID <= 0 || line.PayAmount <= 0) continue;
                using (var cmd = new MySqlCommand(sqlRelation, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@pvId", pvId);
                    cmd.Parameters.AddWithValue("@poId", line.PurchaseOrderID);
                    cmd.Parameters.AddWithValue("@type", line.ClearingType > 0 ? line.ClearingType : 1);
                    cmd.Parameters.AddWithValue("@payAmount", line.PayAmount);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}