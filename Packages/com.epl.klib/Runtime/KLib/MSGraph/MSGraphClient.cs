using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

using Microsoft.Graph;

using Newtonsoft.Json;

using KLib.Network;
using KLib.MSGraph.Data;


using Error = KLib.MSGraph.Data.Error;
using DriveItem = KLib.MSGraph.Data.DriveItem;
namespace KLib.MSGraph
{
    public static class MSGraphClient
    {
        [Flags]
        public enum ConnectionStatus
        {
            Unconnected = 0x00,
            HaveInterface = 0x01,
            HaveAccessToken = 0x02,
            HaveFolderAccess = 0x04,
            Error = 0x08,
            Ready = HaveInterface | HaveAccessToken | HaveFolderAccess
        }

        private static string _accessToken = "";
        private static string _basePath = "";
        private static string _lastError = "";
        private static string _requestedFolder = "";

        private static int _timeOut = 5000;

        static string _graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        public static bool IsReady { get { return GetConnectionStatus() == ConnectionStatus.Ready; } }
        public static string LastError { get { return _lastError; } }
        public static string RestartPath { set; get; } = "";

        private static int _port = 51234;

        private static GraphServiceClient _graphServiceClient;

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Do not allow this client to communicate with unauthenticated servers. 
            return true;
        }

        public static int Timeout
        {
            set { _timeOut = value; }
            get { return _timeOut; }
        }

        public static ConnectionStatus Initialize(string folder)
        {
            _accessToken = "";
            _basePath = "";
            _lastError = "";
            _requestedFolder = folder;

            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    AcquireAccessToken();
                }

                if (!string.IsNullOrEmpty(_accessToken) && string.IsNullOrEmpty(_basePath))
                {
                    SetBaseFolder(folder);
                    _graphServiceClient =  new GraphServiceClient(
                                   "https://graph.microsoft.com/v1.0",
                                   new DelegateAuthenticationProvider(
                                       async (requestMessage) =>
                                       {
                                        // Set bearer authentication on header
                                        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _accessToken);
                                       }));
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }

            return GetConnectionStatus();
        }

        public static ConnectionStatus GetConnectionStatus()
        {
            return GetConnectionStatus(out string details);
        }

        public static ConnectionStatus GetConnectionStatus(out string details)
        {
            ConnectionStatus status = ConnectionStatus.HaveInterface;
            details = "";

            if (!string.IsNullOrEmpty(_accessToken))
            {
                status |= ConnectionStatus.HaveAccessToken;
            }
            if (!string.IsNullOrEmpty(_basePath))
            {
                status |= ConnectionStatus.HaveFolderAccess;
            }

            if (!string.IsNullOrEmpty(_lastError))
            {
                status |= ConnectionStatus.Error;
                if (_lastError.StartsWith("No connection"))
                {
                    status &= ~ConnectionStatus.HaveInterface;
                    details = "No connection to OneDrive Interface";
                }
                else if (_lastError.StartsWith("Timeout"))
                {
                    details = "Network timeout";
                }
                else if (_lastError.StartsWith("Failed to read"))
                {
                    details = "Connection error--no WiFi?";
                }
                else
                {
                    details = _lastError;
                }
            }
            else if (string.IsNullOrEmpty(_accessToken))
            {
                details = "Not signed in";
            }
            else if (string.IsNullOrEmpty(_basePath))
            {
                details = $"Remote folder '{_requestedFolder}' not found";
            }

            return status;
        }

        public static bool TestConnection()
        {
            bool success = false;
            var status = GetConnectionStatus();
            if (status == ConnectionStatus.Ready)
            {
                var content = GetHttpContentRemote("");
                success = !string.IsNullOrEmpty(content);
                if (!success)
                {
                    _accessToken = "";
                }

            }
            return success;
        }

        public static string GetUser()
        {
            string user = "";
            var content = GetHttpContentRemote("");
            if (!string.IsNullOrEmpty(content))
            {
                var userInfo = JsonConvert.DeserializeObject<UserInfo>(content);
                user = userInfo.userPrincipalName;
            }

            return user;
        }

        public static bool SignInUser()
        {
            bool signedIn = false;
            KTcpClient tcpClient = new KTcpClient();

            try
            {
                var cmd = "signin";
                if (!string.IsNullOrEmpty(RestartPath))
                {
                    cmd += $";{RestartPath}";
                }

                tcpClient.Connect("127.0.0.1", _port);
                var result = tcpClient.WriteStringToOutputStream(cmd);
                signedIn = (result == "OK");
            }
            catch (Exception ex)
            {
            }

            return signedIn;
        }

        public static bool SignOutUser()
        {
            bool signedOut = false;
            KTcpClient tcpClient = new KTcpClient();

            try
            {
                tcpClient.Connect("127.0.0.1", _port);
                var result = tcpClient.WriteStringToOutputStream("signout");
                signedOut = (result == "OK");
            }
            catch (Exception ex)
            {
            }

            return signedOut;
        }

        public static bool AcquireAccessToken()
        {
            _accessToken = "";

            KTcpClient tcpClient = new KTcpClient();

            try
            {
                //tcpClient.SendTimeout = 5000;
                //tcpClient.ReceiveTimeout = 2000;
                tcpClient.Connect("127.0.0.1", _port);
                _accessToken = tcpClient.WriteStringToOutputStream("token");
            }
            catch (Exception ex)
            {
                _accessToken = "";
                _lastError = ex.Message;
            }

            return !string.IsNullOrEmpty(_accessToken);
        }

        private static bool SetBaseFolder(string name)
        {
            _basePath = "";

            var user = GetUser();
            if (string.IsNullOrEmpty(user))
            {
                return false;
            }

            string cmd = "";
            DriveItem di = null;

            if (user.ToLower().Contains("hancock"))
            {

                cmd = "/drive/root/children?select=id,name,folder,parentReference";
                var result = GetHttpContentRemote(cmd);

                var items = JsonConvert.DeserializeObject<DriveItemContainer>(result);
                di = items.value.Find(o => o.folder != null && o.name == name);
            }

            if (di != null)
            {
                _basePath = $"/drives/{di.parentReference.driveId}/items/{di.id}";
            }
            else
            {
                cmd = "/drive/sharedWithMe";
                var result = GetHttpContentRemote(cmd);

                result = result.Replace("@microsoft.graph.downloadUrl", "url");
                var items = JsonConvert.DeserializeObject<DriveItemContainer>(result);

                di = items.value.Find(o => o.folder != null && o.name == name);
                if (di != null)
                {
                    _basePath = $"/drives/{di.remoteItem.parentReference.driveId}/items/{di.remoteItem.id}";
                }
            }

            SetBaseFolderRemote(name);

            return !string.IsNullOrEmpty(_basePath);
        }

        private static string GetHttpContentRemote(string cmd)
        {
            string content = "";

            KTcpClient tcpClient = new KTcpClient();

            try
            {
                tcpClient.Connect("127.0.0.1", _port);
                content = tcpClient.WriteStringToOutputStream("GetHttpContent;" + cmd);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }

            return content;
        }

        public static string SendMessageToInterface(string msg)
        {
            var result = "";
            _lastError = "";

            KTcpClient tcpClient = new KTcpClient();

            try
            {
                tcpClient.Connect("127.0.0.1", _port);
                result = tcpClient.WriteStringToOutputStream(msg);
                if (!result.Equals("OK"))
                {
                    result = "Error";
                    _lastError = $"Bad response from OneDrive Interface.{Environment.NewLine}{Environment.NewLine}Rebooting is the easiest option.";
                }
            }
            catch (Exception ex)
            {
                result = "Error";
                if (ex.Message.StartsWith("No connection"))
                {
                    _lastError = $"OneDrive Interface may not be running.{Environment.NewLine}{Environment.NewLine}Rebooting is the easiest option.";
                }
                else
                {
                    _lastError = ex.Message;
                }
            }

            return result;
        }

        public static bool FileExists(string path)
        {
            bool exists = false;
            KTcpClient tcpClient = new KTcpClient();

            try
            {
                tcpClient.Connect("127.0.0.1", _port);
                var resp = tcpClient.WriteStringToOutputStream("FileExists;" + path);
                exists = resp.ToLower().Equals("true");
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
            return exists;
        }

        //private static string PutHttpContentRemote(string cmd, byte[] content)
        //{
        //    string response = "";

        //    KTcpClient tcpClient = new KTcpClient();

        //    try
        //    {
        //        tcpClient.Connect("127.0.0.1", _port);
        //        response = tcpClient.WriteStringAndBytes("PutHttpContent;" + cmd,  content);
        //    }
        //    catch (Exception ex)
        //    {
        //        _lastError = ex.Message;
        //    }

        //    return response;
        //}

        public static bool UploadFile(string remoteFolder, string localPath)
        {
            bool success = false;
            string response = "";

            KTcpClient tcpClient = new KTcpClient();

            try
            {
                tcpClient.Connect("127.0.0.1", _port);
                response = tcpClient.WriteStringToOutputStream("UploadFile;" + remoteFolder + ";" + localPath);
                if (response.Equals("OK"))
                {
                    success = true;
                }
                else if (response.Equals("no connection"))
                {
                    _accessToken = "";
                }

                success = !string.IsNullOrEmpty(response);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }

            return success;
        }

        private static string PostHttpContentRemote(string cmd, string content)
        {
            string response = "";

            KTcpClient tcpClient = new KTcpClient();

            try
            {
                tcpClient.Connect("127.0.0.1", _port);
                response = tcpClient.WriteStringToOutputStream("PostHttpContent;" + cmd + ";" + content);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }

            return response;
        }

        private static string SetBaseFolderRemote(string name)
        {
            string response = "";

            KTcpClient tcpClient = new KTcpClient();

            try
            {
                tcpClient.Connect("127.0.0.1", _port);
                response = tcpClient.WriteStringToOutputStream("SetBaseFolder;" + name);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }

            return response;
        }

        private static string DeleteHttpRemote(string cmd)
        {
            string content = "";

            KTcpClient tcpClient = new KTcpClient();

            try
            {
                tcpClient.Connect("127.0.0.1", _port);
                content = tcpClient.WriteStringToOutputStream("DeleteHttp;" + cmd);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }

            return content;
        }

        private static Error CheckForError(string result)
        {
            Error error = null;
            if (result.StartsWith("{\"error"))
            {
                error = JsonConvert.DeserializeObject<ErrorResource>(result).error;
            }
            return error;
        }

        public static DriveItem GetItem(string itemPath)
        {
            string url = _basePath;
            if (!string.IsNullOrEmpty(itemPath))
            {
                url += $":/{itemPath}:";
            }
            //var result = GetHttpContentRemote(url);
            var result = GetHttpContent(url);

            result = result.Replace("@microsoft.graph.downloadUrl", "url");
            var item = JsonConvert.DeserializeObject<DriveItem>(result);

            return item;
        }

        public static List<DriveItem> GetItems(string folder)
        {
            string url = _basePath;
            if (!string.IsNullOrEmpty(folder))
            {
                url += $":/{folder}:";
            }
            url += "/children?select=name,@microsoft.graph.downloadUrl,folder,fileSystemInfo,parentReference";
            var result = GetHttpContentRemote(url);

            result = result.Replace("@microsoft.graph.downloadUrl", "url");
            var items = JsonConvert.DeserializeObject<DriveItemContainer>(result);

            return items.value;
        }

        public static List<DriveItem> GetFiles(string folder)
        {
            var items = GetItems(folder);
            return items.FindAll(o => o.folder == null);
        }

        public static List<DriveItem> GetFolders(string folder)
        {
            var items = GetItems(folder);
            return items.FindAll(o => o.folder != null);
        }

        public static List<string> GetFolderNames(string folder)
        {
            var items = GetFolders(folder);
            return items.FindAll(o => o.folder != null).Select(o => o.name).ToList();
        }

        public static bool FolderExists(string folderName)
        {
            string parent = "";
            string child = folderName;
            if (folderName.Contains("/"))
            {
                int lastSlash = folderName.LastIndexOf('/');
                parent = folderName.Substring(0, lastSlash);
                child = folderName.Substring(lastSlash + 1);
            }

            var folders = GetFolderNames(parent);
            return folders != null ? folders.Contains(child) : false;
        }

        public static bool CreateFolder(string folderName)
        {
            string parent = "";
            string child = folderName;
            if (folderName.Contains("/"))
            {
                int lastSlash = folderName.LastIndexOf('/');
                parent = folderName.Substring(0, lastSlash);
                child = folderName.Substring(lastSlash + 1);
            }

            string cmd = _basePath;
            if (!string.IsNullOrEmpty(parent))
            {
                cmd += $":/{parent}:";
            }
            cmd += "/children";

            string content = $"{{\"name\": \"{child}\", \"folder\": {{ }} }}";

            var result = PostHttpContentRemote(cmd, content);

            return result != null;
        }

        public static bool FileExistsXXX(string remotePath)
        {
            bool exists = false;
            try
            {
                var item = GetItem(remotePath);
                exists = true;
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("404"))
                    throw (ex);
            }

            return exists;
        }

        public static bool DownloadFileByPath(string remotePath, string localPath)
        {
            bool success = false;

            var item = GetItem(remotePath);
            if (item != null)
            {
                success = DownloadFile(item.url, localPath);
            }
            return success;
        }

        public static bool DownloadFile(DriveItem item, string localPath)
        {
            bool success = false;

            success = DownloadFile(item.url, localPath);

            var tcreated = System.DateTime.Parse(item.fileSystemInfo.createdDateTime).ToUniversalTime();
            var tmodified = System.DateTime.Parse(item.fileSystemInfo.lastModifiedDateTime).ToUniversalTime();

            System.IO.File.SetCreationTimeUtc(localPath, tcreated);
            System.IO.File.SetLastWriteTimeUtc(localPath, tmodified);

            return success;
        }

        public static bool DownloadFile(string url, string localPath)
        {
            bool success = true;
            KTcpClient tcpClient = new KTcpClient();

            try
            {
                tcpClient.Connect("127.0.0.1", _port);
                var result = tcpClient.WriteStringToOutputStream("download;" + url + ";" + localPath);
                success = (result == "OK");
                if (!success) _lastError = result;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                success = false;
            }
            return success;
        }

        //public static bool UploadFile(string remoteFolder, string localPath)
        //{
        //    bool success = true;

        //    try
        //    {
        //        var fn = Path.GetFileName(localPath);
        //        string cmd = _basePath + ":/" + remoteFolder + "/" + fn;

        //        var content = File.ReadAllBytes(localPath);
        //        var result = PutHttpContentRemote(cmd + ":/content", content);

        //        success = (result != null);
        //        //if (!success) _lastError = result;
        //    }
        //    catch (Exception ex)
        //    {
        //        _lastError = ex.Message;
        //        success = false;
        //    }
        //    return success;
        //}

        public static bool DeleteFile(string remoteFolder, string filename)
        {
            string cmd = $"{_basePath}:/{remoteFolder}/{filename}";

            var result = DeleteHttpRemote(cmd);

            return result != null;
        }

        public static bool DeleteFile(DriveItem item)
        {
            string cmd = $"{item.parentReference.path}/{item.name}";

            var result = DeleteHttpRemote(cmd);

            return result != null;
        }

        private static string GetHttpContent(string cmd)
        {
            // https://itecnote.com/tecnote/c-mono-https-webrequest-fails-with-the-authentication-or-decryption-has-failed/
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;

            string content = "";
            string url = _graphAPIEndpoint + cmd;
            bool finished = false;
            Error error = null;
            bool allowRetry = true;

            while (!finished)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                    request.Timeout = _timeOut;
                    request.Headers["Authorization"] = "Bearer " + _accessToken;

                    request.Method = WebRequestMethods.Http.Get;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        var responseStream = new StreamReader(response.GetResponseStream());
                        content = responseStream.ReadToEnd();
                        responseStream.Close();
                        error = CheckForError(content);
                    }
                }
                catch (WebException we)
                {
                    var resp = (HttpWebResponse)we.Response;
                    if (resp != null)
                    {
                        error = new Error(((int)resp.StatusCode).ToString(), resp.StatusDescription);
                    }
                    else
                    {
                        error = new Error(we.Status.ToString(), we.Message);
                    }
                }
                catch (Exception ex)
                {
                    error = new Error("MSGraphError", ex.ToString());
                }

                if (error != null && allowRetry && (error.code == "InvalidAuthenticationToken" || error.code == "401"))
                {
                    var refreshed = AcquireAccessToken();
                    if (!refreshed)
                    {
                        error = new Error("InvalidAuthenticationToken", "failed to refresh token");
                        finished = true;
                    }
                }
                else
                {
                    finished = true;
                }
            }

            if (error != null)
            {
                throw new Exception($"{error.code}: {error.message}");
            }

            return content;
        }

        private static string PutHttpContentXXX(string cmd, byte[] content)
        {
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;

            string url = _graphAPIEndpoint + cmd;

            bool finished = false;
            string result = null;
            Error error = null;
            bool allowRetry = true;

            while (!finished)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = _timeOut;
                    request.Headers["Authorization"] = "Bearer " + _accessToken;

                    // Let the server know we want to "put" a file on it
                    request.Method = WebRequestMethods.Http.Put;

                    // Set the length of the content (file) we are sending
                    request.ContentLength = content.Length;

                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(content, 0, content.Length);
                    // Close the request stream.
                    requestStream.Close();
                    requestStream.Dispose();

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        var responseStream = new StreamReader(response.GetResponseStream());
                        result = responseStream.ReadToEnd();
                        responseStream.Close();
                        error = CheckForError(result);
                    }
                }
                catch (Exception ex)
                {
                    error = new Error("MSGraphError", ex.ToString());
                }

                if (error != null && allowRetry && (error.code == "InvalidAuthenticationToken" || error.code == "401"))
                {
                    var refreshed = AcquireAccessToken();
                    if (!refreshed)
                    {
                        error = new Error("InvalidAuthenticationToken", "failed to refresh token");
                        finished = true;
                    }
                }
                else
                {
                    finished = true;
                }
            }

            if (error != null)
            {
                throw new Exception($"{error.code}: {error.message}");
            }

            return result;
        }

        private static string PostHttpContentXXX(string cmd, byte[] content)
        {
            string url = _graphAPIEndpoint + cmd;

            bool finished = false;
            string result = null;
            Error error = null;
            bool allowRetry = true;

            while (!finished)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = _timeOut;
                    request.Headers["Authorization"] = "Bearer " + _accessToken;

                    request.Method = WebRequestMethods.Http.Post;
                    request.ContentType = "application/json";
                    request.ContentLength = content.Length;

                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(content, 0, content.Length);
                    // Close the request stream.
                    requestStream.Close();
                    requestStream.Dispose();

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        var responseStream = new StreamReader(response.GetResponseStream());
                        result = responseStream.ReadToEnd();
                        responseStream.Close();
                        error = CheckForError(result);
                    }
                }
                catch (Exception ex)
                {
                    error = new Error("MSGraphError", ex.ToString());
                }

                if (error != null && allowRetry && (error.code == "InvalidAuthenticationToken" || error.code == "401"))
                {
                    var refreshed = AcquireAccessToken();
                    if (!refreshed)
                    {
                        error = new Error("InvalidAuthenticationToken", "failed to refresh token");
                        finished = true;
                    }
                }
                else
                {
                    finished = true;
                }
            }

            if (error != null)
            {
                throw new Exception($"{error.code}: {error.message}");
            }

            return result;
        }

        private static string DeleteHttpXXX(string cmd)
        {
            string url = _graphAPIEndpoint + cmd;
            bool finished = false;
            string result = null;
            Error error = null;
            bool allowRetry = true;

            while (!finished)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = _timeOut;
                    request.Headers["Authorization"] = "Bearer " + _accessToken;

                    // Let the server know we want to "put" a file on it
                    request.Method = "DELETE";

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        var responseStream = new StreamReader(response.GetResponseStream());
                        result = responseStream.ReadToEnd();
                        responseStream.Close();
                        error = CheckForError(result);
                    }
                }
                catch (Exception ex)
                {
                    error = new Error("MSGraphError", ex.ToString());
                }

                if (error != null && allowRetry && (error.code == "InvalidAuthenticationToken" || error.code == "401"))
                {
                    var refreshed = AcquireAccessToken();
                    if (!refreshed)
                    {
                        error = new Error("InvalidAuthenticationToken", "failed to refresh token");
                        finished = true;
                    }
                }
                else
                {
                    finished = true;
                }
            }

            if (error != null)
            {
                throw new Exception($"{error.code}: {error.message}");
            }

            return result;
        }


    }
}