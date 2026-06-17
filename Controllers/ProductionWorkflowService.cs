using System;
using System.Data;
using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;

namespace Sales_user.Controllers
{
    public class ProductionWorkflowService
    {
        public const int StatusCompleted = 3;

        private readonly ProductionOrderController _productionCtrl = new ProductionOrderController();
        private readonly ProductController _productCtrl = new ProductController();
        private readonly WarehouseController _warehouseCtrl = new WarehouseController();

        /// <summary>
        /// Completes production in the production warehouse, consumes raw materials there,
        /// then transfers finished goods to the inventory warehouse for delivery preparation.
        /// </summary>
        public WorkflowResult CompleteProductionOrder(
            long productionOrderId,
            long inventoryWarehouseId = WarehouseHelper.DefaultInventoryWarehouseId)
        {
            var invWarehouse = _warehouseCtrl.GetById(inventoryWarehouseId);
            if (inventoryWarehouseId <= 0 || invWarehouse == null
                || !WarehouseHelper.IsInventoryWarehouse(inventoryWarehouseId, invWarehouse.WarehouseName))
                return WorkflowResult.Fail("Please select a valid inventory warehouse.");

            long productionWarehouseId = _warehouseCtrl.GetPairedProductionWarehouseId(inventoryWarehouseId);
            if (productionWarehouseId <= 0)
                return WorkflowResult.Fail("No paired production warehouse found for the selected inventory warehouse.");

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

                        InventoryWorkflowService.UpsertProductStock(
                            conn, trans, productId, productionWarehouseId, productionQty);

                        var bom = _productCtrl.GetBomLinesInternal(productId);
                        if (bom == null || bom.Rows.Count == 0)
                            throw new InvalidOperationException("Product ID " + productId + " has no BOM for material consumption.");

                        foreach (DataRow bomRow in bom.Rows)
                        {
                            long rmId = Convert.ToInt64(bomRow["rawMaterialID"]);
                            decimal needPerUnit = Convert.ToDecimal(bomRow["rawMaterialNeedQty"]);
                            decimal consumeQty = needPerUnit * productionQty;
                            if (consumeQty <= 0) continue;

                            InventoryWorkflowService.DeductRawMaterialStock(
                                conn, trans, rmId, productionWarehouseId, consumeQty);
                        }
                    }

                    foreach (DataRow row in lines.Rows)
                    {
                        long productId = Convert.ToInt64(row["productID"]);
                        int productionQty = Convert.ToInt32(row["productionQty"]);
                        if (productionQty <= 0) continue;

                        TransferFinishedGoodsInTransaction(
                            conn, trans, productionWarehouseId, inventoryWarehouseId, productId, productionQty);

                        if (!InternalSampleProductionService.IsInternalSampleSalesOrder(order.SalesOrderID))
                        {
                            TryReserveProducedStockForSalesOrder(
                                conn, trans, order.SalesOrderID, inventoryWarehouseId, productId, productionQty);
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

                DocumentAuditService.LogAction(DocumentAuditService.Types.ProductionOrder, productionOrderId,
                    order.ProductionOrderCode ?? DocumentCodeHelper.Build("PTO", productionOrderId),
                    DocumentAuditService.Actions.Complete, "Production completed and stocked in");

                bool isSample = InternalSampleProductionService.IsInternalSampleSalesOrder(order.SalesOrderID);
                return WorkflowResult.Ok(
                    productionOrderId,
                    isSample
                        ? "Production completed. Finished goods moved from production warehouse to inventory warehouse."
                        : "Production completed. Finished goods moved to inventory warehouse and reserved for the sales order where applicable.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Production completion failed: " + ex.Message);
            }
        }

        private static void TransferFinishedGoodsInTransaction(
            MySqlConnection conn,
            MySqlTransaction trans,
            long productionWarehouseId,
            long inventoryWarehouseId,
            long productId,
            decimal qty)
        {
            InventoryWorkflowService.DeductProductStock(conn, trans, productId, productionWarehouseId, qty,
                DocumentAuditService.Types.ProductionOrder, 0, null,
                InventoryLedgerService.Actions.ProductionOut,
                "Transfer finished goods to inventory warehouse " + inventoryWarehouseId);
            InventoryWorkflowService.UpsertProductStock(conn, trans, productId, inventoryWarehouseId, qty,
                DocumentAuditService.Types.ProductionOrder, 0, null,
                InventoryLedgerService.Actions.ProductionIn,
                "Received from production warehouse " + productionWarehouseId);
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
