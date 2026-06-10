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
        private readonly CurrencyController _currencyCtrl = new CurrencyController();

        public DataTable GetAllSalesOrders()
        {
            string sql = @"SELECT so.salesOrderCode AS 'Order Code',
                                  so.customerReferenceNumber AS 'Customer Ref Number',
                                  c.customerName AS 'Customer',
                                  cur.currencyCode AS 'Currency',
                                  so.totalAmount AS 'Total',
                                  so.totalAmountBase AS 'Total (HKD)',
                                  so.exchangeRate AS 'Rate',
                                  so.deliveryAddress AS 'Delivery Address',
                                  so.createDate AS 'Create Date',
                                  so.status AS 'Status',
                                  so.salesOrderID AS 'Order ID',
                                  so.customerID AS 'Customer ID'
                           FROM SalesOrder so
                           LEFT JOIN Customer c ON so.customerID = c.customerID
                           LEFT JOIN Currency cur ON so.currencyCurrencyID = cur.currencyID
                           ORDER BY so.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql);
        }

        public long Insert(SalesOrder order)
        {
            long id = DatabaseConnect.InsertWithAllocatedId("salesorder", "salesOrderID",
                BuildInsertHeaderSql(),
                BuildInsertHeaderParameters(order));
            if (id > 0)
                DocumentAuditService.LogCreate(DocumentAuditService.Types.SalesOrder, id, DocumentCodeHelper.Build("SO", id));
            return id;
        }

        public long InsertInTransaction(MySqlConnection conn, MySqlTransaction trans, SalesOrder order)
        {
            return DatabaseConnect.InsertWithAllocatedId(conn, trans, "salesorder", "salesOrderID",
                BuildInsertHeaderSql(),
                BuildInsertHeaderParameters(order));
        }

        private static string BuildInsertHeaderSql()
        {
            // Use @soStatus — not @status (MySQL treats @status as a user variable, often NULL).
            return @"INSERT INTO SalesOrder
                (salesOrderID, salesOrderCode, customerID, staffID, currencyCurrencyID, exchangeRate,
                 totalAmount, totalAmountBase, deliveryAddress,
                 requestedDeliveryDate, customerReferenceNumber, discountType, discount, status, remark)
                VALUES (@id, @code, @customerID, @staffID, @currencyID, @rate,
                        @total, @totalBase, @address,
                        @requestedDeliveryDate, @customerReferenceNumber, @discountType, @discount, @soStatus, @remark)";
        }

        private MySqlParameter[] BuildInsertHeaderParameters(SalesOrder order)
        {
            if (order.CurrencyCurrencyID <= 0) order.CurrencyCurrencyID = 1;
            if (order.ExchangeRate <= 0)
                order.ExchangeRate = _currencyCtrl.LockRateForCurrency(order.CurrencyCurrencyID);

            return new[]
            {
                new MySqlParameter("@code", order.SalesOrderCode),
                new MySqlParameter("@customerID", order.CustomerID),
                new MySqlParameter("@staffID", order.StaffID),
                new MySqlParameter("@currencyID", order.CurrencyCurrencyID),
                new MySqlParameter("@rate", order.ExchangeRate),
                new MySqlParameter("@total", order.TotalAmount),
                new MySqlParameter("@totalBase", order.TotalAmountBase),
                new MySqlParameter("@address", order.DeliveryAddress),
                new MySqlParameter("@requestedDeliveryDate", order.RequestedDeliveryDate ?? (object)DBNull.Value),
                new MySqlParameter("@customerReferenceNumber", string.IsNullOrWhiteSpace(order.CustomerRefNumber) ? (object)DBNull.Value : order.CustomerRefNumber.Trim()),
                new MySqlParameter("@discountType", order.DiscountType ?? (object)DBNull.Value),
                new MySqlParameter("@discount", order.Discount),
                new MySqlParameter("@soStatus", order.Status),
                new MySqlParameter("@remark", order.Remark ?? (object)DBNull.Value)
            };
        }

        public void UpdateCodeAfterInsert(long salesOrderId)
        {
            DatabaseConnect.ExecuteNonQuery(
                "UPDATE SalesOrder SET salesOrderCode = @code WHERE salesOrderID = @id",
                new[] {
                    new MySqlParameter("@code", DocumentCodeHelper.Build("SO", salesOrderId)),
                    new MySqlParameter("@id", salesOrderId)
                });
        }

        public void UpdateCustomerRefNumberAfterInsert(long salesOrderId)
        {
            string refNo = FormatCustomerRefNumber(salesOrderId);
            DatabaseConnect.ExecuteNonQuery(
                @"UPDATE SalesOrder SET customerReferenceNumber = @ref
                  WHERE salesOrderID = @id
                    AND (customerReferenceNumber IS NULL OR TRIM(customerReferenceNumber) = '')",
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
            return DatabaseConnect.ExecuteNonQuery(
                BuildInsertProductLineSql(),
                BuildInsertProductLineParameters(salesOrderId, productId, price, orderQty, discount)) > 0;
        }

        public void InsertProductLineInTransaction(
            MySqlConnection conn,
            MySqlTransaction trans,
            long salesOrderId,
            long productId,
            decimal price,
            decimal orderQty,
            decimal discount)
        {
            DatabaseConnect.ExecuteNonQuery(conn, trans,
                BuildInsertProductLineSql(),
                BuildInsertProductLineParameters(salesOrderId, productId, price, orderQty, discount));
        }

        private static string BuildInsertProductLineSql()
        {
            return @"INSERT INTO SalesOrderProductLine
                (salesOrderID, productID, price, orderQuantity, discountAmount,
                 warehouseReservedQty, shippedQuantity, invoicedQuantity)
                VALUES (@soID, @productID, @price, @qty, @discount, 0, 0, 0)";
        }

        private static MySqlParameter[] BuildInsertProductLineParameters(
            long salesOrderId, long productId, decimal price, decimal orderQty, decimal discount)
        {
            return new[]
            {
                new MySqlParameter("@soID", salesOrderId),
                new MySqlParameter("@productID", productId),
                new MySqlParameter("@price", price),
                new MySqlParameter("@qty", orderQty),
                new MySqlParameter("@discount", discount)
            };
        }

        public void UpdateCodeAfterInsertInTransaction(MySqlConnection conn, MySqlTransaction trans, long salesOrderId)
        {
            DatabaseConnect.ExecuteNonQuery(conn, trans,
                "UPDATE SalesOrder SET salesOrderCode = @code WHERE salesOrderID = @id",
                new[]
                {
                    new MySqlParameter("@code", DocumentCodeHelper.Build("SO", salesOrderId)),
                    new MySqlParameter("@id", salesOrderId)
                });
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
            if (hasAny) RefreshTotals(salesOrderId);
            return hasAny;
        }

        public void RefreshTotals(long salesOrderId)
        {
            var order = GetFullById(salesOrderId);
            if (order == null) return;

            decimal total = GetTotalAmount(salesOrderId);
            decimal rate = order.ExchangeRate > 0
                ? order.ExchangeRate
                : _currencyCtrl.LockRateForCurrency(order.CurrencyCurrencyID);
            decimal baseTotal = CurrencyConversionService.ToBaseAmount(total, rate);

            DatabaseConnect.ExecuteNonQuery(
                @"UPDATE SalesOrder
                  SET totalAmount = @total, totalAmountBase = @base, exchangeRate = @rate, lastModifyDate = NOW()
                  WHERE salesOrderID = @id",
                new[]
                {
                    new MySqlParameter("@total", total),
                    new MySqlParameter("@base", baseTotal),
                    new MySqlParameter("@rate", rate),
                    new MySqlParameter("@id", salesOrderId)
                });
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
                                  spl.warehouseReservedQty AS 'Reserved',
                                  spl.shippedQuantity AS 'Shipped',
                                  spl.invoicedQuantity AS 'Invoiced',
                                  COALESCE(st.available, 0) AS 'Available Stock',
                                  spl.discountAmount AS 'Discount',
                                  (spl.price * spl.orderQuantity - spl.discountAmount) AS 'Amount'
                           FROM SalesOrderProductLine spl
                           INNER JOIN Product p ON spl.productID = p.productID
                           LEFT JOIN (
                               SELECT productID,
                                      SUM(GREATEST(physicalQuantity - reservedQuantity, 0)) AS available
                               FROM WarehouseProduct
                               GROUP BY productID
                           ) st ON p.productID = st.productID
                           WHERE spl.salesOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
        }

        public DataTable GetSalesOrdersForProductionPicker()
        {
            return new ProductionOrderController().GetSalesOrdersForProductionPicker();
        }

        public DataTable GetHeaderDetail(long salesOrderId)
        {
            string sql = @"SELECT so.customerReferenceNumber AS 'Customer Ref Number',
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

        public decimal GetLineSubtotal(long salesOrderId)
        {
            object value = DatabaseConnect.ExecuteScalar(
                @"SELECT COALESCE(SUM(price * orderQuantity - discountAmount), 0)
                  FROM SalesOrderProductLine WHERE salesOrderID = @id",
                new[] { new MySqlParameter("@id", salesOrderId) });
            return value == null || value == System.DBNull.Value ? 0 : System.Convert.ToDecimal(value);
        }

        public decimal GetTotalAmount(long salesOrderId)
        {
            decimal lineSubtotal = GetLineSubtotal(salesOrderId);
            var order = GetFullById(salesOrderId);
            if (order == null) return lineSubtotal;
            return OrderTotalCalculator.ApplyHeaderDiscount(lineSubtotal, order.DiscountType, order.Discount);
        }

        public DataTable GetProductionOrdersBySalesOrder(long salesOrderId)
        {
            string sql = @"SELECT productionOrderCode AS 'Production Code',
                                  estFinishDate AS 'Est. Finish', status AS 'Status'
                           FROM ProductionOrder WHERE salesOrderID = @id";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@id", salesOrderId) });
        }

        public SalesOrder GetByCode(string salesOrderCode)
        {
            if (string.IsNullOrWhiteSpace(salesOrderCode)) return null;
            string sql = @"SELECT salesOrderID, salesOrderCode, customerID, deliveryAddress, requestedDeliveryDate,
                                  customerReferenceNumber,
                                  status, discount, remark
                           FROM SalesOrder WHERE salesOrderCode = @code LIMIT 1";
            DataTable dt = DatabaseConnect.ExecuteQuery(sql, new[] {
                new MySqlParameter("@code", salesOrderCode.Trim())
            });
            if (dt == null || dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new SalesOrder
            {
                SalesOrderID = System.Convert.ToInt64(row["salesOrderID"]),
                SalesOrderCode = row["salesOrderCode"].ToString(),
                CustomerID = System.Convert.ToInt64(row["customerID"]),
                DeliveryAddress = row["deliveryAddress"].ToString(),
                RequestedDeliveryDate = row["requestedDeliveryDate"] == System.DBNull.Value ? (DateTime?)null : System.Convert.ToDateTime(row["requestedDeliveryDate"]),
                CustomerRefNumber = row["customerReferenceNumber"] == System.DBNull.Value ? null : row["customerReferenceNumber"].ToString(),
                Status = System.Convert.ToInt32(row["status"]),
                Discount = System.Convert.ToDecimal(row["discount"]),
                Remark = row["remark"]?.ToString()
            };
        }

        public SalesOrder GetById(long salesOrderId)
        {
            string sql = @"SELECT salesOrderID, salesOrderCode, customerID, deliveryAddress, requestedDeliveryDate,
                                  customerReferenceNumber,
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
                CustomerRefNumber = row["customerReferenceNumber"] == System.DBNull.Value ? null : row["customerReferenceNumber"].ToString(),
                Status = System.Convert.ToInt32(row["status"]),
                Discount = System.Convert.ToDecimal(row["discount"]),
                Remark = row["remark"]?.ToString()
            };
        }

        public SalesOrder GetFullById(long salesOrderId)
        {
            string sql = @"SELECT salesOrderID, salesOrderCode, customerID, staffID, currencyCurrencyID,
                                  exchangeRate, totalAmount, totalAmountBase,
                                  deliveryAddress, requestedDeliveryDate,
                                  customerReferenceNumber,
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
                ExchangeRate = row.Table.Columns.Contains("exchangeRate") && row["exchangeRate"] != System.DBNull.Value
                    ? System.Convert.ToDecimal(row["exchangeRate"]) : 1m,
                TotalAmount = row.Table.Columns.Contains("totalAmount") && row["totalAmount"] != System.DBNull.Value
                    ? System.Convert.ToDecimal(row["totalAmount"]) : 0m,
                TotalAmountBase = row.Table.Columns.Contains("totalAmountBase") && row["totalAmountBase"] != System.DBNull.Value
                    ? System.Convert.ToDecimal(row["totalAmountBase"]) : 0m,
                DeliveryAddress = row["deliveryAddress"].ToString(),
                RequestedDeliveryDate = row["requestedDeliveryDate"] == System.DBNull.Value ? (DateTime?)null : System.Convert.ToDateTime(row["requestedDeliveryDate"]),
                CustomerRefNumber = row["customerReferenceNumber"] == System.DBNull.Value ? null : row["customerReferenceNumber"].ToString(),
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
                                  so.customerReferenceNumber AS 'Customer Ref',
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
            var existing = GetFullById(order.SalesOrderID);
            long currencyId = order.CurrencyCurrencyID > 0 ? order.CurrencyCurrencyID : 1;
            decimal rate = existing != null && existing.CurrencyCurrencyID == currencyId && existing.ExchangeRate > 0
                ? existing.ExchangeRate
                : _currencyCtrl.LockRateForCurrency(currencyId);

            string sql = @"UPDATE SalesOrder SET deliveryAddress = @address, status = @soStatus,
                           requestedDeliveryDate = @requestedDeliveryDate,
                           customerReferenceNumber = @customerReferenceNumber,
                           currencyCurrencyID = @currencyID, exchangeRate = @rate,
                           discount = @discount, remark = @remark, lastModifyDate = NOW()
                           WHERE salesOrderID = @id";
            bool ok = DatabaseConnect.ExecuteNonQuery(sql, new[] {
                new MySqlParameter("@address", order.DeliveryAddress),
                new MySqlParameter("@soStatus", order.Status),
                new MySqlParameter("@requestedDeliveryDate", order.RequestedDeliveryDate ?? (object)System.DBNull.Value),
                new MySqlParameter("@customerReferenceNumber", string.IsNullOrWhiteSpace(order.CustomerRefNumber) ? (object)System.DBNull.Value : order.CustomerRefNumber.Trim()),
                new MySqlParameter("@currencyID", currencyId),
                new MySqlParameter("@rate", rate),
                new MySqlParameter("@discount", order.Discount),
                new MySqlParameter("@remark", order.Remark ?? (object)System.DBNull.Value),
                new MySqlParameter("@id", order.SalesOrderID)
            }) > 0;
            if (ok)
            {
                RefreshTotals(order.SalesOrderID);
                DocumentAuditService.LogUpdate(DocumentAuditService.Types.SalesOrder, order.SalesOrderID,
                    order.SalesOrderCode ?? DocumentCodeHelper.Build("SO", order.SalesOrderID),
                    "Status " + order.Status);
            }
            return ok;
        }

        public DataTable Search(SearchFilterCriteria filter)
        {
            string sql = @"SELECT so.salesOrderCode AS 'Order Code',
                                  so.customerReferenceNumber AS 'Customer Ref Number',
                                  c.customerName AS 'Customer',
                                  so.deliveryAddress AS 'Delivery Address',
                                  so.createDate AS 'Create Date',
                                  so.status AS 'Status',
                                  so.salesOrderID AS 'Order ID'
                           FROM SalesOrder so
                           LEFT JOIN Customer c ON so.customerID = c.customerID
                           WHERE 1=1";
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                conditions.Add("(so.salesOrderCode LIKE @kw OR so.customerReferenceNumber LIKE @kw OR c.customerName LIKE @kw OR so.deliveryAddress LIKE @kw)");
                parameters.Add(new MySqlParameter("@kw", "%" + filter.Keyword.Trim() + "%"));
            }
            SearchQueryHelper.AddDateFrom(conditions, parameters, "so.createDate", filter.FromDate);
            SearchQueryHelper.AddDateTo(conditions, parameters, "so.createDate", filter.ToDate);
            SearchQueryHelper.AddStatus(conditions, parameters, "so.status", filter.StatusInt);
            if (conditions.Count > 0) sql += " AND " + string.Join(" AND ", conditions);
            sql += " ORDER BY so.createDate DESC";
            return DatabaseConnect.ExecuteQuery(sql, parameters.ToArray());
        }

        public DataTable GetProductLinesByCustomerRefNumber(string customerReferenceNumber)
        {
            string sql = @"SELECT so.salesOrderCode AS 'Order Code',
                                  so.customerReferenceNumber AS 'Customer Ref Number',
                                  p.productCode AS 'Item',
                                  p.styleNumber AS 'Style',
                                  spl.orderQuantity AS 'Qty',
                                  spl.price AS 'Unit Price',
                                  spl.discountAmount AS 'Discount',
                                  (spl.price * spl.orderQuantity - spl.discountAmount) AS 'Amount'
                           FROM SalesOrder so
                           INNER JOIN SalesOrderProductLine spl ON so.salesOrderID = spl.salesOrderID
                           INNER JOIN Product p ON spl.productID = p.productID
                           WHERE so.customerReferenceNumber = @ref
                           ORDER BY so.salesOrderID DESC, p.productCode";
            return DatabaseConnect.ExecuteQuery(sql, new[] { new MySqlParameter("@ref", customerReferenceNumber ?? "") });
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
            string sql = @"SELECT DISTINCT customerReferenceNumber AS 'Customer Ref Number'
                           FROM SalesOrder
                           WHERE customerID = @customerID
                             AND customerReferenceNumber IS NOT NULL
                             AND TRIM(customerReferenceNumber) <> ''
                           ORDER BY customerReferenceNumber";
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
