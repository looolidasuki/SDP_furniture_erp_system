using MySql.Data.MySqlClient;
using Sales_user.Models;
using System.Collections.Generic;
using System.Data;

namespace Sales_user.Controllers
{
    public class SalesOrderController
    {
        public DataTable GetAllSalesOrders()
        {
            string sql = @"SELECT so.salesOrderID AS 'Order ID',
                                  so.salesOrderCode AS 'Order Code',
                                  c.customerName AS 'Customer',
                                  so.deliveryAddress AS 'Delivery Address',
                                  so.createDate AS 'Create Date',
                                  so.status AS 'Status'
                           FROM SalesOrder so
                           LEFT JOIN Customer c ON so.customerID = c.customerID
                           ORDER BY so.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(SalesOrder order)
        {
            string sql = @"INSERT INTO SalesOrder
                (salesOrderCode, customerID, staffID, currencyCurrencyID, deliveryAddress,
                 discountType, discount, status, remark)
                VALUES (@code, @customerID, @staffID, @currencyID, @address,
                        @discountType, @discount, @status, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", order.SalesOrderCode),
                new MySqlParameter("@customerID", order.CustomerID),
                new MySqlParameter("@staffID", order.StaffID),
                new MySqlParameter("@currencyID", order.CurrencyCurrencyID),
                new MySqlParameter("@address", order.DeliveryAddress),
                new MySqlParameter("@discountType", order.DiscountType ?? (object)System.DBNull.Value),
                new MySqlParameter("@discount", order.Discount),
                new MySqlParameter("@status", order.Status),
                new MySqlParameter("@remark", order.Remark ?? (object)System.DBNull.Value)
            });
        }

        public void UpdateCodeAfterInsert(long salesOrderId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE SalesOrder SET salesOrderCode = @code WHERE salesOrderID = @id",
                new[] {
                    new MySqlParameter("@code", "SO-" + salesOrderId),
                    new MySqlParameter("@id", salesOrderId)
                });
        }

        public bool InsertProductLine(long salesOrderId, long productId, decimal price, decimal orderQty, decimal discount)
        {
            string sql = @"INSERT INTO SalesOrderProductLine
                (salesOrderID, productID, price, orderQuantity, discountAmount)
                VALUES (@soID, @productID, @price, @qty, @discount)";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@soID", salesOrderId),
                new MySqlParameter("@productID", productId),
                new MySqlParameter("@price", price),
                new MySqlParameter("@qty", orderQty),
                new MySqlParameter("@discount", discount)
            }) > 0;
        }

        public DataTable GetProductLines(long salesOrderId)
        {
            string sql = @"SELECT p.productCode AS 'Product Code', spl.price AS 'Price',
                                  spl.orderQuantity AS 'Order Qty', spl.discountAmount AS 'Discount'
                           FROM SalesOrderProductLine spl
                           INNER JOIN Product p ON spl.productID = p.productID
                           WHERE spl.salesOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
        }

        public DataTable GetProductionOrdersBySalesOrder(long salesOrderId)
        {
            string sql = @"SELECT productionOrderCode AS 'Production Code',
                                  estFinishDate AS 'Est. Finish', status AS 'Status'
                           FROM ProductionOrder WHERE salesOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
        }

        public SalesOrder GetById(long salesOrderId)
        {
            string sql = @"SELECT salesOrderID, salesOrderCode, customerID, deliveryAddress, status, discount, remark
                           FROM SalesOrder WHERE salesOrderID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new SalesOrder
            {
                SalesOrderID = System.Convert.ToInt64(row["salesOrderID"]),
                SalesOrderCode = row["salesOrderCode"].ToString(),
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                DeliveryAddress = row["deliveryAddress"].ToString(),
                Status = System.Convert.ToInt32(row["status"]),
                Discount = System.Convert.ToDecimal(row["discount"]),
                Remark = row["remark"]?.ToString()
            };
        }

        public SalesOrder GetFullById(long salesOrderId)
        {
            string sql = @"SELECT salesOrderID, salesOrderCode, customerID, staffID, currencyCurrencyID,
                                  deliveryAddress, status, discount, discountType, remark
                           FROM SalesOrder WHERE salesOrderID = @id";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new SalesOrder
            {
                SalesOrderID = System.Convert.ToInt64(row["salesOrderID"]),
                SalesOrderCode = row["salesOrderCode"].ToString(),
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                StaffID = System.Convert.ToInt64(row["staffID"]),
                CurrencyCurrencyID = System.Convert.ToInt64(row["currencyCurrencyID"]),
                DeliveryAddress = row["deliveryAddress"].ToString(),
                Status = System.Convert.ToInt32(row["status"]),
                Discount = System.Convert.ToDecimal(row["discount"]),
                DiscountType = row["discountType"] == System.DBNull.Value ? null : row["discountType"].ToString(),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString()
            };
        }

        public DataTable GetProductLinesInternal(long salesOrderId)
        {
            string sql = @"SELECT productID, price, orderQuantity, warehouseReservedQty, shippedQuantity, invoicedQuantity
                           FROM SalesOrderProductLine WHERE salesOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
        }

        public bool Update(SalesOrder order)
        {
            string sql = @"UPDATE SalesOrder SET deliveryAddress = @address, status = @status,
                           discount = @discount, remark = @remark, lastModifyDate = NOW()
                           WHERE salesOrderID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@address", order.DeliveryAddress),
                new MySqlParameter("@status", order.Status),
                new MySqlParameter("@discount", order.Discount),
                new MySqlParameter("@remark", order.Remark ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", order.SalesOrderID)
            }) > 0;
        }

        public DataTable Search(SearchFilterCriteria filter)
        {
            string sql = @"SELECT so.salesOrderID AS 'Order ID',
                                  so.salesOrderCode AS 'Order Code',
                                  c.customerName AS 'Customer',
                                  so.deliveryAddress AS 'Delivery Address',
                                  so.createDate AS 'Create Date',
                                  so.status AS 'Status'
                           FROM SalesOrder so
                           LEFT JOIN Customer c ON so.customerID = c.customerID
                           WHERE 1=1";
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                conditions.Add("(so.salesOrderCode LIKE @kw OR c.customerName LIKE @kw OR so.deliveryAddress LIKE @kw)");
                parameters.Add(new MySqlParameter("@kw", "%" + filter.Keyword.Trim() + "%"));
            }
            SearchQueryHelper.AddDateFrom(conditions, parameters, "so.createDate", filter.FromDate);
            SearchQueryHelper.AddDateTo(conditions, parameters, "so.createDate", filter.ToDate);
            SearchQueryHelper.AddStatus(conditions, parameters, "so.status", filter.StatusInt);
            if (conditions.Count > 0) sql += " AND " + string.Join(" AND ", conditions);
            sql += " ORDER BY so.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, parameters.ToArray());
        }

        public int GetCount()
        {
            string sql = "SELECT COUNT(*) FROM SalesOrder";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                return System.Convert.ToInt32(dt.Rows[0][0]);
            }
            return 0;
        }
    }
}
