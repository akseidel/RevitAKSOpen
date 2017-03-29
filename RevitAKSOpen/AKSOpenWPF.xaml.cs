using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using System.Runtime.InteropServices;
using System.Collections.Specialized;

// use an alias because Autodesk.Revit.UI 
// uses classes which have same names:
using adWin = Autodesk.Windows;

// All about access to ribbon
// note that references AdWindows.dll and UIFramework.dll from Revit folder have been added.

namespace RevitAKSOpen {
    public partial class AKSOpenWPF : Window {
        [DllImport("USER32.DLL")]
        public static extern bool PostMessage(
          IntPtr hWnd, uint msg, uint wParam, uint lParam);

        #region StatusBarText helpers
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int SetWindowText(
          IntPtr hWnd,
          string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(
          IntPtr hwndParent,
          IntPtr hwndChildAfter,
          string lpszClass,
          string lpszWindow);

        public static void SetStatusBarText(IntPtr mainWindow, string text) {
            IntPtr mainWindowHandle = IntPtr.Zero;
            IntPtr statusBar = FindWindowEx(mainWindow, IntPtr.Zero, "msctls_statusbar32", "");
            if (statusBar != IntPtr.Zero) {
                SetWindowText(statusBar, text);
            }
        }
        #endregion

        UIApplication _uiapp;
        string defRevRoot = "J:\\REVIT\\";
        string versionPath;
        string revVersion;
        string dlgPrompt = "              << Select the Revit project file. >>";
        public string selectedFilePathName;
        string _locRevitRoot;
        List<RecentItem> Inventory;
        int stashMax = 4;
        IntPtr _revitHandle;

        public AKSOpenWPF(UIApplication uiapp, IntPtr revitHandle) {
            InitializeComponent();
            _revitHandle = revitHandle;
            _uiapp = uiapp;
            revVersion = _uiapp.Application.VersionNumber;
            versionPath = "J:\\REVIT\\REV" + revVersion + "\\";
            _locRevitRoot = "D:\\Autodesk\\Revit " + revVersion + " Local";
            PositionFormInScreen();
        }

        // places form at saved position or at screen center if never saved before
        private void PositionFormInScreen() {
            double top = Properties.Settings.Default.AKSOpen_Top;
            double left = Properties.Settings.Default.AKSOpen_Left;
            if (top == 0 && left == 0) {
                Top = System.Windows.SystemParameters.PrimaryScreenHeight /4;
                Left = System.Windows.SystemParameters.PrimaryScreenWidth/4 - this.ActualWidth;
            } else { 
                Top = top;
                Left = left;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Properties.Settings.Default.AKSOpen_Top = this.Top;
            Properties.Settings.Default.AKSOpen_Left = this.Left;
            SaveRecents();
            Properties.Settings.Default.Save();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e) {
            UserPicksFile();
        }

        private void RecentsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (RecentsGrid.SelectedItem == null) return;
            var selectedItem = RecentsGrid.SelectedItem as RecentItem;
            if (selectedItem.FileName == null) { return; }
            selectedFilePathName = selectedItem.FileName.ToString();
            if (selectedFilePathName == "") { return; }
            if (File.Exists(selectedFilePathName)) {
                ProcessSelection(selectedFilePathName);
            } else {
                string msg = selectedFilePathName + " is no longer a valid file path name! The file has been moved or deleted.";
                msg = msg + "\n\nYou can remove this entry by pressing the Delete key when it is highlighted.";
                System.Windows.MessageBox.Show(msg, "There Is Trouble In Gotham City", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

        }

        private void UserPicksFile() {
            selectedFilePathName = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = versionPath + dlgPrompt;
            if (!Directory.Exists(versionPath)) {
                versionPath = defRevRoot;
            }
            openFileDialog.InitialDirectory = @versionPath;
            openFileDialog.Filter = "Revit project files (*.rvt)|*.rvt";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                selectedFilePathName = openFileDialog.FileName;
                this.UpdateLayout(); // required to keep the OpenFileDialog from staying visible too long
                ProcessSelection(selectedFilePathName);
            }
        }

        private void ProcessSelection(string selectedFilePathName) {
            string runningMsg;
            if (selectedFilePathName != null) {
                if ((bool)chkInfoOnly.IsChecked) {
                    try {
                        RevitFileSniffer rfs = new RevitFileSniffer(selectedFilePathName);
                        rfs.ReportRevitInfo();
                    } catch (Exception ex) {
                        string ms = "\n\nDo you must have this file open? The sniffer cannot snif if it's open.";
                        System.Windows.MessageBox.Show(ex.Message + ms,"Error At RevitFileSniffer");
                    }
                    return;
                }
                Hide();
                FormMsgWPF HUD = new FormMsgWPF();
                string FileToOpen = selectedFilePathName;
                try {
                    OpenOptions op = new OpenOptions();
                    op.SetOpenWorksetsConfiguration(null);  // makes all worksets opened?
                    op.DetachFromCentralOption = DetachFromCentralOption.DoNotDetach;
                    ModelPath mdlPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(FileToOpen);

                    string fName = System.IO.Path.GetFileNameWithoutExtension(FileToOpen);
                    string fext = System.IO.Path.GetExtension(FileToOpen);

                    string UserInitials = Environment.UserName.ToString();
                    fName = fName + "_" + UserInitials + fext;
                    runningMsg = selectedFilePathName + " for " + UserInitials;
                    HUD.SetMsg(runningMsg, "Processing for " + UserInitials);
                    HUD.Show();
                    string source = FileToOpen;
                    string[] stringSeparators = new string[] { "\\" };
                    var result = source.Split(stringSeparators, StringSplitOptions.None);

                    string localPrjPath = string.Empty;
                    string fullLFname = string.Empty;
                    ModelPath localPath = null;
                    try {
                        if (result.Length > 4) {
                            localPrjPath = _locRevitRoot + "\\" + result[3] + "_" + UserInitials + "\\";
                            fullLFname = localPrjPath + fName;
                            //MessageBox.Show(fullLFname, "First Pass Local Path");
                            localPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(fullLFname);
                        } else {
                            HUD.Close();
                            System.Windows.MessageBox.Show("Based on its location, the file selected does not look like a project file .\n\n" + FileToOpen, "Not Touching This Without A Ten Foot Pole");
                            Close();
                            return;
                        }

                        #region Check For Open Document
                        DocumentSet ds = _uiapp.Application.Documents;
                        foreach (Document d in ds) {
                            String dF = d.PathName.Replace(@"\\SYSFILE\CADD", "J:");
                            if (fullLFname.ToUpper().Equals(dF.ToUpper())) {
                                string msg = "You have " + selectedFilePathName + " already open";
                                msg = msg + " or it is linked to a file you have open. Either way";
                                msg = msg + " Revit will not let you open it until the other one is unloaded.";
                                System.Windows.MessageBox.Show(msg, "Sorry, You Cannot Open The File");
                                HUD.Close();
                                Close();
                                return;
                            }
                        }
                        #endregion

                        if (!Directory.Exists(localPrjPath)) {
                            try {
                                runningMsg = runningMsg + "\n\nCreated Local Folder: " + localPrjPath;
                                HUD.SetMsg(runningMsg, "Creating This Local Folder");
                                Directory.CreateDirectory(localPrjPath);
                            } catch (Exception ex) {
                                HUD.Close();
                                System.Windows.MessageBox.Show(localPrjPath + "\n\n" + ex.Message, "Error Creating Directory");
                                Close();
                                return;
                            }
                        } else {
                            runningMsg = runningMsg + "\n\nUsing Local Folder: " + localPrjPath;
                            HUD.SetMsg(runningMsg, "Using This Local Folder");
                        }

                    } catch (Exception) {
                        HUD.Close();
                        System.Windows.MessageBox.Show("Error: Did Not Create " + localPrjPath, "Debug");
                        Close();
                        return;
                    }

                    #region Handle any existing local file
                    if (File.Exists(fullLFname)) {
                        string StashFolder = localPrjPath + "PriorsStash\\";
                        runningMsg = runningMsg + "\n\nStashing Older Local File To: " + StashFolder;
                        HUD.SetMsg(runningMsg, "Stashing Older Local File To");
                        if (!Directory.Exists(StashFolder)) {
                            try {
                                Directory.CreateDirectory(StashFolder);
                            } catch (Exception ex) {
                                HUD.Close();
                                System.Windows.MessageBox.Show(StashFolder + "\n\n" + ex.Message, "Error Creating Directory");
                                Close();
                                return;
                            }
                        }
                        string OrgFNameWOExt = System.IO.Path.GetFileNameWithoutExtension(fullLFname);
                        string ext = System.IO.Path.GetExtension(fullLFname);
                        string NewFNameWExt = AssignNewUniqueFileName(StashFolder + OrgFNameWOExt + ext);
                        try {
                            File.Move(fullLFname, NewFNameWExt);
                            int qtyDeleted = LimitNumberOfStashedFiles(StashFolder + OrgFNameWOExt + ext, stashMax);
                            if (qtyDeleted > 0) {
                                runningMsg = runningMsg + "\n\nTrashed " + qtyDeleted.ToString() + " stashed older file" + pluralOrNot(qtyDeleted) + " from the PriorsStash folder.";
                            }
                        } catch (Exception ex) {
                            HUD.Close();
                            System.Windows.MessageBox.Show(fullLFname + "\nto\n" + NewFNameWExt + "\n\n" + ex.Message, "File Error Moving File");
                            Close();
                            return;
                        }
                        string backupFldr = localPrjPath + OrgFNameWOExt + "_backup";
                        if (Directory.Exists(backupFldr)) {
                            try {
                                runningMsg = runningMsg + "\n\nTrashing Older Backup Folder: " + backupFldr;
                                HUD.SetMsg(runningMsg, "Trashing Older Backup Folder");
                                Directory.Delete(backupFldr, true);
                            } catch (Exception ex) {
                                HUD.Close();
                                System.Windows.MessageBox.Show(backupFldr + "\n\n" + ex.Message, "Error Deleting Backup Folder");
                                Close();
                                return;
                            }
                        }
                    }
                    #endregion

                    runningMsg = runningMsg + "\n\nLocal Document Is: " + fullLFname;
                    /////runningMsg = runningMsg + "\n\n" + ExcludeTheseMsg();
                    HUD.SetMsg(runningMsg, "Revit Is Opening This Document For " + UserInitials);
                    HUD.UpdateLayout();
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
                    HUD.Close();
                    SetStatusBarText(_revitHandle, "Just guessing that maybe you actually think something is happening.");
                    
                    #region Finally Create and Open
                    Autodesk.Revit.UI.UIDocument newUIDoc = null;

                    if (mdlPath != null || localPath != null) {
                        AKSOpenUtils AKSOpen = new AKSOpenUtils(_uiapp, revVersion);
                        // MessageBox.Show(fullLFname, "Will OpenNewLocalFromModelPath");
                        PokeRevit();
                        newUIDoc = AKSOpen.DoOpenNewLocalFromModelPath(mdlPath, localPath);
                    }
                    #endregion
                    adWin.ComponentManager.Ribbon.Visibility = System.Windows.Visibility.Visible;

                } catch (Exception ex) {
                    HUD.Close();
                    System.Windows.MessageBox.Show(ex.Message + " | " + ex.InnerException.Message, "Where is this Bummer");
                    Close();
                    return;
                }

                UpDateRecentsList(FileToOpen);
                
                Close();
            }
        }

        private string pluralOrNot(int qty) {
            if (qty == 1) { return string.Empty; }
            return "s";
        }

        void PokeRevit() {
            Process revitProcess = Process.GetCurrentProcess();
            if (revitProcess != null && !revitProcess.HasExited) {
                try {
                    IntPtr mainrevitwindowhandle = revitProcess.MainWindowHandle;
                    if (mainrevitwindowhandle != null && mainrevitwindowhandle.ToInt64() != 0) {
                        PostMessage(mainrevitwindowhandle, 0, 0, 0);
                    }
                } catch {
                    //report error      
                }
            }
        }

        #region UpDate Recents
        void UpDateRecentsList(string FileToOpen) {
            RecentItem ri = new RecentItem() { FileName = FileToOpen };
            foreach (RecentItem rl in Inventory) {
                if (rl.FileName == FileToOpen) { return; }
            }
            Inventory.Add(ri);
            RecentsGrid.ItemsSource = Inventory;
        }
        #endregion

        // Given a full path file name will return an indexed unique new file name.
        // 
        private string AssignNewUniqueFileName(string _orgName) {
            var thereIsOneAlready = false;
            var retryCnt = 100; // safety valve
            string _orgNameWOExt = System.IO.Path.GetFileNameWithoutExtension(_orgName);
            string path = System.IO.Path.GetDirectoryName(_orgName);
            string ext = System.IO.Path.GetExtension(_orgName);
            string _newName = path + "\\" + _orgNameWOExt + "_" + GetTimestamp(DateTime.Now) + ext;
            // going to start name indx from the current numver of similar files 
            int count = Directory.GetFiles(path, "*" + _orgNameWOExt + "*", SearchOption.TopDirectoryOnly).Count();
            thereIsOneAlready = File.Exists(_newName); // success needs to be false to drop out
            while (thereIsOneAlready && retryCnt > 0) {
                _newName = path + "\\" + _orgNameWOExt + "_" + GetTimestamp(DateTime.Now) + ext;
                thereIsOneAlready = File.Exists(_newName); // success needs to be false to drop out
                retryCnt -= 1;
            }
            return _newName;
        }

        public static String GetTimestamp(DateTime value) {
            return value.ToString("yyyyMMddHHmmss");
        }

        // deletes all but the newest stashMax number of files in a folder
        private int LimitNumberOfStashedFiles(string _orgName, int stashMax) {
            int qtyD = 0;
            string path = System.IO.Path.GetDirectoryName(_orgName);
            if (stashMax <= 1) { return qtyD; }
            var files = new DirectoryInfo(path).EnumerateFiles()
                        .OrderByDescending(f => f.CreationTime)
                        .Skip(stashMax)
                        .ToList();
            qtyD = files.Count;
            files.ForEach(f => f.Delete());
            return qtyD;
        }

        public void DragWindow(object sender, MouseButtonEventArgs args) {
            // Watch out. Fatal error if not primary button!
            if (args.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }

        private List<RecentItem> LoadRecents() {
            List<RecentItem> r = new List<RecentItem>();
            StringCollection scRF = new StringCollection();
            try {
                scRF = Properties.Settings.Default.RecentFiles;
                if (scRF == null) { return r; }
                if (scRF.Count == 0) { return r; }
                foreach (string rf in scRF) {
                    r.Add(new RecentItem() { FileName = rf.ToString() });
                }
                return r;
            } catch (Exception) {
                System.Windows.MessageBox.Show("Error loading recents from settings", "LoadRecents");
                return r;
            }
        }

        public class RecentItem {
            public string FileName { get; set; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Inventory = LoadRecents();
            RecentsGrid.ItemsSource = Inventory;
        }

        private void SaveRecents() {
            StringCollection scRF = new StringCollection();
            foreach (RecentItem ri in RecentsGrid.ItemsSource) {
                scRF.Add(ri.FileName.ToString());
            }
            Properties.Settings.Default.RecentFiles = scRF;
        }

    } // end class

    public class Press {
        [DllImport("USER32.DLL")]
        public static extern bool PostMessage(
          IntPtr hWnd, uint msg, uint wParam, uint lParam);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(
          uint uCode, uint uMapType);

        enum WH_KEYBOARD_LPARAM : uint {
            KEYDOWN = 0x00000001,
            KEYUP = 0xC0000001
        }

        enum KEYBOARD_MSG : uint {
            WM_KEYDOWN = 0x100,
            WM_KEYUP = 0x101
        }

        enum MVK_MAP_TYPE : uint {
            VKEY_TO_SCANCODE = 0,
            SCANCODE_TO_VKEY = 1,
            VKEY_TO_CHAR = 2,
            SCANCODE_TO_LR_VKEY = 3
        }

        /// <summary>
        /// Post one single keystroke.
        /// </summary>
        static void OneKey(IntPtr handle, char letter) {
            uint scanCode = MapVirtualKey(letter,
              (uint)MVK_MAP_TYPE.VKEY_TO_SCANCODE);

            uint keyDownCode = (uint)
              WH_KEYBOARD_LPARAM.KEYDOWN
              | (scanCode << 16);

            uint keyUpCode = (uint)
              WH_KEYBOARD_LPARAM.KEYUP
              | (scanCode << 16);

            PostMessage(handle,
              (uint)KEYBOARD_MSG.WM_KEYDOWN,
              letter, keyDownCode);

            PostMessage(handle,
              (uint)KEYBOARD_MSG.WM_KEYUP,
              letter, keyUpCode);
        }

        /// <summary>
        /// Post a sequence of keystrokes.
        /// </summary>
        public static void Keys(string command) {
            IntPtr revitHandle = System.Diagnostics.Process
              .GetCurrentProcess().MainWindowHandle;

            foreach (char letter in command) {
                OneKey(revitHandle, letter);
            }
        }
    }

} // end namespace
