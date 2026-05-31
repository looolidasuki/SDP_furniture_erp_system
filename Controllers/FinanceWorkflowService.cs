using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Models;

namespace Sales_user.Controllers
{
    public class FinanceWorkflowService
    {
        private readonly DeliveryNoteController _deliveryCtrl = new DeliveryNoteController();
        private readonly InvoiceController _invoiceCtrl = new InvoiceController();
        private readonly ReceiptVoucherController _receiptCtrl = new ReceiptVoucherController();

        public WorkflowResult CreateInvoiceFromDelivery(long deliveryNoteId, long staffId, int invoiceType = 1)
        {
            var delivery = _deliveryCtrl.GetById(deliveryNoteId);
            if (delivery == null)
                return WorkflowResult.Fail("Delivery note not found.");

            if (delivery.Status < 3)
                return WorkflowResult.Fail("Delivery must be completed before invoicing.");

            var lines = _deliveryCtrl.GetProductLinesInternal(deliveryNoteId);
            if (lines == null || lines.Rows.Count == 0)
                return WorkflowResult.Fail("Delivery note has no product lines.");

            try
            {
                long invoiceId = DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    long invId = DatabaseConnect.ExecuteInsertReturnId(conn, trans,
                        @"INSERT INTO Invoice (invoiceCode, customerID, salesOrderID, staffID, invoiceType, status, remark)
                          VALUES (@code, @customerID, @soID, @staffID, @type, @status, @remark)",
                        new[]
                        {
                            new MySqlParameter("@code", "INV-TEMP"),
                            new MySqlParameter("@customerID", delivery.CustomerID),
                            new MySqlParameter("@soID", delivery.SalesOrderID),
                            new MySqlParameter("@staffID", staffId),
                            new MySqlParameter("@type", invoiceType),
                            new MySqlParameter("@status", 0),
                            new MySqlParameter("@remark", "Generated from " + delivery.DeliveryNoteCode)
                        });

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE Invoice SET invoiceCode = @code WHERE invoiceID = @id",
                        new[]
                        {
                            new MySqlParameter("@code", "INV-" + invId),
                            new MySqlParameter("@id", invId)
                        });

                    foreach (DataRow line in lines.Rows)
                    {
                        long productId = Convert.ToInt64(line["productID"]);
                        int qty = Convert.ToInt32(line["shipQuantity"]);
                        decimal price = GetSalesOrderPrice(conn, trans, delivery.SalesOrderID, productId);
                        decimal amount = price * qty;

                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"INSERT INTO InvoiceLine (invoiceID, deliveryNoteID, productID, invoiceQuantity, amount)
                              VALUES (@invId, @dnId, @productId, @qty, @amount)",
                            new[]
                            {
                                new MySqlParameter("@invId", invId),
                                new MySqlParameter("@dnId", deliveryNoteId),
                                new MySqlParameter("@productId", productId),
                                new MySqlParameter("@qty", qty),
                                new MySqlParameter("@amount", amount)
                            });

                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"UPDATE SalesOrderProductLine
                              SET invoicedQuantity = invoicedQuantity + @qty
                              WHERE salesOrderID = @soId AND productID = @productId",
                            new[]
                            {
                                new MySqlParameter("@qty", qty),
                                new MySqlParameter("@soId", delivery.SalesOrderID),
                                new MySqlParameter("@productId", productId)
                            });
                    }

                    return invId;
                });

                return WorkflowResult.Ok(invoiceId, "Invoice INV-" + invoiceId + " created from delivery note.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Invoice creation failed: " + ex.Message);
            }
        }

        public WorkflowResult ConfirmReceiptWithAllocations(long receiptVoucherId, IList<ReceiptAllocation> allocations)
        {
            var receipt = _receiptCtrl.GetById(receiptVoucherId);
            if (receipt == null)
                return WorkflowResult.Fail("Receipt voucher not found.");

            if (receipt.Status == 1)
                return WorkflowResult.Fail("Receipt voucher is already verified.");

            if (allocations == null || allocations.Count == 0)
                return WorkflowResult.Fail("At least one invoice allocation is required.");

            decimal totalAllocated = 0;
            foreach (var item in allocations)
                totalAllocated += item.ReceivedAmount;

            if (Math.Abs(totalAllocated - receipt.PaymentAmount) > 0.01m)
                return WorkflowResult.Fail("Allocated amount must equal receipt amount (" + receipt.PaymentAmount.ToString("N2") + ").");

            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "DELETE FROM ReceiptVoucherInvoice WHERE receiptVoucherID = @id",
                        new[] { new MySqlParameter("@id", receiptVoucherId) });

                    foreach (var item in allocations)
                    {
                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"INSERT INTO ReceiptVoucherInvoice (receiptVoucherID, invoiceID, receivedAmount, type)
                              VALUES (@rvId, @invId, @amount, @type)",
                            new[]
                            {
                                new MySqlParameter("@rvId", receiptVoucherId),
                                new MySqlParameter("@invId", item.InvoiceId),
                                new MySqlParameter("@amount", item.ReceivedAmount),
                                new MySqlParameter("@type", item.Type)
                            });

                        UpdateInvoicePaymentStatus(conn, trans, item.InvoiceId);
                    }

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE ReceiptVoucher SET status = 1, lastModifyDate = NOW() WHERE receiptVoucherID = @id",
                        new[] { new MySqlParameter("@id", receiptVoucherId) });

                    return 0L;
                });

                return WorkflowResult.Ok(receiptVoucherId, "Receipt voucher verified and invoices updated.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Receipt verification failed: " + ex.Message);
            }
        }

        private static decimal GetSalesOrderPrice(MySqlConnection conn, MySqlTransaction trans, long salesOrderId, long productId)
        {
            object value = DatabaseConnect.ExecuteScalar(conn, trans,
                "SELECT price FROM SalesOrderProductLine WHERE salesOrderID = @soId AND productID = @productId",
                new[]
                {
                    new MySqlParameter("@soId", salesOrderId),
                    new MySqlParameter("@productId", productId)
                });
            return value == null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        }

        private static void UpdateInvoicePaymentStatus(MySqlConnection conn, MySqlTransaction trans, long invoiceId)
        {
            object invoiceTotalObj = DatabaseConnect.ExecuteScalar(conn, trans,
                "SELECT COALESCE(SUM(amount), 0) FROM InvoiceLine WHERE invoiceID = @id",
                new[] { new MySqlParameter("@id", invoiceId) });
            decimal invoiceTotal = Convert.ToDecimal(invoiceTotalObj);

            object paidObj = DatabaseConnect.ExecuteScalar(conn, trans,
                @"SELECT COALESCE(SUM(rvi.receivedAmount), 0)
                  FROM ReceiptVoucherInvoice rvi
                  INNER JOIN ReceiptVoucher rv ON rvi.receiptVoucherID = rv.receiptVoucherID
                  WHERE rvi.invoiceID = @invId AND rv.status = 1",
                new[] { new MySqlParameter("@invId", invoiceId) });
            decimal paid = Convert.ToDecimal(paidObj);

            int status = paid <= 0 ? 0 : (paid >= invoiceTotal ? 2 : 1);
            DatabaseConnect.ExecuteNonQuery(conn, trans,
                "UPDATE Invoice SET status = @status, lastModifyDate = NOW() WHERE invoiceID = @id",
                new[]
                {
                    new MySqlParameter("@status", status),
                    new MySqlParameter("@id", invoiceId)
                });
        }
    }

    public class ReceiptAllocation
    {
        public long InvoiceId { get; set; }
        public decimal ReceivedAmount { get; set; }
        public int Type { get; set; } = 2;
    }
}
