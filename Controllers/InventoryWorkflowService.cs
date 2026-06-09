using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Models;

namespace Sales_user.Controllers
{
    public enum StockTransferItemType
    {
        RawMaterial = 1,
        Product = 2
    }

    public class StockTransferLine
    {
        public long ItemId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class InventoryWorkflowService
    {
        public WorkflowResult ConfirmGoodsReceived(long grnId, long warehouseId)
        {
            if (warehouseId <= 0)
                return WorkflowResult.Fail("A valid warehouse ID is required.");

            var grnCtrl = new GoodsReceivedNoteController();
            var grn = grnCtrl.GetById(grnId);
            if (grn == null)
                return WorkflowResult.Fail("Goods received note not found.");

            if (grn.Status >= 2)
                return WorkflowResult.Fail("This GRN has already been completed.");

            var poLines = new PurchaseOrderController().GetRawMaterialLinesInternal(grn.PurchaseOrderID);
            if (poLines == null || poLines.Rows.Count == 0)
                return WorkflowResult.Fail("Purchase order has no raw material lines.");

            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    long lineCount = Convert.ToInt64(DatabaseConnect.ExecuteScalar(conn, trans,
                        "SELECT COUNT(*) FROM GoodsReceivedNoteRawMaterialLine WHERE goodsReceivedNoteID = @id",
                        new[] { new MySqlParameter("@id", grnId) }));

                    if (lineCount == 0)
                    {
                        foreach (DataRow poLine in poLines.Rows)
                        {
                            long rmId = Convert.ToInt64(poLine["rawMaterialID"]);
                            decimal orderQty = Convert.ToDecimal(poLine["orderQuantity"]);
                            decimal receivedQty = poLine["receivedQuantity"] == DBNull.Value ? 0 : Convert.ToDecimal(poLine["receivedQuantity"]);
                            decimal receiveNow = orderQty - receivedQty;
                            if (receiveNow <= 0) continue;

                            DatabaseConnect.ExecuteNonQuery(conn, trans,
                                @"INSERT INTO GoodsReceivedNoteRawMaterialLine (goodsReceivedNoteID, rawMaterialID, receivedQuantity)
                                  VALUES (@grnId, @rmId, @qty)",
                                new[]
                                {
                                    new MySqlParameter("@grnId", grnId),
                                    new MySqlParameter("@rmId", rmId),
                                    new MySqlParameter("@qty", receiveNow)
                                });
                        }
                    }

                    DataTable grnLines = DatabaseConnect.ExecuteQuery(conn, trans,
                        @"SELECT rawMaterialID, receivedQuantity
                          FROM GoodsReceivedNoteRawMaterialLine
                          WHERE goodsReceivedNoteID = @id",
                        new[] { new MySqlParameter("@id", grnId) });

                    if (grnLines.Rows.Count == 0)
                        throw new InvalidOperationException("No receivable quantity on the linked purchase order.");

                    foreach (DataRow line in grnLines.Rows)
                    {
                        long rmId = Convert.ToInt64(line["rawMaterialID"]);
                        decimal qty = Convert.ToDecimal(line["receivedQuantity"]);
                        if (qty <= 0) continue;

                        UpsertRawMaterialStock(conn, trans, rmId, warehouseId, qty);

                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"UPDATE PurchaseOrderRawMaterialLine
                              SET receivedQuantity = receivedQuantity + @qty
                              WHERE purchaseOrderID = @poId AND rawMaterialID = @rmId",
                            new[]
                            {
                                new MySqlParameter("@qty", qty),
                                new MySqlParameter("@poId", grn.PurchaseOrderID),
                                new MySqlParameter("@rmId", rmId)
                            });
                    }

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE GoodsReceivedNote SET status = 2, lastModifyDate = NOW() WHERE goodsReceivedNoteID = @id",
                        new[] { new MySqlParameter("@id", grnId) });

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE PurchaseOrder SET status = 5, lastModifyDate = NOW() WHERE purchaseOrderID = @id",
                        new[] { new MySqlParameter("@id", grn.PurchaseOrderID) });

                    return 0L;
                });

                return WorkflowResult.Ok(grnId, "Goods received and raw material stock updated.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("GRN confirmation failed: " + ex.Message);
            }
        }

        public WorkflowResult ConfirmDelivery(long deliveryNoteId)
        {
            var note = new DeliveryNoteController().GetById(deliveryNoteId);
            if (note == null)
                return WorkflowResult.Fail("Delivery note not found.");

            if (note.Status >= 3)
                return WorkflowResult.Fail("Delivery note is already completed.");

            if (note.WarehouseID <= 0)
                return WorkflowResult.Fail("Delivery note requires a warehouse ID.");

            var lines = new DeliveryNoteController().GetProductLinesInternal(deliveryNoteId);

            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    long lineCount = Convert.ToInt64(DatabaseConnect.ExecuteScalar(conn, trans,
                        "SELECT COUNT(*) FROM DeliveryProductLine WHERE deliveryNoteID = @id",
                        new[] { new MySqlParameter("@id", deliveryNoteId) }));

                    if (lineCount == 0)
                    {
                        DataTable soLines = DatabaseConnect.ExecuteQuery(conn, trans,
                            @"SELECT productID, orderQuantity, shippedQuantity
                              FROM SalesOrderProductLine
                              WHERE salesOrderID = @soId",
                            new[] { new MySqlParameter("@soId", note.SalesOrderID) });

                        foreach (DataRow soLine in soLines.Rows)
                        {
                            int orderQty = Convert.ToInt32(Convert.ToDecimal(soLine["orderQuantity"]));
                            int shipped = soLine["shippedQuantity"] == DBNull.Value ? 0 : Convert.ToInt32(soLine["shippedQuantity"]);
                            int shipNow = orderQty - shipped;
                            if (shipNow <= 0) continue;

                            DatabaseConnect.ExecuteNonQuery(conn, trans,
                                @"INSERT INTO DeliveryProductLine (deliveryNoteID, productID, shipQuantity)
                                  VALUES (@dnId, @productId, @qty)",
                                new[]
                                {
                                    new MySqlParameter("@dnId", deliveryNoteId),
                                    new MySqlParameter("@productId", Convert.ToInt64(soLine["productID"])),
                                    new MySqlParameter("@qty", shipNow)
                                });
                        }
                    }

                    DataTable deliveryLines = DatabaseConnect.ExecuteQuery(conn, trans,
                        @"SELECT productID, shipQuantity FROM DeliveryProductLine WHERE deliveryNoteID = @id",
                        new[] { new MySqlParameter("@id", deliveryNoteId) });

                    if (deliveryLines.Rows.Count == 0)
                        throw new InvalidOperationException("Delivery note has no shippable product lines.");

                    foreach (DataRow line in deliveryLines.Rows)
                    {
                        long productId = Convert.ToInt64(line["productID"]);
                        int shipQty = Convert.ToInt32(line["shipQuantity"]);
                        if (shipQty <= 0) continue;

                        int affected = DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"UPDATE WarehouseProduct
                              SET physicalQuantity = physicalQuantity - @qty
                              WHERE warehouseID = @whId AND productID = @productId AND physicalQuantity >= @qty",
                            new[]
                            {
                                new MySqlParameter("@qty", shipQty),
                                new MySqlParameter("@whId", note.WarehouseID),
                                new MySqlParameter("@productId", productId)
                            });

                        if (affected == 0)
                            throw new InvalidOperationException("Insufficient stock for product ID " + productId + " in warehouse " + note.WarehouseID + ".");

                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"UPDATE SalesOrderProductLine
                              SET shippedQuantity = shippedQuantity + @qty
                              WHERE salesOrderID = @soId AND productID = @productId",
                            new[]
                            {
                                new MySqlParameter("@qty", shipQty),
                                new MySqlParameter("@soId", note.SalesOrderID),
                                new MySqlParameter("@productId", productId)
                            });
                    }

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE DeliveryNote SET status = 3, lastModifyDate = NOW() WHERE deliveryNoteID = @id",
                        new[] { new MySqlParameter("@id", deliveryNoteId) });

                    UpdateSalesOrderShipStatus(conn, trans, note.SalesOrderID);
                    return 0L;
                });

                return WorkflowResult.Ok(deliveryNoteId, "Delivery confirmed and inventory updated.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Delivery confirmation failed: " + ex.Message);
            }
        }

        public WorkflowResult TransferBetweenWarehouses(
            long fromWarehouseId,
            long toWarehouseId,
            StockTransferItemType itemType,
            IList<StockTransferLine> lines)
        {
            if (fromWarehouseId <= 0 || toWarehouseId <= 0)
                return WorkflowResult.Fail("Source and destination warehouses are required.");

            if (fromWarehouseId == toWarehouseId)
                return WorkflowResult.Fail("Source and destination warehouses must be different.");

            if (lines == null || lines.Count == 0)
                return WorkflowResult.Fail("Add at least one line with a transfer quantity greater than zero.");

            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    foreach (var line in lines)
                    {
                        if (line.ItemId <= 0 || line.Quantity <= 0)
                            throw new InvalidOperationException("Each transfer line needs a positive quantity.");

                        if (itemType == StockTransferItemType.RawMaterial)
                            TransferRawMaterialLine(conn, trans, fromWarehouseId, toWarehouseId, line.ItemId, line.Quantity);
                        else
                            TransferProductLine(conn, trans, fromWarehouseId, toWarehouseId, line.ItemId, line.Quantity);
                    }

                    return 0L;
                });

                return WorkflowResult.Ok(0, "Stock transferred successfully.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Transfer failed: " + ex.Message);
            }
        }

        private static void TransferRawMaterialLine(
            MySqlConnection conn,
            MySqlTransaction trans,
            long fromWarehouseId,
            long toWarehouseId,
            long rawMaterialId,
            decimal qty)
        {
            int affected = DatabaseConnect.ExecuteNonQuery(conn, trans,
                @"UPDATE RawMaterialWarehouse
                  SET physicalQuantity = physicalQuantity - @qty
                  WHERE rawMaterialID = @itemId AND warehouseID = @fromWhId
                    AND physicalQuantity >= @qty
                    AND (physicalQuantity - reservedQuantity) >= @qty",
                new[]
                {
                    new MySqlParameter("@qty", qty),
                    new MySqlParameter("@itemId", rawMaterialId),
                    new MySqlParameter("@fromWhId", fromWarehouseId)
                });

            if (affected == 0)
                throw new InvalidOperationException("Insufficient available raw material stock for item ID " + rawMaterialId + ".");

            UpsertRawMaterialStock(conn, trans, rawMaterialId, toWarehouseId, qty);
        }

        private static void TransferProductLine(
            MySqlConnection conn,
            MySqlTransaction trans,
            long fromWarehouseId,
            long toWarehouseId,
            long productId,
            decimal qty)
        {
            int affected = DatabaseConnect.ExecuteNonQuery(conn, trans,
                @"UPDATE WarehouseProduct
                  SET physicalQuantity = physicalQuantity - @qty
                  WHERE warehouseID = @fromWhId AND productID = @itemId
                    AND physicalQuantity >= @qty
                    AND (physicalQuantity - reservedQuantity) >= @qty",
                new[]
                {
                    new MySqlParameter("@qty", qty),
                    new MySqlParameter("@fromWhId", fromWarehouseId),
                    new MySqlParameter("@itemId", productId)
                });

            if (affected == 0)
                throw new InvalidOperationException("Insufficient available product stock for item ID " + productId + ".");

            UpsertProductStock(conn, trans, productId, toWarehouseId, qty);
        }

        public static void UpsertProductStock(MySqlConnection conn, MySqlTransaction trans, long productId, long warehouseId, decimal qty)
        {
            object exists = DatabaseConnect.ExecuteScalar(conn, trans,
                "SELECT COUNT(*) FROM WarehouseProduct WHERE productID = @itemId AND warehouseID = @whId",
                new[]
                {
                    new MySqlParameter("@itemId", productId),
                    new MySqlParameter("@whId", warehouseId)
                });

            if (Convert.ToInt64(exists) > 0)
            {
                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"UPDATE WarehouseProduct
                      SET physicalQuantity = physicalQuantity + @qty
                      WHERE productID = @itemId AND warehouseID = @whId",
                    new[]
                    {
                        new MySqlParameter("@qty", qty),
                        new MySqlParameter("@itemId", productId),
                        new MySqlParameter("@whId", warehouseId)
                    });
            }
            else
            {
                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"INSERT INTO WarehouseProduct (warehouseID, productID, physicalQuantity, reservedQuantity, purchasedQuantity)
                      VALUES (@whId, @itemId, @qty, 0, 0)",
                    new[]
                    {
                        new MySqlParameter("@whId", warehouseId),
                        new MySqlParameter("@itemId", productId),
                        new MySqlParameter("@qty", qty)
                    });
            }
        }

        public static void UpsertRawMaterialStock(MySqlConnection conn, MySqlTransaction trans, long rawMaterialId, long warehouseId, decimal qty)
        {
            object exists = DatabaseConnect.ExecuteScalar(conn, trans,
                "SELECT COUNT(*) FROM RawMaterialWarehouse WHERE rawMaterialID = @rmId AND warehouseID = @whId",
                new[]
                {
                    new MySqlParameter("@rmId", rawMaterialId),
                    new MySqlParameter("@whId", warehouseId)
                });

            if (Convert.ToInt64(exists) > 0)
            {
                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"UPDATE RawMaterialWarehouse
                      SET physicalQuantity = physicalQuantity + @qty
                      WHERE rawMaterialID = @rmId AND warehouseID = @whId",
                    new[]
                    {
                        new MySqlParameter("@qty", qty),
                        new MySqlParameter("@rmId", rawMaterialId),
                        new MySqlParameter("@whId", warehouseId)
                    });
            }
            else
            {
                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"INSERT INTO RawMaterialWarehouse (rawMaterialID, warehouseID, physicalQuantity, reservedQuantity, purchasedQuantity)
                      VALUES (@rmId, @whId, @qty, 0, 0)",
                    new[]
                    {
                        new MySqlParameter("@rmId", rawMaterialId),
                        new MySqlParameter("@whId", warehouseId),
                        new MySqlParameter("@qty", qty)
                    });
            }
        }

        public static void DeductRawMaterialStock(
            MySqlConnection conn,
            MySqlTransaction trans,
            long rawMaterialId,
            long warehouseId,
            decimal qty)
        {
            int affected = DatabaseConnect.ExecuteNonQuery(conn, trans,
                @"UPDATE RawMaterialWarehouse
                  SET physicalQuantity = physicalQuantity - @qty
                  WHERE rawMaterialID = @rmId AND warehouseID = @whId
                    AND physicalQuantity >= @qty",
                new[]
                {
                    new MySqlParameter("@qty", qty),
                    new MySqlParameter("@rmId", rawMaterialId),
                    new MySqlParameter("@whId", warehouseId)
                });

            if (affected == 0)
                throw new InvalidOperationException(
                    "Insufficient raw material stock in warehouse for item ID " + rawMaterialId + ".");
        }

        public static void AddRawMaterialPurchasedQuantity(
            MySqlConnection conn,
            MySqlTransaction trans,
            long rawMaterialId,
            long warehouseId,
            decimal qty)
        {
            object exists = DatabaseConnect.ExecuteScalar(conn, trans,
                "SELECT COUNT(*) FROM RawMaterialWarehouse WHERE rawMaterialID = @rmId AND warehouseID = @whId",
                new[]
                {
                    new MySqlParameter("@rmId", rawMaterialId),
                    new MySqlParameter("@whId", warehouseId)
                });

            if (Convert.ToInt64(exists) > 0)
            {
                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"UPDATE RawMaterialWarehouse
                      SET purchasedQuantity = purchasedQuantity + @qty
                      WHERE rawMaterialID = @rmId AND warehouseID = @whId",
                    new[]
                    {
                        new MySqlParameter("@qty", qty),
                        new MySqlParameter("@rmId", rawMaterialId),
                        new MySqlParameter("@whId", warehouseId)
                    });
            }
            else
            {
                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"INSERT INTO RawMaterialWarehouse
                      (rawMaterialID, warehouseID, physicalQuantity, reservedQuantity, purchasedQuantity)
                      VALUES (@rmId, @whId, 0, 0, @qty)",
                    new[]
                    {
                        new MySqlParameter("@rmId", rawMaterialId),
                        new MySqlParameter("@whId", warehouseId),
                        new MySqlParameter("@qty", qty)
                    });
            }
        }

        private static void UpdateSalesOrderShipStatus(MySqlConnection conn, MySqlTransaction trans, long salesOrderId)
        {
            object pending = DatabaseConnect.ExecuteScalar(conn, trans,
                @"SELECT COUNT(*) FROM SalesOrderProductLine
                  WHERE salesOrderID = @soId AND shippedQuantity < orderQuantity",
                new[] { new MySqlParameter("@soId", salesOrderId) });

            int newStatus = Convert.ToInt64(pending) == 0 ? 4 : 3;
            DatabaseConnect.ExecuteNonQuery(conn, trans,
                "UPDATE SalesOrder SET status = @status, lastModifyDate = NOW() WHERE salesOrderID = @id",
                new[]
                {
                    new MySqlParameter("@status", newStatus),
                    new MySqlParameter("@id", salesOrderId)
                });
        }
    }
}
