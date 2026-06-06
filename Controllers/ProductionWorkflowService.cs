using System;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Models;

namespace Sales_user.Controllers
{
    public class ProductionWorkflowService
    {
        public const int StatusCompleted = 3;

        private readonly ProductionOrderController _productionCtrl = new ProductionOrderController();

        public WorkflowResult CompleteProductionOrder(
            long productionOrderId,
            long warehouseId = SalesWorkflowService.DefaultFinishedGoodsWarehouseId)
        {
            if (warehouseId <= 0)
                return WorkflowResult.Fail("A valid warehouse is required.");

            var order = _productionCtrl.GetById(productionOrderId);
            if (order == null)
                return WorkflowResult.Fail("Production order not found.");

            if (order.Status >= StatusCompleted)
                return WorkflowResult.Fail("This production order has already been completed and stocked in.");

            var lines = _productionCtrl.GetProductLinesInternal(productionOrderId);
            if (lines == null || lines.Rows.Count == 0)
                return WorkflowResult.Fail("Production order has no product lines.");

            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    foreach (DataRow row in lines.Rows)
                    {
                        long productId = Convert.ToInt64(row["productID"]);
                        int productionQty = Convert.ToInt32(row["productionQty"]);
                        if (productionQty <= 0) continue;

                        InventoryWorkflowService.UpsertProductStock(conn, trans, productId, warehouseId, productionQty);
                        if (!InternalSampleProductionService.IsInternalSampleSalesOrder(order.SalesOrderID))
                        {
                            TryReserveProducedStockForSalesOrder(
                                conn, trans, order.SalesOrderID, warehouseId, productId, productionQty);
                        }
                    }

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        @"UPDATE ProductionOrder
                          SET status = @status, lastModifyDate = NOW()
                          WHERE productionOrderID = @id",
                        new[]
                        {
                            new MySqlParameter("@status", StatusCompleted),
                            new MySqlParameter("@id", productionOrderId)
                        });

                    return 0L;
                });

                bool isSample = InternalSampleProductionService.IsInternalSampleSalesOrder(order.SalesOrderID);
                return WorkflowResult.Ok(
                    productionOrderId,
                    isSample
                        ? "Sample production completed. Finished goods received into warehouse (not reserved for sales)."
                        : "Production completed. Finished goods received into warehouse and reserved for the sales order where applicable.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Production completion failed: " + ex.Message);
            }
        }

        private static void TryReserveProducedStockForSalesOrder(
            MySqlConnection conn,
            MySqlTransaction trans,
            long salesOrderId,
            long warehouseId,
            long productId,
            int producedQty)
        {
            if (salesOrderId <= 0 || producedQty <= 0)
                return;

            DataTable soLine = DatabaseConnect.ExecuteQuery(conn, trans,
                @"SELECT orderQuantity, warehouseReservedQty
                  FROM SalesOrderProductLine
                  WHERE salesOrderID = @soId AND productID = @productId",
                new[]
                {
                    new MySqlParameter("@soId", salesOrderId),
                    new MySqlParameter("@productId", productId)
                });

            if (soLine == null || soLine.Rows.Count == 0)
                return;

            decimal orderQty = Convert.ToDecimal(soLine.Rows[0]["orderQuantity"]);
            int reserved = soLine.Rows[0]["warehouseReservedQty"] == DBNull.Value
                ? 0
                : Convert.ToInt32(soLine.Rows[0]["warehouseReservedQty"]);
            int remainingNeed = (int)Math.Max(0, Math.Floor(orderQty) - reserved);
            int reserveNow = Math.Min(producedQty, remainingNeed);
            if (reserveNow <= 0)
                return;

            int updated = DatabaseConnect.ExecuteNonQuery(conn, trans,
                @"UPDATE WarehouseProduct
                  SET reservedQuantity = reservedQuantity + @qty
                  WHERE warehouseID = @wh AND productID = @productId",
                new[]
                {
                    new MySqlParameter("@qty", reserveNow),
                    new MySqlParameter("@wh", warehouseId),
                    new MySqlParameter("@productId", productId)
                });

            if (updated <= 0)
                return;

            DatabaseConnect.ExecuteNonQuery(conn, trans,
                @"UPDATE SalesOrderProductLine
                  SET warehouseReservedQty = warehouseReservedQty + @qty
                  WHERE salesOrderID = @soId AND productID = @productId",
                new[]
                {
                    new MySqlParameter("@qty", reserveNow),
                    new MySqlParameter("@soId", salesOrderId),
                    new MySqlParameter("@productId", productId)
                });
        }
    }
}
