#region Namespaces
using System;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;
#endregion

namespace RevitAKSOpen {
    public class CommandEnabler : IExternalCommandAvailability {
        public bool IsCommandAvailable(UIApplication a, CategorySet b) {
            return true;
        }
    }
    class App : IExternalApplication {
        string thisUsersInitials;

        public Result OnStartup(UIControlledApplication a) {
            // Add to RibbonTab
            AddAKSOpenTo_RibbonTab(a);
            thisUsersInitials = Environment.UserName.ToString().ToUpper();
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a) {
            return Result.Succeeded;
        }

        private void AddAKSOpenTo_RibbonTab(UIControlledApplication a) {
            string ExecutingAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string ExecutingAssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            // create ribbon tab 

            String thisNewTabName = thisUsersInitials;
            try {
                a.CreateRibbonTab(thisNewTabName);
            } catch (Autodesk.Revit.Exceptions.ArgumentException) {
                ///  Assume error generated is due to Tab already existing
            }

            string cntlName = thisUsersInitials + "Open";
            PushButtonData pbDataAKSOpen = new PushButtonData(cntlName, cntlName, ExecutingAssemblyPath, ExecutingAssemblyName + ".AKSOpen");
            // the following enabled the button for the zero document state
            pbDataAKSOpen.AvailabilityClassName = ExecutingAssemblyName + ".CommandEnabler";
            
            String thisOpenerPanelName = cntlName;
            // Create a ribbon panel for the ribbon tab
            RibbonPanel thisOpenerRibbonPanel = a.CreateRibbonPanel(thisNewTabName, thisOpenerPanelName);
            // add button to ribbon panel
            PushButton pushButtonOpener = thisOpenerRibbonPanel.AddItem(pbDataAKSOpen) as PushButton;

            // Adding to the Revit Add-Ins Also so that the Command is available without an open file
            RibbonPanel thisOpenerRibbonPanelB = a.CreateRibbonPanel(thisOpenerPanelName);
            PushButton pushButtonOpenerB = thisOpenerRibbonPanelB.AddItem(pbDataAKSOpen) as PushButton;

            pushButtonOpener.LargeImage = NewBitmapImage(System.Reflection.Assembly.GetExecutingAssembly(), ExecutingAssemblyName + ".AKSOpen3.png");
            pushButtonOpenerB.LargeImage = pushButtonOpener.LargeImage;

            // provide button tips
            pushButtonOpener.ToolTip = "The quicker openupper.";
            string msg = "Opens workshared files with less manuvering.";
            pushButtonOpener.LongDescription = msg;

        } // AddAKSOpenTo_RibbonTab

        /// Load a new icon bitmap from embedded resources.
        /// For the BitmapImage, make sure you reference WindowsBase and Presentation Core
        /// and PresentationCore, and import the System.Windows.Media.Imaging namespace. 
        BitmapImage NewBitmapImage(System.Reflection.Assembly a, string imageName) {
            Stream s = a.GetManifestResourceStream(imageName);
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = s;
            img.EndInit();
            return img;
        }

    }
}
