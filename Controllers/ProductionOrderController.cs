using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
using System.Data;

namespace Sales_user.Controllers
{
    public class ProductionOrderController
    {
        public DataTable GetAllProductionOrders()
        {
            string sql = @"SELECT productionOrderID AS 'Production Order ID',
                                  productionOrderCode AS 'Production Order Code',
                                  salesOrderID AS 'Sales Order ID',
                                  createDate AS 'Create Date',
                                  estFinishDate AS 'Est. Finish Date',
                                  status AS 'Status'
                           FROM ProductionOrder
                           ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(ProductionOrder order)
        {
            string sql = @"INSERT INTO ProductionOrder
                (productionOrderCode, salesOrderID, staffID, estFinishDate, status, remark)
                VALUES (@code, @soID, @staffID, @finish, @status, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", order.ProductionOrderCode),
                new MySqlParameter("@soID", order.SalesOrderID),
                new MySqlParameter("@staffID", order.StaffID),
                new MySqlParameter("@finish", order.EstFinishDate),
                new MySqlParameter("@status", order.Status),
                new MySqlParameter("@remark", order.Remark ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long id)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE ProductionOrder SET productionOrderCode = @code WHERE productionOrderID = @id",
                new[] {
                    new MySqlParameter("@code", "PO-" + id),
                    new MySqlParameter("@id", id)
                });
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
                    new MySqlParameter("@remark", order.Remark ?? (object)System.DBNull.Value),
                    new MySqlParameter("@id", order.ProductionOrderID)
                });
        }

        public DataTable Search(SearchFilterCriteria criteria)
        {
            string sql = @"SELECT productionOrderID AS 'Production Order ID',
                                  productionOrderCode AS 'Production Order Code',
                                  salesOrderID AS 'Sales Order ID',
                                  createDate AS 'Create Date',
                                  estFinishDate AS 'Est. Finish Date',
                                  status AS 'Status'
                           FROM ProductionOrder
                           WHERE productionOrderCode LIKE @kw OR CAST(salesOrderID AS CHAR) LIKE @kw
                           ORDER BY createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@kw", "%" + (criteria.Keyword ?? "") + "%") });
        }

        public DataTable GetProductLines(long productionOrderId)
        {
            string sql = @"SELECT p.productCode AS 'Product', popl.productionQty AS 'Production Qty'
                           FROM ProductionOrderProductLine popl
                           INNER JOIN Product p ON popl.productID = p.productID
                           WHERE popl.ProductionOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", productionOrderId) });
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
