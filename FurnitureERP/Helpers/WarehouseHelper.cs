using System;

namespace FurnitureERP.Helpers
{
    public static class WarehouseHelper
    {
        public const long DefaultInventoryWarehouseId = 1;
        public const long ProductionWarehouseOffset = 4;

        public static bool IsInventoryWarehouse(long warehouseId, string warehouseName = null)
        {
            if (warehouseId >= 1 && warehouseId <= 4)
                return true;
            return NameIndicatesInventory(warehouseName);
        }

        public static bool IsProductionWarehouse(long warehouseId, string warehouseName = null)
        {
            if (warehouseId >= 5 && warehouseId <= 8)
                return true;
            return NameIndicatesProduction(warehouseName);
        }

        public static long GetPairedProductionWarehouse(long inventoryWarehouseId)
        {
            if (inventoryWarehouseId >= 1 && inventoryWarehouseId <= 4)
                return inventoryWarehouseId + ProductionWarehouseOffset;
            return 0;
        }

        public static long GetPairedInventoryWarehouse(long productionWarehouseId)
        {
            if (productionWarehouseId >= 5 && productionWarehouseId <= 8)
                return productionWarehouseId - ProductionWarehouseOffset;
            return 0;
        }

        public static string ExtractRegionPrefix(string warehouseName)
        {
            if (string.IsNullOrWhiteSpace(warehouseName))
                return null;

            string name = warehouseName.Trim();
            int dash = name.IndexOf(" - ", StringComparison.Ordinal);
            if (dash > 0)
                return name.Substring(0, dash).Trim();

            foreach (string suffix in new[] { " Inventory", " Production" })
            {
                int idx = name.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                    return name.Substring(0, idx).Trim();
            }

            return name;
        }

        public static string BuildPairedProductionName(string inventoryWarehouseName)
        {
            string prefix = ExtractRegionPrefix(inventoryWarehouseName);
            if (string.IsNullOrWhiteSpace(prefix))
                return inventoryWarehouseName + " Production";
            return prefix + " - Production";
        }

        private static bool NameIndicatesInventory(string warehouseName)
        {
            if (string.IsNullOrWhiteSpace(warehouseName))
                return false;
            return warehouseName.IndexOf("inventory", StringComparison.OrdinalIgnoreCase) >= 0
                && warehouseName.IndexOf("production", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool NameIndicatesProduction(string warehouseName)
        {
            if (string.IsNullOrWhiteSpace(warehouseName))
                return false;
            return warehouseName.IndexOf("production", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
