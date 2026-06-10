using System;

using System.Collections.Generic;

using System.Drawing;



namespace FurnitureERP.Helpers

{

    public static class StatusStyleHelper

    {

        public struct StatusColors

        {

            public Color Background;

            public Color Foreground;



            public StatusColors(Color background, Color foreground)

            {

                Background = background;

                Foreground = foreground;

            }

        }



        private static readonly Dictionary<string, StatusColors> LabelPalette =

            new Dictionary<string, StatusColors>(StringComparer.OrdinalIgnoreCase)

            {

                // Common workflow

                ["Draft"] = C(243, 244, 246, 75, 85, 99),

                ["New"] = C(239, 246, 255, 29, 78, 216),

                ["Open"] = C(239, 246, 255, 37, 99, 235),

                ["Pending Scheduling"] = C(241, 245, 249, 71, 85, 105),

                ["Pending Approval"] = C(254, 243, 199, 180, 83, 9),

                ["Pending Verification"] = C(255, 251, 235, 217, 119, 6),



                // In progress / active work

                ["In Progress"] = C(219, 234, 254, 29, 78, 216),

                ["Processing"] = C(207, 250, 254, 14, 116, 144),

                ["Quality Checking"] = C(224, 231, 255, 67, 56, 202),

                ["Receiving"] = C(224, 242, 254, 3, 105, 161),

                ["Partially Issued"] = C(204, 251, 241, 15, 118, 110),

                ["Partial"] = C(204, 251, 241, 15, 118, 110),



                // Sales / quotation

                ["Sent"] = C(191, 219, 254, 37, 99, 235),

                ["Confirmed"] = C(209, 250, 229, 4, 120, 87),

                ["Accepted"] = C(198, 246, 213, 39, 103, 73),

                ["Converted"] = C(237, 233, 254, 91, 33, 182),



                // Approval / payment

                ["Approved"] = C(209, 250, 229, 5, 150, 105),

                ["Verified"] = C(220, 252, 231, 21, 128, 61),

                ["Ordered"] = C(255, 237, 213, 194, 65, 12),

                ["Paid"] = C(187, 247, 208, 20, 83, 45),

                ["Partially Paid"] = C(254, 249, 195, 161, 98, 7),

                ["Fully Paid"] = C(167, 243, 208, 22, 101, 52),

                ["Unpaid"] = C(254, 226, 226, 185, 28, 28),



                // Logistics

                ["Preparing"] = C(241, 245, 249, 100, 116, 139),

                ["Packed"] = C(237, 233, 254, 109, 40, 217),

                ["In Transit"] = C(243, 232, 255, 124, 58, 237),

                ["Shipped"] = C(153, 246, 228, 15, 118, 110),

                ["Delivered"] = C(209, 250, 229, 4, 120, 87),



                // Completion

                ["Completed"] = C(220, 252, 231, 22, 101, 52),

                ["Closed"] = C(236, 253, 245, 6, 95, 70),

                ["Signed"] = C(167, 243, 208, 6, 95, 70),



                // Active / inventory

                ["Active"] = C(236, 253, 245, 4, 120, 87),

                ["Inactive"] = C(243, 244, 246, 156, 163, 175),

                ["Below Safety Stock"] = C(254, 215, 170, 234, 88, 12),

                ["Out of Stock"] = C(254, 202, 202, 220, 38, 38),

                ["Discontinued"] = C(229, 231, 235, 107, 114, 128),



                // Hold / pause

                ["Paused"] = C(254, 243, 199, 202, 138, 4),



                // Negative / terminal

                ["Rejected"] = C(254, 226, 226, 153, 27, 27),

                ["Cancelled"] = C(245, 245, 244, 120, 113, 108),

                ["Voided"] = C(231, 229, 228, 87, 83, 78),

                ["Returned"] = C(252, 231, 243, 190, 24, 93),

                ["Overdue"] = C(254, 202, 202, 153, 27, 27),

                ["Failed"] = C(254, 202, 202, 185, 28, 28),

                ["Blocked"] = C(254, 215, 170, 194, 65, 12),

            };



        private static readonly Dictionary<string, StatusColors> CategoryCodePalette =

            new Dictionary<string, StatusColors>(StringComparer.OrdinalIgnoreCase)

            {

                [Key(DictionaryService.Categories.Production, 0)] = LabelPalette["Pending Scheduling"],

                [Key(DictionaryService.Categories.Production, 1)] = LabelPalette["In Progress"],

                [Key(DictionaryService.Categories.Production, 2)] = LabelPalette["Quality Checking"],

                [Key(DictionaryService.Categories.Production, 3)] = LabelPalette["Completed"],

                [Key(DictionaryService.Categories.Production, 4)] = LabelPalette["Paused"],

                [Key(DictionaryService.Categories.Production, 5)] = LabelPalette["Cancelled"],



                [Key(DictionaryService.Categories.SalesOrder, 0)] = LabelPalette["Draft"],

                [Key(DictionaryService.Categories.SalesOrder, 1)] = LabelPalette["Confirmed"],

                [Key(DictionaryService.Categories.SalesOrder, 2)] = LabelPalette["Processing"],

                [Key(DictionaryService.Categories.SalesOrder, 3)] = LabelPalette["Shipped"],

                [Key(DictionaryService.Categories.SalesOrder, 4)] = LabelPalette["Completed"],

                [Key(DictionaryService.Categories.SalesOrder, 5)] = LabelPalette["Cancelled"],



                [Key(DictionaryService.Categories.Delivery, 0)] = LabelPalette["Preparing"],

                [Key(DictionaryService.Categories.Delivery, 1)] = LabelPalette["Packed"],

                [Key(DictionaryService.Categories.Delivery, 2)] = LabelPalette["In Transit"],

                [Key(DictionaryService.Categories.Delivery, 3)] = LabelPalette["Delivered"],

                [Key(DictionaryService.Categories.Delivery, 4)] = LabelPalette["Returned"],



                [Key(DictionaryService.Categories.Invoice, 0)] = LabelPalette["Unpaid"],

                [Key(DictionaryService.Categories.Invoice, 1)] = LabelPalette["Partially Paid"],

                [Key(DictionaryService.Categories.Invoice, 2)] = LabelPalette["Fully Paid"],

                [Key(DictionaryService.Categories.Invoice, 3)] = LabelPalette["Overdue"],

                [Key(DictionaryService.Categories.Invoice, 4)] = LabelPalette["Voided"],



                [Key(DictionaryService.Categories.PurchaseOrder, 0)] = LabelPalette["Draft"],

                [Key(DictionaryService.Categories.PurchaseOrder, 1)] = LabelPalette["Pending Approval"],

                [Key(DictionaryService.Categories.PurchaseOrder, 2)] = LabelPalette["Approved"],

                [Key(DictionaryService.Categories.PurchaseOrder, 3)] = LabelPalette["Rejected"],

                [Key(DictionaryService.Categories.PurchaseOrder, 4)] = LabelPalette["Ordered"],

                [Key(DictionaryService.Categories.PurchaseOrder, 5)] = LabelPalette["Receiving"],

                [Key(DictionaryService.Categories.PurchaseOrder, 6)] = LabelPalette["Completed"],

                [Key(DictionaryService.Categories.PurchaseOrder, 7)] = LabelPalette["Cancelled"],



                [Key(DictionaryService.Categories.Quotation, 0)] = LabelPalette["Draft"],

                [Key(DictionaryService.Categories.Quotation, 1)] = C(219, 234, 254, 29, 78, 216),

                [Key(DictionaryService.Categories.Quotation, 2)] = LabelPalette["Accepted"],

                [Key(DictionaryService.Categories.Quotation, 3)] = LabelPalette["Rejected"],

                [Key(DictionaryService.Categories.Quotation, 4)] = LabelPalette["Converted"],

                [Key(DictionaryService.Categories.Quotation, 5)] = LabelPalette["Cancelled"],



                [Key(DictionaryService.Categories.RefundStatus, 0)] = LabelPalette["Draft"],

                [Key(DictionaryService.Categories.RefundStatus, 1)] = LabelPalette["Approved"],

                [Key(DictionaryService.Categories.RefundStatus, 2)] = LabelPalette["Paid"],

                [Key(DictionaryService.Categories.RefundStatus, 3)] = LabelPalette["Rejected"],

                [Key(DictionaryService.Categories.RefundStatus, 4)] = LabelPalette["Cancelled"],



                [Key(DictionaryService.Categories.ReceiptVoucher, 0)] = LabelPalette["Pending Verification"],

                [Key(DictionaryService.Categories.ReceiptVoucher, 1)] = LabelPalette["Verified"],

                [Key(DictionaryService.Categories.ReceiptVoucher, 2)] = LabelPalette["Rejected"],



                [Key(DictionaryService.Categories.PaymentVoucher, 0)] = LabelPalette["Draft"],

                [Key(DictionaryService.Categories.PaymentVoucher, 1)] = LabelPalette["Approved"],

                [Key(DictionaryService.Categories.PaymentVoucher, 2)] = LabelPalette["Paid"],

                [Key(DictionaryService.Categories.PaymentVoucher, 3)] = LabelPalette["Cancelled"],



                [Key(DictionaryService.Categories.Product, 0)] = LabelPalette["Inactive"],

                [Key(DictionaryService.Categories.Product, 1)] = LabelPalette["Active"],

                [Key(DictionaryService.Categories.Product, 2)] = LabelPalette["Out of Stock"],

                [Key(DictionaryService.Categories.Product, 3)] = LabelPalette["Discontinued"],



                [Key(DictionaryService.Categories.RawMaterial, 0)] = LabelPalette["Inactive"],

                [Key(DictionaryService.Categories.RawMaterial, 1)] = LabelPalette["Active"],

                [Key(DictionaryService.Categories.RawMaterial, 2)] = LabelPalette["Below Safety Stock"],



                [Key(DictionaryService.Categories.Staff, 0)] = LabelPalette["Inactive"],

                [Key(DictionaryService.Categories.Staff, 1)] = LabelPalette["Active"],



                [Key(DictionaryService.Categories.Supplier, 0)] = LabelPalette["Inactive"],

                [Key(DictionaryService.Categories.Supplier, 1)] = LabelPalette["Active"],



                [Key(DictionaryService.Categories.ReplySlip, 0)] = LabelPalette["Draft"],

                [Key(DictionaryService.Categories.ReplySlip, 1)] = LabelPalette["Sent"],

                [Key(DictionaryService.Categories.ReplySlip, 2)] = LabelPalette["Signed"],

                [Key(DictionaryService.Categories.ReplySlip, 3)] = LabelPalette["Rejected"],

            };



        public static StatusColors GetColors(string category, int statusCode)

        {

            if (!string.IsNullOrWhiteSpace(category)

                && CategoryCodePalette.TryGetValue(Key(category, statusCode), out var byCode))

            {

                return byCode;

            }



            string label = DictionaryService.GetDisplayName(category, statusCode) ?? statusCode.ToString();

            return GetColorsByLabel(label);

        }



        public static StatusColors GetColorsByLabel(string label)

        {

            string text = (label ?? string.Empty).Trim();

            if (text.Length == 0)

                return Neutral();



            if (LabelPalette.TryGetValue(text, out var exact))

                return exact;



            return GetColorsByKeyword(text.ToLowerInvariant());

        }



        private static StatusColors GetColorsByKeyword(string text)

        {

            if (ContainsAny(text, "cancel", "void"))

                return LabelPalette["Cancelled"];

            if (ContainsAny(text, "reject"))

                return LabelPalette["Rejected"];

            if (ContainsAny(text, "overdue", "unpaid"))

                return LabelPalette["Overdue"];

            if (ContainsAny(text, "draft", "pending"))

                return LabelPalette["Draft"];

            if (ContainsAny(text, "pause", "hold"))

                return LabelPalette["Paused"];

            if (ContainsAny(text, "quality", "checking", "verify", "verification"))

                return LabelPalette["Quality Checking"];

            if (ContainsAny(text, "partial"))

                return LabelPalette["Partially Paid"];

            if (ContainsAny(text, "progress", "processing"))

                return LabelPalette["In Progress"];

            if (ContainsAny(text, "transit", "shipping", "shipped"))

                return LabelPalette["In Transit"];

            if (ContainsAny(text, "deliver"))

                return LabelPalette["Delivered"];

            if (ContainsAny(text, "complet", "done", "finished"))

                return LabelPalette["Completed"];

            if (ContainsAny(text, "paid", "received", "confirmed"))

                return LabelPalette["Paid"];

            if (ContainsAny(text, "approv"))

                return LabelPalette["Approved"];

            if (ContainsAny(text, "active"))

                return LabelPalette["Active"];

            if (ContainsAny(text, "inactive"))

                return LabelPalette["Inactive"];

            if (ContainsAny(text, "stock", "short"))

                return LabelPalette["Below Safety Stock"];

            if (ContainsAny(text, "convert"))

                return LabelPalette["Converted"];

            if (ContainsAny(text, "sent"))

                return LabelPalette["Sent"];

            if (ContainsAny(text, "pack"))

                return LabelPalette["Packed"];

            if (ContainsAny(text, "return"))

                return LabelPalette["Returned"];

            if (ContainsAny(text, "fail", "block"))

                return LabelPalette["Failed"];



            return Neutral();

        }



        private static string Key(string category, int code) => $"{category}|{code}";



        private static StatusColors C(int bgR, int bgG, int bgB, int fgR, int fgG, int fgB) =>

            new StatusColors(Color.FromArgb(bgR, bgG, bgB), Color.FromArgb(fgR, fgG, fgB));



        private static StatusColors Neutral() =>

            new StatusColors(Color.FromArgb(249, 250, 251), UITheme.TextDark);



        private static bool ContainsAny(string text, params string[] tokens)

        {

            foreach (string token in tokens)

            {

                if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)

                    return true;

            }

            return false;

        }

    }

}


