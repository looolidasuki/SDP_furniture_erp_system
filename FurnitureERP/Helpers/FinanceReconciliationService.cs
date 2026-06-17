using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class FinanceReconciliationService
    {
        public static DataTable GetAccountsPayableOutstanding()
        {
            string sql = @"
                SELECT po.purchaseOrderCode AS 'PO Code',
                       s.supplierName AS 'Supplier',
                       cur.currencyCode AS 'Currency',
                       po.totalAmount AS 'PO Total',
                       COALESCE(settled.settled, 0) AS 'Paid',
                       GREATEST(po.totalAmount - COALESCE(settled.settled, 0), 0) AS 'Outstanding',
                       po.requestDeliveryDate AS 'Request Delivery',
                       po.status AS 'Status'
                FROM purchaseorder po
                LEFT JOIN Supplier s ON po.supplierID = s.supplierID
                LEFT JOIN Currency cur ON po.currencyID = cur.currencyID
                LEFT JOIN (
                    SELECT purchaseOrderID, SUM(payAmount) AS settled
                    FROM paymentvoucherpurchaseorder pvpo
                    INNER JOIN paymentvoucher pv ON pv.paymentVoucherID = pvpo.paymentVoucherID
                    WHERE pv.status <> 3
                    GROUP BY purchaseOrderID
                ) settled ON settled.purchaseOrderID = po.purchaseOrderID
                WHERE po.status NOT IN (7)
                  AND po.totalAmount > COALESCE(settled.settled, 0)
                ORDER BY po.requestDeliveryDate, po.purchaseOrderCode";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public static DataTable GetAccountsReceivableOutstanding()
        {
            string sql = @"
                SELECT i.invoiceCode AS 'Invoice Code',
                       c.customerName AS 'Customer',
                       cur.currencyCode AS 'Currency',
                       i.totalAmount AS 'Invoice Total',
                       COALESCE(received.received, 0) AS 'Received',
                       GREATEST(i.totalAmount - COALESCE(received.received, 0), 0) AS 'Outstanding',
                       i.createDate AS 'Invoice Date',
                       i.status AS 'Status'
                FROM invoice i
                LEFT JOIN Customer c ON i.customerID = c.customerID
                LEFT JOIN Currency cur ON i.currencyID = cur.currencyID
                LEFT JOIN (
                    SELECT rvi.invoiceID, SUM(rvi.receivedAmount) AS received
                    FROM receiptvoucherinvoice rvi
                    INNER JOIN receiptvoucher rv ON rv.receiptVoucherID = rvi.receiptVoucherID
                    WHERE rv.status <> 2
                    GROUP BY rvi.invoiceID
                ) received ON received.invoiceID = i.invoiceID
                WHERE i.status NOT IN (4, 5)
                  AND i.totalAmount > COALESCE(received.received, 0)
                ORDER BY i.createDate DESC, i.invoiceCode";
            return DatabaseConnect.ExecuteQuery(sql);
        }
    }
}
