using System;
using System.Text;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RevitAKSOpen {

    class RevitFileSniffer {
        private string _pathToRevitFile;
        private const string StreamName = "BasicFileInfo";

        public RevitFileSniffer(string pathToRevitFile) {
            _pathToRevitFile = pathToRevitFile;
        }

        public void ReportRevitInfo() {
            if (!StructuredStorageUtils.IsFileStucturedStorage(_pathToRevitFile))
                throw new NotSupportedException("File is not a structured storage file");

            var rawData = GetRawBasicFileInfo(_pathToRevitFile);

            BasicFileInfo basicFileInfo = new BasicFileInfo();

            #region ReadParser
            using (var ms = new MemoryStream(rawData)) {
                using (var br = new BinaryReader(ms, System.Text.Encoding.Unicode)) {
                    basicFileInfo.A = br.ReadInt32();
                    basicFileInfo.B = br.ReadInt32();
                    basicFileInfo.C = br.ReadInt16();
                    var sLen = br.ReadInt32();
                    basicFileInfo.Path1 = System.Text.Encoding.Unicode.GetString(br.ReadBytes(sLen * 2));
                    sLen = br.ReadInt32();
                    basicFileInfo.Version = System.Text.Encoding.Unicode.GetString(br.ReadBytes(sLen * 2));
                    sLen = br.ReadInt32();
                    basicFileInfo.Path2 = System.Text.Encoding.Unicode.GetString(br.ReadBytes(sLen * 2));
                    basicFileInfo.Unknown = br.ReadBytes(5);
                    sLen = br.ReadInt32();
                    basicFileInfo.UID = System.Text.Encoding.Unicode.GetString(br.ReadBytes(sLen * 2));
                    sLen = br.ReadInt32();
                    basicFileInfo.Localization = System.Text.Encoding.Unicode.GetString(br.ReadBytes(sLen * 2));
                    //read to end
                    br.ReadBytes(2); // \r \n
                    sLen = (int)(br.BaseStream.Length - br.BaseStream.Position) - 2;
                    var buffer = br.ReadBytes(sLen);
                    basicFileInfo.Data = Encoding.Unicode.GetString(buffer);
                    br.ReadBytes(2); // \r \n
                }
            } 
            #endregion

            var rawString = System.Text.Encoding.Unicode.GetString(rawData);
            var fileInfoData = rawString.Split(new string[] { "\0", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            #region OutMessager
            string msg = _pathToRevitFile + "\n+++++++++++++++++++++\n";
            foreach (var info in fileInfoData) {
                if (info.Contains(":")) {
                    msg = msg + info + "\n---------------------\n";
                }
            }
            System.Windows.MessageBox.Show(msg, "Revit File Info"); 
            #endregion
        }

        private  byte[] GetRawBasicFileInfo(string revitFileName) {
            if (!StructuredStorageUtils.IsFileStucturedStorage(revitFileName))
                throw new NotSupportedException("File is not a structured storage file");

            using (StructuredStorageRoot ssRoot = new StructuredStorageRoot(_pathToRevitFile)) {
                if (!ssRoot.BaseRoot.StreamExists(StreamName))
                    throw new NotSupportedException(string.Format("File doesn't contain {0} stream", StreamName));

                StreamInfo imageStreamInfo =
                    ssRoot.BaseRoot.GetStreamInfo(StreamName);
                using (Stream stream = imageStreamInfo.GetStream(FileMode.Open, FileAccess.Read)) {
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
        }

    } //end class RevitFileSniffer

    public static class StructuredStorageUtils {
        [DllImport("ole32.dll")]
        static extern int StgIsStorageFile(
        [MarshalAs(UnmanagedType.LPWStr)]
		    string pwcsName);

        public static bool IsFileStucturedStorage(string fileName) {
            int res = StgIsStorageFile(fileName);
            if (res == 0)
                return true;
            if (res == 1)
                return false;
            throw new FileNotFoundException("File not found", fileName);
        }
    } // end class StructuredStorageUtils

    public class StructuredStorageException : Exception {
        public StructuredStorageException() {

        }

        public StructuredStorageException(string message)
            : base(message) {

        }

        public StructuredStorageException(string message, Exception innerException)
            : base(message, innerException) {

        }
    } // end class

    public class StructuredStorageRoot : IDisposable {
        StorageInfo _storageRoot;

        public StructuredStorageRoot(Stream stream) {

            try {
                _storageRoot = (StorageInfo)InvokeStorageRootMethod(null,
                                                               "CreateOnStream",
                                                               stream);
            } catch (Exception ex) {

                throw new StructuredStorageException("Cannot get StructuredStorageRoot", ex);
            }

        }

        public StructuredStorageRoot(string fileName) {
            try {
                _storageRoot = (StorageInfo)InvokeStorageRootMethod(null,
                    "Open", fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            } catch (Exception ex) {
                throw new StructuredStorageException("Cannot get StructuredStorageRoot", ex);
            }

        }

        private static object InvokeStorageRootMethod(StorageInfo storageRoot, string methodName, params object[] methodArgs) {
            Type storageRootType = typeof(StorageInfo).Assembly.GetType("System.IO.Packaging.StorageRoot", true, false);
            object result = storageRootType.InvokeMember(methodName,
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                null, storageRoot, methodArgs);
            return result;
        }

        private void CloseStorageRoot() {
            InvokeStorageRootMethod(_storageRoot, "Close");
        }

        #region Implementation of IDisposable

        public void Dispose() {
            CloseStorageRoot();
        }

        #endregion

        public StorageInfo BaseRoot {
            get { return _storageRoot; }
        }

    } // end class

    public struct BasicFileInfo {
        public Int32 A { get; set; }
        public Int32 B { get; set; }
        public Int16 C { get; set; }
        public string Path1 { get; set; }
        public string Version { get; set; }
        public string Path2 { get; set; }
        public string Data { get; set; }
        public byte[] Unknown { get; set; }
        public string UID { get; set; }
        public string Localization { get; set; }
    }
}


