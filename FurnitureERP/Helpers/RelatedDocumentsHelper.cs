using System;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class RelatedDocumentsHelper
    {
        public static DataTable GetPaymentVoucherRelated(long paymentVoucherId)
        {
            const string sql = @"SELECT 'Purchase Order' AS 'Document Type',
                                          po.purchaseOrderCode AS 'Code',
                                          po.purchaseOrderID AS 'Id',
                                          'Purchase Orders' AS 'Module'
                                   FROM paymentvoucherpurchaseorder pvpo
                                   INNER JOIN purchaseorder po ON pvpo.purchaseOrderID = po.purchaseOrderID
                                   WHERE pvpo.paymentVoucherID = @id
                                   ORDER BY po.purchaseOrderCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", paymentVoucherId) });
        }

        public static DataTable GetReceiptVoucherRelated(long receiptVoucherId)
        {
            const string sql = @"SELECT 'Invoice' AS 'Document Type',
                                          i.invoiceCode AS 'Code',
                                          i.invoiceID AS 'Id',
                                          'Invoices' AS 'Module'
                                   FROM receiptvoucherinvoice rvi
                                   INNER JOIN invoice i ON rvi.invoiceID = i.invoiceID
                                   WHERE rvi.receiptVoucherID = @id AND rvi.invoiceID IS NOT NULL
                                   ORDER BY rvi.lineNo";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", receiptVoucherId) });
        }

        public static DataTable GetSalesOrderRelated(long salesOrderId)
        {
            var table = CreateRelatedTable();

            const string ptoSql = @"SELECT productionOrderCode, productionOrderID
                                    FROM ProductionOrder
                                    WHERE salesOrderID = @id
                                    ORDER BY productionOrderCode";
            var ptoDt = DatabaseConnect.ExecuteQuery(ptoSql, new[] { new MySqlParameter("@id", salesOrderId) });
            if (ptoDt != null)
            {
                foreach (DataRow row in ptoDt.Rows)
                    AddRow(table, "Production Order", row["productionOrderCode"]?.ToString(),
                        Convert.ToInt64(row["productionOrderID"]), "Production");
            }

            const string dnSql = @"SELECT deliveryNoteCode, deliveryNoteID
                                   FROM deliverynote
                                   WHERE salesOrderID = @id
                                   ORDER BY deliveryNoteCode";
            var dnDt = DatabaseConnect.ExecuteQuery(dnSql, new[] { new MySqlParameter("@id", salesOrderId) });
            if (dnDt != null)
            {
                foreach (DataRow row in dnDt.Rows)
                    AddRow(table, "Delivery Note", row["deliveryNoteCode"]?.ToString(),
                        Convert.ToInt64(row["deliveryNoteID"]), "Delivery Notes");
            }

            const string invSql = @"SELECT i.invoiceCode, i.invoiceID
                                    FROM invoice i
                                    WHERE i.salesOrderID = @id
                                    ORDER BY i.invoiceCode";
            var invDt = DatabaseConnect.ExecuteQuery(invSql, new[] { new MySqlParameter("@id", salesOrderId) });
            if (invDt != null)
            {
                foreach (DataRow row in invDt.Rows)
                    AddRow(table, "Invoice", row["invoiceCode"]?.ToString(), Convert.ToInt64(row["invoiceID"]), "Invoices");
            }

            const string rvSql = @"SELECT DISTINCT rv.receiptVoucherCode, rv.receiptVoucherID
                                   FROM receiptvoucher rv
                                   INNER JOIN receiptvoucherinvoice rvi ON rv.receiptVoucherID = rvi.receiptVoucherID
                                   INNER JOIN invoice i ON rvi.invoiceID = i.invoiceID
                                   WHERE i.salesOrderID = @id
                                   ORDER BY rv.receiptVoucherCode";
            var rvDt = DatabaseConnect.ExecuteQuery(rvSql, new[] { new MySqlParameter("@id", salesOrderId) });
            if (rvDt != null)
            {
                foreach (DataRow row in rvDt.Rows)
                    AddRow(table, "Receipt Voucher", row["receiptVoucherCode"]?.ToString(),
                        Convert.ToInt64(row["receiptVoucherID"]), "Finance Dept");
            }

            return table;
        }

        public static DataTable GetInvoiceRelated(long invoiceId)
        {
            var table = CreateRelatedTable();
            try
            {
                var inv = new InvoiceController().GetById(invoiceId);
                if (inv != null && inv.SalesOrderID > 0)
                {
                    var so = new SalesOrderController().GetById(inv.SalesOrderID);
                    if (so != null)
                        AddRow(table, "Sales Order", so.SalesOrderCode, so.SalesOrderID, "Sales Orders");

                    const string dnSql = @"SELECT deliveryNoteCode, deliveryNoteID
                                           FROM deliverynote
                                           WHERE salesOrderID = @soId
                                           ORDER BY deliveryNoteCode";
                    var dnDt = DatabaseConnect.ExecuteQuery(dnSql, new[] { new MySqlParameter("@soId", inv.SalesOrderID) });
                    if (dnDt != null)
                    {
                        foreach (DataRow row in dnDt.Rows)
                            AddRow(table, "Delivery Note", row["deliveryNoteCode"]?.ToString(),
                                Convert.ToInt64(row["deliveryNoteID"]), "Delivery Notes");
                    }
                }
            }
            catch { }

            const string rvSql = @"SELECT rv.receiptVoucherCode, rv.receiptVoucherID
                                   FROM receiptvoucher rv
                                   INNER JOIN receiptvoucherinvoice rvi ON rv.receiptVoucherID = rvi.receiptVoucherID
                                   WHERE rvi.invoiceID = @id
                                   ORDER BY rv.receiptVoucherCode";
            var rvDt = DatabaseConnect.ExecuteQuery(rvSql, new[] { new MySqlParameter("@id", invoiceId) });
            if (rvDt != null)
            {
                foreach (DataRow row in rvDt.Rows)
                    AddRow(table, "Receipt Voucher", row["receiptVoucherCode"]?.ToString(),
                        Convert.ToInt64(row["receiptVoucherID"]), "Finance Dept");
            }

            const string rfSql = @"SELECT refundRequestCode, refundRequestID
                                   FROM refundrequest
                                   WHERE invoiceID = @id
                                   ORDER BY refundRequestCode";
            var rfDt = DatabaseConnect.ExecuteQuery(rfSql, new[] { new MySqlParameter("@id", invoiceId) });
            if (rfDt != null)
            {
                foreach (DataRow row in rfDt.Rows)
                    AddRow(table, "Refund", row["refundRequestCode"]?.ToString(),
                        Convert.ToInt64(row["refundRequestID"]), "Refunds");
            }

            return table;
        }

        public static System.Windows.Forms.TabPage BuildRelatedDocumentsTab(DataTable related, System.Windows.Forms.Control navigationSource)
        {
            var tab = new System.Windows.Forms.TabPage("Related Documents");
            var grid = GridHelper.CreateStyledGrid();
            grid.ReadOnly = true;
            grid.DataSource = related ?? CreateRelatedTable();
            GridHelper.StyleGrid(grid);
            grid.Dock = System.Windows.Forms.DockStyle.Fill;
            if (grid.Columns.Contains("Id")) grid.Columns["Id"].Visible = false;
            if (grid.Columns.Contains("Module")) grid.Columns["Module"].Visible = false;

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || related == null || e.RowIndex >= related.Rows.Count) return;
                var row = related.Rows[e.RowIndex];
                if (row["Id"] == DBNull.Value) return;
                DocumentNavigationHelper.OpenFromControl(navigationSource, new DocumentSearchResult
                {
                    DocumentType = row["Document Type"]?.ToString(),
                    Code = row["Code"]?.ToString(),
                    Id = Convert.ToInt64(row["Id"]),
                    Module = row["Module"]?.ToString()
                });
            };

            var hint = new System.Windows.Forms.Label
            {
                Text = "Double-click a row to open the document.",
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 24,
                ForeColor = UITheme.TextGray,
                Padding = new System.Windows.Forms.Padding(8, 4, 0, 0)
            };
            tab.Controls.Add(grid);
            tab.Controls.Add(hint);
            return tab;
        }

        private static DataTable CreateRelatedTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Document Type", typeof(string));
            dt.Columns.Add("Code", typeof(string));
            dt.Columns.Add("Id", typeof(long));
            dt.Columns.Add("Module", typeof(string));
            return dt;
        }

        private static void AddRow(DataTable table, string type, string code, long id, string module)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(code)) return;
            table.Rows.Add(type, code, id, module);
        }
    }
}
