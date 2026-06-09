using FurnitureERP.Helpers;
using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class ProductionOrderController
    {
        public DataTable GetAllProductionOrders()
        {
            string sql = @"SELECT po.productionOrderCode AS 'Production Order Code',
                                  CASE WHEN so.salesOrderCode = @sampleCode THEN 'Sample' ELSE 'Sales' END AS 'Order Type',
                                  so.salesOrderCode AS 'Sales Order',
                                  CONCAT(st.firstName, ' ', st.lastName) AS 'Staff',
                                  po.createDate AS 'Create Date',
                                  po.estFinishDate AS 'Est. Finish Date',
                                  po.status AS 'Status',
                                  po.productionOrderID AS 'Production Order ID'
                           FROM ProductionOrder po
                           LEFT JOIN SalesOrder so ON po.salesOrderID = so.salesOrderID
                           LEFT JOIN Staff st ON po.staffID = st.staffID
                           ORDER BY po.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[]
            {
                new MySqlParameter("@sampleCode", InternalSampleProductionService.InternalSalesOrderCode)
            });
        }

        public DataTable GetSalesOrdersForProductionPicker()
        {
            string sql = @"SELECT so.salesOrderID AS 'Sales Order ID',
                                  so.salesOrderCode AS 'Order Code',
                                  c.customerName AS 'Customer',
                                  so.status AS 'Status',
                                  so.createDate AS 'Create Date',
                                  CONCAT(so.salesOrderCode, ' — ', COALESCE(c.customerName, '')) AS DisplayText
                           FROM SalesOrder so
                           LEFT JOIN Customer c ON so.customerID = c.customerID
                           WHERE so.status >= 1 AND so.status < 5
                           ORDER BY so.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetPendingSalesOrdersForQuickEntry()
        {
            string sql = @"SELECT so.salesOrderID AS SalesOrderID,
                                  so.salesOrderCode AS 'Sales Order',
                                  COALESCE(c.customerName, '') AS Customer,
                                  so.status AS SoStatus,
                                  COUNT(DISTINCT spl.productID) AS 'Lines',
                                  SUM(GREATEST(spl.orderQuantity - spl.warehouseReservedQty, 0)) AS 'Need Mfg Qty'
                           FROM SalesOrder so
                           INNER JOIN SalesOrderProductLine spl ON so.salesOrderID = spl.salesOrderID
                           LEFT JOIN Customer c ON so.customerID = c.customerID
                           WHERE so.status >= 1 AND so.status < 5
                             AND NOT EXISTS (
                                 SELECT 1 FROM ProductionOrder po WHERE po.salesOrderID = so.salesOrderID
                             )
                           GROUP BY so.salesOrderID, so.salesOrderCode, c.customerName, so.status, so.createDate
                           HAVING SUM(GREATEST(spl.orderQuantity - spl.warehouseReservedQty, 0)) > 0
                           ORDER BY so.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public bool SalesOrderHasProductionOrder(long salesOrderId)
        {
            object count = DatabaseConnect.ExecuteScalar(
                "SELECT COUNT(*) FROM ProductionOrder WHERE salesOrderID = @id",
                new[] { new MySqlParameter("@id", salesOrderId) });
            return count != null && count != DBNull.Value && Convert.ToInt64(count) > 0;
        }

        public DataTable GetStaffForPicker()
        {
            string sql = @"SELECT staffID AS 'Staff ID',
                                  username AS 'Username',
                                  CONCAT(firstName, ' ', lastName) AS 'Name',
                                  CONCAT(username, ' — ', firstName, ' ', lastName) AS DisplayText
                           FROM Staff
                           WHERE status IS NULL OR status = 1
                           ORDER BY username";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public DataTable GetHeaderDetail(long productionOrderId)
        {
            string sql = @"SELECT po.productionOrderCode AS 'Production Order Code',
                                  so.salesOrderCode AS 'Sales Order',
                                  CONCAT(st.firstName, ' ', st.lastName) AS 'Staff',
                                  po.createDate AS 'Create Date',
                                  po.estFinishDate AS 'Est. Finish Date',
                                  po.status AS 'Status',
                                  po.remark AS 'Remark'
                           FROM ProductionOrder po
                           LEFT JOIN SalesOrder so ON po.salesOrderID = so.salesOrderID
                           LEFT JOIN Staff st ON po.staffID = st.staffID
                           WHERE po.productionOrderID = @id";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", productionOrderId) });
            return DictionaryService.DecorateStatusColumn(dt, "Status", DictionaryService.Categories.Production);
        }

        public DataTable GetProductLines(long productionOrderId)
        {
            string sql = @"SELECT p.productCode AS 'Product',
                                  spl.orderQuantity AS 'Order Qty',
                                  spl.warehouseReservedQty AS 'Reserved',
                                  GREATEST(spl.orderQuantity - spl.warehouseReservedQty, 0) AS 'Need Mfg',
                                  popl.productionQty AS 'Production Qty'
                           FROM ProductionOrderProductLine popl
                           INNER JOIN Product p ON popl.productID = p.productID
                           INNER JOIN ProductionOrder po ON popl.ProductionOrderID = po.productionOrderID
                           LEFT JOIN SalesOrderProductLine spl
                                ON spl.salesOrderID = po.salesOrderID AND spl.productID = popl.productID
                           WHERE popl.ProductionOrderID = @id
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", productionOrderId) });
        }

        public DataTable GetSampleLinesForEditor(long productionOrderId)
        {
            string sql = @"SELECT popl.productID AS ProductID,
                                  p.productCode AS ProductCode,
                                  p.category AS Category,
                                  popl.productionQty AS ProductionQty
                           FROM ProductionOrderProductLine popl
                           INNER JOIN Product p ON popl.productID = p.productID
                           WHERE popl.ProductionOrderID = @id
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", productionOrderId) });
        }

        public long CreateSampleWithLines(
            long staffId,
            DateTime estFinishDate,
            int status,
            string remark,
            System.Collections.Generic.IEnumerable<(long ProductId, int ProductionQty)> lines)
        {
            long salesOrderId = InternalSampleProductionService.GetOrCreateInternalSampleSalesOrderId(staffId);
            return CreateWithLines(new ProductionOrder
            {
                ProductionOrderCode = "PTO-TEMP",
                SalesOrderID = salesOrderId,
                StaffID = staffId > 0 ? staffId : 1,
                EstFinishDate = estFinishDate,
                Status = status,
                Remark = InternalSampleProductionService.EnsureSampleRemark(remark)
            }, lines);
        }

        public bool IsSampleProductionOrder(long productionOrderId)
        {
            var order = GetById(productionOrderId);
            return order != null
                && InternalSampleProductionService.IsInternalSampleSalesOrder(order.SalesOrderID);
        }

        public DataTable GetProductLinesInternal(long productionOrderId)
        {
            string sql = @"SELECT productID, productionQty
                           FROM ProductionOrderProductLine
                           WHERE ProductionOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", productionOrderId) });
        }

        public DataTable GetLinesForEditor(long productionOrderId)
        {
            string sql = @"SELECT popl.productID AS ProductID,
                                  p.productCode AS ProductCode,
                                  spl.orderQuantity AS OrderQty,
                                  spl.warehouseReservedQty AS ReservedQty,
                                  GREATEST(spl.orderQuantity - spl.warehouseReservedQty, 0) AS NeedMfgQty,
                                  popl.productionQty AS ProductionQty
                           FROM ProductionOrderProductLine popl
                           INNER JOIN Product p ON popl.productID = p.productID
                           INNER JOIN ProductionOrder po ON popl.ProductionOrderID = po.productionOrderID
                           LEFT JOIN SalesOrderProductLine spl
                                ON spl.salesOrderID = po.salesOrderID AND spl.productID = popl.productID
                           WHERE popl.ProductionOrderID = @id
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", productionOrderId) });
        }

        public DataTable GetLinesTemplateFromSalesOrder(long salesOrderId)
        {
            string sql = @"SELECT spl.productID AS ProductID,
                                  p.productCode AS ProductCode,
                                  spl.orderQuantity AS OrderQty,
                                  spl.warehouseReservedQty AS ReservedQty,
                                  GREATEST(spl.orderQuantity - spl.warehouseReservedQty, 0) AS NeedMfgQty,
                                  GREATEST(spl.orderQuantity - spl.warehouseReservedQty, 0) AS ProductionQty
                           FROM SalesOrderProductLine spl
                           INNER JOIN Product p ON spl.productID = p.productID
                           WHERE spl.salesOrderID = @id
                           ORDER BY p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
        }

        public long Insert(ProductionOrder order)
        {
            string sql = @"INSERT INTO ProductionOrder
                (productionOrderID, productionOrderCode, salesOrderID, staffID, estFinishDate, status, remark)
                VALUES (@id, @code, @soID, @staffID, @finish, @status, @remark)";
            return DatabaseConnect.InsertWithAllocatedId("productionorder", "productionOrderID", sql, new[] {
                new MySqlParameter("@code", order.ProductionOrderCode),
                new MySqlParameter("@soID", order.SalesOrderID),
                new MySqlParameter("@staffID", order.StaffID),
                new MySqlParameter("@finish", order.EstFinishDate),
                new MySqlParameter("@status", order.Status),
                new MySqlParameter("@remark", order.Remark ?? (object)DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long id)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE ProductionOrder SET productionOrderCode = @code WHERE productionOrderID = @id",
                new[] {
                    new MySqlParameter("@code", DocumentCodeHelper.Build("PTO", id)),
                    new MySqlParameter("@id", id)
                });
        }

        public long CreateFromSalesOrder(
            long salesOrderId,
            long staffId,
            DateTime estFinishDate,
            string remark,
            bool advanceSalesOrderToProcessing,
            int status = 0)
        {
            var lines = new SalesOrderController().GetProductLinesInternal(salesOrderId);
            if (lines == null || lines.Rows.Count == 0)
                throw new InvalidOperationException("Sales order has no product lines.");

            var productionLines = new List<(long ProductId, int Qty)>();
            foreach (DataRow row in lines.Rows)
            {
                decimal orderQty = Convert.ToDecimal(row["orderQuantity"]);
                int reserved = row["warehouseReservedQty"] == DBNull.Value ? 0 : Convert.ToInt32(row["warehouseReservedQty"]);
                int productionQty = (int)Math.Max(0, orderQty - reserved);
                if (productionQty <= 0) continue;
                productionLines.Add((Convert.ToInt64(row["productID"]), productionQty));
            }

            if (productionLines.Count == 0)
                throw new InvalidOperationException("No production quantity required for this sales order.");

            return DatabaseConnect.ExecuteInTransaction((conn, trans) =>
            {
                long poId = DatabaseConnect.InsertWithAllocatedId(conn, trans, "productionorder", "productionOrderID",
                    @"INSERT INTO ProductionOrder
                        (productionOrderID, productionOrderCode, salesOrderID, staffID, estFinishDate, status, remark)
                      VALUES (@id, @code, @soID, @staffID, @finish, @status, @remark)",
                    new[]
                    {
                        new MySqlParameter("@code", "PTO-TEMP"),
                        new MySqlParameter("@soID", salesOrderId),
                        new MySqlParameter("@staffID", staffId),
                        new MySqlParameter("@finish", estFinishDate),
                        new MySqlParameter("@status", status),
                        new MySqlParameter("@remark", remark ?? (object)DBNull.Value)
                    });

                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    "UPDATE ProductionOrder SET productionOrderCode = @code WHERE productionOrderID = @id",
                    new[]
                    {
                        new MySqlParameter("@code", DocumentCodeHelper.Build("PTO", poId)),
                        new MySqlParameter("@id", poId)
                    });

                InsertProductLines(conn, trans, poId, productionLines);

                if (advanceSalesOrderToProcessing)
                {
                    DatabaseConnect.ExecuteNonQuery(conn, trans,
                        "UPDATE SalesOrder SET status = 2, lastModifyDate = NOW() WHERE salesOrderID = @id",
                        new[] { new MySqlParameter("@id", salesOrderId) });
                }

                return poId;
            });
        }

        public long CreateWithLines(ProductionOrder order, IEnumerable<(long ProductId, int ProductionQty)> lines)
        {
            var lineList = new List<(long ProductId, int ProductionQty)>();
            foreach (var line in lines)
            {
                if (line.ProductionQty > 0)
                    lineList.Add(line);
            }
            if (lineList.Count == 0)
                throw new InvalidOperationException("At least one production line with quantity > 0 is required.");

            return DatabaseConnect.ExecuteInTransaction((conn, trans) =>
            {
                long poId = DatabaseConnect.InsertWithAllocatedId(conn, trans, "productionorder", "productionOrderID",
                    @"INSERT INTO ProductionOrder
                        (productionOrderID, productionOrderCode, salesOrderID, staffID, estFinishDate, status, remark)
                      VALUES (@id, @code, @soID, @staffID, @finish, @status, @remark)",
                    new[]
                    {
                        new MySqlParameter("@code", order.ProductionOrderCode ?? "PTO-TEMP"),
                        new MySqlParameter("@soID", order.SalesOrderID),
                        new MySqlParameter("@staffID", order.StaffID),
                        new MySqlParameter("@finish", order.EstFinishDate),
                        new MySqlParameter("@status", order.Status),
                        new MySqlParameter("@remark", order.Remark ?? (object)DBNull.Value)
                    });

                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    "UPDATE ProductionOrder SET productionOrderCode = @code WHERE productionOrderID = @id",
                    new[]
                    {
                        new MySqlParameter("@code", DocumentCodeHelper.Build("PTO", poId)),
                        new MySqlParameter("@id", poId)
                    });

                InsertProductLines(conn, trans, poId, lineList);
                return poId;
            });
        }

        public void UpdateWithLines(ProductionOrder order, IEnumerable<(long ProductId, int ProductionQty)> lines)
        {
            var lineList = new List<(long ProductId, int ProductionQty)>();
            foreach (var line in lines)
            {
                if (line.ProductionQty > 0)
                    lineList.Add(line);
            }

            DatabaseConnect.ExecuteInTransaction<bool>((conn, trans) =>
            {
                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"UPDATE ProductionOrder
                      SET estFinishDate = @finish, status = @status, remark = @remark, lastModifyDate = NOW()
                      WHERE productionOrderID = @id",
                    new[]
                    {
                        new MySqlParameter("@finish", order.EstFinishDate),
                        new MySqlParameter("@status", order.Status),
                        new MySqlParameter("@remark", order.Remark ?? (object)DBNull.Value),
                        new MySqlParameter("@id", order.ProductionOrderID)
                    });

                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    "DELETE FROM ProductionOrderProductLine WHERE ProductionOrderID = @id",
                    new[] { new MySqlParameter("@id", order.ProductionOrderID) });

                if (lineList.Count > 0)
                    InsertProductLines(conn, trans, order.ProductionOrderID, lineList);
                return true;
            });
        }

        private static void InsertProductLines(
            MySqlConnection conn,
            MySqlTransaction trans,
            long productionOrderId,
            IEnumerable<(long ProductId, int Qty)> lines)
        {
            foreach (var line in lines)
            {
                DatabaseConnect.ExecuteNonQuery(conn, trans,
                    @"INSERT INTO ProductionOrderProductLine (ProductionOrderID, productID, productionQty)
                      VALUES (@poID, @productID, @qty)",
                    new[]
                    {
                        new MySqlParameter("@poID", productionOrderId),
                        new MySqlParameter("@productID", line.ProductId),
                        new MySqlParameter("@qty", line.Qty)
                    });
            }
        }

        public ProductionOrder GetById(long id)
        {
            string sql = "SELECT * FROM ProductionOrder WHERE productionOrderID = @id";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", id) });
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new ProductionOrder
            {
                ProductionOrderID = Convert.ToInt64(row["productionOrderID"]),
                ProductionOrderCode = row["productionOrderCode"]?.ToString(),
                SalesOrderID = Convert.ToInt64(row["salesOrderID"]),
                StaffID = Convert.ToInt64(row["staffID"]),
                EstFinishDate = Convert.ToDateTime(row["estFinishDate"]),
                Status = Convert.ToInt32(row["status"]),
                Remark = row["remark"] == DBNull.Value ? null : row["remark"].ToString()
            };
        }

        public void Update(ProductionOrder order)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE ProductionOrder SET estFinishDate=@finish, status=@status, remark=@remark WHERE productionOrderID=@id",
                new[] {
                    new MySqlParameter("@finish", order.EstFinishDate),
                    new MySqlParameter("@status", order.Status),
                    new MySqlParameter("@remark", order.Remark ?? (object)DBNull.Value),
                    new MySqlParameter("@id", order.ProductionOrderID)
                });
        }

        public DataTable Search(SearchFilterCriteria criteria)
        {
            string sql = @"SELECT po.productionOrderCode AS 'Production Order Code',
                                  so.salesOrderCode AS 'Sales Order',
                                  po.createDate AS 'Create Date',
                                  po.estFinishDate AS 'Est. Finish Date',
                                  po.status AS 'Status',
                                  po.productionOrderID AS 'Production Order ID'
                           FROM ProductionOrder po
                           LEFT JOIN SalesOrder so ON po.salesOrderID = so.salesOrderID
                           WHERE po.productionOrderCode LIKE @kw
                              OR so.salesOrderCode LIKE @kw
                              OR CAST(po.salesOrderID AS CHAR) LIKE @kw
                           ORDER BY po.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@kw", "%" + (criteria.Keyword ?? "") + "%") });
        }

        public bool InsertProductLine(long productionOrderId, long productId, int productionQty)
        {
            return DatabaseConnect.ExecuteNonQuery(
                @"INSERT INTO ProductionOrderProductLine (ProductionOrderID, productID, productionQty)
                  VALUES (@poID, @productID, @qty)",
                new[]
                {
                    new MySqlParameter("@poID", productionOrderId),
                    new MySqlParameter("@productID", productId),
                    new MySqlParameter("@qty", productionQty)
                }) > 0;
        }
    }
}
