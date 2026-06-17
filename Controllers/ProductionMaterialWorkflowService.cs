using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;

namespace Sales_user.Controllers
{
    public class MaterialShortageLine
    {
        public long RawMaterialId { get; set; }
        public string RawMaterialCode { get; set; }
        public decimal RequiredQty { get; set; }
        public decimal NetAvailable { get; set; }
        public decimal MinimumStockLevel { get; set; }
        public decimal SuggestedPoQty { get; set; }
        public decimal PoQty { get; set; }
        public decimal UnitPrice { get; set; }
        public long SupplierId { get; set; }
    }

    public class ProductionMaterialWorkflowService
    {
        private readonly RawMaterialRequestNoteController _rmrnCtrl = new RawMaterialRequestNoteController();
        private readonly ProductionOrderController _ptoCtrl = new ProductionOrderController();
        private readonly ProductController _productCtrl = new ProductController();
        private readonly PurchaseOrderController _poCtrl = new PurchaseOrderController();
        private readonly RawMaterialController _rmCtrl = new RawMaterialController();
        private readonly WarehouseController _warehouseCtrl = new WarehouseController();

        public WorkflowResult CreateRequestNoteFromPto(
            long productionOrderId,
            IList<long> productIds,
            long staffId,
            DateTime requestDate,
            string remark)
        {
            if (productionOrderId <= 0 || staffId <= 0)
                return WorkflowResult.Fail("Production order and staff are required.");
            if (productIds == null || productIds.Count == 0)
                return WorkflowResult.Fail("Select at least one product line.");

            var order = _ptoCtrl.GetById(productionOrderId);
            if (order == null)
                return WorkflowResult.Fail("Production order not found.");

            var selected = new HashSet<long>(productIds);
            var ptoLines = _ptoCtrl.GetProductLinesInternal(productionOrderId);
            if (ptoLines == null || ptoLines.Rows.Count == 0)
                return WorkflowResult.Fail("Production order has no product lines.");

            var issueLines = new List<(long ProductId, long RawMaterialId, decimal Qty)>();
            foreach (DataRow row in ptoLines.Rows)
            {
                long productId = Convert.ToInt64(row["productID"]);
                if (!selected.Contains(productId)) continue;

                int productionQty = Convert.ToInt32(row["productionQty"]);
                if (productionQty <= 0) continue;

                var bom = _productCtrl.GetBomLinesInternal(productId);
                if (bom == null || bom.Rows.Count == 0)
                    return WorkflowResult.Fail("Product ID " + productId + " has no BOM defined.");

                foreach (DataRow bomRow in bom.Rows)
                {
                    long rmId = Convert.ToInt64(bomRow["rawMaterialID"]);
                    decimal needPerUnit = Convert.ToDecimal(bomRow["rawMaterialNeedQty"]);
                    decimal totalNeed = needPerUnit * productionQty;
                    if (totalNeed <= 0) continue;
                    issueLines.Add((productId, rmId, totalNeed));
                }
            }

            if (issueLines.Count == 0)
                return WorkflowResult.Fail("No raw material requirements calculated for the selected products.");

            try
            {
                long noteId = DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    long id = _rmrnCtrl.InsertInTransaction(conn, trans, new RawMaterialRequestNote
                    {
                        RawMaterialRequestNoteCode = "SCR-TEMP",
                        ProductionOrderID = productionOrderId,
                        StaffID = staffId,
                        RequestDate = requestDate.Date,
                        Remark = remark,
                        Status = RawMaterialRequestNoteConstants.StatusDraft
                    });
                    if (id <= 0)
                        throw new InvalidOperationException("RM request note insert did not return a valid ID.");

                    string code = DocumentCodeHelper.Build("SCR", id);
                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE RawMaterialRequestNote SET rawMaterialRequestNoteCode = @code WHERE rawMaterialRequestNoteID = @id",
                        new[]
                        {
                            new MySqlParameter("@code", code),
                            new MySqlParameter("@id", id)
                        });

                    foreach (var line in issueLines)
                    {
                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"INSERT INTO RawMaterialRequestNoteRawMaterial_line
                              (rawMaterialRequestNoteID, productID, rawMaterialID, rawMaterialRequestQuantity)
                              VALUES (@noteId, @productId, @rmId, @qty)",
                            new[]
                            {
                                new MySqlParameter("@noteId", id),
                                new MySqlParameter("@productId", line.ProductId),
                                new MySqlParameter("@rmId", line.RawMaterialId),
                                new MySqlParameter("@qty", line.Qty)
                            });
                    }

                    return id;
                });

                DocumentAuditService.LogCreate(DocumentAuditService.Types.RawMaterialRequest, noteId,
                    DocumentCodeHelper.Build("SCR", noteId), "Created from production order");

                return WorkflowResult.Ok(noteId, "RM request note " + DocumentCodeHelper.Build("SCR", noteId) + " created.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Failed to create RM request note: " + ex.Message);
            }
        }

        public DataTable BuildIssuePreview(long requestNoteId, long inventoryWarehouseId, long productionWarehouseId)
        {
            var aggregated = _rmrnCtrl.GetAggregatedRequestQuantities(requestNoteId);
            var result = new DataTable();
            result.Columns.Add("Raw Material ID", typeof(long));
            result.Columns.Add("Raw Material", typeof(string));
            result.Columns.Add("Request Qty", typeof(decimal));
            result.Columns.Add("Inventory Available", typeof(decimal));
            result.Columns.Add("Inventory Net", typeof(decimal));
            result.Columns.Add("Production On Hand", typeof(decimal));
            result.Columns.Add("Min Stock", typeof(decimal));
            result.Columns.Add("Shortage Qty", typeof(decimal));

            if (aggregated == null) return result;

            foreach (DataRow row in aggregated.Rows)
            {
                long rmId = Convert.ToInt64(row["rawMaterialID"]);
                decimal requestQty = Convert.ToDecimal(row["totalQty"]);
                var inv = _rmCtrl.GetWarehouseStockSnapshot(rmId, inventoryWarehouseId);
                var prod = _rmCtrl.GetWarehouseStockSnapshot(rmId, productionWarehouseId);
                decimal net = inv.NetAvailable;
                decimal shortage = Math.Max(0, requestQty - inv.Available);

                result.Rows.Add(
                    rmId,
                    row["rawMaterialCode"]?.ToString(),
                    requestQty,
                    inv.Available,
                    net,
                    prod.Physical,
                    inv.MinimumStockLevel,
                    shortage);
            }

            return result;
        }

        public IList<MaterialShortageLine> EvaluateShortages(long requestNoteId, long inventoryWarehouseId)
        {
            var shortages = new List<MaterialShortageLine>();
            var aggregated = _rmrnCtrl.GetAggregatedRequestQuantities(requestNoteId);
            if (aggregated == null) return shortages;

            foreach (DataRow row in aggregated.Rows)
            {
                long rmId = Convert.ToInt64(row["rawMaterialID"]);
                decimal requiredQty = Convert.ToDecimal(row["totalQty"]);
                var snap = _rmCtrl.GetWarehouseStockSnapshot(rmId, inventoryWarehouseId);
                decimal shortageForIssue = Math.Max(0, requiredQty - snap.Available);
                if (shortageForIssue <= 0) continue;

                decimal safetyTarget = Math.Max(snap.MinimumStockLevel, snap.MinimumStockLevel);
                decimal shortageForSafety = Math.Max(0, safetyTarget - snap.NetAvailable);
                decimal poQty = Math.Max(shortageForIssue, shortageForSafety);
                var quote = _rmCtrl.GetPreferredSupplierQuote(rmId);

                shortages.Add(new MaterialShortageLine
                {
                    RawMaterialId = rmId,
                    RawMaterialCode = row["rawMaterialCode"]?.ToString(),
                    RequiredQty = requiredQty,
                    NetAvailable = snap.NetAvailable,
                    MinimumStockLevel = snap.MinimumStockLevel,
                    SuggestedPoQty = poQty,
                    PoQty = poQty,
                    UnitPrice = quote?.BasePrice ?? 0,
                    SupplierId = quote?.SupplierId ?? 0
                });
            }

            return shortages;
        }

        public WorkflowResult IssueRequestNote(long requestNoteId, long inventoryWarehouseId, long productionWarehouseId)
        {
            if (requestNoteId <= 0)
                return WorkflowResult.Fail("Request note is required.");
            var invWh = _warehouseCtrl.GetById(inventoryWarehouseId);
            var prodWh = _warehouseCtrl.GetById(productionWarehouseId);
            if (invWh == null || prodWh == null)
                return WorkflowResult.Fail("Warehouse not found.");
            if (!WarehouseHelper.IsInventoryWarehouse(inventoryWarehouseId, invWh.WarehouseName)
                || !WarehouseHelper.IsProductionWarehouse(productionWarehouseId, prodWh.WarehouseName))
                return WorkflowResult.Fail("Use paired inventory and production warehouses.");

            var note = _rmrnCtrl.GetById(requestNoteId);
            if (note == null)
                return WorkflowResult.Fail("Request note not found.");
            if (note.Status == RawMaterialRequestNoteConstants.StatusCompleted)
                return WorkflowResult.Fail("This request note has already been completed.");
            if (note.Status == RawMaterialRequestNoteConstants.StatusCancelled)
                return WorkflowResult.Fail("This request note is cancelled.");

            var shortages = EvaluateShortages(requestNoteId, inventoryWarehouseId);
            if (shortages.Count > 0)
            {
                return WorkflowResult.Fail(
                    "Insufficient inventory warehouse stock for " + shortages.Count +
                    " raw material(s). Create purchase orders first, then issue again.");
            }

            var aggregated = _rmrnCtrl.GetAggregatedRequestQuantities(requestNoteId);
            if (aggregated == null || aggregated.Rows.Count == 0)
                return WorkflowResult.Fail("Request note has no material lines.");

            try
            {
                DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    foreach (DataRow row in aggregated.Rows)
                    {
                        long rmId = Convert.ToInt64(row["rawMaterialID"]);
                        decimal qty = Convert.ToDecimal(row["totalQty"]);
                        TransferRawMaterialInTransaction(conn, trans, inventoryWarehouseId, productionWarehouseId, rmId, qty);
                    }

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        @"UPDATE RawMaterialRequestNote
                          SET status = @noteStatus
                          WHERE rawMaterialRequestNoteID = @id",
                        new[]
                        {
                            new MySqlParameter("@noteStatus", MySqlDbType.Int32)
                            {
                                Value = RawMaterialRequestNoteConstants.StatusCompleted
                            },
                            new MySqlParameter("@id", requestNoteId)
                        });

                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        @"UPDATE ProductionOrder
                          SET status = CASE WHEN status = 0 THEN 1 ELSE status END,
                              lastModifyDate = NOW()
                          WHERE productionOrderID = @id",
                        new[] { new MySqlParameter("@id", note.ProductionOrderID) });

                    return 0L;
                });

                DocumentAuditService.LogAction(DocumentAuditService.Types.RawMaterialRequest, requestNoteId,
                    note.RawMaterialRequestNoteCode ?? DocumentCodeHelper.Build("SCR", requestNoteId),
                    DocumentAuditService.Actions.Issue, "Materials issued to production warehouse");

                return WorkflowResult.Ok(requestNoteId, "Materials issued to production warehouse. Request note completed.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Issue failed: " + ex.Message);
            }
        }

        public WorkflowResult CreatePurchaseOrdersForShortagesBySupplier(
            long staffId,
            long inventoryWarehouseId,
            IList<MaterialShortageLine> lines,
            DateTime requestDeliveryDate,
            string remark)
        {
            if (staffId <= 0 || inventoryWarehouseId <= 0)
                return WorkflowResult.Fail("Staff and receiving warehouse are required.");
            if (lines == null || lines.Count == 0)
                return WorkflowResult.Fail("No purchase lines provided.");

            var activeLines = lines.Where(l => l.RawMaterialId > 0 && l.PoQty > 0).ToList();
            if (activeLines.Count == 0)
                return WorkflowResult.Fail("Enter at least one PO quantity.");

            var missingSupplier = activeLines.Where(l => l.SupplierId <= 0).ToList();
            if (missingSupplier.Count > 0)
            {
                string sample = missingSupplier[0].RawMaterialCode ?? missingSupplier[0].RawMaterialId.ToString();
                return WorkflowResult.Fail(
                    missingSupplier.Count == 1
                        ? $"No preferred supplier quote for {sample}. Add a Raw Material Supplier quote first."
                        : $"{missingSupplier.Count} line(s) have no preferred supplier quote (e.g. {sample}). Add quotes first.");
            }

            var created = new List<string>();
            foreach (var group in activeLines.GroupBy(l => l.SupplierId).OrderBy(g => g.Key))
            {
                var result = CreatePurchaseOrderForShortages(
                    group.Key, staffId, inventoryWarehouseId, group.ToList(), requestDeliveryDate, remark);
                if (!result.Success)
                {
                    if (created.Count > 0)
                        return WorkflowResult.Fail(result.Message + " (Earlier PO(s) were already created: " + string.Join(", ", created) + ")");
                    return result;
                }
                created.Add(result.Message);
            }

            string summary = created.Count == 1
                ? created[0]
                : $"Created {created.Count} purchase orders:{Environment.NewLine}{string.Join(Environment.NewLine, created)}";
            return WorkflowResult.Ok(0, summary);
        }

        public WorkflowResult CreatePurchaseOrderForShortages(
            long supplierId,
            long staffId,
            long inventoryWarehouseId,
            IList<MaterialShortageLine> lines,
            DateTime requestDeliveryDate,
            string remark)
        {
            if (supplierId <= 0 || staffId <= 0 || inventoryWarehouseId <= 0)
                return WorkflowResult.Fail("Supplier, staff and receiving warehouse are required.");
            if (lines == null || lines.Count == 0)
                return WorkflowResult.Fail("No purchase lines provided.");

            try
            {
                long poId = DatabaseConnect.ExecuteInTransaction((conn, trans) =>
                {
                    var order = new PurchaseOrder
                    {
                        PurchaseOrderCode = "PO-TEMP",
                        SupplierID = supplierId,
                        StaffID = staffId,
                        WarehouseID = inventoryWarehouseId,
                        RequestDeliveryDate = requestDeliveryDate.Date,
                        Status = 0,
                        Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim()
                    };
                    long id = _poCtrl.InsertInTransaction(conn, trans, order);
                    if (id <= 0)
                        throw new InvalidOperationException("Unable to allocate purchase order ID.");

                    string poCode = DocumentCodeHelper.Build("PO", id);
                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE PurchaseOrder SET purchaseOrderCode = @code WHERE purchaseOrderID = @id",
                        new[]
                        {
                            new MySqlParameter("@code", poCode),
                            new MySqlParameter("@id", id)
                        });

                    foreach (var line in lines)
                    {
                        if (line.RawMaterialId <= 0 || line.PoQty <= 0) continue;

                        var quote = _rmCtrl.TryGetSupplierQuote(line.RawMaterialId, supplierId);
                        if (quote == null)
                        {
                            string rmLabel = string.IsNullOrWhiteSpace(line.RawMaterialCode)
                                ? line.RawMaterialId.ToString()
                                : line.RawMaterialCode;
                            throw new InvalidOperationException(
                                $"Raw material {rmLabel} has no active quote for supplier {supplierId}.");
                        }

                        decimal unitPrice = line.UnitPrice > 0 ? line.UnitPrice : quote.BasePrice;

                        DatabaseConnect.ExecuteNonQuery(conn, trans,
                            @"INSERT INTO PurchaseOrderRawMaterialLine
                              (purchaseOrderID, rawMaterialID, price, orderQuantity)
                              VALUES (@poId, @rmId, @price, @qty)",
                            new[]
                            {
                                new MySqlParameter("@poId", id),
                                new MySqlParameter("@rmId", line.RawMaterialId),
                                new MySqlParameter("@price", unitPrice),
                                new MySqlParameter("@qty", line.PoQty)
                            });

                        InventoryWorkflowService.AddRawMaterialPurchasedQuantity(
                            conn, trans, line.RawMaterialId, inventoryWarehouseId, line.PoQty);
                    }

                    return id;
                });

                if (poId <= 0)
                    return WorkflowResult.Fail("Purchase order creation failed: invalid ID.");

                return WorkflowResult.Ok(poId, "Purchase order " + DocumentCodeHelper.Build("PO", poId) + " created for material shortages.");
            }
            catch (Exception ex)
            {
                return WorkflowResult.Fail("Purchase order creation failed: " + ex.Message);
            }
        }

        public DataTable PreviewBomRequirements(long productionOrderId, IList<long> productIds)
        {
            var result = new DataTable();
            result.Columns.Add("Product ID", typeof(long));
            result.Columns.Add("Product", typeof(string));
            result.Columns.Add("Production Qty", typeof(int));
            result.Columns.Add("Raw Material ID", typeof(long));
            result.Columns.Add("Raw Material", typeof(string));
            result.Columns.Add("Need Per Unit", typeof(decimal));
            result.Columns.Add("Total Need", typeof(decimal));

            if (productIds == null || productIds.Count == 0) return result;
            var selected = new HashSet<long>(productIds);
            var ptoLines = _ptoCtrl.GetProductLinesInternal(productionOrderId);
            if (ptoLines == null) return result;

            foreach (DataRow row in ptoLines.Rows)
            {
                long productId = Convert.ToInt64(row["productID"]);
                if (!selected.Contains(productId)) continue;

                int productionQty = Convert.ToInt32(row["productionQty"]);
                if (productionQty <= 0) continue;

                var bom = _productCtrl.GetBomLinesInternal(productId);
                if (bom == null) continue;

                string productCode = GetProductCode(productId);
                foreach (DataRow bomRow in bom.Rows)
                {
                    long rmId = Convert.ToInt64(bomRow["rawMaterialID"]);
                    decimal needPerUnit = Convert.ToDecimal(bomRow["rawMaterialNeedQty"]);
                    result.Rows.Add(
                        productId,
                        productCode,
                        productionQty,
                        rmId,
                        GetRawMaterialCode(rmId),
                        needPerUnit,
                        needPerUnit * productionQty);
                }
            }

            return result;
        }

        private static void TransferRawMaterialInTransaction(
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

            InventoryWorkflowService.UpsertRawMaterialStock(conn, trans, rawMaterialId, toWarehouseId, qty);
        }

        private static string GetProductCode(long productId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT productCode FROM Product WHERE productID = @id",
                new[] { new MySqlParameter("@id", productId) });
            return value?.ToString() ?? productId.ToString();
        }

        private static string GetRawMaterialCode(long rawMaterialId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT rawMaterialCode FROM RawMaterial WHERE rawMaterialID = @id",
                new[] { new MySqlParameter("@id", rawMaterialId) });
            return value?.ToString() ?? rawMaterialId.ToString();
        }
    }
}
