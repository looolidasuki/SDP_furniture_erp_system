using MySql.Data.MySqlClient;
using Sales_user.Controllers;
using FurnitureERP.Helpers;
using System;
using System.Text;
using System.Windows.Forms;

namespace FurnitureERP
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // PDF 导出依赖字体解析器（避免 Segoe UI 缺失导致导出失败）
            PdfSharpFontResolver.EnsureRegistered();

            if (!CheckDatabaseConnection())
            {
                return;
            }

            try
            {
                CurrencyDualAmountMigration.EnsureApplied();
                DocumentAuditMigration.EnsureApplied();
                StaffPasswordMigration.EnsureApplied();
                InventoryLedgerMigration.EnsureApplied();
                ReceiptVoucherInvoiceLineMigration.EnsureApplied();
                DictionarySeedMigration.EnsureApplied();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Database schema update failed.\n" + ex.Message +
                    "\n\nPlease run Scripts/migrate_currency_dual_amount.sql in MySQL, then restart.",
                    "Schema Migration Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            EnsureDiagnosticAccount();

            DictionaryService.ClearCache();
            StartupHealthChecks.WarnIfDictionaryMissing();

            // Safety notice: app is running on a fallback MySQL connection string (typically root + no password).
            // This keeps demos working but warns admins to configure DefaultConnection in app.config.
            try
            {
                if (DatabaseConnect.UsedFallbackConnectionString)
                {
                    MessageBox.Show(
                        "Warning: Using fallback database connection settings (DefaultConnection is missing).\n" +
                        "Please configure the 'DefaultConnection' connection string in FurnitureERP.exe.config/app.config.\n" +
                        "For production use, do NOT run with root user or empty password.",
                        "Database Connection Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch { }

            Application.Run(new LoginForm());
        }

        private static bool CheckDatabaseConnection()
        {
            try
            {
                DatabaseConnect.ExecuteScalar("SELECT 1;");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    BuildDatabaseErrorMessage(ex),
                    "Database Connection Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private static void EnsureDiagnosticAccount()
        {
            try
            {
                var staffController = new StaffController();
                bool created = staffController.EnsureDiagnosticAccount();
                if (created)
                {
                    MessageBox.Show(
                        "Diagnostic account has been created.\nEmail: diagnostic@erp.local\nPassword: Test@123",
                        "Diagnostic Account Ready",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to initialize diagnostic account.\n" + ex.Message,
                    "Diagnostic Account Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string BuildDatabaseErrorMessage(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Cannot connect to MySQL database at startup.");
            sb.AppendLine();

            if (ex is MySqlException mySqlEx)
            {
                switch (mySqlEx.Number)
                {
                    case 1045:
                        sb.AppendLine("Reason: Access denied (invalid username/password).");
                        break;
                    case 1049:
                        sb.AppendLine("Reason: Database 'furniture_erp_system' does not exist.");
                        break;
                    case 2003:
                        sb.AppendLine("Reason: Cannot reach MySQL server (check host/port/service).");
                        break;
                    default:
                        sb.AppendLine("Reason: MySQL error occurred.");
                        break;
                }
                sb.AppendLine($"MySQL Error Code: {mySqlEx.Number}");
                sb.AppendLine($"Details: {mySqlEx.Message}");
            }
            else
            {
                sb.AppendLine("Reason: Unexpected database initialization error.");
                sb.AppendLine($"Details: {ex.Message}");
            }

            sb.AppendLine();
            sb.AppendLine("Check App.config connection string and local MySQL service.");
            return sb.ToString();
        }
    }
}
