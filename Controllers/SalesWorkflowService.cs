using System;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using FurnitureERP.Helpers;

namespace Sales_user.Controllers
{
    public class SalesWorkflowService
    {
        /// <summary>Default finished-goods warehouse (seed: Kowloon Main WH).</summary>
        public const long DefaultFinishedGoodsWarehouseId = 1;

        private readonly QuotationController _quotationCtrl = new QuotationController();
        private readonly SalesOrderController _salesOrderCtrl = new SalesOrderController();
        private readonly CustomerController _customerCtrl = new CustomerController();
        private readonly ProductionOrderController _productionCtrl = new ProductionOrderController();

        public WorkflowResult ConvertQuotationToSalesOrder(long quotationId, long staffId)
        {
            var quotation = _quotationCtrl.GetById(quotationId);
            if (quotation == null)
                return WorkflowResult.Fail("Quotation not found.");

            if (quotation.Status == 4)
                return WorkflowResult.Fail("Quotation has already been converted to a sales order.");

            if (quotation.Status == 3)
                return WorkflowResult.Fail("Rejected quotations cannot be converted.");

            var lines = _quotationCtrl.GetProductLinesInternal(quotationId);
            if (lines == null || lines.Rows.Count == 0)
                return WorkflowResult.Fail("Quotation has no product lines.");

            string deliveryAddress = ResolveDeliveryAddress(quotation.CustomerID);

            try
            {
                var salesOrder = new SalesOrder
                {
                    SalesOrderCode = "SO-TEMP",
                    CustomerID = quotation.CustomerID,
                    StaffID = staffId,
                    CurrencyCurrencyID = quotation.CurrencyID,
                    DeliveryAddress = deliveryAddress,
                    Discount = 0,
                    Status = 0,
                    Remark = "Converted from " + quotation.QuotationCode
                };

                long salesOrderId = DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    long soId = _salesOrderCtrl.InsertInTransaction(conn, trans, salesOrder);
                    _salesOrderCtrl.UpdateCodeAfterInsertInTransaction(conn, trans, soId);

                    foreach (DataRow row in lines.Rows)
                    {
                        long productId = Convert.ToInt64(row["productID"]);
                        decimal price = Convert.ToDecimal(row["price"]);
                        decimal qty = Convert.ToDecimal(row["quantity"]);
                        decimal discount = row["discountAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["discountAmount"]);
                        _salesOrderCtrl.InsertProductLineInTransaction(conn, trans, soId, productId, price, qty, discount);
                    }

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE Quotation SET status = 4, lastModifyDate = NOW() WHERE quotationID = @id",
                        new[] { new MySqlParameter("@id", quotationId) });

                    return soId;
                });

                if (salesOrderId > 0)
                {
                    string soCode = DocumentCodeHelper.Build("SO", salesOrderId);
                    DocumentAuditService.LogAction(DocumentAuditService.Types.SalesOrder, salesOrderId, soCode,
                        DocumentAuditService.Actions.Convert, "Converted from " + quotation.QuotationCode);
                    DocumentAuditService.LogStatus(DocumentAuditService.Types.Quotation, quotationId,
                        quotation.QuotationCode, 4, "Converted to " + soCode);
                }

                return WorkflowResult.Ok(salesOrderId, "Sales order SO-" + salesOrderId + " created from quotation.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Convert failed: " + ex.Message);
            }
        }

        public WorkflowResult ValidateSalesOrderStatus(long salesOrderId, int newStatus)
        {
            var order = _salesOrderCtrl.GetFullById(salesOrderId);
            if (order == null)
                return WorkflowResult.Fail("Sales order not found.");

            string error = ValidateSalesOrderTransition(order.Status, newStatus);
            if (error != null)
                return WorkflowResult.Fail(error);

            return WorkflowResult.Ok(salesOrderId, "Status change is valid.");
        }

        public WorkflowResult ConfirmSalesOrder(long salesOrderId, long warehouseId = DefaultFinishedGoodsWarehouseId)
        {
            var order = _salesOrderCtrl.GetFullById(salesOrderId);
            if (order == null)
                return WorkflowResult.Fail("Sales order not found.");

            if (order.Status != 0)
                return WorkflowResult.Fail("Only draft sales orders can be confirmed.");

            var lines = _salesOrderCtrl.GetProductLinesInternal(salesOrderId);
            if (lines == null || lines.Rows.Count == 0)
                return WorkflowResult.Fail("Sales order has no product lines.");

            try
            {
                DatabaseConnect.ExecuteInTransaction<int>((conn, trans) =>
                {
                    foreach (DataRow row in lines.Rows)
                    {
                        long productId = Convert.ToInt64(row["productID"]);
                        decimal orderQty = Convert.ToDecimal(row["orderQuantity"]);
                        int reserveQty = GetReserveQtyForLine(conn, trans, warehouseId, productId, orderQty);

                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"UPDATE SalesOrderProductLine
                              SET warehouseReservedQty = @reserved
                              WHERE salesOrderID = @soID AND productID = @productID",
                            new[]
                            {
                                new MySqlParameter("@reserved", reserveQty),
                                new MySqlParameter("@soID", salesOrderId),
                                new MySqlParameter("@productID", productId)
                            });

                        if (reserveQty > 0)
                        {
                            int updated = DatabaseConnect.ExecuteNonQuery(conn, trans,
                                @"UPDATE WarehouseProduct
                                  SET reservedQuantity = reservedQuantity + @qty
                                  WHERE warehouseID = @wh AND productID = @productID",
                                new[]
                                {
                                    new MySqlParameter("@qty", reserveQty),
                                    new MySqlParameter("@wh", warehouseId),
                                    new MySqlParameter("@productID", productId)
                                });
                            if (updated == 0)
                                throw new InvalidOperationException(
                                    "Product " + productId + " is not stocked in warehouse " + warehouseId + ".");
                        }
                    }

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE SalesOrder SET status = 1, lastModifyDate = NOW() WHERE salesOrderID = @id",
                        new[] { new MySqlParameter("@id", salesOrderId) });
                    return 0;
                });

                DocumentAuditService.Log(
                    DocumentAuditService.Types.SalesOrder,
                    salesOrderId,
                    order.SalesOrderCode,
                    "Confirm",
                    "Sales order confirmed");

                return WorkflowResult.Ok(salesOrderId, "Sales order confirmed and warehouse stock reserved where available.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Confirm failed: " + ex.Message);
            }
        }

        public WorkflowResult CreateProductionFromSalesOrder(long salesOrderId, long staffId, DateTime estFinishDate)
        {
            var order = _salesOrderCtrl.GetFullById(salesOrderId);
            if (order == null)
                return WorkflowResult.Fail("Sales order not found.");

            if (order.Status < 1)
                return WorkflowResult.Fail("Sales order must be confirmed before creating a production order.");

            try
            {
                long productionId = _productionCtrl.CreateFromSalesOrder(
                    salesOrderId, staffId, estFinishDate,
                    "Generated from " + order.SalesOrderCode,
                    advanceSalesOrderToProcessing: order.Status == 1);

                return WorkflowResult.Ok(productionId, "Production order PO-" + productionId + " created.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Production creation failed: " + ex.Message);
            }
        }

        private static int GetReserveQtyForLine(
            MySql.Data.MySqlClient.MySqlConnection conn,
            MySql.Data.MySqlClient.MySqlTransaction trans,
            long warehouseId,
            long productId,
            decimal orderQty)
        {
            object availableObj = DatabaseConnect.ExecuteScalar(conn, trans,
                @"SELECT COALESCE(GREATEST(physicalQuantity - reservedQuantity, 0), 0)
                  FROM WarehouseProduct
                  WHERE warehouseID = @wh AND productID = @productID",
                new[]
                {
                    new MySqlParameter("@wh", warehouseId),
                    new MySqlParameter("@productID", productId)
                });

            decimal available = availableObj == null || availableObj == DBNull.Value
                ? 0
                : Convert.ToDecimal(availableObj);

            int reserveQty = (int)Math.Min(Math.Floor(orderQty), Math.Floor(available));
            return Math.Max(0, reserveQty);
        }

        private string ResolveDeliveryAddress(long customerId)
        {
            var addresses = _customerCtrl.GetDeliveryAddresses(customerId);
            if (addresses != null && addresses.Count > 0 && !string.IsNullOrWhiteSpace(addresses[0].DeliveryAddress))
                return addresses[0].DeliveryAddress;

            var customer = _customerCtrl.GetById(customerId);
            if (customer != null && !string.IsNullOrWhiteSpace(customer.BillingAddress))
                return customer.BillingAddress;

            return "TBD";
        }

        private static string ValidateSalesOrderTransition(int fromStatus, int toStatus)
        {
            const string category = "SALES_ORDER_STATUS";
            if (fromStatus == toStatus) return null;

            var transitions = new[]
            {
                new[] { 0, 1, 5 },
                new[] { 1, 2, 5 },
                new[] { 2, 3 },
                new[] { 3, 4 },
                new[] { 4 },
                new[] { 5 }
            };

            if (fromStatus < 0 || fromStatus >= transitions.Length)
                return "Invalid current sales order status.";

            foreach (int allowed in transitions[fromStatus])
            {
                if (allowed == toStatus) return null;
            }

            return "Invalid sales order status transition from " + fromStatus + " to " + toStatus + ".";
        }
    }
}
