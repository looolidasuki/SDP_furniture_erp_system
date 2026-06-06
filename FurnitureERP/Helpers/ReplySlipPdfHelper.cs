using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace FurnitureERP.Helpers
{
    public class ReplySlipPdfData
    {
        public string SlipCode { get; set; }
        public string SalesOrder { get; set; }
        public string Customer { get; set; }
        public string Staff { get; set; }
        public string SignedBy { get; set; }
        public string SignedDate { get; set; }
        public string CreateDate { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public decimal TotalAmount { get; set; }
        public DataTable ProductLines { get; set; }
        public string SuggestedFileName { get; set; }
    }

    public static class ReplySlipPdfHelper
    {
        private const double Margin = 48;
        private const double LineHeight = 15;
        private static readonly XColor HeaderBg = XColor.FromArgb(47, 79, 79);
        private static readonly XColor Accent = XColor.FromArgb(70, 130, 180);
        private static readonly XColor Muted = XColor.FromArgb(100, 100, 100);
        private static readonly XColor Divider = XColor.FromArgb(220, 220, 220);

        public static bool ExportToPdf(ReplySlipPdfData data, IWin32Window owner = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            string defaultName = PdfExportHelper.SanitizeFileName(
                string.IsNullOrWhiteSpace(data.SuggestedFileName)
                    ? "ReplySlip_" + (data.SlipCode ?? "document")
                    : data.SuggestedFileName);
            if (!defaultName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                defaultName += ".pdf";

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF files (*.pdf)|*.pdf";
                sfd.DefaultExt = "pdf";
                sfd.FileName = defaultName;
                sfd.Title = "Save Reply Slip as PDF";
                if (sfd.ShowDialog(owner) != DialogResult.OK)
                    return false;

                WritePdf(sfd.FileName, data);
                return true;
            }
        }

        public static void WritePdf(string filePath, ReplySlipPdfData data)
        {
            var doc = new PdfDocument();
            doc.Info.Title = "Reply Slip — " + (data.SlipCode ?? "");
            doc.Info.Creator = "Furniture ERP";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);
            double pageWidth = page.Width.Point;
            double contentWidth = pageWidth - Margin * 2;
            double y = Margin;

            var titleFont = new XFont("Segoe UI", 20, XFontStyleEx.Bold);
            var codeFont = new XFont("Segoe UI", 13, XFontStyleEx.Bold);
            var metaFont = new XFont("Segoe UI", 8.5, XFontStyleEx.Regular);
            var sectionFont = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
            var labelFont = new XFont("Segoe UI", 8.5, XFontStyleEx.Bold);
            var valueFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var productTitleFont = new XFont("Segoe UI", 10, XFontStyleEx.Bold);
            var productMetaFont = new XFont("Segoe UI", 8.5, XFontStyleEx.Regular);
            var productPriceFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var amountFont = new XFont("Segoe UI", 10, XFontStyleEx.Bold);
            var totalLabelFont = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
            var totalAmountFont = new XFont("Segoe UI", 14, XFontStyleEx.Bold);

            // Header band
            double headerHeight = 72;
            y = EnsureSpace(doc, ref page, ref gfx, y, headerHeight + 8);
            gfx.DrawRectangle(new XSolidBrush(HeaderBg), Margin, y, contentWidth, headerHeight);
            gfx.DrawString("REPLY SLIP", titleFont, XBrushes.White, Margin + 16, y + 14);
            string code = data.SlipCode ?? "";
            double codeWidth = gfx.MeasureString(code, codeFont).Width;
            gfx.DrawString(code, codeFont, XBrushes.White, Margin + contentWidth - codeWidth - 16, y + 18);
            string generated = "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            gfx.DrawString(generated, metaFont, new XSolidBrush(XColor.FromArgb(220, 230, 230)), Margin + 16, y + 46);
            y += headerHeight + 20;

            // Info block — two columns
            double colGap = 24;
            double colWidth = (contentWidth - colGap) / 2;
            double leftX = Margin;
            double rightX = Margin + colWidth + colGap;
            double infoStartY = y;

            y = DrawInfoPair(gfx, leftX, y, colWidth, "Sales Order", data.SalesOrder, labelFont, valueFont);
            double rightY = DrawInfoPair(gfx, rightX, infoStartY, colWidth, "Status", data.Status, labelFont, valueFont);
            y = Math.Max(y, DrawInfoPair(gfx, rightX, rightY, colWidth, "Signed By", data.SignedBy, labelFont, valueFont));

            double row2Y = y;
            y = DrawInfoPair(gfx, leftX, row2Y, colWidth, "Customer", data.Customer, labelFont, valueFont);
            rightY = DrawInfoPair(gfx, rightX, row2Y, colWidth, "Signed Date", data.SignedDate, labelFont, valueFont);
            y = Math.Max(y, rightY);

            double row3Y = y;
            y = DrawInfoPair(gfx, leftX, row3Y, colWidth, "Staff", data.Staff, labelFont, valueFont);
            rightY = DrawInfoPair(gfx, rightX, row3Y, colWidth, "Create Date", data.CreateDate, labelFont, valueFont);
            y = Math.Max(y, rightY) + 8;

            // Remark
            if (!string.IsNullOrWhiteSpace(data.Remark))
            {
                y = EnsureSpace(doc, ref page, ref gfx, y, 50);
                double remarkBoxHeight = MeasureRemarkBox(gfx, data.Remark, valueFont, contentWidth - 24) + 28;
                y = EnsureSpace(doc, ref page, ref gfx, y, remarkBoxHeight);
                gfx.DrawRectangle(new XSolidBrush(XColors.WhiteSmoke), Margin, y, contentWidth, remarkBoxHeight);
                gfx.DrawRectangle(new XPen(Divider, 0.8), Margin, y, contentWidth, remarkBoxHeight);
                gfx.DrawString("Remark", labelFont, new XSolidBrush(Accent), Margin + 12, y + 10);
                DrawWrapped(gfx, data.Remark, valueFont, XBrushes.Black, Margin + 12, y + 26, contentWidth - 24);
                y += remarkBoxHeight + 16;
            }

            // Products section
            y = EnsureSpace(doc, ref page, ref gfx, y, 30);
            gfx.DrawString("Products", sectionFont, new XSolidBrush(HeaderBg), Margin, y);
            y += 6;
            gfx.DrawLine(new XPen(Accent, 1.5), Margin, y, Margin + 80, y);
            y += 18;

            var lines = data.ProductLines;
            if (lines != null && lines.Rows.Count > 0)
            {
                int index = 1;
                foreach (DataRow row in lines.Rows)
                {
                    if (IsTotalRow(row)) continue;

                    string productCode = GetCell(row, "Product Code");
                    string style = GetCell(row, "Style");
                    string category = GetCell(row, "Category");
                    string size = GetCell(row, "Size");
                    string color = GetCell(row, "Color");
                    string unit = GetCell(row, "Unit");
                    decimal unitPrice = ParseDecimal(row, "Unit Price");
                    decimal qty = ParseDecimal(row, "Qty");
                    decimal discount = ParseDecimal(row, "Discount");
                    decimal amount = ParseDecimal(row, "Amount");
                    if (amount == 0 && qty > 0)
                        amount = unitPrice * qty - discount;

                    double blockHeight = 58;
                    y = EnsureSpace(doc, ref page, ref gfx, y, blockHeight + 8);

                    string title = string.IsNullOrWhiteSpace(style)
                        ? productCode
                        : productCode + "  ·  " + style;
                    gfx.DrawString(index + ".", productTitleFont, new XSolidBrush(Accent), Margin, y + 2);
                    gfx.DrawString(title, productTitleFont, XBrushes.Black, Margin + 18, y + 2);

                    string meta = JoinMeta(category, size, color, unit);
                    if (!string.IsNullOrWhiteSpace(meta))
                        gfx.DrawString(meta, productMetaFont, new XSolidBrush(Muted), Margin + 18, y + 20);

                    string priceLine = FormatQty(qty) + " × " + FormatMoney(unitPrice);
                    if (discount > 0)
                        priceLine += "   − Discount " + FormatMoney(discount);
                    gfx.DrawString(priceLine, productPriceFont, XBrushes.Black, Margin + 18, y + 38);

                    string amountText = FormatMoney(amount);
                    double amountWidth = gfx.MeasureString(amountText, amountFont).Width;
                    gfx.DrawString(amountText, amountFont, XBrushes.Black, Margin + contentWidth - amountWidth, y + 36);

                    y += blockHeight;
                    gfx.DrawLine(new XPen(Divider, 0.6), Margin, y, Margin + contentWidth, y);
                    y += 10;
                    index++;
                }
            }
            else
            {
                y = EnsureSpace(doc, ref page, ref gfx, y, LineHeight);
                gfx.DrawString("No product lines.", valueFont, new XSolidBrush(Muted), Margin, y);
                y += LineHeight + 8;
            }

            // Total footer
            double totalBoxHeight = 44;
            y = EnsureSpace(doc, ref page, ref gfx, y, totalBoxHeight + 12);
            y += 6;
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(245, 248, 252)), Margin, y, contentWidth, totalBoxHeight);
            gfx.DrawRectangle(new XPen(Accent, 1), Margin, y, contentWidth, totalBoxHeight);

            string totalLabel = "TOTAL AMOUNT";
            string totalValue = FormatMoney(data.TotalAmount);
            double totalValueWidth = gfx.MeasureString(totalValue, totalAmountFont).Width;
            double totalLabelWidth = gfx.MeasureString(totalLabel, totalLabelFont).Width;

            gfx.DrawString(totalLabel, totalLabelFont, new XSolidBrush(HeaderBg),
                Margin + contentWidth - totalValueWidth - totalLabelWidth - 20, y + 14);
            gfx.DrawString(totalValue, totalAmountFont, new XSolidBrush(Accent),
                Margin + contentWidth - totalValueWidth - 12, y + 12);

            gfx.Dispose();
            doc.Save(filePath);
        }

        public static ReplySlipPdfData FromHeaderAndLines(DataTable header, DataTable lines, decimal total, string fileNameHint = null)
        {
            if (header == null || header.Rows.Count == 0)
                throw new ArgumentException("Header is required.", nameof(header));

            var row = header.Rows[0];
            string slipCode = GetCell(row, "Reply Slip Code");
            string status = GetCell(row, "Status");
            if (int.TryParse(status, out int statusCode))
                status = DictionaryService.GetDisplayName(DictionaryService.Categories.ReplySlip, statusCode);

            var productLines = lines?.Copy();
            RemoveTotalRows(productLines);

            return new ReplySlipPdfData
            {
                SlipCode = slipCode,
                SalesOrder = GetCell(row, "Sales Order"),
                Customer = GetCell(row, "Customer"),
                Staff = GetCell(row, "Staff"),
                SignedBy = GetCell(row, "Signed By"),
                SignedDate = FormatDateValue(row, "Signed Date"),
                CreateDate = FormatDateValue(row, "Create Date"),
                Status = status,
                Remark = GetCell(row, "Remark"),
                TotalAmount = total,
                ProductLines = productLines,
                SuggestedFileName = fileNameHint
            };
        }

        private static void RemoveTotalRows(DataTable lines)
        {
            if (lines == null || !lines.Columns.Contains("Product Code")) return;
            for (int i = lines.Rows.Count - 1; i >= 0; i--)
            {
                if (IsTotalRow(lines.Rows[i]))
                    lines.Rows.RemoveAt(i);
            }
        }

        private static bool IsTotalRow(DataRow row)
        {
            return string.Equals(GetCell(row, "Product Code"), "Total Amount", StringComparison.OrdinalIgnoreCase);
        }

        private static double DrawInfoPair(XGraphics gfx, double x, double y, double width, string label, string value,
            XFont labelFont, XFont valueFont)
        {
            gfx.DrawString(label, labelFont, new XSolidBrush(Muted), x, y);
            double valueY = DrawWrapped(gfx, value ?? "—", valueFont, XBrushes.Black, x, y + 13, width);
            return valueY + 6;
        }

        private static double MeasureRemarkBox(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text)) return LineHeight;
            return WrapText(text, font, gfx, maxWidth).Count * LineHeight;
        }

        private static string JoinMeta(params string[] parts)
        {
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append(part);
            }
            return sb.ToString();
        }

        private static string FormatMoney(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture);
        }

        private static string FormatQty(decimal value)
        {
            return value % 1 == 0 ? value.ToString("0", CultureInfo.InvariantCulture) : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static decimal ParseDecimal(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column)) return 0;
            object val = row[column];
            if (val == null || val == DBNull.Value) return 0;
            return Convert.ToDecimal(val);
        }

        private static string GetCell(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column)) return "";
            object val = row[column];
            return val == null || val == DBNull.Value ? "" : val.ToString().Trim();
        }

        private static string FormatDateValue(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column)) return "";
            object val = row[column];
            if (val == null || val == DBNull.Value) return "";
            if (val is DateTime dt)
                return dt.TimeOfDay.TotalSeconds < 1 ? dt.ToString("yyyy-MM-dd") : dt.ToString("yyyy-MM-dd HH:mm");
            if (DateTime.TryParse(val.ToString(), out DateTime parsed))
                return parsed.TimeOfDay.TotalSeconds < 1 ? parsed.ToString("yyyy-MM-dd") : parsed.ToString("yyyy-MM-dd HH:mm");
            return val.ToString();
        }

        private static double EnsureSpace(PdfDocument doc, ref PdfPage page, ref XGraphics gfx, double y, double needed)
        {
            double bottom = page.Height.Point - Margin;
            if (y + needed <= bottom)
                return y;

            gfx.Dispose();
            page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            return Margin;
        }

        private static double DrawWrapped(XGraphics gfx, string text, XFont font, XBrush brush, double x, double y, double maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return y;

            foreach (var line in WrapText(text, font, gfx, maxWidth))
            {
                gfx.DrawString(line, font, brush, x, y);
                y += LineHeight;
            }
            return y;
        }

        private static System.Collections.Generic.List<string> WrapText(string text, XFont font, XGraphics gfx, double maxWidth)
        {
            var result = new System.Collections.Generic.List<string>();
            foreach (string paragraph in (text ?? "").Replace("\r", "").Split('\n'))
            {
                string remaining = paragraph;
                while (!string.IsNullOrEmpty(remaining))
                {
                    int fit = remaining.Length;
                    while (fit > 0 && gfx.MeasureString(remaining.Substring(0, fit), font).Width > maxWidth)
                        fit--;
                    if (fit == 0) fit = 1;
                    result.Add(remaining.Substring(0, fit).TrimEnd());
                    remaining = remaining.Substring(fit).TrimStart();
                }
            }
            if (result.Count == 0) result.Add("");
            return result;
        }
    }

    public class DeliveryNotePdfData
    {
        public string NoteCode { get; set; }
        public string SalesOrder { get; set; }
        public string CustomerRefNumber { get; set; }
        public string Customer { get; set; }
        public string Warehouse { get; set; }
        public string Staff { get; set; }
        public string ShipMethod { get; set; }
        public string TrackingNumber { get; set; }
        public string CreateDate { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalShipQty { get; set; }
        public DataTable ProductLines { get; set; }
        public string SuggestedFileName { get; set; }
    }

    public static class DeliveryNotePdfHelper
    {
        private const double Margin = 48;
        private const double LineHeight = 15;
        private static readonly XColor HeaderBg = XColor.FromArgb(47, 79, 79);
        private static readonly XColor Accent = XColor.FromArgb(70, 130, 180);
        private static readonly XColor Muted = XColor.FromArgb(100, 100, 100);
        private static readonly XColor Divider = XColor.FromArgb(220, 220, 220);

        public static bool ExportToPdf(DeliveryNotePdfData data, IWin32Window owner = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            string defaultName = PdfExportHelper.SanitizeFileName(
                string.IsNullOrWhiteSpace(data.SuggestedFileName)
                    ? "DeliveryNote_" + (data.NoteCode ?? "document")
                    : data.SuggestedFileName);
            if (!defaultName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                defaultName += ".pdf";

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF files (*.pdf)|*.pdf";
                sfd.DefaultExt = "pdf";
                sfd.FileName = defaultName;
                sfd.Title = "Save Delivery Note as PDF";
                if (sfd.ShowDialog(owner) != DialogResult.OK)
                    return false;

                WritePdf(sfd.FileName, data);
                return true;
            }
        }

        public static void WritePdf(string filePath, DeliveryNotePdfData data)
        {
            var doc = new PdfDocument();
            doc.Info.Title = "Delivery Note — " + (data.NoteCode ?? "");
            doc.Info.Creator = "Furniture ERP";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);
            double contentWidth = page.Width.Point - Margin * 2;
            double y = Margin;

            var titleFont = new XFont("Segoe UI", 20, XFontStyleEx.Bold);
            var codeFont = new XFont("Segoe UI", 13, XFontStyleEx.Bold);
            var metaFont = new XFont("Segoe UI", 8.5, XFontStyleEx.Regular);
            var sectionFont = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
            var labelFont = new XFont("Segoe UI", 8.5, XFontStyleEx.Bold);
            var valueFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var productTitleFont = new XFont("Segoe UI", 10, XFontStyleEx.Bold);
            var productMetaFont = new XFont("Segoe UI", 8.5, XFontStyleEx.Regular);
            var productPriceFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var amountFont = new XFont("Segoe UI", 10, XFontStyleEx.Bold);
            var totalLabelFont = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
            var totalAmountFont = new XFont("Segoe UI", 14, XFontStyleEx.Bold);
            var totalMetaFont = new XFont("Segoe UI", 9, XFontStyleEx.Regular);

            double headerHeight = 72;
            y = EnsureSpace(doc, ref page, ref gfx, y, headerHeight + 8);
            gfx.DrawRectangle(new XSolidBrush(HeaderBg), Margin, y, contentWidth, headerHeight);
            gfx.DrawString("DELIVERY NOTE", titleFont, XBrushes.White, Margin + 16, y + 14);
            string code = data.NoteCode ?? "";
            double codeWidth = gfx.MeasureString(code, codeFont).Width;
            gfx.DrawString(code, codeFont, XBrushes.White, Margin + contentWidth - codeWidth - 16, y + 18);
            gfx.DrawString("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"), metaFont,
                new XSolidBrush(XColor.FromArgb(220, 230, 230)), Margin + 16, y + 46);
            y += headerHeight + 20;

            double colGap = 24;
            double colWidth = (contentWidth - colGap) / 2;
            double leftX = Margin;
            double rightX = Margin + colWidth + colGap;
            double infoStartY = y;

            y = DrawInfoPair(gfx, leftX, y, colWidth, "Sales Order", data.SalesOrder, labelFont, valueFont);
            double rightY = DrawInfoPair(gfx, rightX, infoStartY, colWidth, "Status", data.Status, labelFont, valueFont);
            y = Math.Max(y, DrawInfoPair(gfx, rightX, rightY, colWidth, "Ship Method", data.ShipMethod, labelFont, valueFont));

            double row2Y = y;
            y = DrawInfoPair(gfx, leftX, row2Y, colWidth, "Customer", data.Customer, labelFont, valueFont);
            rightY = DrawInfoPair(gfx, rightX, row2Y, colWidth, "Tracking No.", data.TrackingNumber, labelFont, valueFont);
            y = Math.Max(y, rightY);

            double row3Y = y;
            y = DrawInfoPair(gfx, leftX, row3Y, colWidth, "Warehouse", data.Warehouse, labelFont, valueFont);
            rightY = DrawInfoPair(gfx, rightX, row3Y, colWidth, "Create Date", data.CreateDate, labelFont, valueFont);
            y = Math.Max(y, rightY);

            double row4Y = y;
            y = DrawInfoPair(gfx, leftX, row4Y, colWidth, "Staff", data.Staff, labelFont, valueFont);
            rightY = DrawInfoPair(gfx, rightX, row4Y, colWidth, "Customer Ref", data.CustomerRefNumber, labelFont, valueFont);
            y = Math.Max(y, rightY) + 8;

            if (!string.IsNullOrWhiteSpace(data.Remark))
            {
                double remarkBoxHeight = MeasureRemarkBox(gfx, data.Remark, valueFont, contentWidth - 24) + 28;
                y = EnsureSpace(doc, ref page, ref gfx, y, remarkBoxHeight);
                gfx.DrawRectangle(new XSolidBrush(XColors.WhiteSmoke), Margin, y, contentWidth, remarkBoxHeight);
                gfx.DrawRectangle(new XPen(Divider, 0.8), Margin, y, contentWidth, remarkBoxHeight);
                gfx.DrawString("Remark", labelFont, new XSolidBrush(Accent), Margin + 12, y + 10);
                DrawWrapped(gfx, data.Remark, valueFont, XBrushes.Black, Margin + 12, y + 26, contentWidth - 24);
                y += remarkBoxHeight + 16;
            }

            y = EnsureSpace(doc, ref page, ref gfx, y, 30);
            gfx.DrawString("Products", sectionFont, new XSolidBrush(HeaderBg), Margin, y);
            y += 6;
            gfx.DrawLine(new XPen(Accent, 1.5), Margin, y, Margin + 80, y);
            y += 18;

            var lines = data.ProductLines;
            if (lines != null && lines.Rows.Count > 0)
            {
                int index = 1;
                foreach (DataRow row in lines.Rows)
                {
                    if (IsTotalRow(row)) continue;

                    string productCode = GetCell(row, "Product Code");
                    string style = GetCell(row, "Style");
                    if (string.IsNullOrWhiteSpace(style))
                        style = GetCell(row, "Style Number");
                    string category = GetCell(row, "Category");
                    string size = GetCell(row, "Size");
                    string color = GetCell(row, "Color");
                    string unit = GetCell(row, "Unit");
                    decimal unitPrice = ParseDecimal(row, "Unit Price");
                    decimal shipQty = ParseDecimal(row, "Ship Qty");
                    decimal discount = ParseDecimal(row, "Discount");
                    decimal amount = ParseDecimal(row, "Amount");
                    if (amount == 0 && shipQty > 0)
                        amount = unitPrice * shipQty - discount;

                    double blockHeight = 58;
                    y = EnsureSpace(doc, ref page, ref gfx, y, blockHeight + 8);

                    string title = string.IsNullOrWhiteSpace(style)
                        ? productCode
                        : productCode + "  ·  " + style;
                    gfx.DrawString(index + ".", productTitleFont, new XSolidBrush(Accent), Margin, y + 2);
                    gfx.DrawString(title, productTitleFont, XBrushes.Black, Margin + 18, y + 2);

                    string meta = JoinMeta(category, size, color, unit);
                    if (!string.IsNullOrWhiteSpace(meta))
                        gfx.DrawString(meta, productMetaFont, new XSolidBrush(Muted), Margin + 18, y + 20);

                    string priceLine = "Ship " + FormatQty(shipQty) + "  ×  " + FormatMoney(unitPrice);
                    if (discount > 0)
                        priceLine += "   − Discount " + FormatMoney(discount);
                    gfx.DrawString(priceLine, productPriceFont, XBrushes.Black, Margin + 18, y + 38);

                    string amountText = FormatMoney(amount);
                    double amountWidth = gfx.MeasureString(amountText, amountFont).Width;
                    gfx.DrawString(amountText, amountFont, XBrushes.Black, Margin + contentWidth - amountWidth, y + 36);

                    y += blockHeight;
                    gfx.DrawLine(new XPen(Divider, 0.6), Margin, y, Margin + contentWidth, y);
                    y += 10;
                    index++;
                }
            }
            else
            {
                y = EnsureSpace(doc, ref page, ref gfx, y, LineHeight);
                gfx.DrawString("No product lines.", valueFont, new XSolidBrush(Muted), Margin, y);
                y += LineHeight + 8;
            }

            double totalBoxHeight = 52;
            y = EnsureSpace(doc, ref page, ref gfx, y, totalBoxHeight + 12);
            y += 6;
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(245, 248, 252)), Margin, y, contentWidth, totalBoxHeight);
            gfx.DrawRectangle(new XPen(Accent, 1), Margin, y, contentWidth, totalBoxHeight);

            gfx.DrawString("Total Ship Qty: " + data.TotalShipQty, totalMetaFont, new XSolidBrush(Muted), Margin + 14, y + 18);

            string totalLabel = "TOTAL AMOUNT";
            string totalValue = FormatMoney(data.TotalAmount);
            double totalValueWidth = gfx.MeasureString(totalValue, totalAmountFont).Width;
            double totalLabelWidth = gfx.MeasureString(totalLabel, totalLabelFont).Width;
            gfx.DrawString(totalLabel, totalLabelFont, new XSolidBrush(HeaderBg),
                Margin + contentWidth - totalValueWidth - totalLabelWidth - 20, y + 18);
            gfx.DrawString(totalValue, totalAmountFont, new XSolidBrush(Accent),
                Margin + contentWidth - totalValueWidth - 12, y + 16);

            gfx.Dispose();
            doc.Save(filePath);
        }

        public static DeliveryNotePdfData FromHeaderAndLines(DataTable header, DataTable lines, decimal total, int totalShipQty, string fileNameHint = null)
        {
            if (header == null || header.Rows.Count == 0)
                throw new ArgumentException("Header is required.", nameof(header));

            var row = header.Rows[0];
            string status = GetCell(row, "Status");
            if (int.TryParse(status, out int statusCode))
                status = DictionaryService.GetDisplayName(DictionaryService.Categories.Delivery, statusCode);

            var productLines = lines?.Copy();
            RemoveTotalRows(productLines);

            return new DeliveryNotePdfData
            {
                NoteCode = GetCell(row, "Delivery Note Code"),
                SalesOrder = GetCell(row, "Sales Order"),
                CustomerRefNumber = GetCell(row, "Customer Ref Number"),
                Customer = GetCell(row, "Customer"),
                Warehouse = GetCell(row, "Warehouse"),
                Staff = GetCell(row, "Staff"),
                ShipMethod = GetCell(row, "Ship Method"),
                TrackingNumber = GetCell(row, "Tracking Number"),
                CreateDate = FormatDateValue(row, "Create Date"),
                Status = status,
                Remark = GetCell(row, "Remark"),
                TotalAmount = total,
                TotalShipQty = totalShipQty,
                ProductLines = productLines,
                SuggestedFileName = fileNameHint
            };
        }

        private static void RemoveTotalRows(DataTable lines)
        {
            if (lines == null || !lines.Columns.Contains("Product Code")) return;
            for (int i = lines.Rows.Count - 1; i >= 0; i--)
            {
                if (IsTotalRow(lines.Rows[i]))
                    lines.Rows.RemoveAt(i);
            }
        }

        private static bool IsTotalRow(DataRow row)
        {
            return string.Equals(GetCell(row, "Product Code"), "Total Amount", StringComparison.OrdinalIgnoreCase);
        }

        private static double DrawInfoPair(XGraphics gfx, double x, double y, double width, string label, string value,
            XFont labelFont, XFont valueFont)
        {
            gfx.DrawString(label, labelFont, new XSolidBrush(Muted), x, y);
            double valueY = DrawWrapped(gfx, value ?? "—", valueFont, XBrushes.Black, x, y + 13, width);
            return valueY + 6;
        }

        private static double MeasureRemarkBox(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text)) return LineHeight;
            return WrapText(text, font, gfx, maxWidth).Count * LineHeight;
        }

        private static string JoinMeta(params string[] parts)
        {
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append(part);
            }
            return sb.ToString();
        }

        private static string FormatMoney(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);

        private static string FormatQty(decimal value)
        {
            return value % 1 == 0 ? value.ToString("0", CultureInfo.InvariantCulture) : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static decimal ParseDecimal(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column)) return 0;
            object val = row[column];
            if (val == null || val == DBNull.Value) return 0;
            return Convert.ToDecimal(val);
        }

        private static string GetCell(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column)) return "";
            object val = row[column];
            return val == null || val == DBNull.Value ? "" : val.ToString().Trim();
        }

        private static string FormatDateValue(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column)) return "";
            object val = row[column];
            if (val == null || val == DBNull.Value) return "";
            if (val is DateTime dt)
                return dt.TimeOfDay.TotalSeconds < 1 ? dt.ToString("yyyy-MM-dd") : dt.ToString("yyyy-MM-dd HH:mm");
            if (DateTime.TryParse(val.ToString(), out DateTime parsed))
                return parsed.TimeOfDay.TotalSeconds < 1 ? parsed.ToString("yyyy-MM-dd") : parsed.ToString("yyyy-MM-dd HH:mm");
            return val.ToString();
        }

        private static double EnsureSpace(PdfDocument doc, ref PdfPage page, ref XGraphics gfx, double y, double needed)
        {
            double bottom = page.Height.Point - Margin;
            if (y + needed <= bottom) return y;

            gfx.Dispose();
            page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            return Margin;
        }

        private static double DrawWrapped(XGraphics gfx, string text, XFont font, XBrush brush, double x, double y, double maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return y;
            foreach (var line in WrapText(text, font, gfx, maxWidth))
            {
                gfx.DrawString(line, font, brush, x, y);
                y += LineHeight;
            }
            return y;
        }

        private static System.Collections.Generic.List<string> WrapText(string text, XFont font, XGraphics gfx, double maxWidth)
        {
            var result = new System.Collections.Generic.List<string>();
            foreach (string paragraph in (text ?? "").Replace("\r", "").Split('\n'))
            {
                string remaining = paragraph;
                while (!string.IsNullOrEmpty(remaining))
                {
                    int fit = remaining.Length;
                    while (fit > 0 && gfx.MeasureString(remaining.Substring(0, fit), font).Width > maxWidth)
                        fit--;
                    if (fit == 0) fit = 1;
                    result.Add(remaining.Substring(0, fit).TrimEnd());
                    remaining = remaining.Substring(fit).TrimStart();
                }
            }
            if (result.Count == 0) result.Add("");
            return result;
        }
    }
}
