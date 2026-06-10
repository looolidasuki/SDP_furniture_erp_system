using System;
using System.Windows.Forms;
using FurnitureERP.Forms;

namespace FurnitureERP.Helpers
{
    public static class DocumentNavigationHelper
    {
        public static void OpenFromControl(Control source, DocumentSearchResult hit)
        {
            if (hit == null) return;
            var main = source?.FindForm() as MainForm;
            if (main == null)
            {
                UITheme.ShowWarning("Cannot navigate — main window not found.");
                return;
            }
            main.NavigateToDocument(hit);
        }

        public static void OpenFromControl(Control source, string documentType, long id, string code, string module)
        {
            OpenFromControl(source, new DocumentSearchResult
            {
                DocumentType = documentType,
                Id = id,
                Code = code,
                Module = module
            });
        }
    }
}
