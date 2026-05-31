using System;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Models;

namespace Sales_user.Controllers
{
    public class SalesWorkflowService
    {
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
                long salesOrderId = DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    long soId = DatabaseConnect.ExecuteInsertReturnId(conn, trans,
                        @"INSERT INTO SalesOrder
                            (salesOrderCode, customerID, staffID, currencyCurrencyID, deliveryAddress,
                             discountType, discount, status, remark)
                          VALUES (@code, @customerID, @staffID, @currencyID, @address,
                                  NULL, 0, @status, @remark)",
                        new[]
                        {
                            new MySqlParameter("@code", "SO-TEMP"),
                            new MySqlParameter("@customerID", quotation.CustomerID),
                            new MySqlParameter("@staffID", staffId),
                            new MySqlParameter("@currencyID", quotation.CurrencyID),
                            new MySqlParameter("@address", deliveryAddress),
                            new MySqlParameter("@status", 0),
                            new MySqlParameter("@remark", "Converted from " + quotation.QuotationCode)
                        });

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE SalesOrder SET salesOrderCode = @code WHERE salesOrderID = @id",
                        new[]
                        {
                            new MySqlParameter("@code", "SO-" + soId),
                            new MySqlParameter("@id", soId)
                        });

                    foreach (DataRow row in lines.Rows)
                    {
                        long productId = Convert.ToInt64(row["productID"]);
                        decimal price = Convert.ToDecimal(row["price"]);
                        decimal qty = Convert.ToDecimal(row["quantity"]);
                        decimal discount = row["discountAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["discountAmount"]);

                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"INSERT INTO SalesOrderProductLine
                                (salesOrderID, productID, price, orderQuantity, discountAmount)
                              VALUES (@soID, @productID, @price, @qty, @discount)",
                            new[]
                            {
                                new MySqlParameter("@soID", soId),
                                new MySqlParameter("@productID", productId),
                                new MySqlParameter("@price", price),
                                new MySqlParameter("@qty", qty),
                                new MySqlParameter("@discount", discount)
                            });
                    }

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE Quotation SET status = 4, lastModifyDate = NOW() WHERE quotationID = @id",
                        new[] { new MySqlParameter("@id", quotationId) });

                    return soId;
                });

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

        public WorkflowResult CreateProductionFromSalesOrder(long salesOrderId, long staffId, DateTime estFinishDate)
        {
            var order = _salesOrderCtrl.GetFullById(salesOrderId);
            if (order == null)
                return WorkflowResult.Fail("Sales order not found.");

            if (order.Status < 1)
                return WorkflowResult.Fail("Sales order must be confirmed before creating a production order.");

            var lines = _salesOrderCtrl.GetProductLinesInternal(salesOrderId);
            if (lines == null || lines.Rows.Count == 0)
                return WorkflowResult.Fail("Sales order has no product lines.");

            try
            {
                long productionId = DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    long poId = DatabaseConnect.ExecuteInsertReturnId(conn, trans,
                        @"INSERT INTO ProductionOrder
                            (productionOrderCode, salesOrderID, staffID, estFinishDate, status, remark)
                          VALUES (@code, @soID, @staffID, @finish, @status, @remark)",
                        new[]
                        {
                            new MySqlParameter("@code", "PO-TEMP"),
                            new MySqlParameter("@soID", salesOrderId),
                            new MySqlParameter("@staffID", staffId),
                            new MySqlParameter("@finish", estFinishDate),
                            new MySqlParameter("@status", 0),
                            new MySqlParameter("@remark", "Generated from " + order.SalesOrderCode)
                        });

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE ProductionOrder SET productionOrderCode = @code WHERE productionOrderID = @id",
                        new[]
                        {
                            new MySqlParameter("@code", "PO-" + poId),
                            new MySqlParameter("@id", poId)
                        });

                    bool hasLines = false;
                    foreach (DataRow row in lines.Rows)
                    {
                        decimal orderQty = Convert.ToDecimal(row["orderQuantity"]);
                        int reserved = row["warehouseReservedQty"] == DBNull.Value ? 0 : Convert.ToInt32(row["warehouseReservedQty"]);
                        int productionQty = (int)Math.Max(0, orderQty - reserved);
                        if (productionQty <= 0) continue;

                        long productId = Convert.ToInt64(row["productID"]);
                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"INSERT INTO ProductionOrderProductLine (ProductionOrderID, productID, productionQty)
                              VALUES (@poID, @productID, @qty)",
                            new[]
                            {
                                new MySqlParameter("@poID", poId),
                                new MySqlParameter("@productID", productId),
                                new MySqlParameter("@qty", productionQty)
                            });
                        hasLines = true;
                    }

                    if (!hasLines)
                        throw new InvalidOperationException("No production quantity required for this sales order.");

                    if (order.Status == 1)
                    {
                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            "UPDATE SalesOrder SET status = 2, lastModifyDate = NOW() WHERE salesOrderID = @id",
                            new[] { new MySqlParameter("@id", salesOrderId) });
                    }

                    return poId;
                });

                return WorkflowResult.Ok(productionId, "Production order PO-" + productionId + " created.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Production creation failed: " + ex.Message);
            }
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
