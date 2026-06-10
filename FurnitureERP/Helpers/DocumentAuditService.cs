using System;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class DocumentAuditService
    {
        public static class Types
        {
            public const string SalesOrder = "Sales Order";
            public const string PurchaseOrder = "Purchase Order";
            public const string Invoice = "Invoice";
            public const string PaymentVoucher = "Payment Voucher";
            public const string ReceiptVoucher = "Receipt Voucher";
            public const string Refund = "Refund";
            public const string DeliveryNote = "Delivery Note";
        }

        public static void Log(string documentType, long documentId, string documentCode, string action, string summary = null)
        {
            if (documentId <= 0 || string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(action))
                return;

            try
            {
                long staffId = AppSession.IsLoggedIn && AppSession.CurrentUser != null
                    ? AppSession.CurrentUser.StaffID : 0;
                string staffName = AppSession.IsLoggedIn && AppSession.CurrentUser != null
                    ? AppSession.CurrentUser.FullName : "System";

                DatabaseConnect.ExecuteNonQuery(
                    @"INSERT INTO documentauditlog
                      (documentType, documentId, documentCode, action, staffID, staffName, summary, actionDate)
                      VALUES (@type, @id, @code, @action, @staffId, @staffName, @summary, NOW())",
                    new[]
                    {
                        new MySqlParameter("@type", documentType.Trim()),
                        new MySqlParameter("@id", documentId),
                        new MySqlParameter("@code", string.IsNullOrWhiteSpace(documentCode) ? (object)DBNull.Value : documentCode.Trim()),
                        new MySqlParameter("@action", action.Trim()),
                        new MySqlParameter("@staffId", staffId > 0 ? (object)staffId : DBNull.Value),
                        new MySqlParameter("@staffName", staffName ?? "System"),
                        new MySqlParameter("@summary", string.IsNullOrWhiteSpace(summary) ? (object)DBNull.Value : summary.Trim())
                    });
            }
            catch
            {
                // Audit is best-effort; must not break business operations.
            }
        }

        public static DataTable GetForDocument(string documentType, long documentId, int maxRows = 50)
        {
            var empty = new DataTable();
            empty.Columns.Add("Action", typeof(string));
            empty.Columns.Add("Staff", typeof(string));
            empty.Columns.Add("Date", typeof(string));
            empty.Columns.Add("Summary", typeof(string));

            if (documentId <= 0 || string.IsNullOrWhiteSpace(documentType))
                return empty;

            try
            {
                const string sql = @"SELECT action AS 'Action',
                                            COALESCE(staffName, '—') AS 'Staff',
                                            DATE_FORMAT(actionDate, '%Y-%m-%d %H:%i') AS 'Date',
                                            COALESCE(summary, '') AS 'Summary'
                                     FROM documentauditlog
                                     WHERE documentType = @type AND documentId = @id
                                     ORDER BY actionDate DESC, auditLogID DESC
                                     LIMIT @max";
                var dt = DatabaseConnect.ExecuteQuery(sql, new[]
                {
                    new MySqlParameter("@type", documentType),
                    new MySqlParameter("@id", documentId),
                    new MySqlParameter("@max", maxRows)
                });
                return dt ?? empty;
            }
            catch
            {
                return empty;
            }
        }

        public static System.Windows.Forms.TabPage BuildActivityTab(string documentType, long documentId)
        {
            var tab = new System.Windows.Forms.TabPage("Activity");
            var grid = GridHelper.CreateStyledGrid();
            grid.ReadOnly = true;
            grid.DataSource = GetForDocument(documentType, documentId);
            GridHelper.StyleGrid(grid);
            grid.Dock = System.Windows.Forms.DockStyle.Fill;

            var hint = new System.Windows.Forms.Label
            {
                Text = "Recent changes logged for this document.",
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 24,
                ForeColor = UITheme.TextGray,
                Padding = new System.Windows.Forms.Padding(8, 4, 0, 0)
            };
            tab.Controls.Add(grid);
            tab.Controls.Add(hint);
            return tab;
        }
    }
}
