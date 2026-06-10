using System;
using System.Collections.Generic;
using FurnitureERP.Helpers;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Models;

namespace Sales_user.Controllers
{
    public class FinanceWorkflowService
    {
        public const int RefundStatusPaid = 2;

        private readonly DeliveryNoteController _deliveryCtrl = new DeliveryNoteController();
        private readonly InvoiceController _invoiceCtrl = new InvoiceController();
        private readonly ReceiptVoucherController _receiptCtrl = new ReceiptVoucherController();
        private readonly ProductController _productCtrl = new ProductController();

        public WorkflowResult CreateDepositInvoice(long customerId, long salesOrderId, long staffId, decimal depositAmount, string remark, int status = 0)
        {
            if (customerId <= 0 || salesOrderId <= 0 || staffId <= 0)
                return WorkflowResult.Fail("Customer, sales order and staff are required.");
            if (depositAmount <= 0)
                return WorkflowResult.Fail("Deposit amount must be greater than zero.");

            long depositProductId = _productCtrl.EnsureDepositProductId();
            long depositDeliveryNoteId = _deliveryCtrl.EnsureDepositDeliveryNoteId();
            var existingDeposit = _invoiceCtrl.GetActiveDepositInvoiceIdForSalesOrder(salesOrderId, depositProductId);
            if (existingDeposit.HasValue)
                return WorkflowResult.Fail("This sales order already has a deposit invoice. Edit the existing deposit invoice or void it first.");

            try
            {
                long invoiceId = DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    long invId = DatabaseConnect.InsertWithAllocatedId(conn, trans, "invoice", "invoiceID",
                        @"INSERT INTO Invoice (invoiceID, invoiceCode, customerID, salesOrderID, staffID, invoiceType, status, remark)
                          VALUES (@id, @code, @customerID, @soID, @staffID, @type, @status, @remark)",
                        new[]
                        {
                            new MySqlParameter("@code", "INV-TEMP"),
                            new MySqlParameter("@customerID", customerId),
                            new MySqlParameter("@soID", salesOrderId),
                            new MySqlParameter("@staffID", staffId),
                            new MySqlParameter("@type", InvoiceController.InvoiceTypeDeposit),
                            new MySqlParameter("@status", status),
                            new MySqlParameter("@remark", string.IsNullOrWhiteSpace(remark) ? (object)System.DBNull.Value : remark.Trim())
                        });

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE Invoice SET invoiceCode = @code WHERE invoiceID = @id",
                        new[]
                        {
                            new MySqlParameter("@code", DocumentCodeHelper.FormatInvoiceCode(invId)),
                            new MySqlParameter("@id", invId)
                        });

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        @"INSERT INTO InvoiceLine (invoiceID, deliveryNoteID, productID, invoiceQuantity, amount)
                          VALUES (@invId, @dnId, @pid, 1, @amount)",
                        new[]
                        {
                            new MySqlParameter("@invId", invId),
                            DepositDnParameter("@dnId", depositDeliveryNoteId),
                            new MySqlParameter("@pid", depositProductId),
                            new MySqlParameter("@amount", depositAmount)
                        });

                    return invId;
                });

                try
                {
                    _invoiceCtrl.SyncCurrencyFromSalesOrder(invoiceId, salesOrderId);
                    _invoiceCtrl.RefreshTotals(invoiceId);
                }
                catch (Exception syncEx)
                {
                    return WorkflowResult.Ok(invoiceId,
                        "Deposit invoice " + DocumentCodeHelper.FormatInvoiceCode(invoiceId) +
                        " created, but currency totals sync failed: " + syncEx.Message);
                }

                return WorkflowResult.Ok(invoiceId, "Deposit invoice " + DocumentCodeHelper.FormatInvoiceCode(invoiceId) + " created.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Deposit invoice creation failed: " + ex.Message);
            }
        }

        public WorkflowResult CreateInvoiceFromDelivery(long deliveryNoteId, long staffId, int invoiceType = InvoiceController.InvoiceTypeNormal, bool applyDepositOffset = false)
        {
            var delivery = _deliveryCtrl.GetById(deliveryNoteId);
            if (delivery == null)
                return WorkflowResult.Fail("Delivery note not found.");

            if (delivery.Status < 3)
                return WorkflowResult.Fail("Delivery must be completed before invoicing.");

            var lines = _deliveryCtrl.GetProductLinesInternal(deliveryNoteId);
            if (lines == null || lines.Rows.Count == 0)
                return WorkflowResult.Fail("Delivery note has no product lines.");

            var qtyMap = new Dictionary<long, int>();
            foreach (DataRow line in lines.Rows)
            {
                long productId = Convert.ToInt64(line["productID"]);
                int qty = Convert.ToInt32(line["shipQuantity"]);
                if (qty > 0) qtyMap[productId] = qty;
            }
            return CreateInvoiceFromDeliveryPartial(deliveryNoteId, staffId, qtyMap, invoiceType, applyDepositOffset);
        }

        public WorkflowResult CreateInvoiceFromDeliveryPartial(long deliveryNoteId, long staffId, IDictionary<long, int> productQty, int invoiceType = InvoiceController.InvoiceTypeNormal, bool applyDepositOffset = false)
        {
            var delivery = _deliveryCtrl.GetById(deliveryNoteId);
            if (delivery == null)
                return WorkflowResult.Fail("Delivery note not found.");

            if (delivery.Status < 3)
                return WorkflowResult.Fail("Delivery must be completed before invoicing.");

            if (invoiceType == InvoiceController.InvoiceTypeDeposit)
                return WorkflowResult.Fail("Deposit invoices cannot be created from delivery notes. Use New Invoice with type Deposit.");

            if (productQty == null || productQty.Count == 0)
                return WorkflowResult.Fail("At least one invoice line is required.");

            long depositProductId = _productCtrl.EnsureDepositProductId();
            long depositDeliveryNoteId = _deliveryCtrl.EnsureDepositDeliveryNoteId();
            decimal depositOffset = 0;
            if (applyDepositOffset)
            {
                var depositInvId = _invoiceCtrl.GetActiveDepositInvoiceIdForSalesOrder(delivery.SalesOrderID, depositProductId);
                if (!depositInvId.HasValue)
                    return WorkflowResult.Fail("No deposit invoice found for this sales order.");
                if (_invoiceCtrl.HasDepositOffsetOnNormalInvoice(delivery.SalesOrderID, depositProductId))
                    return WorkflowResult.Fail("Deposit has already been offset on a normal invoice for this sales order.");
                depositOffset = _invoiceCtrl.GetDepositLineAmount(depositInvId.Value, depositProductId);
                if (depositOffset <= 0)
                    return WorkflowResult.Fail("Deposit invoice has no deposit amount to offset.");
            }

            try
            {
                long invoiceId = DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    long invId = DatabaseConnect.InsertWithAllocatedId(conn, trans, "invoice", "invoiceID",
                        @"INSERT INTO Invoice (invoiceID, invoiceCode, customerID, salesOrderID, staffID, invoiceType, status, remark)
                          VALUES (@id, @code, @customerID, @soID, @staffID, @type, @status, @remark)",
                        new[]
                        {
                            new MySqlParameter("@code", "INV-TEMP"),
                            new MySqlParameter("@customerID", delivery.CustomerID),
                            new MySqlParameter("@soID", delivery.SalesOrderID),
                            new MySqlParameter("@staffID", staffId),
                            new MySqlParameter("@type", InvoiceController.InvoiceTypeNormal),
                            new MySqlParameter("@status", 0),
                            new MySqlParameter("@remark", "Generated from " + delivery.DeliveryNoteCode)
                        });

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE Invoice SET invoiceCode = @code WHERE invoiceID = @id",
                        new[]
                        {
                            new MySqlParameter("@code", DocumentCodeHelper.FormatInvoiceCode(invId)),
                            new MySqlParameter("@id", invId)
                        });

                    bool hasAny = false;
                    foreach (var kv in productQty)
                    {
                        long productId = kv.Key;
                        int qty = kv.Value;
                        if (qty <= 0) continue;

                        object remainingObj = DatabaseConnect.ExecuteScalar(conn, trans,
                            @"SELECT (dpl.shipQuantity - COALESCE(SUM(il.invoiceQuantity), 0))
                              FROM DeliveryProductLine dpl
                              LEFT JOIN InvoiceLine il
                                ON il.deliveryNoteID = dpl.deliveryNoteID AND il.productID = dpl.productID
                              WHERE dpl.deliveryNoteID = @dnId AND dpl.productID = @pid
                              GROUP BY dpl.shipQuantity",
                            new[]
                            {
                                new MySqlParameter("@dnId", deliveryNoteId),
                                new MySqlParameter("@pid", productId)
                            });
                        int remaining = remainingObj == null || remainingObj == System.DBNull.Value ? 0 : Convert.ToInt32(remainingObj);
                        if (qty > remaining)
                            throw new Exception("Invoice quantity exceeds remaining shipment qty for productID=" + productId + ". Remaining=" + remaining);

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
                        hasAny = true;
                    }

                    if (!hasAny)
                        throw new Exception("No invoice quantity selected.");

                    if (applyDepositOffset && depositOffset > 0)
                    {
                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"INSERT INTO InvoiceLine (invoiceID, deliveryNoteID, productID, invoiceQuantity, amount)
                              VALUES (@invId, @dnId, @pid, -1, @amount)",
                            new[]
                            {
                                new MySqlParameter("@invId", invId),
                                DepositDnParameter("@dnId", depositDeliveryNoteId),
                                new MySqlParameter("@pid", depositProductId),
                                new MySqlParameter("@amount", -depositOffset)
                            });
                    }

                    return invId;
                });

                _invoiceCtrl.SyncCurrencyFromSalesOrder(invoiceId, delivery.SalesOrderID);
                _invoiceCtrl.RefreshTotals(invoiceId);

                string msg = "Invoice " + DocumentCodeHelper.FormatInvoiceCode(invoiceId) + " created from delivery note.";
                if (applyDepositOffset)
                    msg += " Deposit offset applied.";
                return WorkflowResult.Ok(invoiceId, msg);
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

            var seenInvoices = new HashSet<long>();
            decimal totalAllocated = 0;
            foreach (var item in allocations)
            {
                if (item.ReceivedAmount <= 0)
                    return WorkflowResult.Fail("Allocated amount must be greater than zero on each line.");
                if (item.Type <= 0)
                    return WorkflowResult.Fail("Clearing type is required on each line.");

                if (ReceiptVoucherConstants.IsExchangeLoss(item.Type))
                {
                    if (item.InvoiceId.HasValue && item.InvoiceId.Value > 0)
                        return WorkflowResult.Fail("Exchange loss lines must not be linked to an invoice.");
                }
                else
                {
                    if (!item.InvoiceId.HasValue || item.InvoiceId.Value <= 0)
                        return WorkflowResult.Fail("Each invoice line must reference a valid invoice.");
                    if (!seenInvoices.Add(item.InvoiceId.Value))
                        return WorkflowResult.Fail("Duplicate invoice on allocation lines. Combine amounts into one line per invoice.");
                }

                totalAllocated += item.ReceivedAmount;
            }

            if (Math.Abs(totalAllocated - receipt.PaymentAmount) > 0.01m)
                return WorkflowResult.Fail(
                    "Total allocated (" + totalAllocated.ToString("N2") +
                    ") must equal receipt amount (" + receipt.PaymentAmount.ToString("N2") +
                    "). Add an exchange-loss line for any difference.");

            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "DELETE FROM ReceiptVoucherInvoice WHERE receiptVoucherID = @id",
                        new[] { new MySqlParameter("@id", receiptVoucherId) });

                    int insertLine = 0;
                    foreach (var item in allocations)
                    {
                        insertLine++;
                        object invoiceParam = item.InvoiceId.HasValue && item.InvoiceId.Value > 0
                            ? (object)item.InvoiceId.Value
                            : DBNull.Value;

                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"INSERT INTO ReceiptVoucherInvoice (receiptVoucherID, lineNo, invoiceID, receivedAmount, type)
                              VALUES (@rvId, @lineNo, @invId, @amount, @type)",
                            new[]
                            {
                                new MySqlParameter("@rvId", receiptVoucherId),
                                new MySqlParameter("@lineNo", insertLine),
                                new MySqlParameter("@invId", invoiceParam),
                                new MySqlParameter("@amount", item.ReceivedAmount),
                                new MySqlParameter("@type", item.Type)
                            });

                        if (item.InvoiceId.HasValue && item.InvoiceId.Value > 0)
                            UpdateInvoicePaymentStatus(conn, trans, item.InvoiceId.Value);
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
            return value == null || value == System.DBNull.Value ? 0 : Convert.ToDecimal(value);
        }

        public WorkflowResult RecalculateInvoicePaymentStatus(long invoiceId)
        {
            if (invoiceId <= 0)
                return WorkflowResult.Fail("Invalid invoice.");
            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    UpdateInvoicePaymentStatus(conn, trans, invoiceId);
                    return 0L;
                });
                return WorkflowResult.Ok(invoiceId, "Invoice payment status recalculated.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Failed to recalculate invoice status: " + ex.Message);
            }
        }

        public WorkflowResult ApplyRefundPaidSettlement(RefundRequest refund)
        {
            if (refund == null || !refund.InvoiceID.HasValue || refund.InvoiceID.Value <= 0)
                return WorkflowResult.Fail("Refund must be linked to an invoice.");
            if (refund.Status != RefundStatusPaid)
                return WorkflowResult.Ok(refund.RefundRequestID, "Refund is not in Paid status.");

            decimal grossReceived = GetGrossReceivedAmount(refund.InvoiceID.Value);
            decimal refundsPaid = GetRefundsPaidTotal(refund.InvoiceID.Value, refund.RefundRequestID);
            if (refund.RefundAmount + refundsPaid > grossReceived + 0.01m)
                return WorkflowResult.Fail(
                    "Refund total (" + (refundsPaid + refund.RefundAmount).ToString("N2") +
                    ") exceeds verified receipts on this invoice (" + grossReceived.ToString("N2") + ").");

            return RecalculateInvoicePaymentStatus(refund.InvoiceID.Value);
        }

        private static decimal GetGrossReceivedAmount(long invoiceId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(rvi.receivedAmount), 0)
                  FROM ReceiptVoucherInvoice rvi
                  INNER JOIN ReceiptVoucher rv ON rvi.receiptVoucherID = rv.receiptVoucherID
                  WHERE rvi.invoiceID = @invId AND rvi.invoiceID IS NOT NULL AND rv.status = 1",
                new[] { new MySqlParameter("@invId", invoiceId) });
            return value == null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        }

        private static decimal GetRefundsPaidTotal(long invoiceId, long excludeRefundRequestId = 0)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(refundAmount), 0)
                  FROM RefundRequest
                  WHERE InvoiceID = @invId AND status = @paid
                    AND (@excludeId = 0 OR refundRequestID <> @excludeId)",
                new[]
                {
                    new MySqlParameter("@invId", invoiceId),
                    new MySqlParameter("@paid", RefundStatusPaid),
                    new MySqlParameter("@excludeId", excludeRefundRequestId)
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
                  WHERE rvi.invoiceID = @invId AND rvi.invoiceID IS NOT NULL AND rv.status = 1",
                new[] { new MySqlParameter("@invId", invoiceId) });
            decimal grossPaid = Convert.ToDecimal(paidObj);

            object refundObj = DatabaseConnect.ExecuteScalar(conn, trans,
                @"SELECT COALESCE(SUM(refundAmount), 0)
                  FROM RefundRequest
                  WHERE InvoiceID = @invId AND status = @paid",
                new[]
                {
                    new MySqlParameter("@invId", invoiceId),
                    new MySqlParameter("@paid", RefundStatusPaid)
                });
            decimal refundsPaid = Convert.ToDecimal(refundObj);

            decimal netPaid = grossPaid - refundsPaid;
            if (netPaid < 0) netPaid = 0;

            int status = netPaid <= 0 ? 0 : (netPaid >= invoiceTotal - 0.01m ? 2 : 1);
            DatabaseConnect.ExecuteNonQuery(conn, trans,
                "UPDATE Invoice SET status = @status, lastModifyDate = NOW() WHERE invoiceID = @id",
                new[]
                {
                    new MySqlParameter("@status", status),
                    new MySqlParameter("@id", invoiceId)
                });
        }

        private static MySqlParameter DepositDnParameter(string name, long depositDeliveryNoteId) =>
            new MySqlParameter(name, MySqlDbType.Int64) { Value = depositDeliveryNoteId };
    }

    public class ReceiptAllocation
    {
        public long? InvoiceId { get; set; }
        public decimal ReceivedAmount { get; set; }
        public int Type { get; set; } = ReceiptVoucherConstants.ClearingPartial;
    }
}
