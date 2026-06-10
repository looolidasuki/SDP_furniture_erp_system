using System;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class DashboardOverviewService
    {
        public static decimal GetMonthReceiptIncomeHkd()
        {
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var end = start.AddMonths(1);
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(paymentAmountBase), 0)
                  FROM receiptvoucher
                  WHERE status = 1
                    AND paymentReceivedDate >= @start AND paymentReceivedDate < @end",
                new[]
                {
                    new MySqlParameter("@start", start),
                    new MySqlParameter("@end", end)
                });
            return ToDecimal(value);
        }

        public static decimal GetMonthPaymentExpenseHkd()
        {
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var end = start.AddMonths(1);
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(totalAmountBase), 0)
                  FROM paymentvoucher
                  WHERE status IN (1, 2)
                    AND createDate >= @start AND createDate < @end",
                new[]
                {
                    new MySqlParameter("@start", start),
                    new MySqlParameter("@end", end)
                });
            return ToDecimal(value);
        }

        public static decimal GetTotalAccountsReceivableHkd()
        {
            const string sql = @"SELECT COALESCE(SUM(outstanding), 0)
                                 FROM (
                                     SELECT GREATEST(
                                         (SELECT COALESCE(SUM(il.amount), 0) FROM InvoiceLine il WHERE il.invoiceID = i.invoiceID)
                                         - (SELECT COALESCE(SUM(rvi.receivedAmount), 0)
                                            FROM receiptvoucherinvoice rvi
                                            INNER JOIN receiptvoucher rv ON rvi.receiptVoucherID = rv.receiptVoucherID
                                            WHERE rvi.invoiceID = i.invoiceID AND rvi.invoiceID IS NOT NULL AND rv.status = 1)
                                         + (SELECT COALESCE(SUM(rr.refundAmount), 0)
                                            FROM RefundRequest rr
                                            WHERE rr.InvoiceID = i.invoiceID AND rr.status = 2),
                                         0) AS outstanding
                                     FROM Invoice i
                                     WHERE i.status <> 4
                                 ) ar";
            return ToDecimal(DatabaseConnect.ExecuteScalar(sql));
        }

        public static decimal GetTotalAccountsPayableHkd()
        {
            const string sql = @"SELECT COALESCE(SUM(GREATEST(po.totalAmount - COALESCE(settled.settledAmt, 0), 0)), 0)
                                 FROM purchaseorder po
                                 LEFT JOIN (
                                     SELECT purchaseOrderID, SUM(payAmount) AS settledAmt
                                     FROM paymentvoucherpurchaseorder
                                     GROUP BY purchaseOrderID
                                 ) settled ON settled.purchaseOrderID = po.purchaseOrderID
                                 WHERE po.status NOT IN (6, 7)";
            return ToDecimal(DatabaseConnect.ExecuteScalar(sql));
        }

        public static int GetOpenSalesOrderCount()
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT COUNT(*) FROM SalesOrder WHERE status IN (1, 2, 3)");
            return ToInt(value);
        }

        public static int GetOpenPurchaseOrderCount()
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT COUNT(*) FROM purchaseorder WHERE status NOT IN (6, 7)");
            return ToInt(value);
        }

        public static int GetActiveProductionOrderCount()
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT COUNT(*) FROM ProductionOrder WHERE status IN (0, 1, 2)");
            return ToInt(value);
        }

        public static DataTable GetUnsettledSalesOrders(int maxRows = 50)
        {
            string sql = @"SELECT so.salesOrderCode AS 'Order Code',
                                  c.customerName AS 'Customer',
                                  so.totalAmount AS 'Total',
                                  GREATEST(so.totalAmount - COALESCE(inv.invTotal, 0), 0) AS 'Uninvoiced',
                                  so.status AS 'Status',
                                  so.salesOrderID AS 'Order ID'
                           FROM SalesOrder so
                           LEFT JOIN Customer c ON so.customerID = c.customerID
                           LEFT JOIN (
                               SELECT i.salesOrderID,
                                      SUM((SELECT COALESCE(SUM(il.amount), 0)
                                           FROM InvoiceLine il WHERE il.invoiceID = i.invoiceID)) AS invTotal
                               FROM Invoice i
                               WHERE i.salesOrderID IS NOT NULL AND i.status <> 4
                               GROUP BY i.salesOrderID
                           ) inv ON inv.salesOrderID = so.salesOrderID
                           WHERE so.status IN (1, 2, 3)
                             AND GREATEST(so.totalAmount - COALESCE(inv.invTotal, 0), 0) > 0.01
                           ORDER BY so.createDate DESC
                           LIMIT @max";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@max", maxRows) });
        }

        public static DataTable GetUnsettledPurchaseOrders(int maxRows = 50)
        {
            string sql = @"SELECT po.purchaseOrderCode AS 'PO Code',
                                  s.supplierName AS 'Supplier',
                                  po.totalAmount AS 'Total',
                                  GREATEST(po.totalAmount - COALESCE(settled.settledAmt, 0), 0) AS 'Outstanding',
                                  po.status AS 'Status',
                                  po.purchaseOrderID AS 'Purchase Order ID'
                           FROM purchaseorder po
                           LEFT JOIN Supplier s ON po.supplierID = s.supplierID
                           LEFT JOIN (
                               SELECT purchaseOrderID, SUM(payAmount) AS settledAmt
                               FROM paymentvoucherpurchaseorder
                               GROUP BY purchaseOrderID
                           ) settled ON settled.purchaseOrderID = po.purchaseOrderID
                           WHERE po.status NOT IN (6, 7)
                             AND GREATEST(po.totalAmount - COALESCE(settled.settledAmt, 0), 0) > 0.01
                           ORDER BY po.createDate DESC
                           LIMIT @max";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@max", maxRows) });
        }

        public static DataTable GetMyPendingTasks(int maxRows = 40)
        {
            var table = CreateTaskTable();
            int remaining = maxRows;

            if (AppSession.CanEdit(PermissionModule.SalesOrder))
                remaining = AppendTasks(table, remaining, @"
                    SELECT 'Confirm order' AS Task,
                           'Sales Order' AS 'Document Type',
                           so.salesOrderCode AS Code,
                           so.salesOrderID AS Id,
                           'Sales Orders' AS Module,
                           so.createDate AS 'Date'
                    FROM salesorder so
                    WHERE so.status = 0
                    ORDER BY so.createDate DESC");

            if (AppSession.CanEdit(PermissionModule.ReceiptVoucher))
                remaining = AppendTasks(table, remaining, @"
                    SELECT 'Verify receipt' AS Task,
                           'Receipt Voucher' AS 'Document Type',
                           rv.receiptVoucherCode AS Code,
                           rv.receiptVoucherID AS Id,
                           'Finance Dept' AS Module,
                           rv.createDate AS 'Date'
                    FROM receiptvoucher rv
                    WHERE rv.status = 0
                    ORDER BY rv.createDate DESC");

            if (AppSession.CanEdit(PermissionModule.PaymentVoucher))
            {
                remaining = AppendTasks(table, remaining, @"
                    SELECT 'Approve payment' AS Task,
                           'Payment Voucher' AS 'Document Type',
                           pv.paymentVoucherCode AS Code,
                           pv.paymentVoucherID AS Id,
                           'Finance Dept' AS Module,
                           pv.createDate AS 'Date'
                    FROM paymentvoucher pv
                    WHERE pv.status = 0
                    ORDER BY pv.createDate DESC");

                remaining = AppendTasks(table, remaining, @"
                    SELECT 'Mark paid' AS Task,
                           'Payment Voucher' AS 'Document Type',
                           pv.paymentVoucherCode AS Code,
                           pv.paymentVoucherID AS Id,
                           'Finance Dept' AS Module,
                           pv.createDate AS 'Date'
                    FROM paymentvoucher pv
                    WHERE pv.status = 1
                    ORDER BY pv.createDate DESC");
            }

            if (AppSession.CanView(PermissionModule.Invoice))
                remaining = AppendTasks(table, remaining, @"
                    SELECT 'Collect payment' AS Task,
                           'Invoice' AS 'Document Type',
                           i.invoiceCode AS Code,
                           i.invoiceID AS Id,
                           'Invoices' AS Module,
                           i.createDate AS 'Date'
                    FROM invoice i
                    WHERE i.status = 3
                    ORDER BY i.createDate DESC");

            if (AppSession.CanEdit(PermissionModule.Refund))
                remaining = AppendTasks(table, remaining, @"
                    SELECT 'Approve refund' AS Task,
                           'Refund' AS 'Document Type',
                           rr.refundRequestCode AS Code,
                           rr.refundRequestID AS Id,
                           'Refunds' AS Module,
                           rr.createDate AS 'Date'
                    FROM refundrequest rr
                    WHERE rr.status = 0
                    ORDER BY rr.createDate DESC");

            if (AppSession.CanEdit(PermissionModule.GoodsReceivedNote))
                remaining = AppendTasks(table, remaining, @"
                    SELECT 'Confirm GRN' AS Task,
                           'Goods Received' AS 'Document Type',
                           grn.goodsReceivedNoteCode AS Code,
                           grn.goodsReceivedNoteID AS Id,
                           'Goods Received' AS Module,
                           grn.createDate AS 'Date'
                    FROM goodsreceivednote grn
                    WHERE grn.status = 0
                    ORDER BY grn.createDate DESC");

            if (AppSession.CanView(PermissionModule.SalesOrder))
                AppendTasks(table, remaining, @"
                    SELECT 'Overdue delivery' AS Task,
                           'Sales Order' AS 'Document Type',
                           so.salesOrderCode AS Code,
                           so.salesOrderID AS Id,
                           'Sales Orders' AS Module,
                           so.requestedDeliveryDate AS 'Date'
                    FROM salesorder so
                    WHERE so.status IN (1, 2, 3)
                      AND so.requestedDeliveryDate IS NOT NULL
                      AND so.requestedDeliveryDate < CURDATE()
                    ORDER BY so.requestedDeliveryDate ASC");

            return table;
        }

        private static DataTable CreateTaskTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Task", typeof(string));
            dt.Columns.Add("Document Type", typeof(string));
            dt.Columns.Add("Code", typeof(string));
            dt.Columns.Add("Id", typeof(long));
            dt.Columns.Add("Module", typeof(string));
            dt.Columns.Add("Date", typeof(DateTime));
            return dt;
        }

        private static int AppendTasks(DataTable target, int remaining, string sql)
        {
            if (remaining <= 0) return 0;
            try
            {
                var dt = DatabaseConnect.ExecuteQuery(sql + " LIMIT " + remaining);
                if (dt == null) return remaining;
                foreach (DataRow row in dt.Rows)
                {
                    target.ImportRow(row);
                    remaining--;
                    if (remaining <= 0) break;
                }
            }
            catch { }
            return remaining;
        }

        private static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            return Convert.ToDecimal(value);
        }

        private static int ToInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return Convert.ToInt32(value);
        }
    }
}
