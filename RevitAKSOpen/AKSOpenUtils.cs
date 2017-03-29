using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Net;
using System.Runtime.Serialization.Json;


namespace RevitAKSOpen {
    class AKSOpenUtils {
        string verRootNet = "J:\\REVIT\\REV";
        string _version;
        string _versionPath;
        string _locRevitRoot;
        UIApplication _uiApp;
        Application _app;

        public AKSOpenUtils(UIApplication uiApplication, string revVersion) {
            _uiApp = uiApplication;
            _app = _uiApp.Application;
            _version = revVersion;
            _versionPath = verRootNet + revVersion;
            _locRevitRoot = "D:\\Autodesk\\Revit " + revVersion + " Local";
        }

        #region Open Workshared Document

        // In the example below, a document is opened with two worksets specified to be opened. 
        // Note that the WorksharingUtils.GetUserWorksetInfo() method can be used to access workset 
        // information from a closed Revit document.

        Document OpenDocumentWithWorksets(ModelPath projectPath) {
            Document doc = null;
            try {
                // Get info on all the user worksets in the project prior to opening
                IList<WorksetPreview> worksets = WorksharingUtils.GetUserWorksetInfo(projectPath);
                IList<WorksetId> worksetIds = new List<WorksetId>();
                // Find two predetermined worksets
                foreach (WorksetPreview worksetPrev in worksets) {
                    if (worksetPrev.Name.CompareTo("Workset1") == 0 ||
                        worksetPrev.Name.CompareTo("Workset2") == 0) {
                        worksetIds.Add(worksetPrev.Id);
                    }
                }

                OpenOptions openOptions = new OpenOptions();
                // Setup config to close all worksets by default
                WorksetConfiguration openConfig = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
                // Set list of worksets for opening 
                openConfig.Open(worksetIds);
                openOptions.SetOpenWorksetsConfiguration(openConfig);
                doc = _app.OpenDocumentFile(projectPath, openOptions);
            } catch (Exception e) {
                TaskDialog.Show("Open File Failed", e.Message);
            }

            return doc;
        }

        #endregion

        #region Open last viewed worksets
        //Another option is to open the document while just opening the last viewed worksets.

        public Document OpenLastViewed() {
            // Setup options
            OpenOptions options1 = new OpenOptions();

            // Default config opens all.  Close all first, then open last viewed to get the correct settings.
            WorksetConfiguration worksetConfig = new WorksetConfiguration(WorksetConfigurationOption.OpenLastViewed);
            options1.SetOpenWorksetsConfiguration(worksetConfig);

            // Open the document
            Document openedDoc = _app.OpenDocumentFile(GetWSAPIModelPath("WorkaredFileSample.rvt"), options1);

            return openedDoc;
        }

        private static ModelPath GetWSAPIModelPath(string fileName, string locRevitRoot = "") {
            // Utility to get a local path for a target model file
            if (locRevitRoot == "") {
                locRevitRoot = "C:\\Documents\\Revit Projects";
            }
            //FileInfo filePath = new FileInfo(Path.Combine(@"C:\Documents\Revit Projects", fileName));
            FileInfo filePath = new FileInfo(Path.Combine(@locRevitRoot, fileName));
            ModelPath mp = ModelPathUtils.ConvertUserVisiblePathToModelPath(filePath.FullName);

            return mp;
        }
        #endregion

        #region Open new local from disk
        // The following two examples demonstrate how to create a new local first from disk or from a Revit server,
        // and then open it. Note that the sample below uses the GetWSAPIModelPath() method used in the previous example.

        public Document OpenNewLocalFromDisk() {
            bool isWrkShared = true;
            List<string> InverseWorksetList = new List<string>();
            // Create new local from a disk location
            ModelPath newLocalPath = GetWSAPIModelPath("LocalWorksharing.rvt", _locRevitRoot);
            ModelPath nlb = GetWSAPIModelPath("NewLocalWorksharing.rvt", _locRevitRoot);
            return (OpenNewLocalFromModelPath(nlb, newLocalPath, InverseWorksetList, out isWrkShared));
        }

        public Document OpenNewLocalFromModelPath(ModelPath centralPath, ModelPath localPath, List<string> InverseWorksetList, out bool isWrkShared) {
            List<WorksetId> worksetsToOpen = new List<WorksetId>();
            // First set to close all worksets
            WorksetConfiguration worksetConfig = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
            OpenOptions theOpenOptions = new OpenOptions();
            try {
                // Create the new local at the given path
                WorksharingUtils.CreateNewLocal(centralPath, localPath);
                // Select specific worksets to open
                // Get a list of worksets from the unopened document
                IList<WorksetPreview> worksets = WorksharingUtils.GetUserWorksetInfo(localPath);
                foreach (WorksetPreview preview in worksets) {
                    bool Include = true;
                    // The inverse list is the inverse of the worksets checked. In other
                    // words an exclusion list.
                    foreach (string ws in InverseWorksetList) {
                        if (ws == "") { continue; }
                        if (preview.Name.StartsWith(ws)) {
                            Include = false;
                            continue;
                        } else {
                        }
                    }
                    if (Include) {
                        worksetsToOpen.Add(preview.Id);
                    } else {
                        //System.Windows.MessageBox.Show("Excluding " + preview.Name);
                    }
                }
                // Setup option to open the target worksets
                // then set specific ones to open
                worksetConfig.Open(worksetsToOpen);
                theOpenOptions.SetOpenWorksetsConfiguration(worksetConfig);
                // Now open the new local
                Document openedDoc = _app.OpenDocumentFile(localPath, theOpenOptions);
               
                isWrkShared = true;
                return openedDoc;
            } catch (Exception ex) {
                System.Windows.MessageBox.Show("Opening the file from its location.\n\n" + ex.Message, "This Is Not A Workshared File");
            }
            // If here then not a workshared file.
            string fname = ModelPathUtils.ConvertModelPathToUserVisiblePath(centralPath);
            Document openedDocN = _app.OpenDocumentFile(fname);
            isWrkShared = false;
            return openedDocN;
        }


        public UIDocument DoOpenNewLocalFromModelPath(ModelPath centralPath, ModelPath localPath) {
            List<WorksetId> worksetsToOpen = new List<WorksetId>();
            // First set to close all worksets
            WorksetConfiguration worksetConfig = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
            OpenOptions theOpenOptions = new OpenOptions();
            try {
                // Create the new local at the given path
                WorksharingUtils.CreateNewLocal(centralPath, localPath);
                // Select specific worksets to open
                // Get a list of worksets from the unopened document
                IList<WorksetPreview> worksets = WorksharingUtils.GetUserWorksetInfo(localPath);
                foreach (WorksetPreview preview in worksets) {
                    bool Include = true;
                    ////// The inverse list is the inverse of the worksets checked. In other
                    ////// words an exclusion list.
                    //////foreach (string ws in InverseWorksetList) {
                    //////    if (ws == "") { continue; }
                    //////    if (preview.Name.StartsWith(ws)) {
                    //////        Include = false;
                    //////        continue;
                    //////    } else {
                    //////    }
                    //////}
                    if (Include) {
                        worksetsToOpen.Add(preview.Id);
                    } else {
                        //System.Windows.MessageBox.Show("Excluding " + preview.Name);
                    }
                }
                // Setup option to open the target worksets
                // then set specific ones to open
                worksetConfig.Open(worksetsToOpen);
                theOpenOptions.SetOpenWorksetsConfiguration(worksetConfig);
                // Now open the new local     
                UIDocument openedDoc = _uiApp.OpenAndActivateDocument(localPath, theOpenOptions, false);
                return openedDoc;
            } catch (Exception ex) {
                System.Windows.MessageBox.Show("Opening the file from its location.\n\n" + ex.Message, "This Is Not A Workshared File");
            }
            // If here then not a workshared file.
            string fname = ModelPathUtils.ConvertModelPathToUserVisiblePath(centralPath);
            UIDocument openedDocN = _uiApp.OpenAndActivateDocument(fname);
            return openedDocN;
        }


        #endregion

        #region Open new local from Revit Server
        // The following example uses the OpenNewLocalFromModelPath() method demonstrated as part
        // of the previous example.
        /// <summary>
        /// Get the server path for a particular model and open a new local copy
        /// </summary>
        public Document OpenNewLocalFromServer() {

            // Get the host id/IP of the server
            String hostId = _app.GetRevitServerNetworkHosts().First();

            // try to get the server path for the particular model on the server
            String rootFolder = "|";
            ModelPath serverPath = FindWSAPIModelPathOnServer(_app, hostId, rootFolder, "WorksharingOnServer.rvt");

            ModelPath newLocalPath = GetWSAPIModelPath("WorksharingLocalFromServer.rvt");
            bool isWrkShared = true;
            List<string> InverseWorksetList = new List<string>();
            return (OpenNewLocalFromModelPath(serverPath, newLocalPath, InverseWorksetList, out isWrkShared));
        }

        /// <summary>
        /// Uses the Revit Server REST API to recursively search the folders of the Revit Server for a particular model.
        /// </summary>
        private ModelPath FindWSAPIModelPathOnServer(Application app, string hostId, string folderName, string fileName) {
            // Connect to host to find list of available models (the "/contents" flag)
            XmlDictionaryReader reader = GetResponse(app, hostId, folderName + "/contents");
            bool found = false;

            // Look for the target model name in top level folder
            List<String> folders = new List<String>();
            while (reader.Read()) {
                // Save a list of subfolders, if found
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Folders") {
                    while (reader.Read()) {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Folders")
                            break;

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "Name") {
                            reader.Read();
                            folders.Add(reader.Value);
                        }
                    }
                }
                // Check for a matching model at this folder level
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Models") {
                    found = FindModelInServerResponseJson(reader, fileName);
                    if (found)
                        break;
                }
            }

            reader.Close();

            // Build the model path to match the found model on the server
            if (found) {
                // Server URLs use "|" for folder separation, Revit API uses "/"
                String folderNameFragment = folderName.Replace('|', '/');

                // Add trailing "/" if not present
                if (!folderNameFragment.EndsWith("/"))
                    folderNameFragment += "/";

                // Build server path
                ModelPath modelPath = new ServerPath(hostId, folderNameFragment + fileName);
                return modelPath;
            } else {
                // Try subfolders
                foreach (String folder in folders) {
                    ModelPath modelPath = FindWSAPIModelPathOnServer(app, hostId, folder, fileName);
                    if (modelPath != null)
                        return modelPath;
                }
            }

            return null;
        }

        // This string is different for each RevitServer version
        private static string s_revitServerVersion = "/RevitServerAdminRESTService2014/AdminRESTService.svc/";

        /// <summary>
        /// Connect to server to get list of available models and return server response
        /// </summary>
        private static XmlDictionaryReader GetResponse(Application app, string hostId, string info) {
            // Create request	
            WebRequest request = WebRequest.Create("http://" + hostId + s_revitServerVersion + info);
            request.Method = "GET";

            // Add the information the request needs

            request.Headers.Add("User-Name", app.Username);
            request.Headers.Add("User-Machine-Name", app.Username);
            request.Headers.Add("Operation-GUID", Guid.NewGuid().ToString());

            // Read the response
            XmlDictionaryReaderQuotas quotas =
                new XmlDictionaryReaderQuotas();
            XmlDictionaryReader jsonReader =
                JsonReaderWriterFactory.CreateJsonReader(request.GetResponse().GetResponseStream(), quotas);

            return jsonReader;
        }

        /// <summary>
        /// Read through server response to find particular model
        /// </summary>
        private static bool FindModelInServerResponseJson(XmlDictionaryReader reader, string fileName) {
            // Read through entries in this section
            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Models")
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Name") {
                    reader.Read();
                    String modelName = reader.Value;
                    if (modelName.Equals(fileName)) {
                        // Match found, stop looping and return
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region  Open detached
        // If an add-in will be working on a workshared file but does not need to make
        // permanet changes, it can open the model detached from the central file.
        private Document OpenDetached(ModelPath modelPath) {
            OpenOptions options1 = new OpenOptions();
            options1.DetachFromCentralOption = DetachFromCentralOption.DetachAndDiscardWorksets;
            Document openedDoc = _app.OpenDocumentFile(modelPath, options1);

            return openedDoc;
        }

        #endregion

        #region Copy and open detached
        // If an application only needs read-only access to a server file, the example below 
        // demonstrates how to copy the server model locally and open it detached. Note this 
        // code sample re-uses methods demonstrated in previous examples.
        public Document CopyAndOpenDetached(UIApplication uiApp) {
            // Copy a server model locally and open detached
            Application application = uiApp.Application;
            String hostId = application.GetRevitServerNetworkHosts().First();

            // Try to get the server path for the particular model on the server
            String rootFolder = "|";
            ModelPath serverPath = FindWSAPIModelPathOnServer(application, hostId, rootFolder, "ServerModel.rvt");

            // For debugging
            String sourcePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(serverPath);

            // Setup the target location for the copy
            ModelPath localPath = GetWSAPIModelPath("CopiedModel.rvt");

            // Copy, allowing overwrite
            application.CopyModel(serverPath, ModelPathUtils.ConvertModelPathToUserVisiblePath(localPath), true);

            // Open the copy as detached
            Document openedDoc = OpenDetached(localPath);

            return openedDoc;
        }
        #endregion
    }
}
