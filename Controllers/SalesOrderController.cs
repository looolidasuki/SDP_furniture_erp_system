using FurnitureERP.Helpers;
using MySql.Data.MySqlClient;
using Sales_user.Models;
using System;
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
                                  so.customerRefNumber AS 'Customer Ref Number',
                                  so.customerID AS 'Customer ID',
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
                 requestedDeliveryDate, customerRefNumber, discountType, discount, status, remark)
                VALUES (@code, @customerID, @staffID, @currencyID, @address,
                        @requestedDeliveryDate, @customerRefNumber, @discountType, @discount, @status, @remark)";
            return DatabaseConnect.ExecuteInsertReturnId(sql, new[] {
                new MySqlParameter("@code", order.SalesOrderCode),
                new MySqlParameter("@customerID", order.CustomerID),
                new MySqlParameter("@staffID", order.StaffID),
                new MySqlParameter("@currencyID", order.CurrencyCurrencyID),
                new MySqlParameter("@address", order.DeliveryAddress),
                new MySqlParameter("@requestedDeliveryDate", order.RequestedDeliveryDate ?? (object)System.DBNull.Value),
                new MySqlParameter("@customerRefNumber", string.IsNullOrWhiteSpace(order.CustomerRefNumber) ? (object)System.DBNull.Value : order.CustomerRefNumber.Trim()),
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

        public void UpdateCustomerRefNumberAfterInsert(long salesOrderId)
        {
            string refNo = FormatCustomerRefNumber(salesOrderId);
            DatabaseConnect.ExecuteNonQuery(
                @"UPDATE SalesOrder SET customerRefNumber = @ref
                  WHERE salesOrderID = @id
                    AND (customerRefNumber IS NULL OR TRIM(customerRefNumber) = '')",
                new[] {
                    new MySqlParameter("@ref", refNo),
                    new MySqlParameter("@id", salesOrderId)
                });
        }

        public static string FormatCustomerRefNumber(long id)
        {
            return "PO-PL-" + id.ToString("D9");
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

        public bool DeleteProductLines(long salesOrderId)
        {
            string sql = "DELETE FROM SalesOrderProductLine WHERE salesOrderID = @soID";
            DatabaseConnect.ExecuteNonQuery(sql, new[]
            {
                new MySqlParameter("@soID", salesOrderId)
            });
            return true;
        }

        public bool ReplaceProductLines(long salesOrderId, IEnumerable<(long ProductID, decimal Price, decimal Quantity, decimal Discount)> lines)
        {
            DeleteProductLines(salesOrderId);
            bool hasAny = false;
            foreach (var line in lines)
            {
                InsertProductLine(salesOrderId, line.ProductID, line.Price, line.Quantity, line.Discount);
                hasAny = true;
            }
            return hasAny;
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

        public DataTable GetProductLinesDetailed(long salesOrderId)
        {
            string sql = @"SELECT p.productCode AS 'Item',
                                  p.styleNumber AS 'Style',
                                  p.category AS 'Category',
                                  p.size AS 'Size',
                                  p.color AS 'Color',
                                  p.unit AS 'Unit',
                                  spl.price AS 'Unit Price',
                                  spl.orderQuantity AS 'Qty',
                                  spl.discountAmount AS 'Discount',
                                  (spl.price * spl.orderQuantity - spl.discountAmount) AS 'Amount'
                           FROM SalesOrderProductLine spl
                           INNER JOIN Product p ON spl.productID = p.productID
                           WHERE spl.salesOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
        }

        public DataTable GetHeaderDetail(long salesOrderId)
        {
            string sql = @"SELECT so.customerRefNumber AS 'Customer Ref Number',
                                  so.salesOrderCode AS 'Order Code',
                                  c.customerName AS 'Customer',
                                  cda.contactPerson AS 'Contact Person',
                                  c.billingAddress AS 'Customer Address',
                                  cda.phone AS 'Phone',
                                  c.paymentTerm AS 'Payment Terms',
                                  so.deliveryAddress AS 'Delivery Address',
                                  so.requestedDeliveryDate AS 'Requested Delivery Date',
                                  so.createDate AS 'Create Date',
                                  so.status AS 'Status',
                                  so.remark AS 'Remark'
                           FROM SalesOrder so
                           LEFT JOIN Customer c ON so.customerID = c.customerID
                           LEFT JOIN customerdeliveryaddress cda
                                ON cda.customerID = so.customerID
                               AND TRIM(cda.deliveryAddress) = TRIM(
                                   CASE
                                     WHEN LOCATE('(', so.deliveryAddress) > 0
                                       THEN TRIM(SUBSTRING(so.deliveryAddress, 1, LOCATE('(', so.deliveryAddress) - 1))
                                     ELSE so.deliveryAddress
                                   END)
                           LEFT JOIN Staff st ON so.staffID = st.staffID
                           WHERE so.salesOrderID = @id";
            var dt = DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
            EnrichHeaderDeliveryContact(dt);
            return dt;
        }

        private static void EnrichHeaderDeliveryContact(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            var row = dt.Rows[0];
            string delivery = row.Table.Columns.Contains("Delivery Address")
                ? row["Delivery Address"]?.ToString()
                : null;

            if (!DeliveryAddressDisplayHelper.TryParseCombined(delivery, out string addressOnly, out string parsedContact, out string parsedPhone))
                return;

            row["Delivery Address"] = addressOnly;
            if (dt.Columns.Contains("Contact Person") && string.IsNullOrWhiteSpace(row["Contact Person"]?.ToString()))
                row["Contact Person"] = parsedContact;
            if (dt.Columns.Contains("Phone") && string.IsNullOrWhiteSpace(row["Phone"]?.ToString()))
                row["Phone"] = parsedPhone;
        }

        public decimal GetTotalAmount(long salesOrderId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(price * orderQuantity - discountAmount), 0)
                  FROM SalesOrderProductLine WHERE salesOrderID = @id",
                new[] { new MySqlParameter("@id", salesOrderId) });
            return value == null || value == System.DBNull.Value ? 0 : System.Convert.ToDecimal(value);
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
            string sql = @"SELECT salesOrderID, salesOrderCode, customerID, deliveryAddress, requestedDeliveryDate,
                                  customerRefNumber,
                                  status, discount, remark
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
                RequestedDeliveryDate = row["requestedDeliveryDate"] == System.DBNull.Value ? (DateTime?)null : System.Convert.ToDateTime(row["requestedDeliveryDate"]),
                CustomerRefNumber = row["customerRefNumber"] == System.DBNull.Value ? null : row["customerRefNumber"].ToString(),
                Status = System.Convert.ToInt32(row["status"]),
                Discount = System.Convert.ToDecimal(row["discount"]),
                Remark = row["remark"]?.ToString()
            };
        }

        public SalesOrder GetFullById(long salesOrderId)
        {
            string sql = @"SELECT salesOrderID, salesOrderCode, customerID, staffID, currencyCurrencyID,
                                  deliveryAddress, requestedDeliveryDate,
                                  customerRefNumber,
                                  status, discount, discountType, remark
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
                RequestedDeliveryDate = row["requestedDeliveryDate"] == System.DBNull.Value ? (DateTime?)null : System.Convert.ToDateTime(row["requestedDeliveryDate"]),
                CustomerRefNumber = row["customerRefNumber"] == System.DBNull.Value ? null : row["customerRefNumber"].ToString(),
                Status = System.Convert.ToInt32(row["status"]),
                Discount = System.Convert.ToDecimal(row["discount"]),
                DiscountType = row["discountType"] == System.DBNull.Value ? null : row["discountType"].ToString(),
                Remark = row["remark"] == System.DBNull.Value ? null : row["remark"].ToString()
            };
        }

        public DataTable GetSalesOrdersPickerByCustomer(long customerId)
        {
            string sql = @"SELECT so.salesOrderID AS 'Order ID',
                                  so.salesOrderCode AS 'Order Code',
                                  so.customerRefNumber AS 'Customer Ref',
                                  so.status AS 'Status',
                                  so.createDate AS 'Create Date'
                           FROM SalesOrder so
                           WHERE so.customerID = @cid
                           ORDER BY so.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@cid", customerId) });
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
                           requestedDeliveryDate = @requestedDeliveryDate,
                           customerRefNumber = @customerRefNumber,
                           discount = @discount, remark = @remark, lastModifyDate = NOW()
                           WHERE salesOrderID = @id";
            return DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@address", order.DeliveryAddress),
                new MySqlParameter("@status", order.Status),
                new MySqlParameter("@requestedDeliveryDate", order.RequestedDeliveryDate ?? (object)System.DBNull.Value),
                new MySqlParameter("@customerRefNumber", string.IsNullOrWhiteSpace(order.CustomerRefNumber) ? (object)System.DBNull.Value : order.CustomerRefNumber.Trim()),
                new MySqlParameter("@discount", order.Discount),
                new MySqlParameter("@remark", order.Remark ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", order.SalesOrderID)
            }) > 0;
        }

        public DataTable Search(SearchFilterCriteria filter)
        {
            string sql = @"SELECT so.salesOrderID AS 'Order ID',
                                  so.salesOrderCode AS 'Order Code',
                                  so.customerRefNumber AS 'Customer Ref Number',
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
                conditions.Add("(so.salesOrderCode LIKE @kw OR so.customerRefNumber LIKE @kw OR c.customerName LIKE @kw OR so.deliveryAddress LIKE @kw)");
                parameters.Add(new MySqlParameter("@kw", "%" + filter.Keyword.Trim() + "%"));
            }
            SearchQueryHelper.AddDateFrom(conditions, parameters, "so.createDate", filter.FromDate);
            SearchQueryHelper.AddDateTo(conditions, parameters, "so.createDate", filter.ToDate);
            SearchQueryHelper.AddStatus(conditions, parameters, "so.status", filter.StatusInt);
            if (conditions.Count > 0) sql += " AND " + string.Join(" AND ", conditions);
            sql += " ORDER BY so.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, parameters.ToArray());
        }

        public DataTable GetProductLinesByCustomerRefNumber(string customerRefNumber)
        {
            string sql = @"SELECT so.salesOrderCode AS 'Order Code',
                                  so.customerRefNumber AS 'Customer Ref Number',
                                  p.productCode AS 'Item',
                                  p.styleNumber AS 'Style',
                                  spl.orderQuantity AS 'Qty',
                                  spl.price AS 'Unit Price',
                                  spl.discountAmount AS 'Discount',
                                  (spl.price * spl.orderQuantity - spl.discountAmount) AS 'Amount'
                           FROM SalesOrder so
                           INNER JOIN SalesOrderProductLine spl ON so.salesOrderID = spl.salesOrderID
                           INNER JOIN Product p ON spl.productID = p.productID
                           WHERE so.customerRefNumber = @ref
                           ORDER BY so.salesOrderID DESC, p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@ref", customerRefNumber ?? "") });
        }

        public DataTable GetCommonDeliveryAddressesByCustomer(long customerId)
        {
            string sql = @"SELECT DISTINCT deliveryAddress AS 'Delivery Address'
                           FROM SalesOrder
                           WHERE customerID = @customerID
                             AND deliveryAddress IS NOT NULL
                             AND TRIM(deliveryAddress) <> ''
                           ORDER BY deliveryAddress";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@customerID", customerId) });
        }

        public DataTable GetCommonCustomerRefNumbersByCustomer(long customerId)
        {
            string sql = @"SELECT DISTINCT customerRefNumber AS 'Customer Ref Number'
                           FROM SalesOrder
                           WHERE customerID = @customerID
                             AND customerRefNumber IS NOT NULL
                             AND TRIM(customerRefNumber) <> ''
                           ORDER BY customerRefNumber";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@customerID", customerId) });
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
