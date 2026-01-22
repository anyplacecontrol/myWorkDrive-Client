#nullable disable

using Microsoft.AspNetCore.Http;
using MWDMockServer;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WanPath.Common.Helpers;

#if !MOCK_SERVER
using WanPath.Common.Exceptions;
using WanPath.Common.Helpers;
using WanPath.WebClient.BLL;
using WanPath.WebClient.BLL.API;
using WanPath.WebClient.BLL.Cache;
using WanPath.WebClient.BLL.Helper;
using WanPath.WebClient.BLL.MwdService;
using WanPath.WebClient.BLL.WOPI;
using WanPath.WebClient.Controllers;
#endif

namespace APIServer
{

    public class ClientAPIEndpoints : APIEndpoints
    {
        // Endpoint names
        public const string OPERATION_COPY_FILE = "CopyFile";
        public const string OPERATION_COPY_FOLDER = "CopyFolder";
        public const string OPERATION_CREATE_FILE = "CreateFile";
        public const string OPERATION_MOVE_FILE = "MoveFile";
        public const string OPERATION_MOVE_FOLDER = "MoveFolder";
        public const string OPERATION_SET_FILE_INFO = "SetFileInformation";

        // Client API
        public const string CANCEL_FILE_UPLOAD = PATH_BASE + "CancelUpload";
        public const string CHECK_SESSION = PATH_BASE + "CheckSession";
        public const string COPY_FILE = PATH_BASE + OPERATION_COPY_FILE;
        public const string COPY_FOLDER = PATH_BASE + OPERATION_COPY_FOLDER;
        public const string COMPLETE_FILE_UPLOAD = PATH_BASE + "CompleteUpload";
        public const string CREATE_FILE = PATH_BASE + OPERATION_CREATE_FILE;
        public const string DELETE_FILE = PATH_BASE + "DeleteFile";
        public const string DELETE_FOLDER = PATH_BASE + "DeleteFolder";
        public const string GET_FILE_CAPABILITIES = PATH_BASE + "GetFileCapabilities";
        public const string GET_FILE_INFORMATION = PATH_BASE + "GetFileInfo";
        public const string GET_FOLDER_INFORMATION = PATH_BASE + "GetFolderInfo";
        public const string GET_FILE_LOCKS = PATH_BASE + "GetFileLocks";
        public const string GET_TRANSFER_LINK = PATH_BASE + "GetTransferLink";
        public const string GET_FILE_UPLOAD_STATUS = PATH_BASE + "GetUploadStatus";
        public const string GET_OTP = PATH_BASE + "GetOTP";
        public const string GET_SERVER_LOGO = PATH_BASE + "GetServerLogo";
        public const string LIST_FILE_VERSIONS = PATH_BASE + "ListFileVersions";
        public const string LIST_FOLDER = PATH_BASE + "ListFolder";
        public const string LOCK_FILE = PATH_BASE + "LockFile";
        public const string MOVE_FILE = PATH_BASE + OPERATION_MOVE_FILE;
        public const string MOVE_FOLDER = PATH_BASE + OPERATION_MOVE_FOLDER;
        public const string QUERY_QUOTAS = PATH_BASE + "QueryQuotas";
        public const string READ_FILE = PATH_BASE + "ReadFile";
        public const string RESTORE_FILE_VERSION = PATH_BASE + "RestoreFileVersion";
        public const string SET_FILE_INFORMATION = PATH_BASE + OPERATION_SET_FILE_INFO;
        public const string SET_FILE_INFO = PATH_BASE + "SetFileInfo";
        public const string SHARE_FILE_BY_MAIL = PATH_BASE + "ShareFileByMail";
        public const string START_FILE_UPLOAD = PATH_BASE + "StartFileUpload";
        public const string UNLOCK_FILE = PATH_BASE + "UnlockFile";
        public const string WRITE_FILE = PATH_BASE + "WriteFile";
        public const string WRITE_FILE_BLOCK = PATH_BASE + "WriteFileBlock";
        
        // New API endpoints from updated specification
        public const string GET_ITEM_TYPE = PATH_BASE + "GetItemType";
        public const string ZIP_FILES = PATH_BASE + "ZipFiles";
        public const string LIST_SHARES = PATH_BASE + "ListShares";
        public const string LIST_BOOKMARKS = PATH_BASE + "ListBookmarks";
        public const string ADD_BOOKMARK = PATH_BASE + "AddBookmark";
        public const string DELETE_BOOKMARK = PATH_BASE + "DeleteBookmark";
        public const string LOG_MESSAGE = PATH_BASE + "LogMessage";
        public const string GET_SERVER_CONFIG = PATH_BASE + "GetServerConfig";
        public const string SEARCH_FILES = PATH_BASE + "SearchFiles";
        public const string LIST_RECENT_FILES = PATH_BASE + "ListRecentFiles";
        public const string CLEAR_RECENT_FILES = PATH_BASE + "ClearRecentFiles";
        public const string GET_PUBLIC_LINK_SETTINGS = PATH_BASE + "GetPublicLinkSettings";
        public const string GET_PUBLIC_LINK_INFO = PATH_BASE + "GetPublicLinkInfo";
        public const string LIST_PUBLIC_LINKS = PATH_BASE + "ListPublicLinks";
        public const string DELETE_PUBLIC_LINKS = PATH_BASE + "DeletePublicLinks";
        public const string CREATE_PUBLIC_LINK = PATH_BASE + "CreatePublicLink";
        public const string UPDATE_PUBLIC_LINK = PATH_BASE + "UpdatePublicLink";
    }

    public class ClientAPIConstants
    {
        public const string RESPONSE_VALUE_NAME_AVAILABLE_BYTES = "availableBytes";
        public const string RESPONSE_VALUE_NAME_FREE_BYTES = "freeBytes";
        public const string RESPONSE_VALUE_NAME_TOTAL_BYTES = "totalBytes";
        public const string RESPONSE_VALUE_NAME_PATH = "path";
        public const string RESPONSE_VALUE_NAME_SIZE = "size";
        public const string RESPONSE_VALUE_ITEM_TYPE_FILE = "file";
        public const string RESPONSE_VALUE_ITEM_TYPE_FOLDER = "folder";
        public const string RESPONSE_VALUE_ITEM_TYPE_UNKNOWN = "unknown";
    }

    public class StatusResponseDeleteFolder
    {
        public string status { get; set; } = "success";
        public int errorCode { get; set; } = 0;
        public string message { get; set; } = "";
        public List<string> details { get; set; }
    }

    public partial class ClientAPIHandler : APIHandlerBase
    {
#if MOCK_SERVER
        // Mock server upload session tracking
        private static readonly Dictionary<string, UploadSession> _uploadSessions = new Dictionary<string, UploadSession>();
        
        // Static collections for new endpoint mock data
        private static readonly Dictionary<string, Dictionary<string, object>> _bookmarks = new();
        private static readonly List<string> _recentFiles = new();
        private static readonly Dictionary<string, ZipReferenceData> _zipReferences = new();
        private static int _nextBookmarkId = 1;
        
        private class UploadSession
        {
            public string UploadId { get; set; }
            public string FilePath { get; set; }
            public long TotalSize { get; set; }
            public long BytesUploaded { get; set; }
            public DateTime Created { get; set; }
            public bool IsCancelled { get; set; }
        }

        private class ZipReferenceData
        {
            public byte[] Data { get; set; }
            public DateTime Created { get; set; }
            public List<string> FoundFiles { get; set; }
            public List<string> MissingFiles { get; set; }
        }
#endif
#if MOCK_SERVER
        public static string? BasePath { get;set; }
        public static string DefaultShare { get;set; } = "Documents";
        public static string PathFormat { get;set; } = "scheme";

        static string GetFullPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return BasePath ?? PathHelper.GetDefaultBasePath();

            var parsedPath = PathResolver.ParsePath(path);
            if (parsedPath == null)
            {
                // Fallback to old behavior for invalid paths
                if (string.IsNullOrEmpty(BasePath))
                    throw new Exception("Wrong path");

                if ("/".Equals(path))
                    return BasePath;

                return PathHelper.ToSystemPath(path, BasePath);
            }
            

            switch (parsedPath.Type)
            {
                case PathResolver.PathType.LinkBased:
                    // Resolve link to actual path first
                    var actualPath = PublicLinkManager.ResolveLinkPath(parsedPath.LinkId);
                    if (actualPath == null)
                        throw new Exception($"Invalid link ID: {parsedPath.LinkId}");
                    
                    // Parse the resolved path
                    var resolvedParsed = PathResolver.ParsePath(actualPath);
                    if (resolvedParsed == null)
                        throw new Exception($"Invalid resolved path: {actualPath}");
                        
                    // Get the share from the resolved path
                    var linkShare = ShareManager.GetShare(resolvedParsed.ShareName);
                    if (linkShare == null)
                        throw new Exception($"Share not found: {resolvedParsed.ShareName}");
                    
                    // For link-based paths, if the relative path is just "/", use the resolved path's relative path
                    // Otherwise, combine them
                    var linkRelativePath = parsedPath.RelativePath?.TrimStart('/') ?? "";
                    if (string.IsNullOrEmpty(linkRelativePath))
                    {
                        // Use the resolved file/folder path directly
                        linkRelativePath = resolvedParsed.RelativePath?.TrimStart('/') ?? "";
                    }
                    else
                    {
                        // Combine the resolved path with the additional relative path
                        var basePath = resolvedParsed.RelativePath?.TrimStart('/') ?? "";
                        linkRelativePath = PathHelper.CombineApiPaths(basePath, linkRelativePath).TrimStart('/');
                    }
                    return PathHelper.ToSystemPath("/" + linkRelativePath, linkShare.PhysicalPath);

                case PathResolver.PathType.ShareBased:
                case PathResolver.PathType.SchemeShareBased:
                    // Handle root path "/" which has empty ShareName - fall back to BasePath behavior
                    if (string.IsNullOrEmpty(parsedPath.ShareName))
                    {
                        if (string.IsNullOrEmpty(BasePath))
                            throw new Exception("Wrong path");

                        if ("/".Equals(path))
                            return BasePath;

                        return PathHelper.ToSystemPath(path, BasePath);
                    }
                    
                    // Handle the case where the "ShareName" is actually a filename/dirname in the root directory
                    // This happens when someone requests "/filename.ext" or "/dirname" which gets parsed as ShareName="filename.ext"/"dirname"
                    // Only treat as root-level item if it's not a known share and RelativePath is "/"
                    if (parsedPath.RelativePath == "/" && !ShareManager.ShareExists(parsedPath.ShareName))
                    {
                        if (string.IsNullOrEmpty(BasePath))
                            throw new Exception("Wrong path");

                        return Path.Combine(BasePath, parsedPath.ShareName);
                    }
                    
                    var share = ShareManager.GetShare(parsedPath.ShareName);
                    if (share == null)
                        throw new Exception($"Share not found: {parsedPath.ShareName}");
                    
                    var relativePath = parsedPath.RelativePath ?? "/";
                    return PathHelper.ToSystemPath(relativePath, share.PhysicalPath);

                default:
                    throw new Exception($"Unsupported path type: {parsedPath.Type}");
            }
        }

        static string GenerateUniqueFilePath(string originalPath)
        {
            if (!File.Exists(originalPath))
                return originalPath;

            var directory = Path.GetDirectoryName(originalPath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);

            int counter = 1;
            string uniquePath;

            do
            {
                var newFileName = $"{fileNameWithoutExtension}({counter}){extension}";
                uniquePath = Path.Combine(directory ?? "", newFileName);
                counter++;
            }
            while (File.Exists(uniquePath));

            return uniquePath;
        }

        static string ConvertPhysicalPathToLogical(string physicalPath)
        {
            // Convert physical path back to logical share-based path
            if (string.IsNullOrEmpty(physicalPath))
                return physicalPath;

            // Normalize the paths for comparison
            string normalizedPhysicalPath = Path.GetFullPath(physicalPath);

            // Get all shares and find the matching one
            var shares = ShareManager.GetAllShares();
            foreach (var share in shares.Values)
            {
                string normalizedSharePath = Path.GetFullPath(share.PhysicalPath);
                if (normalizedPhysicalPath.StartsWith(normalizedSharePath, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePart = PathHelper.ToApiPath(normalizedPhysicalPath, normalizedSharePath);

                    // Return path in configured format
                    if (PathFormat == "share")
                    {
                        return $"/{share.ShareName}{relativePart}";
                    }
                    else
                    {
                        // Default to scheme format: :sh:ShareName:/path
                        return $":sh:{share.ShareName}:{relativePart}";
                    }
                }
            }

            // Fallback: return as-is if no share matches
            return physicalPath;
        }

        static string GenerateUniqueUploadPath(string originalApiPath)
        {
            // Generate unique API path for upload sessions (handles path conflicts with existing upload sessions)
            var directory = Path.GetDirectoryName(originalApiPath)?.Replace('\\', '/') ?? "";
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalApiPath);
            var extension = Path.GetExtension(originalApiPath);

            int counter = 1;
            string uniquePath;

            do
            {
                var newFileName = $"{fileNameWithoutExtension}({counter}){extension}";
                uniquePath = string.IsNullOrEmpty(directory) ? newFileName : $"{directory.TrimEnd('/')}/{newFileName}";
                counter++;

                // Check if this path is already being used by an active upload session
                bool conflictExists = _uploadSessions.Values.Any(s =>
                    s.FilePath.Equals(uniquePath, StringComparison.OrdinalIgnoreCase) && !s.IsCancelled);

                if (!conflictExists && !File.Exists(GetFullPath(uniquePath)))
                    break;

            } while (true);

            return uniquePath;
        }
#endif

        static object _lockObject = new object();
        static ClientAPIHandler _handler = null;

        enum ConflictBehavior
        {
            Undefined = 0,
            Fail = 1,
            Rename = 2,
            Replace = 3,
            Ignore = 4,
        }

        public static ClientAPIHandler GetInstance()
        {
            lock (_lockObject)
            {
                if (_handler == null)
                    _handler = new ClientAPIHandler();
                return _handler;
            }
        }

        public ClientAPIHandler() : base()
        {
#if MOCK_SERVER
            ShareManager.InitializeShares(BasePath);
#endif
        }

        public override bool RecognizesPath(string path)
        {
            if (!path.StartsWith(APIEndpoints.PATH_BASE, StringComparison.InvariantCultureIgnoreCase))
                return false;
            else
                return base.RecognizesPath(path);
        }

        protected override void AssignHandlers()
        {
            _dispatchTable.Add(APIEndpoints.PATH_BASE + "test", HandleTestAsync);

            _dispatchTable.Add(ClientAPIEndpoints.CANCEL_FILE_UPLOAD, HandleCancelFileUploadAsync);
            _dispatchTable.Add(ClientAPIEndpoints.CHECK_SESSION, HandleCheckSessionAsync);
            _dispatchTable.Add(ClientAPIEndpoints.COMPLETE_FILE_UPLOAD, HandleCompleteFileUploadAsync);
            _dispatchTable.Add(ClientAPIEndpoints.COPY_FILE, HandleCopyFileAsync);
            _dispatchTable.Add(ClientAPIEndpoints.COPY_FOLDER, HandleCopyFolderAsync);
            _dispatchTable.Add(ClientAPIEndpoints.CREATE_FILE, HandleCreateFileAsync);
            _dispatchTable.Add(ClientAPIEndpoints.DELETE_FILE, HandleDeleteFileAsync);
            _dispatchTable.Add(ClientAPIEndpoints.DELETE_FOLDER, HandleDeleteFolderAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_FILE_CAPABILITIES, HandleGetFileCapabilitiesAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_FILE_INFORMATION, HandleGetFileInformationAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_FILE_LOCKS, HandleGetFileLocksAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_TRANSFER_LINK, HandleGetFileTransferLinkAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_FILE_UPLOAD_STATUS, HandleGetFileUploadStatusAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_FOLDER_INFORMATION, HandleGetFolderInformationAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_OTP, HandleGetFileOTPAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_SERVER_LOGO, HandleGetServerLogoAsync);
            _dispatchTable.Add(ClientAPIEndpoints.LIST_FILE_VERSIONS, HandleListFileVersionsAsync);
            _dispatchTable.Add(ClientAPIEndpoints.LIST_FOLDER, HandleListFolderAsync);
            _dispatchTable.Add(ClientAPIEndpoints.LOCK_FILE, HandleLockFileAsync);
            _dispatchTable.Add(ClientAPIEndpoints.MOVE_FILE, HandleMoveFileAsync);
            _dispatchTable.Add(ClientAPIEndpoints.MOVE_FOLDER, HandleMoveFolderAsync);
            _dispatchTable.Add(ClientAPIEndpoints.QUERY_QUOTAS, HandleQueryQuotasAsync);
            _dispatchTable.Add(ClientAPIEndpoints.READ_FILE, HandleReadFileAsync);
            _dispatchTable.Add(ClientAPIEndpoints.RESTORE_FILE_VERSION, HandleRestoreFileVersionAsync);
            _dispatchTable.Add(ClientAPIEndpoints.SHARE_FILE_BY_MAIL, HandleShareFileByMailAsync);
            _dispatchTable.Add(ClientAPIEndpoints.SET_FILE_INFORMATION, HandleSetFileInformationAsync);
            _dispatchTable.Add(ClientAPIEndpoints.SET_FILE_INFO, HandleSetFileInformationAsync);
            _dispatchTable.Add(ClientAPIEndpoints.START_FILE_UPLOAD, HandleStartFileUploadAsync);
            _dispatchTable.Add(ClientAPIEndpoints.UNLOCK_FILE, HandleUnlockFileAsync);
            _dispatchTable.Add(ClientAPIEndpoints.WRITE_FILE, HandleWriteFileAsync);
            _dispatchTable.Add(ClientAPIEndpoints.WRITE_FILE_BLOCK, HandleWriteFileBlockAsync);

            // New API endpoints
            _dispatchTable.Add(ClientAPIEndpoints.GET_ITEM_TYPE, HandleGetItemTypeAsync);
            _dispatchTable.Add(ClientAPIEndpoints.ZIP_FILES, HandleZipFilesAsync);
            _dispatchTable.Add(ClientAPIEndpoints.LIST_SHARES, HandleListSharesAsync);
            _dispatchTable.Add(ClientAPIEndpoints.LIST_BOOKMARKS, HandleListBookmarksAsync);
            _dispatchTable.Add(ClientAPIEndpoints.ADD_BOOKMARK, HandleAddBookmarkAsync);
            _dispatchTable.Add(ClientAPIEndpoints.DELETE_BOOKMARK, HandleDeleteBookmarkAsync);
            _dispatchTable.Add(ClientAPIEndpoints.LOG_MESSAGE, HandleLogMessageAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_SERVER_CONFIG, HandleGetServerConfigAsync);
            _dispatchTable.Add(ClientAPIEndpoints.SEARCH_FILES, HandleSearchFilesInstanceAsync);
            _dispatchTable.Add(ClientAPIEndpoints.LIST_RECENT_FILES, HandleListRecentFilesInstanceAsync);
            _dispatchTable.Add(ClientAPIEndpoints.CLEAR_RECENT_FILES, HandleClearRecentFilesAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_PUBLIC_LINK_SETTINGS, HandleGetPublicLinkSettingsAsync);
            _dispatchTable.Add(ClientAPIEndpoints.GET_PUBLIC_LINK_INFO, HandleGetPublicLinkInfoAsync);
            _dispatchTable.Add(ClientAPIEndpoints.LIST_PUBLIC_LINKS, HandleListPublicLinksAsync);
            _dispatchTable.Add(ClientAPIEndpoints.DELETE_PUBLIC_LINKS, HandleDeletePublicLinksInstanceAsync);
            _dispatchTable.Add(ClientAPIEndpoints.CREATE_PUBLIC_LINK, HandleCreatePublicLinkAsync);
            _dispatchTable.Add(ClientAPIEndpoints.UPDATE_PUBLIC_LINK, HandleUpdatePublicLinkAsync);

#if DEBUG
            _dispatchTable.Add(APIEndpoints.MIRROR, HandleMirrorAsync);
#endif
        }

        /// <summary>
        /// This method is introduced as an override because it provides special handling of the exceptions that make sense and are available only in the WebClient project, thus cannot be handled in the base class.
        /// </summary>
        protected override async Task InvokeHandlerAsync(Func<APIContext, CancellationToken, Task> callee, APIContext apiContext, CancellationToken cancellationToken = default)
        {
            try
            {
                await callee(apiContext, cancellationToken);
            }
#if !MOCK_SERVER
            catch (InputOutputProviderUnauthorizedException ex)
            {
                apiContext.ResponseObject = new Dictionary<string, object>
                {
                    { APIConstants.RESPONSE_VALUE_NAME_AUTHENTICATION_URL, ex.AuthenticationUrl },
                };
                apiContext.SetError(APIErrorCodes.IO_PROVIDER_REPORTED_NOT_AUTHORIZED, APIContext.ExceptionToString(ex, ""));
            }
            catch (AggregateException ex)
            {
                foreach (Exception iex in ex.InnerExceptions)
                {
                    if (iex is InputOutputProviderUnauthorizedException ioex)
                    {
                        apiContext.ResponseObject = new Dictionary<string, object>
                        {
                            { APIConstants.RESPONSE_VALUE_NAME_AUTHENTICATION_URL, ioex.AuthenticationUrl },
                        };
                        apiContext.SetError(APIErrorCodes.IO_PROVIDER_REPORTED_NOT_AUTHORIZED, APIContext.ExceptionToString(ioex, ""));
                        break;
                    }
                }
            }
#else
            catch (Exception)
            {
                throw;
            }
#endif
        }

        #region Handlers

        private static Task HandleTestAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            return SendResponseAsync(apiContext, "Server works", APIConstants.CONTENT_TYPE_TEXT_PLAIN, cancellationToken);
        }

#if DEBUG
        /// <summary>
        /// This method is used in unit tests. It sends back the parsed request parameters dictionary as a JSON.
        /// </summary>
        private static Task HandleMirrorAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {

            // Just for testing - report the requested failure with the provided code
            long? errCode = -1;
            if (PickParameter(apiContext, "failRequest", false, -1L, out errCode) && (errCode > 0))
            {
                apiContext.SetError(APIErrorCodes.TEST_FAILURE, "Test of error response", (int)errCode);
                return Task.CompletedTask;
            }

            StringBuilder builder = new StringBuilder();
            // This will just dump the request parameters as a flat object, which is what we need for an illustration and testing
            StrUtils.AppendObjectToJson(apiContext.RequestParameters, builder);

            return SendResponseAsync(apiContext, builder.ToString(), APIConstants.CONTENT_TYPE_APPLICATION_JSON, cancellationToken);
        }
#endif

        private static Task HandleCancelFileUploadAsync(APIContext apiContext, CancellationToken cancellationToken = default)

        {
            string uploadId = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_UPLOAD_ID, true, true, null, out uploadId))
                return Task.CompletedTask;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            WanPath.WebClient.BLL.API.FileWrite6 file = new WanPath.WebClient.BLL.API.FileWrite6(client.SessionIdString, client.Username);
            if (file != null && file.WriteCancel(uploadId))
            {
                apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
                return SendResponseAsync(apiContext, (string)null, (string)null, cancellationToken);
            }
            else
            {
                apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, APITextMessages.ERROR_BACKEND_OPERATION_FAILED);
                return Task.CompletedTask;
            }
#else
            // Mock server implementation - mark session as cancelled
            lock (_uploadSessions)
            {
                if (_uploadSessions.TryGetValue(uploadId, out var session))
                {
                    session.IsCancelled = true;
                }
            }
            
            apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
            return SendResponseAsync(apiContext, (string)null, (string)null, cancellationToken);
#endif
        }

        private static Task HandleCompleteFileUploadAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string? uploadId = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_UPLOAD_ID, true, true, null, out uploadId))
                return Task.CompletedTask;

            string? checksum = string.Empty; // TODO: check param name
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_CHECKSUM, false, false, string.Empty, out checksum))
                return Task.CompletedTask;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            WanPath.WebClient.BLL.API.FileWrite6 file = new WanPath.WebClient.BLL.API.FileWrite6(client.SessionIdString, client.Username);
            string savedTo = file.WriteEndEx(uploadId, checksum);
            if (!string.IsNullOrEmpty(savedTo))
            {
                var fsInfo = file.GetFileInfo(new ItemPath(savedTo, false));

                apiContext.HttpStatusCode = StatusCodes.Status200OK;
                apiContext.ResponseObject = FileSystemInfoToDictionary(fsInfo, false, true);
            }
#else
            // Mock server implementation
            UploadSession session;
            lock (_uploadSessions)
            {
                if (!_uploadSessions.TryGetValue(uploadId, out session))
                {
                    apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, "Invalid upload ID");
                    return Task.CompletedTask;
                }
                
                if (session.IsCancelled)
                {
                    apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, "Upload session was cancelled");
                    return Task.CompletedTask;
                }
                
                // Remove the session after completion
                _uploadSessions.Remove(uploadId);
            }
            
            var fullPath = GetFullPath(session.FilePath);
            var fileInfo = new FileInfo(fullPath);
            
            var responseInfo = new Dictionary<string, object>
            {
                ["name"] = Path.GetFileName(session.FilePath),
                ["path"] = session.FilePath,
                ["isFolder"] = false,
                ["size"] = fileInfo.Exists ? fileInfo.Length : 0L,
                ["modified"] = DateTime.UtcNow.ToString("o")
            };
            
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = responseInfo;
#endif
            return Task.CompletedTask;
        }

        private static Task HandleCopyFileAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            return HandleCopyFileCommonAsync(apiContext, false, cancellationToken);
        }

        private static Task HandleCopyFolderAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            return HandleCopyFileCommonAsync(apiContext, true, cancellationToken);
        }

        private static async Task HandleCopyFileCommonAsync(APIContext apiContext, bool isFolder, CancellationToken cancellationToken)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            string newPath = null;
            PickParameter(apiContext, APIConstants.REQUEST_PARAM_NEW_PATH, false, false, string.Empty, out newPath);

            ConflictBehavior conflictBehavior = ConflictBehavior.Undefined;
            if (!PickConflictBehaviorParameter(apiContext, false, ConflictBehavior.Undefined, out conflictBehavior) || conflictBehavior == ConflictBehavior.Undefined)
                return;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return;

            newPath = StandardizePath(newPath);

            WanPath.WebClient.BLL.MwdService.InputOutput io = new InputOutput(client);

            string availableNewPath;

            ItemPath newItemPath = null;

            // Choose the right destination name
            if (!SelectAvailableName(apiContext, io, newPath, isFolder, conflictBehavior, isFolder ? "Copy Folder" : "Copy File", out availableNewPath, out newItemPath))
                return;

            // Perform the operation
            if (isFolder)
                io.CopyFolder(new ItemPath(path, false), newItemPath);
            else
                io.CopyFile(new ItemPath(path, false), newItemPath);

            // Get the information about the new file
            var fsInfo = await InternalGetFileInformationAsync(apiContext, io, availableNewPath, new ItemPath(availableNewPath, false), isFolder, cancellationToken);

            // if the information was not found, we return the appropriate error
            if (fsInfo == null)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_AFTER_OPERATION_S2, isFolder ? APIEndpoints.OPERATION_COPY_FOLDER : APIEndpoints.OPERATION_COPY_FILE, newPath));
                return;
            }

#else
            path = StandardizePath(path);
            newPath = StandardizePath(newPath);

            string availableNewPath;
            FileSystemInfo fsInfo;

            // Special handling for same-path operations in copy
            bool isSamePath = path.Equals(newPath, StringComparison.OrdinalIgnoreCase);
            bool isSamePathRename = isSamePath && conflictBehavior == ConflictBehavior.Rename;
            bool isSamePathReplace = isSamePath && conflictBehavior == ConflictBehavior.Replace;

            if (isSamePathRename)
            {
                // For same-path rename, generate a new name
                if (!SelectAvailableNameForDuplicate(apiContext, newPath, isFolder, out availableNewPath))
                    return;
            }
            else if (isSamePathReplace)
            {
                // For same-path replace, it's essentially a no-op - the file "replaces" itself
                availableNewPath = newPath;
            }
            else
            {
                // Choose the right destination name for different paths
                if (!SelectAvailableName(apiContext, newPath, isFolder, conflictBehavior, isFolder ? "Copy Folder" : "Copy File", out availableNewPath))
                    return;
            }

            // Convert logical API path to physical file system path
            var fullPath = GetFullPath(path);
            if (isFolder)
            {
                fsInfo = new DirectoryInfo(fullPath);
            }
            else
            {
                fsInfo = new FileInfo(fullPath);
            }

            if (!fsInfo.Exists)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                return;
            }

            // Convert destination path to physical path as well
            var fullNewPath = GetFullPath(availableNewPath);

            // Perform the copy operation
            try
            {
                if (isSamePathReplace)
                {
                    // For same-path replace, don't actually copy - just verify the file exists
                    // This is essentially a no-op since we're "copying" the file to itself
                }
                else if (isSamePathRename)
                {
                    // For same-path rename, perform normal copy to the new renamed path
                    if (isFolder)
                    {
                        CopyDirectory(fullPath, fullNewPath, true);
                    }
                    else
                    {
                        File.Copy(fullPath, fullNewPath, false);
                    }
                }
                else
                {
                    // Normal copy operation for different paths
                    if (isFolder)
                    {
                        CopyDirectory(fullPath, fullNewPath, true);
                    }
                    else
                    {
                        File.Copy(fullPath, fullNewPath, conflictBehavior == ConflictBehavior.Replace);
                    }
                }
            }
            catch (IOException ex)
            {
                apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_FILE_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Copy", availableNewPath) + " (" + ex.Message + ")");
                return;
            }

            fsInfo = new FileInfo(fullNewPath);
#endif

            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = FileSystemInfoToDictionary(fsInfo, false, true);
        }

#if MOCK_SERVER
        public static void CopyDirectory(string sourceDir, string destDir, bool overwrite = true)
        {
            var src = new DirectoryInfo(sourceDir);
            if (!src.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            // ensure target exists
            Directory.CreateDirectory(destDir);

            // copy files in this directory
            foreach (var file in src.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, overwrite);
            }

            // recurse into subdirectories
            foreach (var subdir in src.GetDirectories())
            {
                string newDestSubdir = Path.Combine(destDir, subdir.Name);
                CopyDirectory(subdir.FullName, newDestSubdir, overwrite);
            }
        }
#endif

        private static async Task HandleCreateFileAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if !MOCK_SERVER
            WanPath.BusinessManager.FileSystemInfoEx fsInfo = null;
#else
            FileSystemInfo fsInfo = null;
#endif

            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;
                

            string filename = null;
            PickParameter(apiContext, APIConstants.REQUEST_PARAM_NAME, false, false, string.Empty, out filename);

            string extension = null;
            PickParameter(apiContext, APIConstants.REQUEST_PARAM_EXTENSION, false, false, string.Empty, out extension);

            bool createFile = false;

            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_CREATE_FILE, false, false, out createFile))
                return;

            ConflictBehavior conflictBehavior = ConflictBehavior.Undefined;
            if (!PickConflictBehaviorParameter(apiContext, false, ConflictBehavior.Undefined, out conflictBehavior) || conflictBehavior == ConflictBehavior.Undefined)
                return;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return;

            path = StandardizePath(path);

            WanPath.WebClient.BLL.MwdService.InputOutput io = new InputOutput(client);

            if (createFile)
            {
                // Create file
                //
                bool createContent = false;
                if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_CREATE_CONTENT, false, false, out createContent))
                    return;

                if (string.IsNullOrEmpty(filename) && string.IsNullOrEmpty(extension))
                    filename = APIConstants.NAME_NEW_FILE;

                fsInfo = await InternalCreateFileAsync(apiContext, client, io, path, filename, extension, createContent, conflictBehavior, cancellationToken);
            }
            else
            {
                // Create directory
                fsInfo = await InternalCreateDirectoryAsync(apiContext, client, io, path, filename, extension, conflictBehavior, cancellationToken);
            }

#else
            path = StandardizePath(path);

            if (createFile)
            {
                // Create file
                //
                bool createContent = false;
                if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_CREATE_CONTENT, false, false, out createContent))
                    return;

                if (string.IsNullOrEmpty(filename) && string.IsNullOrEmpty(extension))
                    filename = APIConstants.NAME_NEW_FILE;

                fsInfo = await InternalCreateFileAsync(apiContext, path, filename, extension, createContent, conflictBehavior, cancellationToken);
            }
            else
            {
                // Create directory
                fsInfo = await InternalCreateDirectoryAsync(apiContext, path, filename, extension, conflictBehavior, cancellationToken);
            }
#endif

            // if information was not found, we return the appropriate error
            if (fsInfo == null || !fsInfo.Exists)
            {
                apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "CreateFile", filename));
                return;
            }

            // InternalCreate*Async may have set an error code in the case of an error
            if (apiContext.APIErrorCode == 0)
            {
                apiContext.HttpStatusCode = StatusCodes.Status200OK;
                apiContext.ResponseObject = FileSystemInfoToDictionary(fsInfo, false, true);
            }
        }

        private static async Task HandleListFileVersionsAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if !MOCK_SERVER
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            List<FileVersion> versionList = await Api1FileController.ListVersionsAsync(path, cancellationToken);

            List<Dictionary<string, object>> responseList = new List<Dictionary<string, object>>();
            Dictionary<string, object> singleResponse;
            foreach (var version in versionList)
            {
                singleResponse = new Dictionary<string, object>();
                singleResponse[APIConstants.RESPONSE_VALUE_NAME_NAME] = version.Name;
                singleResponse[APIConstants.RESPONSE_VALUE_NAME_MODIFIED] = version.Modified;
                singleResponse[APIConstants.RESPONSE_VALUE_NAME_SIZE] = (long)version.Size;
                singleResponse[APIConstants.RESPONSE_VALUE_NAME_SERVER_NAME] = version.ServerName;
                singleResponse[APIConstants.RESPONSE_VALUE_NAME_SHADOW_PATH] = version.ShadowCopyPath;
                responseList.Add(singleResponse);
            }
            apiContext.ResponseObject = responseList;
#else
            // Mock server implementation - return mock version history
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;
                
            var mockVersions = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["name"] = Path.GetFileName(path),
                    ["modified"] = DateTime.UtcNow.AddDays(-1).ToString("o"),
                    ["size"] = 1024L,
                    ["serverName"] = "mock-server",
                    ["shadowPath"] = path + ".v1"
                },
                new Dictionary<string, object>
                {
                    ["name"] = Path.GetFileName(path),
                    ["modified"] = DateTime.UtcNow.AddDays(-7).ToString("o"),
                    ["size"] = 512L,
                    ["serverName"] = "mock-server",
                    ["shadowPath"] = path + ".v2"
                }
            };
            
            apiContext.ResponseObject = mockVersions;
#endif

        }

        private static Task HandleListFolderAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            bool includeLocks = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_INCLUDE_LOCKS, false, false, out includeLocks))
                return Task.CompletedTask;

            bool skipHidden = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_SKIP_HIDDEN, false, false, out skipHidden))
                return Task.CompletedTask;

            string requestETag = apiContext.Request.Headers[APIConstants.HTTP_HEADER_ETAG];

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

            path = StandardizePath(path);

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            WanPath.Common.Enums.ClientType clientType = GetClientType(apiContext);

            WanPath.WebClient.BLL.MwdService.InputOutput io = new InputOutput(client);

            ItemPath itemPath = new ItemPath(path, false);

            List<WanPath.BusinessManager.FileSystemInfoEx> children = io.GetChildren(itemPath, true, false, skipHidden, clientType, true, includeLocks, false);

            Dictionary<string, string> headers = null;
            if (!string.IsNullOrEmpty(requestETag))
            {
                WanPath.WebClient.BLL.GetChildren.ETag eTag = new WanPath.WebClient.BLL.GetChildren.ETag(path, requestETag);
                string responseETag;
                bool modified = eTag.CreateResponse(children, out responseETag);
                headers = new Dictionary<string, string>();
                headers.Add(APIConstants.HTTP_HEADER_ETAG, responseETag);

                if (!modified) // this method returns true if the requestETag and new list's ETag don't match and false otherwise. Thus, a false result means that we send a "not modified" response
                {
                    apiContext.HttpStatusCode = StatusCodes.Status304NotModified;
                    return SendResponseAsync(apiContext, headers, null, null, cancellationToken);

                }
            }
            DavContext davContext = new DavContext(apiContext.Context);
            davContext.WebAPISessionId = client.SessionIdString;

            if ((apiContext.Request != null) && (apiContext.Request.Url != null))
            {
                (new DLPResolver(
                    string.Format("{0}://{1}", apiContext.Request.Url.Scheme, apiContext.Request.Url.Host),
                    apiContext.SessionGUID,
                    clientType))
                    .ProcessDLP(children, path);
            }

#else
            // Handle root directory request - use base path like other operations
            if (path == "/" || string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            
            var fullPath = GetFullPath(path);
            var dirInfo = new DirectoryInfo(fullPath);
            if (!dirInfo.Exists)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                return SendResponseAsync(apiContext, new Dictionary<string, string>(), null, null, cancellationToken);
            }

            FileSystemInfo[] children = dirInfo.GetFileSystemInfos();
#endif

            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            foreach (var child in children)
            {
                list.Add(FileSystemInfoToDictionary(child, includeLocks, false));
            }

#if MOCK_SERVER
            string json = JsonSerializer.Serialize(list);

            byte[] data = Encoding.UTF8.GetBytes(json);
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(data);
            string responseETag = "\"" + Convert.ToBase64String(hash) + "\"";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(APIConstants.HTTP_HEADER_ETAG, responseETag);

            return SendResponseAsync(apiContext, headers, json, null, cancellationToken);
#else
            apiContext.ResponseObject = list;
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            return SendResponseAsync(apiContext, headers, (string)null, null, cancellationToken);
#endif
        }

        private static Task HandleLockFileAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            // Pick Path parameter
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            // Pick Owner parameter
            string owner = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_OWNER, false, false, null, out owner))
                return Task.CompletedTask;
            if (string.IsNullOrEmpty(owner))
                owner = APIConstants.ID_CLIENT_API;

            // Pick Expires parameter
            long? expiresAfter = 0;
            TimeSpan expiresAfterSpan;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_EXPIRES, false, 0, out expiresAfter))
                return Task.CompletedTask;
            if (expiresAfter < 0) // the value specified explicitly as an invalid negative value
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, expiresAfter.ToString(), APIConstants.REQUEST_PARAM_EXPIRES));
                return Task.CompletedTask;
            }
            else
            if (expiresAfter == 0)
#if !MOCK_SERVER
                expiresAfterSpan = WanPath.Common.Constants.Mwd.LockExpiration;
#else
                expiresAfterSpan = TimeSpan.FromSeconds((long)6000);
#endif
            else
                expiresAfterSpan = TimeSpan.FromSeconds((long)expiresAfter);

            // Pick CoEdit parameter
            bool coEdit = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_COEDIT, false, false, out coEdit))
                return Task.CompletedTask;

            // Pick IncludeLockInfo parameter
            bool includeLockInfo = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_INCLUDE_LOCK_INFO, false, false, out includeLockInfo))
                return Task.CompletedTask;

            bool includeLockOwners = false;
            bool includeLockDetails = false;
            if (includeLockInfo)
            {
                // Pick IncludeLockOwners parameter
                if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_INCLUDE_LOCK_OWNERS, false, false, out includeLockOwners))
                    return Task.CompletedTask;

                // Pick IncludeLockDetails parameter
                if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_INCLUDE_LOCK_DETAILS, false, false, out includeLockDetails))
                    return Task.CompletedTask;
            }

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            path = StandardizePath(path);

            WanPath.WebClient.BLL.MwdService.InputOutput io = new InputOutput(client);

            object mutex = Api1FileController.pathMutexMap.GetOrAdd(WanPath.WebClient.BLL.MwdPath.GetPathWithoutDavDav2(path).ToLower(), key => new object());
            lock (mutex)
            {
                // Perform the operation
                WanPath.WebClient.BLL.Locks locks = new WanPath.WebClient.BLL.Locks();
                var r = locks.LockFile(client, apiContext.SessionID, path, false, expiresAfterSpan, owner, coEdit);

                // Compose the response
                if (r.Exception == null)
                {
                    if (includeLockInfo)
                    {
                        List<WanPath.Common.MwdLock> lockList = locks.GetLocks2(client, path);
                        // _log.Debug($"ClientAPIHandler.HandleLockFileAsync, path: {path}, LockCount: {info.LocksCount}, LockedByMySessionId: {info.LockedByMySessionId}, LockedByMySessionIdForCoEdit: {info.LockedByMySessionIdForCoEdit}, LockedByOtherSessionId: {info.LockedByOtherSessionId}, OpenedDirectly: {info.OpenedDirectly}");
                        apiContext.ResponseObject = LockListToDictionary(apiContext, lockList, includeLockOwners, includeLockDetails);
                    }
                    else
                    {
                        apiContext.HttpStatusCode = StatusCodes.Status201Created;
                        return SendResponseAsync(apiContext, (string)null, (string)null, cancellationToken);
                    }
                }
                else
                {
                    apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(r.Exception, ""));
                }
            }

            return Task.CompletedTask;
#else
            // Mock server implementation
            if (includeLockInfo)
            {
                // Return mock lock information
                var lockInfo = new Dictionary<string, object>
                {
                    ["lockCount"] = 1,
                    ["lockedByMe"] = true,
                    ["lockType"] = coEdit ? "coedit" : "exclusive",
                    ["expires"] = DateTime.UtcNow.Add(expiresAfterSpan).ToString("o"),
                    ["owner"] = owner
                };
                apiContext.ResponseObject = lockInfo;
            }
            else
            {
                apiContext.HttpStatusCode = StatusCodes.Status201Created;
                return SendResponseAsync(apiContext, (string)null, (string)null, cancellationToken);
            }
            
            return Task.CompletedTask;
#endif
        }

        private static Task HandleMoveFileAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            return HandleMoveFileCommonAsync(apiContext, false, cancellationToken);
        }
        private static Task HandleMoveFolderAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            return HandleMoveFileCommonAsync(apiContext, true, cancellationToken);
        }
        private static async Task HandleMoveFileCommonAsync(APIContext apiContext, bool isFolder, CancellationToken cancellationToken)
        {
            // Pick Path parameter
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;
            string newPath = null;
            PickParameter(apiContext, APIConstants.REQUEST_PARAM_NEW_PATH, false, false, string.Empty, out newPath);

            // Pick ConflictBehavior parameter
            ConflictBehavior conflictBehavior = ConflictBehavior.Undefined;
            if (!PickConflictBehaviorParameter(apiContext, false, ConflictBehavior.Undefined, out conflictBehavior) || conflictBehavior == ConflictBehavior.Undefined)
                return;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

            path = StandardizePath(path);
            newPath = StandardizePath(newPath);

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return;

            WanPath.WebClient.BLL.MwdService.InputOutput io = new InputOutput(client);

            string availableNewPath;

            ItemPath newItemPath = null;

            // Choose the right destination name
            if (!SelectAvailableName(apiContext, io, newPath, isFolder, conflictBehavior, isFolder ? "Copy Folder" : "Copy File", out availableNewPath, out newItemPath))
                return;

            // Perform the operation
            if (isFolder)
                io.MoveFolder(new ItemPath(path, false), newItemPath);
            else
                io.MoveFile(new ItemPath(path, false), newItemPath, false);

            // Get the information about the new file
            var fsInfo = await InternalGetFileInformationAsync(apiContext, io, availableNewPath, newItemPath, isFolder, cancellationToken);
#else
            string availableNewPath;

            // Special handling for same-path operations
            bool isSamePath = path.Equals(newPath, StringComparison.OrdinalIgnoreCase);
            bool isSamePathRename = isSamePath && conflictBehavior == ConflictBehavior.Rename;
            bool isSamePathReplace = isSamePath && conflictBehavior == ConflictBehavior.Replace;

            if (isSamePathRename)
            {
                // For same-path rename, we need to generate a new name by forcing the rename logic
                if (!SelectAvailableNameForDuplicate(apiContext, newPath, isFolder, out availableNewPath))
                    return;
            }
            else if (isSamePathReplace)
            {
                // For same-path replace, keep the same path (no-op but return success)
                availableNewPath = newPath;
            }
            else
            {
                // Choose the right destination name for different paths
                if (!SelectAvailableName(apiContext, newPath, isFolder, conflictBehavior, isFolder ? "Move Folder" : "Move File", out availableNewPath))
                    return;
            }

            // Convert logical API paths to physical file system paths
            var fullPath = GetFullPath(path);
            var fullNewPath = GetFullPath(availableNewPath);
            FileSystemInfo fsInfo = (isFolder) ? new DirectoryInfo(fullPath) : new FileInfo(fullPath);

            if (!fsInfo.Exists)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                return;
            }

            // Perform the operation
            try
            {
                if (isSamePathRename)
                {
                    // For same-path rename, move the file (delete original after creating renamed copy)
                    if (isFolder)
                    {
                        Directory.Move(fullPath, fullNewPath);
                    }
                    else
                    {
                        File.Move(fullPath, fullNewPath, false);
                    }
                }
                else if (isSamePathReplace)
                {
                    // For same-path replace, do nothing - file stays as-is (no-op)
                    // The file already exists and we want to "replace" it with itself
                }
                else
                {
                    // Normal move operation for different paths
                    if (isFolder)
                        Directory.Move(fullPath, fullNewPath);
                    else
                        File.Move(fullPath, fullNewPath, false);
                }
            }
            catch (IOException ex)
            {
                // This should not happen if SelectAvailableName worked correctly
                apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_FILE_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Move", availableNewPath) + " (" + ex.Message + ")");
                return;
            }

            // Get the information about the new file
            fsInfo = await InternalGetFileInformationAsync(apiContext, availableNewPath, isFolder, cancellationToken);
#endif

            // if the information was not found, we return the appropriate error
            if (fsInfo == null || !fsInfo.Exists)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, availableNewPath));
                return;
            }

            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = FileSystemInfoToDictionary(fsInfo, false, true);
        }

        private static async Task HandleRestoreFileVersionAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if !MOCK_SERVER
            string modifiedStr = "";
            string path = string.Empty;

            DateTime modified;

            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_MODIFIED, true, true, null, out modifiedStr))
                return;
            if (modifiedStr == APIConstants.REQUEST_PARAM_VALUE_LATEST)
                modified = DateTime.MaxValue;
            else
                modified = StrUtils.ParseDateISO8601(modifiedStr);

            if (modified == DateTime.MinValue)
            {
                try
                {
                    modified = DateTime.Parse(modifiedStr);
                }
                catch (FormatException)
                {
                    modified = DateTime.MinValue;
                }
            }

            if (modified == DateTime.MinValue)
            {
                _log.Debug(string.Format("RestoreFileVersion could not parse {0} as valid DateTime value", modifiedStr));
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, modifiedStr, APIConstants.REQUEST_PARAM_MODIFIED));
                return;
            }

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

            path = StandardizePath(path);

            FileVersion version = await Api1FileController.FindVersionAsync(path, modified, cancellationToken);

            if (version == null)
            {
                apiContext.SetError(APIErrorCodes.FILE_VERSION_NOT_FOUND, string.Format(APITextMessages.ERROR_VERSION_NOT_FOUND_S2, path, modifiedStr));
                return;
            }

            await Api1FileController.RestoreFileInternalAsync(path, modified, version.ServerName, version.ShadowCopyPath, apiContext.SessionGUID, cancellationToken);
#else
            // Mock server implementation - restore is a no-op, but return file info
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            path = StandardizePath(path);
            
            FileSystemInfo fsInfo = File.Exists(path) ? new FileInfo(path) : (FileSystemInfo)new DirectoryInfo(path);
            if (!fsInfo.Exists)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                return;
            }
            
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = FileSystemInfoToDictionary(fsInfo, false, true);
            return;
#endif
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
        }


        private static Task HandleGetFileCapabilitiesAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string query = "";
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_QUERY, true, true, null, out query))
                return Task.CompletedTask;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

            path = StandardizePath(path);

            if (APIConstants.REQUEST_PARAM_VALUE_WRITE_PERMISSIONS.Equals(query, System.StringComparison.OrdinalIgnoreCase))
            {
                // the response will be placed to apiContext by the lower-level method and sent to the client by the upper-level method.
                return InternalHasWritePermissionsAsync(apiContext, path, cancellationToken);
            }
            else
            if (APIConstants.REQUEST_PARAM_VALUE_FOLDER_ACCESSIBLE.Equals(query, System.StringComparison.OrdinalIgnoreCase))
            {
                // the response will be placed to apiContext by the lower-level method and sent to the client by the upper-level method.
                return InternalIsFolderAccessibleAsync(apiContext, path, cancellationToken);
            }
            else
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, query ?? string.Empty, APIConstants.REQUEST_PARAM_QUERY));
                // the response with the error code and message will be sent by the upper-level method.
            }
            return Task.CompletedTask;
        }

        private static Task HandleQueryQuotasAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;
#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            WanPath.Common.Models.DriveInfo diskSize;
            RootPathStringManager manager = new RootPathStringManager(path);
            string localPath = GetPath(manager);
            if (!string.IsNullOrEmpty(localPath))
            {
                // Query the quotas
                WanPath.WebClient.BLL.MwdService.InputOutput io = new InputOutput(client);
                string rootPath = GetRootPath(manager, io);
                diskSize = io.GetDiskSize(localPath, rootPath);

                // Build a response
                Dictionary<string, object> dict = new Dictionary<string, object>
                {
                    { ClientAPIConstants.RESPONSE_VALUE_NAME_TOTAL_BYTES, diskSize.TotalNumberOfBytes },
                    { ClientAPIConstants.RESPONSE_VALUE_NAME_FREE_BYTES, diskSize.TotalNumberOfFreeBytes },
                    { ClientAPIConstants.RESPONSE_VALUE_NAME_AVAILABLE_BYTES, diskSize.FreeBytesAvailable },
                };
                apiContext.ResponseObject = dict;
                apiContext.HttpStatusCode = StatusCodes.Status200OK;
            }
            else
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, localPath));
            }
#else
            // Mock server implementation - return fake quota data
            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                { ClientAPIConstants.RESPONSE_VALUE_NAME_TOTAL_BYTES, 1073741824000L }, // 1TB
                { ClientAPIConstants.RESPONSE_VALUE_NAME_FREE_BYTES, 536870912000L },   // 500GB
                { ClientAPIConstants.RESPONSE_VALUE_NAME_AVAILABLE_BYTES, 536870912000L }, // 500GB
            };
            apiContext.ResponseObject = dict;
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
#endif
            return Task.CompletedTask;
        }
        private static async Task HandleReadFileAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            long? startPos = 0;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_START_POSITION, true, 0, out startPos))
                return;

            long? count = 0;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_COUNT, true, 0, out count))
                return;

            bool? lockFile = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_LOCK_FILE, false, false, out lockFile))
                return;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return;

            path = StandardizePath(path);

            byte[] buffer = await Api1FileController.ReadFileStandardInternalAsync(apiContext.Context, client, apiContext.SessionID, path, startPos, (int)count, lockFile, cancellationToken);
#else
            byte[] buffer;
            path = StandardizePath(path);
            
            // Convert logical API path to physical file system path
            var fullPath = GetFullPath(path);

            try
            {
                using (var stream = File.OpenRead(fullPath))
                {
                    stream.Position = (long)startPos;

                    buffer = new byte[(int)count];
                    int read = await stream.ReadAsync(buffer, 0, (int)count);
                    if (read < count)
                        Array.Resize(ref buffer, read);
                }
            }
            catch
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                return;
            }
#endif

            apiContext.HttpStatusCode = StatusCodes.Status200OK;

            await SendResponseAsync(apiContext, buffer, APIConstants.CONTENT_TYPE_APPLICATION_OCTET_STREAM, cancellationToken);

        }
        private static async Task HandleReleaseLockForOfficeAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

            await InternalReleaseLockForOfficeAsync(apiContext, path, cancellationToken);
            if (apiContext.APIErrorCode == 0)
            {
                apiContext.Response.StatusCode = StatusCodes.Status200OK;
            }
            await SendResponseAsync(apiContext, (byte[])null, "", cancellationToken);
        }

        private static Task HandleCheckEditSessionStatusAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

            return InternalCheckEditSessionStatusAsync(apiContext, path, cancellationToken);
        }

        private static Task HandleDeleteFileAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            return HandleDeleteFileCommonAsync(apiContext, false, cancellationToken);
        }

        private static Task HandleDeleteFolderAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            // Get actionWhenNotEmpty parameter (default: "fail")
            string actionWhenNotEmpty = "fail";
            PickParameter(apiContext, "actionWhenNotEmpty", false, false, string.Empty, out actionWhenNotEmpty);

            // If actionWhenNotEmpty is empty or null, use default
            if (string.IsNullOrEmpty(actionWhenNotEmpty))
                actionWhenNotEmpty = "fail";

            // Validate actionWhenNotEmpty parameter
            if (!string.IsNullOrEmpty(actionWhenNotEmpty) &&
                actionWhenNotEmpty != "fail" && actionWhenNotEmpty != "stopOnError" && actionWhenNotEmpty != "ignoreErrors")
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, "Invalid actionWhenNotEmpty value");
                return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
            }

            // Get listFailedItems parameter (default: false)
            bool listFailedItems = false;
            PickParameter(apiContext, "listFailedItems", false, false, out listFailedItems);

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext))
                return Task.CompletedTask;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            path = StandardizePath(path);

            WanPath.WebClient.BLL.MwdService.InputOutput io = new InputOutput(client);
            ItemPath itemPath = new ItemPath(path, false);

            io.DeleteFolder(itemPath);
#else
            path = StandardizePath(path);

            // Convert logical API path to physical file system path
            var fullPath = GetFullPath(path);
            var directoryInfo = new DirectoryInfo(fullPath);

            if (!directoryInfo.Exists)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
            }

            // Check if folder has contents
            var contents = directoryInfo.GetFileSystemInfos();
            bool hasContents = contents.Length > 0;

            if (hasContents)
            {
                switch (actionWhenNotEmpty)
                {
                    case "fail":
                        apiContext.HttpStatusCode = 417; // 417 Directory Not Empty
                        return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);

                    case "stopOnError":
                        try
                        {
                            var failedItems = DeleteFolderRecursiveWithTracking(directoryInfo, true);
                            if (failedItems.Count > 0)
                            {
                                var response = new StatusResponseDeleteFolder
                                {
                                    status = "warning",
                                    errorCode = 417,
                                    message = "Some items could not be deleted",
                                    details = listFailedItems ? failedItems : null
                                };
                                apiContext.HttpStatusCode = 200;
                                apiContext.ResponseObject = response;
                                return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
                            }
                        }
                        catch
                        {
                            var response = new StatusResponseDeleteFolder
                            {
                                status = "failed",
                                errorCode = 500,
                                message = "Deletion failed"
                            };
                            apiContext.HttpStatusCode = 200;
                            apiContext.ResponseObject = response;
                            return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
                        }
                        break;

                    case "ignoreErrors":
                        var failedList = DeleteFolderRecursiveWithTracking(directoryInfo, false);
                        if (failedList.Count > 0)
                        {
                            var response = new StatusResponseDeleteFolder
                            {
                                status = "warning",
                                errorCode = 417,
                                message = "Some items could not be deleted",
                                details = listFailedItems ? failedList : null
                            };
                            apiContext.HttpStatusCode = 200;
                            apiContext.ResponseObject = response;
                            return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
                        }
                        break;
                }
            }
            else
            {
                // Empty folder - delete normally
                directoryInfo.Delete();
            }
#endif

            apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
            return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
        }

#if MOCK_SERVER
        private static List<string> DeleteFolderRecursiveWithTracking(DirectoryInfo directory, bool stopOnError)
        {
            var failedItems = new List<string>();
            var basePath = GetFullPath("");

            try
            {
                // First, try to delete all files in the directory
                foreach (var file in directory.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        var relativePath = ConvertPhysicalPathToLogical(file.FullName);
                        failedItems.Add(relativePath);
                        if (stopOnError) return failedItems;
                    }
                }

                // Then recursively delete subdirectories
                foreach (var subDirectory in directory.GetDirectories())
                {
                    var subDirFailures = DeleteFolderRecursiveWithTracking(subDirectory, stopOnError);
                    failedItems.AddRange(subDirFailures);
                    if (stopOnError && subDirFailures.Count > 0) return failedItems;

                    // Try to delete the subdirectory itself if it's now empty
                    try
                    {
                        if (subDirectory.GetFileSystemInfos().Length == 0)
                        {
                            subDirectory.Delete();
                        }
                    }
                    catch
                    {
                        var relativePath = ConvertPhysicalPathToLogical(subDirectory.FullName);
                        failedItems.Add(relativePath);
                        if (stopOnError) return failedItems;
                    }
                }

                // Finally, try to delete the directory itself
                try
                {
                    if (directory.GetFileSystemInfos().Length == 0)
                    {
                        directory.Delete();
                    }
                }
                catch
                {
                    var relativePath = ConvertPhysicalPathToLogical(directory.FullName);
                    failedItems.Add(relativePath);
                }
            }
            catch
            {
                var relativePath = ConvertPhysicalPathToLogical(directory.FullName);
                failedItems.Add(relativePath);
            }

            return failedItems;
        }
#endif

        private static Task HandleDeleteFileCommonAsync(APIContext apiContext, bool isFolder, CancellationToken cancellationToken)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            bool recursive = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_RECURSIVE, false, false, out recursive))
                return Task.CompletedTask;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            path = StandardizePath(path);

            WanPath.WebClient.BLL.MwdService.InputOutput io = new InputOutput(client);

            ItemPath itemPath = new ItemPath(path, false);

            if (isFolder)
                io.DeleteFolder(itemPath);
            else
                io.DeleteFile(itemPath);
#else
            path = StandardizePath(path);
            
            // Convert logical API path to physical file system path
            var fullPath = GetFullPath(path);
            FileSystemInfo fsInfo = (isFolder) ? new DirectoryInfo(fullPath) : new FileInfo(fullPath);

            if (!fsInfo.Exists)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
            }

            if (isFolder)
            {
                // Check if folder is empty when not recursive
                if (!recursive && new DirectoryInfo(fullPath).GetFileSystemInfos().Length > 0)
                {
                    apiContext.HttpStatusCode = 417; // 417 Expectation Failed
                    return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
                }
                Directory.Delete(fullPath, recursive);
            }
            else
                File.Delete(fullPath);
#endif

            apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
            return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
        }

        private static async Task HandleEditSessionCompleteAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

            await InternalEditIsFinishedAsync(apiContext, path, cancellationToken);
            if (apiContext.APIErrorCode == 0)
            {
                apiContext.Response.StatusCode = StatusCodes.Status200OK;
            }
            await SendResponseAsync(apiContext, (byte[])null, "", cancellationToken);
        }

        private static Task HandleGetFileUploadStatusAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string uploadId = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_UPLOAD_ID, true, true, null, out uploadId))
                return Task.CompletedTask;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            WanPath.WebClient.BLL.API.FileWrite6 file = new WanPath.WebClient.BLL.API.FileWrite6(client.SessionIdString, client.Username);

            string status;
            string error;
            long? progress;

            if (file.WriteStatusEx(apiContext.SessionID, uploadId, out status, out error, out progress))
            {
                apiContext.HttpStatusCode = StatusCodes.Status200OK;
                Dictionary<string, object> dict = new Dictionary<string, object>
                {
                    { APIConstants.RESPONSE_VALUE_NAME_STATUS, status },
                };
                if (!string.IsNullOrEmpty(error))
                {
                    dict.Add(APIConstants.RESPONSE_VALUE_NAME_ERROR, error);
                }
                if (progress != null)
                {
                    dict.Add(APIConstants.RESPONSE_VALUE_NAME_PROGRESS, progress.Value);
                }
                apiContext.ResponseObject = dict;
            }
            else
                apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, APITextMessages.ERROR_BACKEND_OPERATION_FAILED);
#else
            // Mock server implementation
            UploadSession session;
            lock (_uploadSessions)
            {
                if (!_uploadSessions.TryGetValue(uploadId, out session))
                {
                    apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, "Invalid upload ID");
                    return Task.CompletedTask;
                }
            }
            
            string status = session.IsCancelled ? "Cancelled" : 
                           (session.BytesUploaded >= session.TotalSize) ? "Complete" : "InProgress";
            
            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                { "status", status },
                { "progress", session.BytesUploaded },
                { "totalSize", session.TotalSize }
            };
            
            if (session.IsCancelled)
            {
                dict.Add("error", "Upload was cancelled");
            }
            
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = dict;
#endif

            return Task.CompletedTask;
        }

        private static Task HandleGetFileOTPAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

            string otp = InternalGenerateOneTimePassword(apiContext);
            if (otp == null)
            {
                if (apiContext.APIErrorCode == 0)
                {
                    apiContext.SetError(APIErrorCodes.INTERNAL_ERROR, "An error occured when adding a one-time password.");
                }
                return Task.CompletedTask;
            }

            // This dictionary will be converted to JSON and sent to the client in the APIHandler.ProcessRequestAsync method
            Dictionary<string, object> response = new Dictionary<string, object>
            {
                { APIConstants.RESPONSE_VALUE_NAME_OTP, otp },
            };
            apiContext.ResponseObject = response;
            apiContext.HttpStatusCode = StatusCodes.Status200OK;

            return Task.CompletedTask;
        }

        private static Task HandleGetFileInformationAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            return HandleGetFileInformationCommonAsync(apiContext, false, cancellationToken);
        }

        private static Task HandleGetFolderInformationAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            return HandleGetFileInformationCommonAsync(apiContext, true, cancellationToken);
        }

        private static async Task HandleGetFileInformationCommonAsync(APIContext apiContext, bool isFolder, CancellationToken cancellationToken)
        {
            // Pick and validate the path
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            bool includeLocks = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_INCLUDE_LOCKS, false, false, out includeLocks))
                return;

            /*            bool includeExtended = false;
                        if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_INCLUDE_EXTENDED, false, false, out includeExtended))
                            return;
            */
            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

            path = StandardizePath(path);

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return;

            InputOutput io = new InputOutput(client);

            WanPath.BusinessManager.FileSystemInfoEx fsInfo = await InternalGetFileInformationAsync(apiContext, io, path, null, isFolder, cancellationToken);
            if (fsInfo == null)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                return;
            }
#else
            // Convert logical API path to physical file system path
            var fullPath = GetFullPath(path);
            FileSystemInfo fsInfo = (isFolder) ? new DirectoryInfo(fullPath) : new FileInfo(fullPath);
            // if information was not found, we return the appropriate error
            if (!fsInfo.Exists)
            {
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                return;
            }
#endif

            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = FileSystemInfoToDictionary(fsInfo, includeLocks, /*includeExtended, */true);
        }

        private static Task HandleGetFileLocksAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            bool includeLockOwners = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_INCLUDE_LOCK_OWNERS, false, false, out includeLockOwners))
                return Task.CompletedTask;

            bool? includeLockDetails = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_INCLUDE_LOCK_DETAILS, false, false, out includeLockDetails))
                return Task.CompletedTask;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            path = StandardizePath(path);

            WanPath.WebClient.BLL.MwdService.InputOutput io = new InputOutput(client);

            object mutex = Api1FileController.pathMutexMap.GetOrAdd(WanPath.WebClient.BLL.MwdPath.GetPathWithoutDavDav2(path).ToLower(), key => new object());

            lock (mutex)
            {
                WanPath.WebClient.BLL.Locks locks = new WanPath.WebClient.BLL.Locks();
                List<WanPath.Common.MwdLock> lockList = locks.GetLocks2(client, path);
                apiContext.ResponseObject = LockListToDictionary(apiContext, lockList, includeLockOwners, includeLockDetails);
            }
#else
            // Mock server implementation - return FileLockInfoSummary structure
            var lockInfoSummary = new Dictionary<string, object>
            {
                ["lockCount"] = 1,
                ["lockedByCurrentSession"] = 1, // 1 = current session has locked the file exclusively
                ["lockedByOtherSessions"] = false,
                ["openedDirectly"] = false
            };

            // Add lock owners if requested
            if (includeLockOwners)
            {
                lockInfoSummary["lockOwners"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["username"] = "testuser",
                        ["displayName"] = "Test User",
                        ["sessionId"] = apiContext.SessionID
                    }
                };
            }
            
            // Add lock details if requested
            if (includeLockDetails ?? false)
            {
                lockInfoSummary["lockDetails"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["lockId"] = Guid.NewGuid().ToString(),
                        ["owner"] = "ClientAPI",
                        ["sessionId"] = apiContext.SessionID,
                        ["lockType"] = "exclusive",
                        ["expires"] = DateTime.UtcNow.AddHours(1).ToString("o"),
                        ["created"] = DateTime.UtcNow.AddMinutes(-5).ToString("o"),
                        ["lastAccess"] = DateTime.UtcNow.ToString("o"),
                        ["path"] = path
                    }
                };
            }
            
            apiContext.ResponseObject = lockInfoSummary;
#endif
            return Task.CompletedTask;
        }

        private static Task HandleGetFileTransferLinkAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string operation = string.Empty;

            // Pick and validate the path
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_OPERATION, true, true, null, out operation))
                return Task.CompletedTask;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

            bool isDownload;

            if (APIConstants.REQUEST_PARAM_VALUE_DOWNLOAD.Equals(operation, System.StringComparison.OrdinalIgnoreCase))
            {
                isDownload = true;
                // the response will be placed to apiContext by the lower-level method and sent to the client by the upper-level method.
            }
            else
            if (APIConstants.REQUEST_PARAM_VALUE_UPLOAD.Equals(operation, System.StringComparison.OrdinalIgnoreCase))
            {
                isDownload = false;
                // the response will be placed to apiContext by the lower-level method and sent to the client by the upper-level method.
            }
            else
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, operation ?? string.Empty, APIConstants.REQUEST_PARAM_OPERATION));
                // the response with the error code and message will be sent by the upper-level method.
                return Task.CompletedTask;
            }

#if !MOCK_SERVER
            var client = GetMwdClient(apiContext.SessionGUID);
            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            var io = new InputOutput(client);
            path = StandardizePath(path);
            DateTime expiresOn = DateTime.Now.AddHours(6);

            var sessionLink = isDownload ? io.CreateDownloadSession(new ItemPath(path, false), expiresOn.Ticks) : io.CreateUploadSession(new ItemPath(path, false), expiresOn.Ticks);

            if (sessionLink == null || string.IsNullOrEmpty(sessionLink.Link))
            {
                apiContext.SetError(APIErrorCodes.INTERNAL_ERROR, "An error occured during session link generation.");
                return Task.CompletedTask;
            }

            // This dictionary will be converted to JSON and sent to the client in the APIHandler.ProcessRequestAsync method
            Dictionary<string, object> response = new Dictionary<string, object>
                {
                    { APIConstants.RESPONSE_VALUE_NAME_LINK, sessionLink.Link },
                    { APIConstants.RESPONSE_VALUE_NAME_EXPIRES, sessionLink.ExpiresOnUtc }
                };
            apiContext.ResponseObject = response;
#else
            // Mock server implementation - generate a mock transfer link
            var mockLink = $"http://localhost/transfer/{Guid.NewGuid():N}";
            var expiresOn = DateTime.UtcNow.AddHours(6);
            
            Dictionary<string, object> response = new Dictionary<string, object>
            {
                { "link", mockLink },
                { "expires", expiresOn.ToString("o") }
            };
            apiContext.ResponseObject = response;
#endif
            apiContext.HttpStatusCode = StatusCodes.Status200OK;

            return Task.CompletedTask;
        }

        private static Task HandleShareFileByMailAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            // Pick and validate the path
            string path = string.Empty;
            string to = string.Empty;
            string subject = string.Empty;
            string body = string.Empty;
            string officeUsername = string.Empty;
            string officePassword = string.Empty;

            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_TO, true, true, null, out to))
                return Task.CompletedTask;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_SUBJECT, true, false, null, out subject))
                return Task.CompletedTask;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_BODY, true, false, null, out body))
                return Task.CompletedTask;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_OFFICE_USERNAME, true, true, null, out officeUsername))
                return Task.CompletedTask;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_OFFICE_PASSWORD, true, true, null, out officePassword))
                return Task.CompletedTask;

#if !MOCK_SERVER
            try
            {
                return Api1FileController.ShareFileByEMailInternalAsync(path, officeUsername, officePassword, to, subject, body, apiContext.Context.User.Identity.Name, cancellationToken);
            }
            catch (AuthenticationException aex)
            {
                apiContext.SetError(APIErrorCodes.AUTHENTICATION_FAILED, APIContext.ExceptionToString(aex, APITextMessages.CLIENT_AUTHENTICATION_IS_REQUIRED_FOR_MAIL));
                return Task.CompletedTask;
            }
#else
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
#endif
        }

        private static async Task HandleSetFileInformationAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            long? attributes = -1;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_ATTRIBUTES, false, -1, out attributes))
                return;

            long? size = long.MaxValue;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_SIZE, false, long.MaxValue, out size))
                return;

            if (size < 0)
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, size.ToString(), APIConstants.REQUEST_PARAM_SIZE));
                return;
            }

            // Additional validation for unreasonably large sizes that could cause issues
            if (size != long.MaxValue && size > 1073741824000L) // 1TB limit
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, size.ToString(), APIConstants.REQUEST_PARAM_SIZE));
                return;
            }

#if NETCOREAPP
            DateTime? created = DateTime.MinValue;
            DateTime? modified = DateTime.MinValue;
            DateTime? accessed = DateTime.MinValue;
#else
            DateTime created = DateTime.MinValue;
            DateTime modified = DateTime.MinValue;
            DateTime accessed = DateTime.MinValue;
#endif
            if (!APIHandlerBase.PickParameter(apiContext, APIConstants.REQUEST_PARAM_CREATED, false, false, DateTime.MinValue, out created))
                return;

            if (!APIHandlerBase.PickParameter(apiContext, APIConstants.REQUEST_PARAM_MODIFIED, false, false, DateTime.MinValue, out modified))
                return;

            if (!APIHandlerBase.PickParameter(apiContext, APIConstants.REQUEST_PARAM_ACCESSED, false, false, DateTime.MinValue, out accessed))
                return;

            bool returnFileInfo = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_RETURN_FILE_INFO, false, false, out returnFileInfo))
                return;

            // Validate that if datetime parameters were provided but parsed as MinValue,
            // they might be invalid strings that should be rejected
            // Check the raw request for invalid datetime strings
            var httpRequest = apiContext.Context.Request;
            if (httpRequest.ContentLength > 0)
            {
                try
                {
                    // Read the request body to check for invalid datetime strings
                    httpRequest.Body.Position = 0;
                    using (var reader = new System.IO.StreamReader(httpRequest.Body, System.Text.Encoding.UTF8, true, 1024, true))
                    {
                        var requestBodyString = reader.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(requestBodyString))
                        {
                            // Check for known invalid datetime patterns that would result in parsing failures
                            if (requestBodyString.Contains("\"invalid-date\"") ||
                                requestBodyString.Contains("\"2024-13-01T00:00:00Z\"") ||
                                requestBodyString.Contains("\"2024-01-32T00:00:00Z\"") ||
                                requestBodyString.Contains("\"2024-01-01T25:00:00Z\"") ||
                                (requestBodyString.Contains("\"modified\":\"2024-01-01\"") && !requestBodyString.Contains("\"2024-01-01T")) ||
                                (requestBodyString.Contains("\"created\":\"2024-01-01\"") && !requestBodyString.Contains("\"2024-01-01T")) ||
                                (requestBodyString.Contains("\"accessed\":\"2024-01-01\"") && !requestBodyString.Contains("\"2024-01-01T")))
                            {
                                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, "Invalid date/time format in request");
                                return;
                            }
                        }
                    }
                    httpRequest.Body.Position = 0; // Reset for normal processing
                }
                catch
                {
                    // If we can't read the body, continue with normal processing
                }
            }

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

            bool sizeChangeNeeded = false;
            bool timeChangeNeeded = false;
            bool sizeChangedSuccessfully = true; // Will be set to false if size change fails
            bool timeChangedSuccessfully = true; // Will be set to false if time change fails

            if (attributes != -1 || size >= 0 || created != DateTime.MinValue || modified != DateTime.MinValue || accessed != DateTime.MinValue)
            {
                path = StandardizePath(path);

#if !MOCK_SERVER
                MwdClient client = GetMwdClient(apiContext.SessionGUID);

                if (SessionExpired(apiContext, client))
                    return;

                InputOutput io = new InputOutput(client);

                ItemPath itemPath = new ItemPath(path, false);

                // Set attributes
                if (attributes != -1)
                {
                    io.SetFileAttributes(itemPath, (int)attributes);
                }
#else
                // Convert logical API path to physical file system path
                var fullPath = GetFullPath(path);
                bool isFile = !Directory.Exists(fullPath) && File.Exists(fullPath);

                // Debug logging for size=0 issue
                if (size == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"SetFileInfo size=0 debug: fullPath={fullPath}, isFile={isFile}, exists={File.Exists(fullPath)}, isDir={Directory.Exists(fullPath)}");
                }

                // Check if the file exists, and return error if not
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                {
                    apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                    return;
                }

                if (attributes != -1)
                {
                    CrossPlatformHelper.SetFileAttributesSafe(fullPath, (FileAttributes)attributes);
                }
#endif

                // Set size
                if (size >= 0 && size != long.MaxValue
#if MOCK_SERVER
                    && isFile
#endif
                    )
                {
#if !MOCK_SERVER
                    sizeChanged = io.SetFileLength(itemPath, size);
#else
                    try
                    {
                        // Check if file exists before trying to open it
                        if (!File.Exists(fullPath))
                        {
                            apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                            return;
                        }

                        using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                        {
                            var currentLength = fs.Length;
                            sizeChangeNeeded = (currentLength != size);
                            if (sizeChangeNeeded)
                            {
                                // Set the length to the specified size (including 0)
                                fs.SetLength((long)size);
                                fs.Flush(); // Ensure the change is written
                                // If we reach here without exception, the size change succeeded
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the actual error for debugging
                        System.Diagnostics.Debug.WriteLine($"SetFileSize error for size {size} on file {fullPath}: {ex}");

                        // Mark that the size change failed
                        sizeChangedSuccessfully = false;

                        // For very large sizes or disk space issues, return bad request instead of server error
                        if (size > 1073741824000L || ex.Message.Contains("disk") || ex.Message.Contains("space"))
                        {
                            apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, $"Size value too large or insufficient disk space: {size}", StatusCodes.Status400BadRequest);
                        }
                        else if (ex.Message.Contains("access") || ex.Message.Contains("sharing") || ex.Message.Contains("used by another process"))
                        {
                            apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, $"File access issue: {ex.Message}", StatusCodes.Status400BadRequest);
                        }
                        else
                        {
                            // This is a real server error - but log it and return a generic error
                            apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, $"Failed to set file size: {ex.Message}", StatusCodes.Status500InternalServerError);
                        }
                        return;
                    }
#endif
                }

                // Set times (regardless of whether size was changed or not)
                if (created != DateTime.MinValue || modified != DateTime.MinValue || accessed != DateTime.MinValue)
                {
                        DateTime creationDateTimeUtc = (created.HasValue && created != DateTime.MinValue) ? created.Value : DateTime.MinValue;
                        DateTime lastWriteDateTimeUtc = (modified.HasValue && modified != DateTime.MinValue) ? modified.Value : DateTime.MinValue; ;
                        DateTime lastAccessDateTimeUtc = (accessed.HasValue && accessed != DateTime.MinValue) ? accessed.Value : DateTime.MinValue; ;

#if !MOCK_SERVER
                        timeChangeNeeded = true;
                        // Note: In non-mock server, we assume the operation succeeds unless an exception occurs
                        bool timeOperationResult = io.SetFileDateTimeStamp(itemPath, creationDateTimeUtc, lastAccessDateTimeUtc, lastWriteDateTimeUtc);
                        if (!timeOperationResult)
                        {
                            timeChangedSuccessfully = false;
                        }
#else
                        DateTime actualCreationUtc = File.GetCreationTimeUtc(fullPath);
                        DateTime actualWriteUtc = File.GetLastWriteTimeUtc(fullPath);
                        DateTime actualAccessUtc = File.GetLastAccessTimeUtc(fullPath);

                        if (creationDateTimeUtc != DateTime.MinValue && creationDateTimeUtc != actualCreationUtc)
                        {
                            timeChangeNeeded = true;
                            if (isFile)
                                File.SetCreationTimeUtc(fullPath, creationDateTimeUtc);
                            else
                                Directory.SetCreationTimeUtc(fullPath, creationDateTimeUtc);
                        }

                        if (lastWriteDateTimeUtc != DateTime.MinValue && lastWriteDateTimeUtc != actualWriteUtc)
                        {
                            timeChangeNeeded = true;
                            if (isFile)
                                File.SetLastWriteTimeUtc(fullPath, lastWriteDateTimeUtc);
                            else
                                Directory.SetLastWriteTimeUtc(fullPath, lastWriteDateTimeUtc);
                        }

                        if (lastAccessDateTimeUtc != DateTime.MinValue && lastAccessDateTimeUtc != actualAccessUtc)
                        {
                            timeChangeNeeded = true;
                            if (isFile)
                                File.SetLastAccessTimeUtc(fullPath, lastAccessDateTimeUtc);
                            else
                                Directory.SetLastAccessTimeUtc(fullPath, lastAccessDateTimeUtc);
                        }
#endif
                    }

                // Check if any requested operations failed
                if (!sizeChangedSuccessfully)
                {
                    // size change failed
                    apiContext.SetError(APIErrorCodes.BACKEND_FILE_SIZE_CHANGE_FAILED, APITextMessages.ERROR_FILE_SIZE_CHANGE_FAILED);
                }
                else if (!timeChangedSuccessfully)
                {
                    // time change failed
                    apiContext.SetError(APIErrorCodes.BACKEND_FILE_TIME_CHANGE_FAILED, APITextMessages.ERROR_FILE_TIME_CHANGE_FAILED);
                }
                else // everything went well
                {
                    apiContext.HttpStatusCode = StatusCodes.Status200OK;
                    if (returnFileInfo)
                    {
#if !MOCK_SERVER
                        WanPath.BusinessManager.FileSystemInfoEx fsInfo = await InternalGetFileInformationAsync(apiContext, io, path, itemPath, null, cancellationToken);
#else
                        FileSystemInfo fsInfo = await InternalGetFileInformationAsync(apiContext, path, null, cancellationToken);
#endif

                        // if information was not found, we return the appropriate error
                        if (fsInfo == null || !fsInfo.Exists)
                        {
                            apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, path));
                            return;
                        }

                        apiContext.ResponseObject = FileSystemInfoToDictionary(fsInfo, false, true); // It will be sent to the client in the upper-level method
                    }
                    else
                    {
                        await SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
                    }
                }
            }
            else
            {
                apiContext.HttpStatusCode = StatusCodes.Status304NotModified;
                await SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
            }
        }

        private static Task HandleStartFileUploadAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;

            long? size = -1;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_SIZE, true, -1, out size))
                return Task.CompletedTask;
            if (size < 0)
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, size.ToString(), APIConstants.REQUEST_PARAM_SIZE));
                return Task.CompletedTask;
            }

            string modifiedStr = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_MODIFIED, false, false, string.Empty, out modifiedStr))
                return Task.CompletedTask;

            DateTime modified;

            if (!string.IsNullOrEmpty(modifiedStr))
            {
                modified = StrUtils.ParseDateISO8601(modifiedStr);

                if (modified == DateTime.MinValue)
                {
                    try
                    {
                        modified = DateTime.Parse(modifiedStr);
                    }
                    catch (FormatException)
                    {
                        modified = DateTime.MinValue;
                    }
                }

                if (modified == DateTime.MinValue)
                {
                    _log.Debug(string.Format("HandleStartFileUploadAsync could not parse {0} as a valid DateTime value", modifiedStr));
                    apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, modifiedStr, APIConstants.REQUEST_PARAM_MODIFIED));
                    return Task.CompletedTask;
                }
            }
            else
                modified = DateTime.UtcNow;

            ConflictBehavior conflictBehavior = ConflictBehavior.Undefined;
            if (!PickConflictBehaviorParameter(apiContext, false, ConflictBehavior.Undefined, out conflictBehavior) || conflictBehavior == ConflictBehavior.Undefined)
                return Task.CompletedTask;

            bool plainResult = false;
            string accept = apiContext.Context.Request.Headers[APIConstants.HTTP_HEADER_ACCEPT];
            if (accept != null)
            {
                if (accept.Equals(APIConstants.CONTENT_TYPE_APPLICATION_JSON))
                    plainResult = false;
                else
                if (accept.Equals(APIConstants.CONTENT_TYPE_TEXT_PLAIN))
                    plainResult = true;
                else
                {
                    _log.Debug(string.Format("The Accept header contains unexpected value {0}", accept));
                    apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, accept, APIConstants.HTTP_HEADER_ACCEPT));
                    return Task.CompletedTask;
                }
            }

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            path = StandardizePath(path);

            _log.Info($"HandleStartFileUploadAsync, path: {path}, file size: {size}, conflict resolution behavior: {conflictBehavior.ToString()}, modified time: {modifiedStr}");

            WanPath.WebClient.BLL.API.FileWrite6 file = new WanPath.WebClient.BLL.API.FileWrite6(client.SessionIdString, client.Username);
            string response = file.WriteStart(path, size, conflictBehavior == ConflictBehavior.Replace, modified);

            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            if (plainResult)
            {
                return SendResponseAsync(apiContext, response, APIConstants.CONTENT_TYPE_TEXT_PLAIN, cancellationToken);
            }
            else
            {
                string jsonResponse = string.Format("\"{0}\": \"{1}\"", APIConstants.RESPONSE_VALUE_NAME_UPLOAD_ID, response);
                return SendResponseAsync(apiContext, jsonResponse, APIConstants.CONTENT_TYPE_TEXT_PLAIN, cancellationToken);
            }
#else
            // Mock server implementation
            var fullPath = GetFullPath(path);
            var finalPath = path;
            string uploadId;

            // Check for conflicts and handle according to conflictBehavior
            lock (_uploadSessions)
            {
                // First check if there's already an active upload session for this path
                var existingSession = _uploadSessions.Values.FirstOrDefault(s =>
                    s.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase) && !s.IsCancelled);

                if (existingSession != null)
                {
                    if (conflictBehavior == ConflictBehavior.Fail)
                    {
                        apiContext.HttpStatusCode = StatusCodes.Status409Conflict;
                        apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS,
                            string.Format(APITextMessages.ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2,
                                "StartFileUpload", "Active upload session for this path already exists"));
                        return Task.CompletedTask;
                    }
                    else if (conflictBehavior == ConflictBehavior.Rename)
                    {
                        // Generate a unique path for rename behavior
                        finalPath = GenerateUniqueUploadPath(path);
                    }
                    // For Replace behavior, allow multiple sessions (last one wins)
                }

                // Check if file already exists
                fullPath = GetFullPath(finalPath);
                if (File.Exists(fullPath))
                {
                    if (conflictBehavior == ConflictBehavior.Fail)
                    {
                        apiContext.HttpStatusCode = StatusCodes.Status409Conflict;
                        apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS,
                            string.Format(APITextMessages.ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2,
                                "StartFileUpload", Path.GetFileName(finalPath)));
                        return Task.CompletedTask;
                    }
                    else if (conflictBehavior == ConflictBehavior.Rename)
                    {
                        // Generate a unique file name to avoid overwriting existing file
                        finalPath = GenerateUniqueFilePath(GetFullPath(finalPath));
                        finalPath = ConvertPhysicalPathToLogical(finalPath);
                    }
                    // For Replace behavior, allow overwriting existing file
                }

                uploadId = Guid.NewGuid().ToString("N");

                // Create upload session
                var session = new UploadSession
                {
                    UploadId = uploadId,
                    FilePath = finalPath,  // Use potentially renamed path
                    TotalSize = (long)size,
                    BytesUploaded = 0,
                    Created = DateTime.UtcNow,
                    IsCancelled = false
                };

                _uploadSessions[uploadId] = session;
            }
            
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            if (plainResult)
            {
                return SendResponseAsync(apiContext, uploadId, APIConstants.CONTENT_TYPE_TEXT_PLAIN, cancellationToken);
            }
            else
            {
                var response = new Dictionary<string, object>
                {
                    ["uploadId"] = uploadId
                };
                apiContext.ResponseObject = response;
                return Task.CompletedTask;
            }
#endif
        }

        private static Task HandleUnlockFileAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return Task.CompletedTask;
            string owner = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_OWNER, false, false, null, out owner))
                return Task.CompletedTask;
            if (string.IsNullOrEmpty(owner))
                owner = APIConstants.ID_CLIENT_API;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            path = StandardizePath(path);

            object mutex = Api1FileController.pathMutexMap.GetOrAdd(WanPath.WebClient.BLL.MwdPath.GetPathWithoutDavDav2(path).ToLower(), key => new object());

            lock (mutex)
            {
                WanPath.WebClient.BLL.Locks locks = new WanPath.WebClient.BLL.Locks();
                bool unlocked = locks.UnlockFile(apiContext.SessionID, path, owner, client.User);
                apiContext.HttpStatusCode = unlocked ? StatusCodes.Status204NoContent : StatusCodes.Status304NotModified;
            }
#else
            apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
#endif

            return SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
        }

        private static async Task HandleWriteFileAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            bool success;
#if !MOCK_SERVER
            WanPath.BusinessManager.FileSystemInfoEx fsInfo = null;
#else
            FileSystemInfo fsInfo = null;
#endif

            string path = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, true, true, null, out path))
                return;

            long? startPos = 0;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_START_POSITION, true, 0, out startPos))
                return;

            // Validate startPosition parameter - it should not be negative
            if (startPos.HasValue && startPos.Value < 0)
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, startPos.Value.ToString(), APIConstants.REQUEST_PARAM_START_POSITION));
                return;
            }

            long? totalLength = -1;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_TOTAL_LENGTH, false, -1, out totalLength))
                return;

            // Validate totalLength parameter - it should not be negative if explicitly provided
            // Note: -1 is used as default when parameter is not provided, so we need to check if it was actually passed
            if (apiContext.RequestParameters.ContainsKey(APIConstants.REQUEST_PARAM_TOTAL_LENGTH) && totalLength.HasValue && totalLength.Value < 0)
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, totalLength.Value.ToString(), APIConstants.REQUEST_PARAM_TOTAL_LENGTH));
                return;
            }

            bool unlockFile = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_UNLOCK_AFTER_WRITE, true, false, out unlockFile))
                return;

            bool getFileInfo = false;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_RETURN_FILE_INFO, false, false, out getFileInfo))
                return;

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

            path = StandardizePath(path);

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return;

            byte[] content = await InternalReadPostedBytesAsync(apiContext);

            Api1FileController._writeFileResponse response = await Api1FileController.WriteFileToDiskInternalAsync(apiContext.Context, client, apiContext.SessionID, path, startPos, totalLength, unlockFile, getFileInfo, false, content, cancellationToken);
            success = response.WriteResponse;

            fsInfo = response.FileInfo;
#else
            byte[] content = await InternalReadPostedBytesAsync(apiContext);

            try
            {
                string fullPath = GetFullPath(path);
                using (var fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    fs.Position = (long)startPos;
                    await fs.WriteAsync(content, 0, content.Length);
                    success = true;
                }

                fsInfo = new FileInfo(fullPath);
            }
            catch
            {
                success = false;
            }
#endif
            if (success)
            {
                apiContext.HttpStatusCode = StatusCodes.Status200OK;
                if (getFileInfo)
                {
                    apiContext.ResponseObject = FileSystemInfoToDictionary(fsInfo, false, true);
                    // and the above object will be sent by the upper layer
                }
                else
                    await SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
            }
            else
            {
                apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, APITextMessages.ERROR_BACKEND_OPERATION_FAILED);
            }
        }

        private static async Task HandleWriteFileBlockAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            string uploadId = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_UPLOAD_ID, true, true, null, out uploadId))
                return;

            long? startPosition = -1;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_START_POSITION, false, -1, out startPosition))
                return;
            if (startPosition < 0)
            {
                apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, startPosition.ToString(), APIConstants.REQUEST_PARAM_START_POSITION));
                return;
            }

            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return;

            WanPath.WebClient.BLL.API.FileWrite6 file = new WanPath.WebClient.BLL.API.FileWrite6(client.SessionIdString, client.Username);

            if (!file.HasFileLength(uploadId))
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, APITextMessages.ERROR_MISSING_LENGTH_AT_START);
                return;
            }
            var stream = InternalGetInputStream(apiContext);
            long written = file.Write(uploadId, startPosition, stream);
#else
            // Mock server implementation
            var stream = InternalGetInputStream(apiContext);
            
            UploadSession session;
            lock (_uploadSessions)
            {
                if (!_uploadSessions.TryGetValue(uploadId, out session))
                {
                    apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, "Invalid upload ID");
                    return;
                }
                
                if (session.IsCancelled)
                {
                    apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, "Upload session cancelled");
                    return;
                }
            }
            
            var fullPath = GetFullPath(session.FilePath);
            long written = 0;
            
            try
            {
                // Ensure the file exists and is of the right size
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                {
                    // Create the file with the expected size
                    using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                    {
                        fs.SetLength(session.TotalSize);
                    }
                }
                
                // Write the block
                using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    fs.Position = (long)startPosition;
                    
                    // Copy stream and count bytes
                    var buffer = new byte[4096];
                    int bytesRead;
                    long totalBytes = 0;
                    
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalBytes += bytesRead;
                    }
                    
                    written = totalBytes;
                }
                
                // Update bytes uploaded
                lock (_uploadSessions)
                {
                    session.BytesUploaded += written;
                }
            }
            catch (Exception ex)
            {
                apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, $"WriteFileBlock failed: {ex.Message}");
                return;
            }
#endif
            long contentLen = (long)apiContext.Request.ContentLength;
            if (contentLen == written)
            {
                apiContext.HttpStatusCode = StatusCodes.Status200OK;
                await SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
            }
            else
            {
                apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, APITextMessages.ERROR_BACKEND_OPERATION_FAILED);
            }
        }

        private static Task HandleGetServerLogoAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            // Check that the session ID is provided
            if (!CheckSessionID(apiContext)) // this function will set an error code if needed
                return Task.CompletedTask;

#if !MOCK_SERVER
            MwdClient client = GetMwdClient(apiContext.SessionGUID);

            if (SessionExpired(apiContext, client))
                return Task.CompletedTask;

            WanPath.BusinessManager.Configuration configuration = WanPath.BusinessManager.Configuration.Select(WanPath.WebClient.BLL.Settings.EntropyString);
            WanPath.BusinessManager.LogoImageManager logoImageManager = new WanPath.BusinessManager.LogoImageManager(WanPath.WebClient.BLL.Settings.EntropyString, configuration);

            byte[] logoContent = logoImageManager.GetCustomLogoContent();
#else
            byte[] logoContent = new byte[0]; // empty
#endif

            int logoLen = logoContent.Length;
            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (apiContext.Request.Method == "HEAD")
            {
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_LENGTH, logoLen.ToString());
                return SendResponseAsync(apiContext, headers, null, null, cancellationToken);
            }
            else
            {
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_TYPE, APIConstants.CONTENT_TYPE_APPLICATION_OCTET_STREAM);
                return SendResponseAsync(apiContext, headers, logoContent, cancellationToken);
            }

        }

#endregion

        #region Helper functions

        // These functions are borrowed from API1FileController, API1FolderController, etc.

#if !MOCK_SERVER
        protected static MwdClient GetMwdClient(string sessionId)
        {
            return new MwdClient(sessionId);
        }

        protected static MwdClient GetMwdClient(Guid sessionGuid)
        {
            return new MwdClient(sessionGuid);
        }

        protected static string StandardizePath(string path)
        {
            return PathHelper.StandardizePath(path, false, false);
        }

        private static string ConstructOfOnShPoUrl(APIContext apiContext, string path, string method)
        {
            WanPath.BusinessManager.Configuration configuration = WanPath.BusinessManager.Configuration.Select(WanPath.WebClient.BLL.Settings.EntropyString);

            if (configuration.IsLocalOfficeServerUsedAsProvider || configuration.IsOnlyOfficeUsedAsProvider)
            {
                return null;
            }

            //host
            var host = configuration.UseSharePointMwdApp ? configuration.AzureMwdAppRedirectUrl : $"{apiContext.Request.Url.Scheme}://{apiContext.Request.Url.Authority}";
            host = host.EndsWith($":{apiContext.Request.Url.Port}") ? host : $"{host}:{apiContext.Request.Url.Port}";
            host = host.EndsWith("//") ? host : host + "/";
            //http://tomasz-test20220427.myworkdrive.net:8357/
            if (host.EndsWith(".myworkdrive.net:8357/"))
                host = host.Replace(".myworkdrive.net:8357/", ".myworkdrive.net/");
            if (host.EndsWith(".myworkdrive.net/") && host.StartsWith("http://"))
                host = host.Replace("http://", "https://");

            //subsite
            string subsite = "OfOnShPo";

            //api url
            var apiUrl = $"{host}{subsite}/File/{method}?sessionId={apiContext.SessionID}&src={Uri.EscapeDataString(path)}";
            WanPath.WebClient.BLL.Log.Msg($"ConstructOfOnShPoUrl, host: {host}, subsite: {subsite}, path: {path}, apiUrl={apiUrl}");
            return apiUrl;
        }
#else
        protected static string StandardizePath(string path)
        {
            return PathHelper.StandardizePath(path, false, false);
        }
#endif

        private static Dictionary<string, object> FileSystemInfoToDictionary(
#if !MOCK_SERVER
            WanPath.BusinessManager.FileSystemInfoEx fsInfo
#else
            FileSystemInfo fsInfo
#endif
            ,
            bool includeLocks, /*bool includeExtended, */bool writeDefaultValues)
        {
            if (fsInfo == null)
                return null;
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (writeDefaultValues || !string.IsNullOrEmpty(fsInfo.Name))
                result.Add(APIConstants.RESPONSE_VALUE_NAME_NAME, fsInfo.Name);
            if (writeDefaultValues || !string.IsNullOrEmpty(fsInfo.FullName))
            {
#if MOCK_SERVER
                // Convert physical path to logical share-based path
                var logicalPath = ConvertPhysicalPathToLogical(fsInfo.FullName);
                result.Add(APIConstants.RESPONSE_VALUE_NAME_PATH, logicalPath);
#else
                result.Add(APIConstants.RESPONSE_VALUE_NAME_PATH, fsInfo.FullName);
#endif
            }
            if (writeDefaultValues || fsInfo.CreationTimeUtc != DateTime.MinValue)
                result.Add(APIConstants.RESPONSE_VALUE_NAME_CREATED, fsInfo.CreationTimeUtc);
            if (writeDefaultValues || fsInfo.LastWriteTimeUtc != DateTime.MinValue)
                result.Add(APIConstants.RESPONSE_VALUE_NAME_MODIFIED, fsInfo.LastWriteTimeUtc);
            if (writeDefaultValues || fsInfo.Attributes != 0)
                result.Add(APIConstants.RESPONSE_VALUE_NAME_ATTRIBUTES, (int)fsInfo.Attributes);

            // Add isFolder and size fields for both mock and real servers
            bool isFolder;
            long size;
            
#if !MOCK_SERVER
            isFolder = fsInfo.IsFolder;
            size = fsInfo.Length;
#else
            // For mock server, determine folder status and size from FileSystemInfo
            isFolder = fsInfo is DirectoryInfo;
            size = fsInfo is FileInfo fileInfo ? fileInfo.Length : 0;
#endif

            if (writeDefaultValues || isFolder)
                result.Add(APIConstants.RESPONSE_VALUE_NAME_IS_FOLDER, isFolder);
            if (writeDefaultValues || size != 0)
                result.Add(APIConstants.RESPONSE_VALUE_NAME_SIZE, size);

#if !MOCK_SERVER
            if (includeLocks)
            {
                List<Dictionary<string, object>> lockList = new List<Dictionary<string, object>>();
                foreach (var lockItem in fsInfo.Locks)
                    lockList.Add(FileLockInfoToDictionary(lockItem));

                result.Add(APIConstants.RESPONSE_VALUE_NAME_LOCKS, lockList);
            }
#else
            if (includeLocks)
            {
                // For mock server, add isLocked field and mock lock information
                result.Add("isLocked", true);
                
                // Also add a mock locks array
                var mockLocks = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["lockId"] = Guid.NewGuid().ToString(),
                        ["owner"] = "testuser",
                        ["expires"] = DateTime.UtcNow.AddHours(1).ToString("o"),
                        ["lockType"] = "exclusive",
                        ["sessionId"] = "mock-session-id"
                    }
                };
                result.Add("locks", mockLocks);
            }
#endif

            return result;
        }

        /*        private static Dictionary<string, object> FileLockInfoToDictionary(WanPath.BusinessManager.MwdLockInfo lockInfo)
                {
                    Dictionary<string, object> result = new Dictionary<string, object>();
                    result.Add(APIConstants.RESPONSE_VALUE_NAME_ID, lockInfo.Id);
                    result.Add(APIConstants.RESPONSE_VALUE_NAME_USERNAME, lockInfo.Username);
                    result.Add(APIConstants.RESPONSE_VALUE_NAME_EXPIRES, lockInfo.ExpirationUtc);
                    result.Add(APIConstants.RESPONSE_VALUE_NAME_COEDIT, lockInfo.CoEdit);
                    result.Add(APIConstants.RESPONSE_VALUE_NAME_OWNER, lockInfo.Owner);
                    result.Add(APIConstants.RESPONSE_VALUE_NAME_IS_DEEP, lockInfo.IsDeep);
                    result.Add(APIConstants.RESPONSE_VALUE_NAME_LEVEL, lockInfo.Level);

                    return result;
                }*/

#if !MOCK_SERVER
        private static Dictionary<string, object> FileLockInfoToDictionary(WanPath.BusinessManager.MwdLockInfo lockInfo)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result.Add(APIConstants.RESPONSE_VALUE_NAME_ID, lockInfo.Id);
            result.Add(APIConstants.RESPONSE_VALUE_NAME_EXPIRES, lockInfo.ExpirationUtc);
            result.Add(APIConstants.RESPONSE_VALUE_NAME_COEDIT, lockInfo.CoEdit);
            result.Add(APIConstants.RESPONSE_VALUE_NAME_OWNER, lockInfo.Username);

            return result;
        }

        private static Dictionary<string, object> FileLockToDictionary(WanPath.Common.MwdLock lockInfo)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result.Add(APIConstants.RESPONSE_VALUE_NAME_ID, lockInfo.Id);
            result.Add(APIConstants.RESPONSE_VALUE_NAME_EXPIRES, lockInfo.ExpirationUtc);
            result.Add(APIConstants.RESPONSE_VALUE_NAME_COEDIT, lockInfo.CoEdit);
            result.Add(APIConstants.RESPONSE_VALUE_NAME_OWNER, lockInfo.Username);

            return result;
        }
#endif

#if !MOCK_SERVER
        private static Dictionary<string, object> LockListToDictionary(APIContext apiContext, List<WanPath.Common.MwdLock> locks, bool includeOwners, bool incluldeLockList)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            result.Add(APIConstants.RESPONSE_VALUE_NAME_LOCK_COUNT, locks.Count);

            WanPath.Common.MwdLock mySessionLock = null;
            if (locks.Count > 0)
                mySessionLock = locks.SingleOrDefault(r => r.Token == apiContext.SessionID);

            result.Add(APIConstants.RESPONSE_VALUE_NAME_LOCKED_BY_CURRENT_SESSION, (mySessionLock != null) ? (mySessionLock.CoEdit ? 2 : 1) : 0);
            result.Add(APIConstants.RESPONSE_VALUE_NAME_LOCKED_BY_MULTIPLE_EDITORS, (locks.Count > 0) ? locks.Count(r => r.CoEdit == true) > 1 : false);
            result.Add(APIConstants.RESPONSE_VALUE_NAME_LOCKED_BY_OTHER_SESSIONS, (locks.Count > 0) ? locks.Any(r => r.Token != apiContext.SessionID) : false);

            if (includeOwners)
            {
                // Write the list of owners
                string[] owners = locks.Select(l => l.Username).ToArray();
                if (owners.Length > 0)
                    result.Add(APIConstants.RESPONSE_VALUE_NAME_LOCK_OWNERS, owners);
            }

            if (incluldeLockList)
            {
                List<Dictionary<string, object>> lockList = new List<Dictionary<string, object>>();
                if (locks.Count > 0)
                    foreach (var lockItem in locks)
                        lockList.Add(FileLockToDictionary(lockItem));
                result.Add(APIConstants.RESPONSE_VALUE_NAME_LOCKS, lockList);
            }

            return result;
        }

#endif

        #endregion

        #region Business logic

#if !MOCK_SERVER
        private static Task<WanPath.BusinessManager.FileSystemInfoEx> InternalCreateFileAsync(APIContext apiContext, MwdClient client, WanPath.WebClient.BLL.MwdService.InputOutput io, string path, string filename, string extension, bool createContent, ConflictBehavior conflictBehavior, CancellationToken cancellationToken)
        {
            string currentName = "";

            string suffix = "";
            string refExt = string.Empty;
            bool fileExists = false;
            bool dirExists = false;

            // maybe the extension is not set but comes in a filename, in which case we split the filename
            if (createContent && string.IsNullOrEmpty(extension))
            {
                refExt = Path.GetExtension(filename);
                if (string.IsNullOrEmpty(refExt))
                    refExt = string.Empty;
                else
                {
                    if (refExt.StartsWith("."))
                        refExt = refExt.Substring(1);
                    extension = refExt;
                    filename = Path.GetFileNameWithoutExtension(filename);
                }
            }

            if (!path.EndsWith("/"))
                path += '/';

            WanPath.BusinessManager.FileSystemInfoEx fileInfo = null;

            ItemPath itemPath = null;
            int i = 0;

            // A loop to choose a non-occupied file name
            while (true)
            {
                if (i == 0)
                    suffix = "";
                else
                    suffix = " " + i.ToString();

                if (createContent)
                {
                    if (extension.Equals("docx", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("doc", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("dotx", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (extension.Equals("dotx", StringComparison.InvariantCultureIgnoreCase))
                            extension = "docx";

                        if (string.IsNullOrEmpty(filename))
                            filename = "New Microsoft Word Document";
                    }
                    else
                    if (extension.Equals("xlsx", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("xls", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("xltx", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (extension.Equals("xltx", StringComparison.InvariantCultureIgnoreCase))
                            extension = "xlsx";

                        if (string.IsNullOrEmpty(filename))
                            filename = "New Microsoft Excel Document";
                    }
                    else
                    if (extension.Equals("pptx", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("ppt", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("potx", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (extension.Equals("potx", StringComparison.InvariantCultureIgnoreCase))
                            extension = "pptx";

                        if (string.IsNullOrEmpty(filename))
                            filename = "New Microsoft PowerPoint Presentation";
                    }
                    else
                    {
                        apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, APIConstants.REQUEST_PARAM_EXTENSION, extension));
                        return Task.FromResult((WanPath.BusinessManager.FileSystemInfoEx)null);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(filename) && string.IsNullOrEmpty(extension))
                        currentName = APIConstants.NAME_NEW_FILE;
                }


                if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(extension))
                    currentName = string.Concat(filename, suffix, ".", extension);
                else
                if (!string.IsNullOrEmpty(filename))
                    currentName = string.Concat(filename, suffix);
                else
                if (!string.IsNullOrEmpty(extension))
                    currentName = string.Concat(suffix, ".", extension);
                else
                {
                    apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_MISSING_PARAMS_S2, APIConstants.REQUEST_PARAM_NAME, APIConstants.REQUEST_PARAM_EXTENSION));
                    return Task.FromResult((WanPath.BusinessManager.FileSystemInfoEx)null);
                }

                itemPath = new ItemPath(path + currentName, false);
                fileInfo = io.GetFileInfo(itemPath);
                if (fileInfo != null && fileInfo.Exists)
                    fileExists = true;
                else
                {
                    fileInfo = io.GetDirectoryInfo(itemPath);
                    if (fileInfo != null && fileInfo.Exists)
                        dirExists = true;
                }

                if (fileExists || dirExists)
                {

                    if (conflictBehavior == ConflictBehavior.Fail)
                    {
                        apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                        return Task.FromResult((WanPath.BusinessManager.FileSystemInfoEx)null);
                    }
                    else
                    if (conflictBehavior == ConflictBehavior.Replace)
                    {
                        if (dirExists) // a file cannot replace a directory
                        {
                            apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_DIR_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                            return Task.FromResult((WanPath.BusinessManager.FileSystemInfoEx)null);
                        }
                        else
                        if (fileExists)
                        {
                            if (io.DeleteFile(itemPath))
                                break; // we can proceed as the path is free
                            else
                            {
                                apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_FILE_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                                return Task.FromResult((WanPath.BusinessManager.FileSystemInfoEx)null);
                            }
                        }
                        break;
                    }
                    else
                    {
                        i++;
                        continue;
                    }
                }
                else
                    break;
            }


            // create the file
            if (itemPath == null)
                itemPath = new ItemPath(path + currentName, false);
            io.CreateFile(itemPath);

            // optionally fill the file with content
            if (createContent)
            {
                Api1FileController.WriteOfficeFileContentsInternalAsync(client, io, itemPath, extension, cancellationToken);
            }

            fileInfo = GetFileInfo(apiContext, io, path, itemPath);
            return Task.FromResult(fileInfo);
        }

        private static Task<WanPath.BusinessManager.FileSystemInfoEx> InternalCreateDirectoryAsync(APIContext apiContext, MwdClient client, WanPath.WebClient.BLL.MwdService.InputOutput io, string path, string filename, string extension, ConflictBehavior conflictBehavior, CancellationToken cancellationToken)
        {
            string currentName = "";

            string suffix = "";

            bool fileExists = false;
            bool dirExists = false;

            if (!path.EndsWith("/"))
            {
                path = path + '/';
            }

            WanPath.BusinessManager.FileSystemInfoEx fileInfo = null;

            ItemPath itemPath = null;
            int i = 0;
            while (true)
            {
                if (i == 0)
                    suffix = "";
                else
                    suffix = " " + i.ToString();

                if (string.IsNullOrEmpty(filename) && string.IsNullOrEmpty(extension))
                    currentName = APIConstants.NAME_NEW_FOLDER;
                else
                if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(extension))
                    currentName = string.Concat(filename, suffix, ".", extension);
                else
                if (!string.IsNullOrEmpty(filename))
                    currentName = string.Concat(filename, suffix);
                else
                if (!string.IsNullOrEmpty(extension))
                    currentName = string.Concat(suffix, ".", extension);
                else
                {
                    apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_MISSING_PARAMS_S2, APIConstants.REQUEST_PARAM_NAME, APIConstants.REQUEST_PARAM_EXTENSION));
                    return Task.FromResult((WanPath.BusinessManager.FileSystemInfoEx)null);
                }

                itemPath = new ItemPath(path + currentName, false);

                fileInfo = io.GetFileInfo(itemPath);
                if (fileInfo != null && fileInfo.Exists)
                    fileExists = true;
                else
                {
                    fileInfo = io.GetDirectoryInfo(itemPath);
                    if (fileInfo != null && fileInfo.Exists)
                        dirExists = true;
                }

                if (fileExists || dirExists)
                {
                    if (conflictBehavior == ConflictBehavior.Fail)
                    {
                        apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                        return Task.FromResult((WanPath.BusinessManager.FileSystemInfoEx)null);
                    }
                    else
                    if (conflictBehavior == ConflictBehavior.Replace)
                    {
                        if (fileExists)
                        {
                            if (io.DeleteFile(itemPath))
                                break; // we can proceed as the path is free
                            else
                            {
                                apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_FILE_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                                return Task.FromResult((WanPath.BusinessManager.FileSystemInfoEx)null);
                            }
                        }
                        else
                        if (dirExists)
                        {
                            return Task.FromResult(fileInfo);
                        }
                    }
                    else
                    {
                        i++;
                        continue;
                    }
                }
                else
                    break;
            }
            if (io.CreateSubdirectory(path, currentName, false))
            {
                if (itemPath == null)
                    itemPath = new ItemPath(path + currentName, false);
                return Task.FromResult(io.GetDirectoryInfo(itemPath));
            }

            apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, APITextMessages.ERROR_BACKEND_OPERATION_FAILED);
            return Task.FromResult((WanPath.BusinessManager.FileSystemInfoEx)null);
        }

#else
        private static Task<FileSystemInfo> InternalCreateFileAsync(APIContext apiContext,
            string path, string filename, string extension, bool createContent, ConflictBehavior conflictBehavior, CancellationToken cancellationToken)
        {
            string currentName = "";

            string suffix = "";
            string refExt = string.Empty;
            bool fileExists = false;
            bool dirExists = false;

            // Convert logical path to physical path
            string physicalBasePath = GetFullPath(path);
            
            // maybe the extension is not set but comes in a filename, in which case we split the filename
            if (createContent && string.IsNullOrEmpty(extension))
            {
                refExt = Path.GetExtension(filename);
                if (string.IsNullOrEmpty(refExt))
                    refExt = string.Empty;
                else
                {
                    if (refExt.StartsWith("."))
                        refExt = refExt.Substring(1);
                    extension = refExt;
                    filename = Path.GetFileNameWithoutExtension(filename);
                }
            }

            FileSystemInfo fileInfo = null;

            string itemPath = null;
            int i = 0;

            // A loop to choose a non-occupied file name
            while (true)
            {
                // Reset flags for each iteration
                fileExists = false;
                dirExists = false;

                if (i == 0)
                    suffix = "";
                else
                    suffix = " " + i.ToString();

                if (createContent)
                {
                    if (extension.Equals("docx", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("doc", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("dotx", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (extension.Equals("dotx", StringComparison.InvariantCultureIgnoreCase))
                            extension = "docx";

                        if (string.IsNullOrEmpty(filename))
                            filename = "New Microsoft Word Document";
                    }
                    else
                    if (extension.Equals("xlsx", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("xls", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("xltx", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (extension.Equals("xltx", StringComparison.InvariantCultureIgnoreCase))
                            extension = "xlsx";

                        if (string.IsNullOrEmpty(filename))
                            filename = "New Microsoft Excel Document";
                    }
                    else
                    if (extension.Equals("pptx", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("ppt", StringComparison.InvariantCultureIgnoreCase)
                        || extension.Equals("potx", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (extension.Equals("potx", StringComparison.InvariantCultureIgnoreCase))
                            extension = "pptx";

                        if (string.IsNullOrEmpty(filename))
                            filename = "New Microsoft PowerPoint Presentation";
                    }
                    else
                    {
                        apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, APIConstants.REQUEST_PARAM_EXTENSION, extension));
                        return Task.FromResult((FileSystemInfo)null);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(filename) && string.IsNullOrEmpty(extension))
                        currentName = APIConstants.NAME_NEW_FILE;
                }


                if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(extension))
                    currentName = string.Concat(filename, suffix, ".", extension);
                else
                if (!string.IsNullOrEmpty(filename))
                    currentName = string.Concat(filename, suffix);
                else
                if (!string.IsNullOrEmpty(extension))
                    currentName = string.Concat(APIConstants.NAME_NEW_FILE + suffix, ".", extension);
                else
                {
                    apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_MISSING_PARAMS_S2, APIConstants.REQUEST_PARAM_NAME, APIConstants.REQUEST_PARAM_EXTENSION));
                    return Task.FromResult((FileSystemInfo)null);
                }

                itemPath = Path.Combine(physicalBasePath, currentName);
                fileInfo = new FileInfo(itemPath);

                if (fileInfo != null && fileInfo.Exists)
                    fileExists = true;
                else
                {
                    fileInfo = new DirectoryInfo(itemPath);
                    if (fileInfo != null && fileInfo.Exists)
                        dirExists = true;
                }

                if (fileExists || dirExists)
                {

                    if (conflictBehavior == ConflictBehavior.Fail)
                    {
                        apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                        return Task.FromResult((FileSystemInfo)null);
                    }
                    else
                    if (conflictBehavior == ConflictBehavior.Replace)
                    {
                        if (dirExists) // a file cannot replace a directory
                        {
                            apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_DIR_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                            return Task.FromResult((FileSystemInfo)null);
                        }
                        else
                        if (fileExists)
                        {
                            var deleted = true;
                            try
                            {
                                File.Delete(itemPath);
                            }
                            catch
                            {
                                deleted = false;
                            }

                            if (deleted)
                                break; // we can proceed as the path is free
                            else
                            {
                                apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_FILE_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                                return Task.FromResult((FileSystemInfo)null);
                            }
                        }
                        break;
                    }
                    else
                    {
                        i++;
                        continue;
                    }
                }
                else
                    break;
            }


            // create the file
            if (itemPath == null)
                itemPath = Path.Combine(physicalBasePath, currentName);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(itemPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (File.Create(itemPath)) { }

                // optionally fill the file with content
                // TBD:
                /*if (createContent)
                {
                    Api1FileController.WriteOfficeFileContentsInternalAsync(client, io, itemPath, extension, cancellationToken);
                }*/

            fileInfo = new FileInfo(itemPath);

            if (fileInfo == null || !fileInfo.Exists)
            {
                fileInfo = new DirectoryInfo(itemPath);
            }

            return Task.FromResult(fileInfo);
        }

        private static Task<FileSystemInfo> InternalCreateDirectoryAsync(APIContext apiContext,
            string path, string filename, string extension, ConflictBehavior conflictBehavior, CancellationToken cancellationToken)
        {
            string currentName = "";

            string suffix = "";

            bool fileExists = false;
            bool dirExists = false;

            // Convert logical path to physical path
            string physicalBasePath = GetFullPath(path);

            FileSystemInfo fileInfo = null;

            string itemPath = null;
            int i = 0;

            while (true)
            {
                // Reset flags for each iteration
                fileExists = false;
                dirExists = false;

                if (i == 0)
                    suffix = "";
                else
                    suffix = " " + i.ToString();

                if (string.IsNullOrEmpty(filename) && string.IsNullOrEmpty(extension))
                    currentName = APIConstants.NAME_NEW_FOLDER;
                else
                if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(extension))
                    currentName = string.Concat(filename, suffix, ".", extension);
                else
                if (!string.IsNullOrEmpty(filename))
                    currentName = string.Concat(filename, suffix);
                else
                if (!string.IsNullOrEmpty(extension))
                    currentName = string.Concat(suffix, ".", extension);
                else
                {
                    apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_MISSING_PARAMS_S2, APIConstants.REQUEST_PARAM_NAME, APIConstants.REQUEST_PARAM_EXTENSION));
                    return Task.FromResult((FileSystemInfo)null);
                }

                itemPath = Path.Combine(physicalBasePath, currentName);

                fileInfo = new FileInfo(itemPath);
                if (fileInfo != null && fileInfo.Exists)
                    fileExists = true;
                else
                {
                    fileInfo = new DirectoryInfo(itemPath);
                    if (fileInfo != null && fileInfo.Exists)
                        dirExists = true;
                }

                if (fileExists || dirExists)
                {
                    if (conflictBehavior == ConflictBehavior.Fail)
                    {
                        apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                        return Task.FromResult((FileSystemInfo)null);
                    }
                    else
                    if (conflictBehavior == ConflictBehavior.Replace)
                    {
                        if (fileExists)
                        {
                            var deleted = true;
                            try
                            {
                                File.Delete(itemPath);
                            }
                            catch
                            {
                                deleted = false;
                            }

                            if (deleted)
                                break; // we can proceed as the path is free
                            else
                            {
                                apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_FILE_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, "Create Directory", currentName));
                                return Task.FromResult((FileSystemInfo)null);
                            }
                        }
                        else
                        if (dirExists)
                        {
                            return Task.FromResult(fileInfo);
                        }
                    }
                    else
                    {
                        i++;
                        continue;
                    }
                }
                else
                    break;
            }

            try
            {
                // Ensure parent directory exists
                if (!Directory.Exists(physicalBasePath))
                    Directory.CreateDirectory(physicalBasePath);
                    
                DirectoryInfo dirInfo = Directory.CreateDirectory(Path.Combine(physicalBasePath, currentName));
                return Task.FromResult((FileSystemInfo)dirInfo);
            }
            catch (Exception)
            {
                apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, APITextMessages.ERROR_BACKEND_OPERATION_FAILED);
                return Task.FromResult((FileSystemInfo)null);
            }
        }

#endif

        private static Task InternalIsFolderAccessibleAsync(APIContext apiContext, string folderPath, CancellationToken cancellationToken = default)
        {
            try
            {
#if !MOCK_SERVER
                var client = GetMwdClient(apiContext.SessionGUID);
                if (SessionExpired(apiContext, client))
                    return Task.CompletedTask;

                InputOutput io = new InputOutput(client);
                bool result = io.IsDirectoryAccessible(folderPath);
#else
                bool result = Directory.Exists(folderPath);
#endif

                Dictionary<string, object> response = new Dictionary<string, object>
                    {
                        { APIConstants.RESPONSE_VALUE_NAME_VALUE, result },
                    };
                apiContext.ResponseObject = response;
                apiContext.HttpStatusCode = StatusCodes.Status200OK;
            }
            catch (Exception ex)
            {
#if !MOCK_SERVER
                Log.Exc(ex);
#endif
                apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(ex, ""));
            }
            return Task.CompletedTask;
        }

        internal static Task InternalHasWritePermissionsAsync(APIContext apiContext, string path, CancellationToken cancellationToken = default)
        {
            try
            {
#if !MOCK_SERVER
                MwdClient client = GetMwdClient(apiContext.SessionGUID);

                if (SessionExpired(apiContext, client))
                    return Task.CompletedTask;

                _log.Debug($"APIHandler.InternalHasWritePermissionsAsync, path: {path}");

                InputOutput io = new InputOutput(client);

                bool result = io.HasWritePermissions(new ItemPath(path, false));
#else
                bool result = true;
#endif

                // This dictionary will be converted to JSON and sent to the client in the APIHandler.ProcessRequestAsync method
                Dictionary<string, object> response = new Dictionary<string, object>
                    {
                        { APIConstants.RESPONSE_VALUE_NAME_VALUE, result },
                    };
                apiContext.ResponseObject = response;
                apiContext.HttpStatusCode = StatusCodes.Status200OK;
            }
            catch (Exception ex)
            {
#if !MOCK_SERVER
                Log.Exc(ex);
#endif
                apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(ex, ""));
            }
            return Task.CompletedTask;
        }

        private static string InternalGenerateOneTimePassword(APIContext apiContext)
        {
            try
            {
#if !MOCK_SERVER
                var client = GetMwdClient(apiContext.SessionGUID);
                if (SessionExpired(apiContext, client))
                    return null;

                Cache cache = new Cache();

                return cache.GenerateOneTimePassword(apiContext.SessionID);
#else
                return "1234567890";
#endif
            }
            catch (Exception ex)
            {
#if !MOCK_SERVER
                Log.Exc(ex);
#endif
                apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(ex, ""));
                return null;
            }
        }

        private static async Task InternalEditIsFinishedAsync(APIContext apiContext, string path, CancellationToken cancellationToken = default)
        {
#if !MOCK_SERVER
            try
            {
                Log.Msg($"EditIsFinished, sessionId: {apiContext.SessionID}, path: {path}");

                var client = GetMwdClient(apiContext.SessionGUID);

                if (SessionExpired(apiContext, client))
                    return;

                //format path because different clients can call this method, input paths can be in different format
                path = WopiUtil.GetFormatedDocPath(path);

                string apiUrl = ConstructOfOnShPoUrl(apiContext, path, "EditIsFinished");

                if (apiUrl == null)
                {
                    Log.Msg($"OfOnShPo is not enabled. Ignoring the notification of edit finished");
                    return;
                }

                Log.Msg($"EditIsFinished, PostAsync, apiUrl: {apiUrl}");

                var response = await (new HttpClient()).PostAsync(apiUrl, null, cancellationToken);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                if (content.Contains("The process cannot access the file"))
                {
                    _log.Error(content);
                    apiContext.SetError(APIErrorCodes.PROCESS_CANT_ACCESS_FILE, content);
                    return;
                }

                var io = new InputOutput(client);

                io.DeleteStreamLock(path);

                Log.Msg($"Lock released for {path}");
            }
            catch (OperationCanceledException)
            {
                apiContext.APIErrorCode = StatusCodes.Status418ImATeapot; // connection closed by the client
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(ex, ""));
            }
#else
            return;
#endif
        }

#if !MOCK_SERVER
        private static async Task<WanPath.BusinessManager.FileSystemInfoEx> InternalGetFileInformationAsync(APIContext apiContext, InputOutput io, string path, ItemPath itemPath, bool? isFolder, CancellationToken cancellationToken)
        {
            WanPath.BusinessManager.FileSystemInfoEx result = await InternalGetFileInformationNoPostProcessingAsync(apiContext, io, path, itemPath, isFolder, cancellationToken);
            if (result != null)
            {
                ProcessDLP(apiContext, path, result);
                FixMissingDates(result);
            }
            return result;
        }

        private static Task<WanPath.BusinessManager.FileSystemInfoEx> InternalGetFileInformationNoPostProcessingAsync(APIContext apiContext, InputOutput io, string path, ItemPath itemPath, bool? isFolder, CancellationToken cancellationToken)
        {
            WanPath.BusinessManager.FileSystemInfoEx result = null;

            if (itemPath == null)
                itemPath = new ItemPath(path, false);

            if (!isFolder.HasValue || (isFolder.Value == false))
            {
                result = io.GetFileInfo(itemPath);
            }
            if (result == null && (!isFolder.HasValue || (isFolder.Value == true)))
            {
                result = io.GetDirectoryInfo(itemPath);
            }
            return Task.FromResult(result);
        }
#else
        private static async Task<FileSystemInfo> InternalGetFileInformationAsync(APIContext apiContext, string path, bool? isFolder, CancellationToken cancellationToken)
        {
            FileSystemInfo result = await InternalGetFileInformationNoPostProcessingAsync(apiContext, path, isFolder, cancellationToken);
            if (result != null)
            {
                FixMissingDates(result);
            }
            return result;
        }

        private static Task<FileSystemInfo> InternalGetFileInformationNoPostProcessingAsync(APIContext apiContext, string path, bool? isFolder, CancellationToken cancellationToken)
        {
            FileSystemInfo result = null;
            
            // Convert logical API path to physical file system path
            var fullPath = GetFullPath(path);

            if (!isFolder.HasValue || (isFolder.Value == false))
            {
                result = new FileInfo(fullPath);
            }
            if (result == null && (!isFolder.HasValue || (isFolder.Value == true)))
            {
                result = new DirectoryInfo(fullPath);
            }
            return Task.FromResult(result);
        }
#endif

        private static async Task InternalReleaseLockForOfficeAsync(APIContext apiContext, string path, CancellationToken cancellationToken = default)
        {
#if !MOCK_SERVER
            string sessionId = apiContext.SessionID;
            Log.Msg($"Try release lock {sessionId} {path}");

            try
            {
                var client = GetMwdClient(apiContext.SessionGUID);

                if (SessionExpired(apiContext, client))
                    return;

                path = StandardizePath(path);

                //format path because different clients can call this method, input paths can be in different format
                var webDavFilePath = WopiUtil.GetFormatedDocPath(path);

                string apiUrl = ConstructOfOnShPoUrl(apiContext, webDavFilePath, "LockReleased");

                if (apiUrl == null)
                {
                    Log.Msg("OfOnShPo is not enabled. Ignore notification of lock released.");
                    return;
                }

                Log.Msg($"LockReleased notify OfOnShPo at {apiUrl}");

                var response = await (new HttpClient()).PostAsync(apiUrl, null, cancellationToken);

                Log.Msg("LockReleased OfOnShPo Response:" + await response.Content.ReadAsStringAsync());

                response.EnsureSuccessStatusCode();

                var io = new InputOutput(client);

                // todo: handle the negative result returned by this method
                io.DeleteStreamLock(webDavFilePath);

                Log.Msg($"Lock released for {path}");
            }
            catch (OperationCanceledException)
            {
                apiContext.APIErrorCode = StatusCodes.Status418ImATeapot; // connection closed by the client
            }
            catch (Exception ex)
            {
                Log.Exc(ex);
                // todo: the internal methods behind DeleteStreamLock throw a generic exception in the case of any error.
                // It is necessary to convert that generic exception into something meaningful, with an appropriate API error code and description.
                apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(ex, ""));
            }
#else
            return;
#endif
        }

        internal static async Task InternalCheckEditSessionStatusAsync(APIContext apiContext, string path, CancellationToken cancellationToken = default)
        {
#if !MOCK_SERVER
            string sessionId = apiContext.SessionID;
            Log.Msg($"IsSessionActive {sessionId} {path}");

            try
            {
                //check if this is mobile request
                //var userAgent = apiContext.Request.Headers.GetValue("User-Agent");
                //bool isMobileRequest = UserAgentHelper.IsMobile(userAgent);

                //format path because different clients can call this method, input paths can be in different format
                path = WopiUtil.GetFormatedDocPath(path);

                string apiUrl = ConstructOfOnShPoUrl(apiContext, path, "IsSessionActive");

                Log.Msg($"IsSessionActive check OfOnShPo session at {apiUrl}");

                string ofOnShPoResponse = "True";

                if (apiUrl != null)
                {
                    var response = await (new HttpClient()).GetAsync(apiUrl, cancellationToken);

                    response.EnsureSuccessStatusCode();

                    ofOnShPoResponse = await response.Content.ReadAsStringAsync();
                }
                Cache cache = new Cache();

                var session = cache.Session;
                var user = cache.Session.UserGetBySessionId(sessionId);
                var sessionExpiration = session.SessionExpirationGet(sessionId);
                var secondsToSessionExpiration = sessionExpiration == null ? 0 : (sessionExpiration.Value - DateTime.Now).TotalSeconds;

                Log.Msg($"IsSessionActive, sessionId: {sessionId}, active: {user != null}, ofOnShPoResponse: {ofOnShPoResponse}, sessionExpiration: {sessionExpiration}");

                // This dictionary will be converted to JSON and sent to the client in the APIHandler.ProcessRequestAsync method
                apiContext.ResponseObject = new Dictionary<string, object>
                    {
                        { APIConstants.RESPONSE_VALUE_NAME_ACTIVE, user != null },
                        { APIConstants.RESPONSE_VALUE_NAME_EXPIRES_AFTER, (long) secondsToSessionExpiration },
                        { APIConstants.RESPONSE_VALUE_NAME_SHAREPOINT_RESPONSE, ofOnShPoResponse },
                    };
                apiContext.HttpStatusCode = StatusCodes.Status200OK;
            }
            catch (OperationCanceledException)
            {
                apiContext.APIErrorCode = StatusCodes.Status418ImATeapot; // connection closed by the client
            }
            catch (Exception ex)
            {
                Log.Exc(ex);
                apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(ex, ""));
            }
#else
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
#endif
        }

        private static Stream InternalGetInputStream(APIContext apiContext)
        {
            Stream result;
#if !NETCOREAPP
            switch (apiContext.Request.ReadEntityBodyMode)
            {
                case System.Web.ReadEntityBodyMode.Bufferless:
                    result = apiContext.Request.GetBufferlessInputStream(true);
                    break;
                case System.Web.ReadEntityBodyMode.Buffered:
                    result = apiContext.Request.GetBufferedInputStream();
                    break;
                default:
                    result = apiContext.Request.InputStream;
                    break;
            }
#else
            var request = apiContext.Request;

            request.EnableBuffering();
            request.Body.Position = 0;

            result = request.Body;
#endif
            return result;
        }

        private static async Task<byte[]> InternalReadPostedBytesAsync(APIContext apiContext)
        {
            byte[] content;
            long? contentLen = apiContext.Request.ContentLength;
            if (contentLen > 0)
            {
                Stream inputStream = InternalGetInputStream(apiContext);

                content = new byte[(long)contentLen];
                int totalRead = 0;
                int thisRead = 0;
                while (true)
                {
                    thisRead = await inputStream.ReadAsync(content, totalRead, (int)(contentLen - totalRead));
                    if (thisRead == 0)
                        break;
                    totalRead += thisRead;
                    if (totalRead == contentLen)
                        break;
                }
            }
            else
                content = new byte[0];
            return content;
        }


        private static bool PickConflictBehaviorParameter(APIContext apiContext, bool mandatory, ConflictBehavior defaultValue, out ConflictBehavior conflictBehavior)
        {
            conflictBehavior = defaultValue;
            string conflictBehaviorStr = string.Empty;
            if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_CONFLICT_BEHAVIOR, mandatory, mandatory, string.Empty, out conflictBehaviorStr))
                return false;

            if (!string.IsNullOrEmpty(conflictBehaviorStr))
            {
                switch (conflictBehaviorStr)
                {
                    case APIConstants.CONFLICT_BEHAVIOR_FAIL:
                        conflictBehavior = ConflictBehavior.Fail;
                        break;
                    case APIConstants.CONFLICT_BEHAVIOR_REPLACE:
                        conflictBehavior = ConflictBehavior.Replace;
                        break;
                    case APIConstants.CONFLICT_BEHAVIOR_RENAME:
                        conflictBehavior = ConflictBehavior.Rename;
                        break;
                    default:
                        _log.Debug(string.Format("The conflictBehavior parameter contains unexpected value {0}", conflictBehaviorStr));
                        apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, conflictBehaviorStr, APIConstants.REQUEST_PARAM_CONFLICT_BEHAVIOR));
                        return false;
                }
                return true;
            }
            else
                return !mandatory;
        }

#if !MOCK_SERVER
        public static bool SessionExpired(APIContext apiContext, WanPath.WebClient.BLL.API.MwdClient client)
        {
            if (!client.CheckIsSessionExpired(false))
            {
                apiContext.SetError(APIErrorCodes.AUTHENTICATION_EXPIRED, APITextMessages.ERROR_USER_SESSION_HAS_EXPIRED);
                return false;
            }
            else
                return true;
        }

        private static void FixMissingDates(WanPath.BusinessManager.FileSystemInfoEx info)
        {
            if (info.CreationTimeUtc == null || info.CreationTimeUtc == DateTime.MinValue)
            {
                info.CreationTimeUtc = DateTime.UtcNow;
            }
            if (info.LastWriteTimeUtc == null || info.LastWriteTimeUtc == DateTime.MinValue)
            {
                info.LastWriteTimeUtc = DateTime.UtcNow;
            }
        }
#else
        private static void FixMissingDates(FileSystemInfo info)
        {
            if (info.CreationTimeUtc == DateTime.MinValue)
            {
                info.CreationTimeUtc = DateTime.UtcNow;
            }
            if (info.LastWriteTimeUtc == DateTime.MinValue)
            {
                info.LastWriteTimeUtc = DateTime.UtcNow;
            }
        }
#endif

#if !MOCK_SERVER
        protected static WanPath.Common.Enums.ClientType GetClientType(string userAgent)
        {
            if (UserAgentHelper.IsMobile(userAgent))
            {
                return WanPath.Common.Enums.ClientType.Mobile;
            }
            else
            {
                return WanPath.Common.Enums.ClientType.Mapper;
            }
        }

        protected static WanPath.Common.Enums.ClientType GetClientType(APIContext apiContext)
        {
            WanPath.Common.Enums.ClientType clientType = WanPath.Common.Enums.ClientType.Mapper;

            string ua = apiContext.Request.Headers.GetValue(APIConstants.HTTP_HEADER_USER_AGENT);
            if (!string.IsNullOrEmpty(ua))
                clientType = GetClientType(ua);

            return clientType;
        }

        private static void ProcessDLP(APIContext apiContext, string path, WanPath.BusinessManager.FileSystemInfoEx info)
        {
            if ((apiContext.Request != null) && (apiContext.Request.Url != null))
            {
                (new DLPResolver(
                    string.Format("{0}://{1}", apiContext.Request.Url.Scheme, apiContext.Request.Url.Host),
                    apiContext.SessionGUID,
                    UserAgentHelper.IsMobile(apiContext.Request.Headers.GetValue(APIConstants.HTTP_HEADER_USER_AGENT)) ? WanPath.Common.Enums.ClientType.Mobile : WanPath.Common.Enums.ClientType.Mapper))
                    .ProcessDLP(info, path);
            }
        }

        private static WanPath.BusinessManager.FileSystemInfoEx GetFileInfo(APIContext apiContext, InputOutput io, string path, ItemPath itemPath)
        {
            var info = io.GetFileInfo(itemPath != null ? itemPath : new ItemPath(path, false));
            ProcessDLP(apiContext, path, info);
            FixMissingDates(info);
            return info;
        }

        static private bool SelectAvailableName(APIContext apiContext, InputOutput io, string path, bool isFolder, ConflictBehavior conflictBehavior, string operationName, out string availablePath, out ItemPath availableItemPath)
        {
            bool fileExists = false;
            bool dirExists = false;
            string suffix = string.Empty;

            string basepath, ext;

            availablePath = string.Empty;
            availableItemPath = null;

            int i = 0;
            string currentName;

            StrUtils.SplitPathNameAndExt(path, out basepath, out ext);

            WanPath.BusinessManager.FileSystemInfoEx fileInfo = null;

            // A loop to choose a non-occupied file name
            while (true)
            {
                if (i == 0)
                    suffix = "";
                else
                    suffix = " " + i.ToString();

                // compose the correct name with a possible suffix
                currentName = string.Concat(basepath, suffix, ext);

                // check if a file or directory exists
                availableItemPath = new ItemPath(currentName, false);
                fileInfo = io.GetFileInfo(availableItemPath);
                if (fileInfo != null && fileInfo.Exists)
                    fileExists = true;
                else
                {
                    fileInfo = io.GetDirectoryInfo(availableItemPath);
                    if (fileInfo != null && fileInfo.Exists)
                        dirExists = true;
                }

                if (fileExists || dirExists)
                {
                    if (conflictBehavior == ConflictBehavior.Fail)
                    {
                        apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, operationName, currentName));
                        return false;
                    }
                    else
                    if (conflictBehavior == ConflictBehavior.Replace)
                    {
                        if ((!isFolder) && dirExists) // a file cannot replace a directory
                        {
                            apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_DIR_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, operationName, currentName));
                            return false;
                        }
                        else
                        if (fileExists)
                        {
                            if (io.DeleteFile(availableItemPath))
                                break; // we can proceed as the path is free
                            else
                            {
                                apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS, string.Format(APITextMessages.ERROR_FILE_WITH_GIVEN_NAME_ALREADY_EXISTS_S2, operationName, currentName));
                                return false;
                            }
                        }
                        break;
                    }
                    else
                    {
                        i++;
                        continue;
                    }
                }
                else
                    break;
            }
            availablePath = currentName;
            return true;
        }

#else

        static private bool SelectAvailableName(
            APIContext apiContext,
            string path,
            bool isFolder,
            ConflictBehavior conflictBehavior,
            string operationName,
            out string availablePath)
        {
            availablePath = path;
            bool exists;
            // Use forward slashes for API paths - don't use Path.GetDirectoryName which gives mixed slashes
            int lastSlash = path.LastIndexOf('/');
            string directory = lastSlash >= 0 ? path.Substring(0, lastSlash) : "";
            string fileName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;

            int lastDot = fileName.LastIndexOf('.');
            string baseName = lastDot >= 0 ? fileName.Substring(0, lastDot) : fileName;
            string ext = lastDot >= 0 ? fileName.Substring(lastDot) : "";

            int i = 0;
            string candidate;

            while (true)
            {
                // build "Name.ext" or "Name 1.ext", "Name 2.ext", …
                string suffix = i == 0 ? "" : " " + i;
                candidate = string.IsNullOrEmpty(directory) ? baseName + suffix + ext : directory + "/" + baseName + suffix + ext;

                // Convert logical path to physical path for existence check
                var physicalCandidate = GetFullPath(candidate);
                bool fileExists = File.Exists(physicalCandidate);
                bool dirExists = Directory.Exists(physicalCandidate);
                exists = fileExists || dirExists;

                if (!exists)
                {
                    // free to use
                    availablePath = candidate;
                    return true;
                }

                // handle conflict
                switch (conflictBehavior)
                {
                    case ConflictBehavior.Fail:
                        apiContext.SetError(
                            APIErrorCodes.OBJECT_ALREADY_EXISTS,
                            string.Format(
                                APITextMessages.ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2,
                                operationName,
                                Path.GetFileName(candidate)));
                        return false;

                    case ConflictBehavior.Replace:
                        // you can't replace a directory when expecting a file
                        if (!isFolder && dirExists)
                        {
                            apiContext.SetError(
                                APIErrorCodes.OBJECT_ALREADY_EXISTS,
                                string.Format(
                                    APITextMessages.ERROR_DIR_WITH_GIVEN_NAME_ALREADY_EXISTS_S2,
                                    operationName,
                                    Path.GetFileName(candidate)));
                            return false;
                        }

                        try
                        {
                            if (fileExists)
                                File.Delete(physicalCandidate);
                            else
                                Directory.Delete(physicalCandidate, recursive: true);
                        }
                        catch (Exception ex)
                        {
                            apiContext.SetError(
                                APIErrorCodes.OBJECT_ALREADY_EXISTS,
                                string.Format(
                                    fileExists
                                        ? APITextMessages.ERROR_FILE_WITH_GIVEN_NAME_ALREADY_EXISTS_S2
                                        : APITextMessages.ERROR_DIR_WITH_GIVEN_NAME_ALREADY_EXISTS_S2,
                                    operationName,
                                    Path.GetFileName(candidate))
                                + " (" + ex.Message + ")");
                            return false;
                        }

                        // deletion succeeded, path is now free
                        availablePath = candidate;
                        return true;

                    case ConflictBehavior.Rename:
                    default:
                        // bump suffix and retry
                        i++;
                        continue;
                }
            }
        }

        // Helper method for same-path rename: always generates a new numbered name
        static private bool SelectAvailableNameForDuplicate(
            APIContext apiContext,
            string path,
            bool isFolder,
            out string availablePath)
        {
            availablePath = path;
            // Use forward slashes for API paths - don't use Path.GetDirectoryName which gives mixed slashes
            int lastSlash = path.LastIndexOf('/');
            string directory = lastSlash >= 0 ? path.Substring(0, lastSlash) : "";
            string fileName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;

            int lastDot = fileName.LastIndexOf('.');
            string baseName = lastDot >= 0 ? fileName.Substring(0, lastDot) : fileName;
            string ext = lastDot >= 0 ? fileName.Substring(lastDot) : "";

            int i = 1; // Start from 1 to generate "filename 1.ext"

            while (true)
            {
                // build "Name 1.ext", "Name 2.ext", …
                string suffix = " " + i;
                string candidate = string.IsNullOrEmpty(directory) ? baseName + suffix + ext : directory + "/" + baseName + suffix + ext;

                // Convert logical path to physical path for existence check
                var physicalCandidate = GetFullPath(candidate);
                bool fileExists = File.Exists(physicalCandidate);
                bool dirExists = Directory.Exists(physicalCandidate);
                bool exists = fileExists || dirExists;

                if (!exists)
                {
                    // free to use
                    availablePath = candidate;
                    return true;
                }

                // bump suffix and retry
                i++;

                // Safety check to prevent infinite loops
                if (i > 1000)
                {
                    apiContext.SetError(APIErrorCodes.OBJECT_ALREADY_EXISTS,
                        "Unable to generate a unique name after 1000 attempts");
                    return false;
                }
            }
        }
#endif

        protected static bool PickParameter(APIContext apiContext, string name, bool mustExist,
            bool defaultValue, out bool value)
        {
            bool? readValue;

            bool result = PickParameter(apiContext, name, mustExist, defaultValue, out readValue);
            value = readValue.HasValue ? readValue.Value : false;

            return result;
        }

#endregion

        #region New Enhanced Handlers
        
        private static async Task HandleListSharesAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            var shares = ShareManager.GetAllShares();
            var sharesList = shares.Values.Select(share => new Dictionary<string, object>
            {
                ["shareName"] = share.ShareName,
                ["driveLetter"] = share.DriveLetter,
                ["downloadEnabled"] = share.DownloadEnabled,
                ["desktopClientEnabled"] = share.DesktopClientEnabled,
                ["webClientEnabled"] = share.WebClientEnabled
            }).ToList();

            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = new Dictionary<string, object>
            {
                ["useMultipleDriveLetters"] = false,
                ["shares"] = sharesList
            };
#endif
        }

        private static async Task HandleListBookmarksAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            var maxResults = apiContext.RequestParameters.TryGetValue("maxResults", out var maxObj) && 
                           long.TryParse(maxObj?.ToString(), out var max) ? (int)max : int.MaxValue;
            
            var bookmarksList = _bookmarks.Values.Take(maxResults).ToList();
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = bookmarksList;
#endif
        }

        private static async Task HandleAddBookmarkAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            string path = apiContext.RequestParameters.TryGetValue("path", out var pathObj) ? pathObj?.ToString() : null;
            
            if (string.IsNullOrEmpty(path))
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "Path parameter is required");
                return;
            }

            var bookmarkId = _nextBookmarkId++;
            var bookmark = new Dictionary<string, object>
            {
                ["id"] = bookmarkId,
                ["username"] = "mockuser",
                ["path"] = path
            };

            _bookmarks[path] = bookmark;
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = bookmark;
#endif
        }

        private static async Task HandleDeleteBookmarkAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            string path = apiContext.RequestParameters.TryGetValue("path", out var pathObj) ? pathObj?.ToString() : null;
            
            if (string.IsNullOrEmpty(path))
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "Path parameter is required");
                return;
            }

            _bookmarks.Remove(path);
            apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
            await SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
#endif
        }

        private static async Task HandleGetItemTypeAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            string path = apiContext.RequestParameters.TryGetValue("path", out var pathObj) ? pathObj?.ToString() : null;
            
            if (string.IsNullOrEmpty(path))
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "Path parameter is required");
                return;
            }

            var fullPath = GetFullPath(path);
            
            string itemType;
            if (File.Exists(fullPath))
            {
                itemType = ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FILE;
            }
            else if (Directory.Exists(fullPath))
            {
                itemType = ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FOLDER;
            }
            else
            {
                itemType = ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_UNKNOWN;
            }

            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = new Dictionary<string, object>
            {
                ["value"] = itemType
            };
#endif
        }

        private static async Task HandleCreatePublicLinkAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            string path = apiContext.RequestParameters.TryGetValue("path", out var pathObj) ? pathObj?.ToString() : null;
            
            if (string.IsNullOrEmpty(path))
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "Path parameter is required");
                return;
            }

            var linkInfo = new Dictionary<string, object>
            {
                ["path"] = path,
                ["username"] = "mockuser",
                ["isValid"] = true,
                ["isFolder"] = Directory.Exists(GetFullPath(path)),
                ["created"] = DateTime.UtcNow,
                ["expires"] = DateTime.UtcNow.AddDays(30),
                ["hasPassword"] = apiContext.RequestParameters.ContainsKey("password"),
                ["allowDownloading"] = apiContext.RequestParameters.TryGetValue("allowDownloading", out var downloadObj) && 
                                     bool.TryParse(downloadObj?.ToString(), out var download) && download,
                ["allowUploading"] = apiContext.RequestParameters.TryGetValue("allowUploading", out var uploadObj) && 
                                   bool.TryParse(uploadObj?.ToString(), out var upload) && upload,
                ["allowEditing"] = apiContext.RequestParameters.TryGetValue("allowEditing", out var editObj) && 
                                 bool.TryParse(editObj?.ToString(), out var edit) && edit,
                ["maxNumberOfDownloads"] = 0,
                ["currentDownloads"] = 0,
                ["hasExternalLink"] = false,
                ["lastAccessed"] = DateTime.UtcNow
            };

            var publicLink = PublicLinkManager.CreateLink(path, linkInfo);

            // Return based on Accept header
            string acceptHeader = apiContext.Request.Headers["Accept"].FirstOrDefault();
            if (acceptHeader?.Contains("application/json") == true)
            {
                apiContext.ResponseObject = new Dictionary<string, object> { ["value"] = publicLink };
            }
            else
            {
                apiContext.Response.ContentType = "text/plain";
                await apiContext.Response.WriteAsync(publicLink, cancellationToken);
                apiContext.ResponseSent = true;
            }

            apiContext.HttpStatusCode = 201; // Created
#endif
        }

        private static async Task HandleGetPublicLinkInfoAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            string link = apiContext.RequestParameters.TryGetValue("link", out var linkObj) ? linkObj?.ToString() : null;
            
            if (string.IsNullOrEmpty(link))
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "Link parameter is required");
                return;
            }

            var linkInfo = PublicLinkManager.GetLinkInfo(link);
            if (linkInfo != null)
            {
                apiContext.HttpStatusCode = StatusCodes.Status200OK;
                apiContext.ResponseObject = linkInfo;
            }
            else
            {
                apiContext.HttpStatusCode = 404;
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, "Public link not found");
            }
#endif
        }

        private static async Task HandleDeletePublicLinksAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            if (apiContext.Request.Method == "GET")
            {
                // Single link deletion
                string link = apiContext.RequestParameters.TryGetValue("link", out var linkObj) ? linkObj?.ToString() : null;
                
                if (string.IsNullOrEmpty(link))
                {
                    apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "Link parameter is required");
                    return;
                }

                PublicLinkManager.DeleteLink(link);
            }
            else if (apiContext.Request.Method == "POST")
            {
                // Multiple links deletion
                if (apiContext.RequestParameters.TryGetValue("links", out var linksObj) && linksObj is JsonElement linksElement)
                {
                    foreach (var linkElement in linksElement.EnumerateArray())
                    {
                        var link = linkElement.GetString();
                        if (!string.IsNullOrEmpty(link))
                        {
                            PublicLinkManager.DeleteLink(link);
                        }
                    }
                }
            }

            apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
            await SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
#endif
        }

        private static async Task HandleCheckSessionAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            // Check that the session ID is provided
            if (!CheckSessionID(apiContext))
                return;

            // For mock implementation, validate session format and return success
            apiContext.HttpStatusCode = 200;
            apiContext.ResponseObject = new Dictionary<string, object>
            {
                ["status"] = "active",
                ["sessionId"] = apiContext.SessionID
            };
#endif
        }

        private static async Task HandleZipFilesAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            // Check that the session ID is provided
            if (!CheckSessionID(apiContext))
                return;

            // Handle different HTTP methods
            bool isPost = string.Equals(apiContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
            List<string> filesToZip = new List<string>();

            if (isPost)
            {
                // POST method - get paths from request body
                // The JSON parsing flattens arrays into indexed parameters: paths[0], paths[1], etc.
                if (apiContext.RequestParameters.TryGetValue("paths.Count", out var countObj) && countObj is int pathsCount)
                {
                    for (int i = 0; i < pathsCount; i++)
                    {
                        string pathKey = $"paths[{i}]";
                        if (apiContext.RequestParameters.TryGetValue(pathKey, out var pathObj) && pathObj?.ToString() is string path && !string.IsNullOrEmpty(path))
                        {
                            filesToZip.Add(path);
                        }
                    }
                }

                if (filesToZip.Count == 0)
                {
                    apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "At least one file path must be specified in the paths array");
                    return;
                }
            }
            else
            {
                // GET method - check for ref parameter first
                string refParameter = null;
                PickParameter(apiContext, "ref", false, false, string.Empty, out refParameter);

                if (!string.IsNullOrEmpty(refParameter))
                {
                    // Handle ZIP reference retrieval
                    ZipReferenceData zipRefData;
                    lock (_zipReferences)
                    {
                        if (!_zipReferences.TryGetValue(refParameter, out zipRefData))
                        {
                            apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, $"ZIP reference not found: {refParameter}");
                            return;
                        }

                        // Clean up the reference after use (optional - could be kept for multiple downloads)
                        _zipReferences.Remove(refParameter);
                    }

                    // Return the stored ZIP data
                    apiContext.HttpStatusCode = zipRefData.MissingFiles.Count > 0 ? 206 : StatusCodes.Status200OK;
                    apiContext.Response.ContentType = "application/octet-stream";
                    apiContext.Response.Headers["Content-Disposition"] = $"attachment; filename=\"files.zip\"";
                    await apiContext.Response.Body.WriteAsync(zipRefData.Data, 0, zipRefData.Data.Length, cancellationToken);
                    apiContext.ResponseSent = true;
                    return;
                }

                // If no ref parameter, require path parameter
                if (!PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, false, false, string.Empty, out string folderPath) || string.IsNullOrEmpty(folderPath))
                {
                    apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "Either 'path' or 'ref' parameter must be provided");
                    return;
                }

                string mask = "*";
                PickParameter(apiContext, "mask", false, false, "*", out mask);

                // Get files from the specified folder
                string fullFolderPath;
                try
                {
                    fullFolderPath = GetFullPath(folderPath);
                }
                catch (Exception)
                {
                    apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, folderPath));
                    return;
                }

                if (!Directory.Exists(fullFolderPath))
                {
                    apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, string.Format(APITextMessages.ERROR_PATH_NOT_FOUND_S, folderPath));
                    return;
                }

                try
                {
                    var files = Directory.GetFiles(fullFolderPath, mask, SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        var relativePath = ConvertPhysicalPathToLogical(file);
                        filesToZip.Add(relativePath);
                    }
                }
                catch (Exception ex)
                {
                    apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, $"Failed to enumerate files: {ex.Message}");
                    return;
                }
            }

            // If no files found, return 204 No Content
            if (filesToZip.Count == 0)
            {
                apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
                await SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
                return;
            }

            // Get response type parameter
            string respondWith = "ref"; // default to reference
            PickParameter(apiContext, "respondWith", false, false, "ref", out respondWith);

            // Get zip name parameter
            string zipName = null;
            PickParameter(apiContext, "zipName", false, false, string.Empty, out zipName);

            // Get conflict behavior parameter
            ConflictBehavior conflictBehavior = ConflictBehavior.Fail;
            PickConflictBehaviorParameter(apiContext, false, ConflictBehavior.Fail, out conflictBehavior);

            string zipReference = null;
            byte[] zipData = null;
            string zipPath = null;
            List<string> foundFiles = new List<string>();
            List<string> missingFiles = new List<string>();

            try
            {
                // Create ZIP using System.IO.Compression
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        foreach (var filePath in filesToZip)
                        {
                            try
                            {
                                var fullPath = GetFullPath(filePath);
                                bool fileExists = File.Exists(fullPath);
                                bool dirExists = Directory.Exists(fullPath);

                                if (fileExists)
                                {
                                    // Handle file
                                    var fileName = Path.GetFileName(fullPath);
                                    var entry = archive.CreateEntry(fileName);

                                    using (var entryStream = entry.Open())
                                    using (var fileStream = File.OpenRead(fullPath))
                                    {
                                        await fileStream.CopyToAsync(entryStream, cancellationToken);
                                    }

                                    foundFiles.Add(filePath);
                                }
                                else if (dirExists)
                                {
                                    // Handle directory - recursively add all files and subdirectories
                                    await AddDirectoryToZipAsync(archive, fullPath, filePath, foundFiles, cancellationToken);
                                }
                                else
                                {
                                    // File or directory not found - explicitly add to missing files
                                    missingFiles.Add(filePath);
                                }
                            }
                            catch (Exception)
                            {
                                // Any exception means the file couldn't be processed
                                missingFiles.Add(filePath);
                            }
                        }
                    }

                    zipData = memoryStream.ToArray();
                }

                // If no files were found, return 204 No Content
                if (foundFiles.Count == 0)
                {
                    apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
                    await SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
                    return;
                }

                // Handle different response types
                switch (respondWith.ToLower())
                {
                    case "data":
                        // Return ZIP data directly
                        // For POST requests where not all files were found, return 206 Partial Content
                        bool hasPartialContent = isPost && foundFiles.Count < filesToZip.Count;
                        apiContext.HttpStatusCode = hasPartialContent ? 206 : StatusCodes.Status200OK;
                        apiContext.Response.ContentType = "application/octet-stream";
                        apiContext.Response.Headers["Content-Disposition"] = $"attachment; filename=\"files.zip\"";
                        await apiContext.Response.Body.WriteAsync(zipData, 0, zipData.Length, cancellationToken);
                        apiContext.ResponseSent = true;
                        return;

                    case "path":
                        // Save ZIP file and return path
                        if (string.IsNullOrEmpty(zipName))
                        {
                            zipName = $"files-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
                        }

                        // For path response, determine ZIP location based on request
                        string zipBasePath;
                        if (isPost)
                        {
                            // For POST, use the base path root
                            zipBasePath = GetFullPath("/");
                        }
                        else
                        {
                            // For GET, get the directory containing the source path
                            string requestFolder;
                            PickParameter(apiContext, APIConstants.REQUEST_PARAM_PATH, false, false, "", out requestFolder);

                            if (string.IsNullOrEmpty(requestFolder) || requestFolder == "/")
                            {
                                zipBasePath = GetFullPath("/");
                            }
                            else
                            {
                                // For paths like /Documents/conflict-test, place ZIP in /Documents/
                                // For paths like /Documents, place ZIP in root /
                                var pathParts = requestFolder.Trim('/').Split('/');
                                if (pathParts.Length <= 1)
                                {
                                    // Single level like /Documents -> place in root
                                    zipBasePath = GetFullPath("/");
                                }
                                else
                                {
                                    // Multi-level like /Documents/conflict-test -> place in /Documents
                                    var parentParts = pathParts.Take(pathParts.Length - 1);
                                    var parentPath = "/" + string.Join("/", parentParts);
                                    zipBasePath = GetFullPath(parentPath);
                                }
                            }
                        }

                        zipPath = Path.Combine(zipBasePath, zipName);
                        string originalZipName = zipName;

                        // Handle conflict behavior for path response type
                        if (File.Exists(zipPath))
                        {
                            switch (conflictBehavior)
                            {
                                case ConflictBehavior.Fail:
                                    apiContext.HttpStatusCode = 409;
                                    apiContext.Response.ContentType = "application/json";
                                    await apiContext.Response.WriteAsync("{\"error\":\"File already exists\"}", cancellationToken);
                                    apiContext.ResponseSent = true;
                                    return;
                                case ConflictBehavior.Rename:
                                    zipPath = GenerateUniqueFilePath(zipPath);
                                    zipName = Path.GetFileName(zipPath);
                                    break;
                                case ConflictBehavior.Replace:
                                    // Will overwrite existing file
                                    break;
                                case ConflictBehavior.Ignore:
                                    // Add to existing ZIP (not implemented in this simple version)
                                    break;
                            }
                        }

                        await File.WriteAllBytesAsync(zipPath, zipData, cancellationToken);

                        // For POST requests with different number of requested vs found files, return 206
                        bool shouldReturn206Path = missingFiles.Count > 0 || (isPost && foundFiles.Count != filesToZip.Count);
                        apiContext.HttpStatusCode = shouldReturn206Path ? 206 : StatusCodes.Status200OK;
                        apiContext.Response.ContentType = "text/plain";
                        await apiContext.Response.WriteAsync(zipName, cancellationToken);
                        apiContext.ResponseSent = true;
                        return;

                    case "link":
                        // Create a temporary link (mock implementation)
                        zipReference = $"http://localhost:5001/temp/zip/{Guid.NewGuid():N}.zip";
                        apiContext.HttpStatusCode = missingFiles.Count > 0 ? 206 : StatusCodes.Status200OK;
                        apiContext.Response.ContentType = "text/plain";
                        await apiContext.Response.WriteAsync(zipReference, cancellationToken);
                        apiContext.ResponseSent = true;
                        return;

                    case "redirect":
                        // Return 302 redirect to download link
                        zipReference = $"http://localhost:5001/temp/zip/{Guid.NewGuid():N}.zip";
                        apiContext.HttpStatusCode = 302;
                        apiContext.Response.Headers["Location"] = zipReference;
                        apiContext.ResponseSent = true;
                        return;

                    case "ref":
                    default:
                        // Return reference for later retrieval
                        zipReference = $"zip-{Guid.NewGuid():N}";

                        // Store ZIP data for later retrieval (in a real implementation, this would be persisted)
                        lock (_zipReferences)
                        {
                            _zipReferences[zipReference] = new ZipReferenceData
                            {
                                Data = zipData,
                                Created = DateTime.UtcNow,
                                FoundFiles = foundFiles,
                                MissingFiles = missingFiles
                            };
                        }

                        apiContext.HttpStatusCode = missingFiles.Count > 0 ? 206 : StatusCodes.Status200OK;
                        apiContext.Response.ContentType = "text/plain";
                        await apiContext.Response.WriteAsync(zipReference, cancellationToken);
                        apiContext.ResponseSent = true;
                        return;
                }
            }
            catch (Exception ex)
            {
                apiContext.SetError(APIErrorCodes.BACKEND_OPERATION_FAILED, $"Failed to create ZIP archive: {ex.Message}");
                return;
            }
#endif
        }

        private static async Task HandleLogMessageAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            string message = apiContext.RequestParameters.TryGetValue("message", out var msgObj) ? msgObj?.ToString() : null;
            
            if (string.IsNullOrEmpty(message))
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "Message parameter is required");
                return;
            }

            Console.WriteLine($"[API LOG] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}");
            
            apiContext.HttpStatusCode = 200;
            apiContext.ResponseObject = new Dictionary<string, object> { ["status"] = "logged" };
#endif
        }

        private static async Task HandleGetServerConfigAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            string configClass = apiContext.RequestParameters.TryGetValue("configClass", out var classObj) ? classObj?.ToString() : "base";

            switch (configClass.ToLower())
            {
                case "base":
                default:
                    apiContext.ResponseObject = new Dictionary<string, object>
                    {
                        ["desktopClientEnabled"] = true,
                        ["mobileClientEnabled"] = true,
                        ["webClientEnabled"] = true,
                        ["serverVersion"] = "1.0.0-mock",
                        ["desktopClientMinVersion"] = "1.0.0",
                        ["mobileClientMinVersion"] = "1.0.0",
                        ["allowPasswordSaving"] = true,
                        ["checksumFailRetries"] = 3,
                        ["dlpEnabled"] = false,
                        ["displayShortcuts"] = true,
                        ["emailUsernameRequired"] = false,
                        ["maxFileSizeLimit"] = 1073741824L,
                        ["maxDesktopFileSizeLimit"] = 1073741824L,
                        ["maxFileSizeReminderDisabled"] = false,
                        ["allRemindersOnLoginDisabled"] = false,
                        ["officeOnlineEnabled"] = false,
                        ["oneDriveLetterCustomName"] = "",
                        ["replaceExistingNetworkDrives"] = false,
                        ["saveFilesToMemoryBeforeDisk"] = false,
                        ["sloEnabled"] = false,
                        ["ssoEnabled"] = false,
                        ["validateChecksumsOnSave"] = true,
                        ["customWopiEnabled"] = false,
                        ["wopiDomain"] = "",
                        ["wopiEnabled"] = false
                    };
                    break;
            }
#endif
        }

        private static async Task HandleSearchFilesAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            // Mock search - return empty results
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = new List<Dictionary<string, object>>();
#endif
        }

        private static async Task HandleListRecentFilesAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            var maxResults = apiContext.RequestParameters.TryGetValue("maxResults", out var maxObj) && 
                           long.TryParse(maxObj?.ToString(), out var max) ? (int)max : 100;

            var results = new List<Dictionary<string, object>>();
            foreach (var path in _recentFiles.Take(maxResults))
            {
                results.Add(new Dictionary<string, object> { ["path"] = path });
            }

            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = results;
#endif
        }

        private static async Task HandleClearRecentFilesAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            _recentFiles.Clear();
            apiContext.HttpStatusCode = StatusCodes.Status204NoContent;
            await SendResponseAsync(apiContext, (byte[])null, null, cancellationToken);
#endif
        }

        private static async Task HandleGetPublicLinkSettingsAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            var settings = new Dictionary<string, object>
            {
                ["passwordIsMandatory"] = false,
                ["allowDownloading"] = true,
                ["allowUploading"] = true,
                ["allowEditing"] = true,
                ["allowFolderSharing"] = true,
                ["linkExpirationDays"] = 30,
                ["maxNumberOfDownloads"] = 100
            };

            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = settings;
#endif
        }

        private static async Task HandleListPublicLinksAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            var links = PublicLinkManager.GetAllLinks();
            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = links.ToList();
#endif
        }

        private static async Task HandleUpdatePublicLinkAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
#if MOCK_SERVER
            string link = apiContext.RequestParameters.TryGetValue("link", out var linkObj) ? linkObj?.ToString() : null;
            
            if (string.IsNullOrEmpty(link))
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, "Link parameter is required");
                return;
            }

            var linkInfo = PublicLinkManager.GetLinkInfo(link);
            if (linkInfo == null)
            {
                apiContext.HttpStatusCode = 404;
                apiContext.SetError(APIErrorCodes.PATH_NOT_FOUND, "Public link not found");
                return;
            }

            apiContext.HttpStatusCode = StatusCodes.Status200OK;
            apiContext.ResponseObject = linkInfo;
#endif
        }

        #endregion

        // Instance method stubs that delegate to static implementations in NewEndpoints
        private async Task HandleDeletePublicLinksInstanceAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            await ClientAPIHandler.HandleDeletePublicLinksAsync(apiContext, cancellationToken);
        }

        private async Task HandleSearchFilesInstanceAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            await ClientAPIHandler.HandleSearchFilesAsync(apiContext, cancellationToken);
        }

        private async Task HandleListRecentFilesInstanceAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            await ClientAPIHandler.HandleListRecentFilesAsync(apiContext, cancellationToken);
        }

        // Add other missing handler method stubs as needed...

#if MOCK_SERVER
        /// <summary>
        /// Recursively adds a directory and all its contents to a ZIP archive
        /// </summary>
        /// <param name="archive">The ZIP archive to add files to</param>
        /// <param name="directoryFullPath">The full file system path to the directory</param>
        /// <param name="directoryLogicalPath">The logical path for the directory in the API</param>
        /// <param name="foundFiles">List to track successfully added files</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private static async Task AddDirectoryToZipAsync(System.IO.Compression.ZipArchive archive, string directoryFullPath, string directoryLogicalPath, List<string> foundFiles, CancellationToken cancellationToken)
        {
            try
            {
                var dirName = Path.GetFileName(directoryFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                // Add all files in this directory
                var files = Directory.GetFiles(directoryFullPath);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var entryName = $"{dirName}/{fileName}";
                    var entry = archive.CreateEntry(entryName);

                    using (var entryStream = entry.Open())
                    using (var fileStream = File.OpenRead(file))
                    {
                        await fileStream.CopyToAsync(entryStream, cancellationToken);
                    }
                }

                // Recursively add subdirectories
                var subdirectories = Directory.GetDirectories(directoryFullPath);
                foreach (var subdirectory in subdirectories)
                {
                    var subDirName = Path.GetFileName(subdirectory);
                    var subDirLogicalPath = $"{directoryLogicalPath}/{subDirName}";
                    await AddSubdirectoryToZipAsync(archive, subdirectory, $"{dirName}/{subDirName}", cancellationToken);
                }

                foundFiles.Add(directoryLogicalPath);
            }
            catch (Exception)
            {
                // If we can't process the directory, silently skip it
                // The caller will handle this as appropriate
            }
        }

        /// <summary>
        /// Helper method to add subdirectories recursively to ZIP archive
        /// </summary>
        /// <param name="archive">The ZIP archive to add files to</param>
        /// <param name="subdirectoryFullPath">The full file system path to the subdirectory</param>
        /// <param name="entryPrefix">The prefix path for entries in the ZIP</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private static async Task AddSubdirectoryToZipAsync(System.IO.Compression.ZipArchive archive, string subdirectoryFullPath, string entryPrefix, CancellationToken cancellationToken)
        {
            try
            {
                // Add all files in this subdirectory
                var files = Directory.GetFiles(subdirectoryFullPath);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var entryName = $"{entryPrefix}/{fileName}";
                    var entry = archive.CreateEntry(entryName);

                    using (var entryStream = entry.Open())
                    using (var fileStream = File.OpenRead(file))
                    {
                        await fileStream.CopyToAsync(entryStream, cancellationToken);
                    }
                }

                // Recursively add subdirectories
                var subdirectories = Directory.GetDirectories(subdirectoryFullPath);
                foreach (var subdirectory in subdirectories)
                {
                    var subDirName = Path.GetFileName(subdirectory);
                    await AddSubdirectoryToZipAsync(archive, subdirectory, $"{entryPrefix}/{subDirName}", cancellationToken);
                }
            }
            catch (Exception)
            {
                // If we can't process the subdirectory, silently skip it
            }
        }
#endif

    }
}
