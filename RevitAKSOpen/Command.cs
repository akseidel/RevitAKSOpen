#region Namespaces
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace RevitAKSOpen {
    [Transaction(TransactionMode.Manual)]
    public class AKSOpen : IExternalCommand {
   
        AKSOpenWPF AKSOpenDialog;

        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements) {
            try {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
                IntPtr revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                AKSOpenDialog = new AKSOpenWPF(uiapp, revitHandle);
                AKSOpenDialog.ShowDialog();

            } catch (Exception ex) {
                System.Windows.MessageBox.Show(ex.Message + " | " + ex.InnerException.Message, "Sorry Charlie",
                                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            return Result.Succeeded;
        }


        public Result T(ExternalCommandData commandData,
          ref string message,
          ElementSet elements) {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            View v = uidoc.ActiveView;

            ElementId i = new ElementId(123);
            Element e = uidoc.Document.GetElement(i);

            e.get_BoundingBox(v);

           // v.get_BoundingBox()

            return Result.Succeeded;
        }
    }
}
