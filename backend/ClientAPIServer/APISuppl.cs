#if !NETCOREAPP
using System;
using System.Collections.Generic;
#endif
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace APIServer
{
    /// <summary>
    /// Contains the list of API endpoint names. When adding new endpoints, add them here AND to the static constructor of APIHandler together with a reference to the endpoint handler.
    /// </summary>
    public class APIEndpoints
    {
        public const string PATH_BASE = "/api/v3/";

#if DEBUG
        public const string MIRROR = PATH_BASE + "Mirror";
#endif

    }

    public class APIErrorCodes
    {
#if DEBUG
        // This is just a test error code
        public const int TEST_FAILURE = 42;
#endif

        // Errors that map to HTTP status code 400
        public const int FAILED_TO_LOAD_REQUEST = 40001;
        public const int FAILED_TO_PARSE_REQUEST = 40002;
        public const int MISSING_PARAMETER_VALUE = 40003;
        public const int INVALID_PARAMETER_VALUE = 40004;

        public const int INVALID_AUTH_FORMAT = 40005; // a session ID is not a GUID, a token is not a valid token etc.

        // Errors that map to HTTP status code 401
        public const int NO_SESSION_ID = 40101;
        public const int NO_API_KEY = 40102;
        public const int IO_PROVIDER_REPORTED_NOT_AUTHORIZED = 40102;

        // Errors that map to HTTP status code 403
        public const int AUTHENTICATION_FAILED = 40301;
        public const int AUTHENTICATION_EXPIRED = 40302;

        // Errors that map to HTTP status code 404

        public const int ENDPOINT_NOT_FOUND = 40401;
        public const int FILE_VERSION_NOT_FOUND = 40402;
        public const int PATH_NOT_FOUND = 40403;
        public const int SHARE_NOT_FOUND = 40404;

        // Errors that map to HTTP status code 409
        public const int OBJECT_ALREADY_EXISTS = 40901;

        // Errors that map to HTTP status code 500
        public const int INTERNAL_REQUEST_INITIALIZATION_ERROR = 50001;
        public const int INTERNAL_EXCEPTION = 50002; // an exception
        public const int INTERNAL_ERROR = 50003; // not an exception ;)
        public const int PROCESS_CANT_ACCESS_FILE = 50004;

        public const int BACKEND_OPERATION_FAILED = 50006;
        public const int BACKEND_FILE_SIZE_CHANGE_FAILED = 50007;
        public const int BACKEND_FILE_TIME_CHANGE_FAILED = 50008;

        public const int FEATURE_DISABLED = 50101;
        public const int FEATURE_NOT_IMPLEMENTED = 50102;



        public static int ErrorCodeToHttpResponse(int errorCode)
        {
            if (errorCode == 0)
                return 200;
            if (errorCode < 600)
                return errorCode;
            return errorCode / 100;
        }
    }

    public class APIConstants
    {
        public const string CONTENT_TYPE_APPLICATION_JSON = "application/json";
        public const string CONTENT_TYPE_APPLICATION_OCTET_STREAM = "application/octet-stream";
        public const string CONTENT_TYPE_APPLICATION_WWWFORM = "application/x-www-form-urlencoded";
        public const string CONTENT_TYPE_MULTIPART_FORMDATA = "multipart/form-data";
        public const string CONTENT_TYPE_TEXT_PLAIN = "text/plain";

        public const string HTTP_HEADER_ACCEPT = "Accept";
        public const string HTTP_HEADER_AUTHORIZATION = "Authorization";
        public const string HTTP_HEADER_CONTENT_DISPOSITION = "Content-Disposition";
        public const string HTTP_HEADER_CONTENT_LANGUAGE = "Content-Language";
        public const string HTTP_HEADER_CONTENT_LENGTH = "Content-Length";
        public const string HTTP_HEADER_CONTENT_TYPE = "Content-Type";
        public const string HTTP_HEADER_DATE = "Date";
        public const string HTTP_HEADER_ETAG = "ETag";
        public const string HTTP_HEADER_SERVER = "Server";
        public const string HTTP_HEADER_USER_AGENT = "User-Agent";
        public const string HTTP_HEADER_WWW_AUTHENTICATE = "WWW-Authenticate";

        public const string HTTP_HEADER_VALUE_SESSIONID = "SessionID";

        public const string REQUEST_PARAM_ACCESSED = "accessed";
        public const string REQUEST_PARAM_ATTRIBUTES = "attributes";
        public const string REQUEST_PARAM_BODY = "body";
        public const string REQUEST_PARAM_CHECKSUM = "checksum";
        public const string REQUEST_PARAM_COEDIT = "coedit";
        public const string REQUEST_PARAM_CONFLICT_BEHAVIOR = "conflictBehavior";
        public const string REQUEST_PARAM_COUNT = "count";
        public const string REQUEST_PARAM_CREATE_CONTENT = "createContent";
        public const string REQUEST_PARAM_CREATE_FILE = "createFile";
        public const string REQUEST_PARAM_CREATED = "created";
        public const string REQUEST_PARAM_DESKTOP_CLIENT_ALLOWED = "desktopClientAllowed";
        public const string REQUEST_PARAM_DLP_DOWNLOAD_ENABLED = "dlpDownloadEnabled";
        public const string REQUEST_PARAM_DLP_OFFICE_ONLINE_EDIT_ENABLED = "dlpOfficeOnlineEditEnabled";
        public const string REQUEST_PARAM_DOMAIN_NAME = "domainName";
        public const string REQUEST_PARAM_EXPIRES = "expires";
        public const string REQUEST_PARAM_EXTENSION = "extension";
        public const string REQUEST_PARAM_RETURN_FILE_INFO = "returnFileInfo";
        public const string REQUEST_PARAM_IF_MODIFIED = "ifModified";
        public const string REQUEST_PARAM_INCLUDE_EXTENDED = "includeExtended";
        public const string REQUEST_PARAM_INCLUDE_LOCK_DETAILS = "includeLockDetails";
        public const string REQUEST_PARAM_INCLUDE_LOCK_INFO = "includeLockInfo";
        public const string REQUEST_PARAM_INCLUDE_LOCK_OWNERS = "includeLockOwners";
        public const string REQUEST_PARAM_INCLUDE_LOCKS = "includeLocks";
        public const string REQUEST_PARAM_IS_GROUP = "isGroup";
        public const string REQUEST_PARAM_MOBILE_CLIENT_ALLOWED = "mobileClientAllowed";
        public const string REQUEST_PARAM_LOCK_FILE = "lockFile";
        public const string REQUEST_PARAM_MODIFIED = "modified";
        public const string REQUEST_PARAM_NAME = "name";
        public const string REQUEST_PARAM_NEW_PATH = "newPath";
        public const string REQUEST_PARAM_OPERATION = "operation";
        public const string REQUEST_PARAM_OFFICE_PASSWORD = "officePassword";
        public const string REQUEST_PARAM_OFFICE_USERNAME = "officeUsername";
        public const string REQUEST_PARAM_OWNER = "owner";
        public const string REQUEST_PARAM_PATH = "path";
        public const string REQUEST_PARAM_PUBLIC_SHARING_ENABLED = "publicSharingEnabled";
        public const string REQUEST_PARAM_QUERY = "query";
        public const string REQUEST_PARAM_RECURSIVE = "recursive";
        public const string REQUEST_PARAM_REPOSITORY = "repository";
        public const string REQUEST_PARAM_SESSION_ID = "sessionId";
        public const string REQUEST_PARAM_SHARE_NAME = "shareName";
        public const string REQUEST_PARAM_SIZE = "size";
        public const string REQUEST_PARAM_SKIP_HIDDEN = "skipHidden";
        public const string REQUEST_PARAM_START_POSITION = "startPosition";
        public const string REQUEST_PARAM_STORAGE_PROVIDER_ID = "storageProviderId";
        public const string REQUEST_PARAM_SUBJECT = "subject";
        public const string REQUEST_PARAM_TO = "to";
        public const string REQUEST_PARAM_TOTAL_LENGTH = "totalLength";
        public const string REQUEST_PARAM_UNLOCK_AFTER_WRITE = "unlockAfterWrite";
        public const string REQUEST_PARAM_UPLOAD_ID = "uploadId";
        public const string REQUEST_PARAM_WEB_CLIENT_ALLOWED = "webClientAllowed";

        public const string REQUEST_PARAM_VALUE_DOWNLOAD = "download";
        public const string REQUEST_PARAM_VALUE_LATEST = "latest";
        public const string REQUEST_PARAM_VALUE_UPLOAD = "upload";
        public const string REQUEST_PARAM_VALUE_FOLDER_ACCESSIBLE = "folderAccessible";
        public const string REQUEST_PARAM_VALUE_FOLDER_WRITABLE = "folderWritable";
        public const string REQUEST_PARAM_VALUE_WRITE_PERMISSIONS = "writePermissions";

        public const string RESPONSE_VALUE_NAME_ACTIVE = "active";
        public const string RESPONSE_VALUE_NAME_ATTRIBUTES = "attributes";
        public const string RESPONSE_VALUE_NAME_AUTHENTICATION_URL = "authenticationUrl";
        public const string RESPONSE_VALUE_NAME_COEDIT = "coedit";
        public const string RESPONSE_VALUE_NAME_CREATED = "created";
        public const string RESPONSE_VALUE_NAME_ERROR = "error";
        public const string RESPONSE_VALUE_NAME_EXPIRES = "expires";
        public const string RESPONSE_VALUE_NAME_EXPIRES_AFTER = "expiresAfter";
        public const string RESPONSE_VALUE_NAME_ID = "id";
        public const string RESPONSE_VALUE_NAME_IS_DEEP = "isDeep";
        public const string RESPONSE_VALUE_NAME_IS_FOLDER = "isFolder";
        public const string RESPONSE_VALUE_NAME_LEVEL = "level";
        public const string RESPONSE_VALUE_NAME_LINK = "link";
        public const string RESPONSE_VALUE_NAME_LOCK_COUNT = "lockCount";
        public const string RESPONSE_VALUE_NAME_LOCK_OWNERS = "lockOwners";
        public const string RESPONSE_VALUE_NAME_LOCKED_BY_MULTIPLE_EDITORS = "lockedByMultipleEditors";
        public const string RESPONSE_VALUE_NAME_LOCKED_BY_CURRENT_SESSION = "lockedByCurrentSession";
        public const string RESPONSE_VALUE_NAME_LOCKED_BY_OTHER_SESSIONS = "lockedByOtherSessions";
        public const string RESPONSE_VALUE_NAME_LOCKS = "locks";
        public const string RESPONSE_VALUE_NAME_MODIFIED = "modified";
        public const string RESPONSE_VALUE_NAME_NAME = "name";
        public const string RESPONSE_VALUE_NAME_OTP = "otp";
        public const string RESPONSE_VALUE_NAME_OWNER = "owner";
        public const string RESPONSE_VALUE_NAME_PATH = "path";
        public const string RESPONSE_VALUE_NAME_PROGRESS = "progress";
        public const string RESPONSE_VALUE_NAME_SERVER_NAME = "serverName";
        public const string RESPONSE_VALUE_NAME_SHADOW_PATH = "shadowPath";
        public const string RESPONSE_VALUE_NAME_SHAREPOINT_RESPONSE = "sharepointResponse";
        public const string RESPONSE_VALUE_NAME_SIZE = "size";
        public const string RESPONSE_VALUE_NAME_STATUS = "status";
        public const string RESPONSE_VALUE_NAME_UPLOAD_ID = "uploadId";
        public const string RESPONSE_VALUE_NAME_USERNAME = "username";
        public const string RESPONSE_VALUE_NAME_VALUE = "value";

        public const string REST_JSON_RESPONSE_STATUS_FAILED = "failed";
        public const string REST_JSON_RESPONSE_STATUS_SUCCESS = "success";

        public const string REST_PROP_NAME_DETAILS = "details";
        public const string REST_PROP_NAME_ERROR_CODE = "errorCode";
        public const string REST_PROP_NAME_MESSAGE = "message";
        public const string REST_PROP_NAME_STATUS = "status";

        public const string CONFLICT_BEHAVIOR_FAIL = "fail";
        public const string CONFLICT_BEHAVIOR_REPLACE = "replace";
        public const string CONFLICT_BEHAVIOR_RENAME = "rename";

        public const string DATE_FORMAT_ISO_8601 = "yyyy-MM-dd'T'HH:mm:ss'Z'";
        public const string DATE_FORMAT_ISO_8601_WITH_MS = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        public const string NAME_NEW_FILE = "New File";
        public const string NAME_NEW_FOLDER = "New Folder";

        public const string ID_MAPPER_CLIENT = "MapperClient";
        public const string ID_CLIENT_API = "ClientAPI";


    }

    public class APITextMessages
    {
        // Error messages
        public const string ERROR_API_IS_DISABLED = "API is disabled";
        public const string ERROR_BACKEND_OPERATION_FAILED = "The backend operation has failed";
        public const string ERROR_CLIENT_AUTHENTICATION_FAILED = "The authentication attempt has failed";
        public const string ERROR_ENDPOINT_NOT_FOUND_S = "API endpoint '{0}' was not found";
        public const string ERROR_FILE_SIZE_CHANGE_FAILED = "File size change operation has failed";
        public const string ERROR_FILE_TIME_CHANGE_FAILED = "File time change operation has failed";
        public const string ERROR_INITIALIZATION_FAILED = "Initialization error";
        public const string ERROR_MISSING_LENGTH_AT_START = "The length of the file was not found. It must have been specified when the upload session was started.";
        public const string ERROR_INVALID_PARAM_OS = "The value {0} of parameter '{1}' is not valid";
        public const string ERROR_MISSING_PARAM_S = "Parameter '{0}' must be specified and non-empty";
        public const string ERROR_MISSING_PARAMS_S2 = "Either parameter '{0}' or '{1}' must be specified and non-empty";
        public const string ERROR_NO_API_KEY_PROVIDED = "An API Key was not provided";
        public const string ERROR_NO_RESPONSE_OBJECT = "No response object has been provided";
        public const string ERROR_NO_SESSION_ID = "No session ID found in the request";
        public const string ERROR_DIR_WITH_GIVEN_NAME_ALREADY_EXISTS_S2 = "The '{0}' operation has failed: a directory with the name '{1}' already exists";
        public const string ERROR_FILE_WITH_GIVEN_NAME_ALREADY_EXISTS_S2 = "The '{0}' operation has failed: a file with the name '{1}' already exists";
        public const string ERROR_OBJECT_WITH_GIVEN_NAME_ALREADY_EXISTS_S2 = "The '{0}' operation has failed: an object with the name '{1}' already exists";
        public const string ERROR_PATH_NOT_FOUND_AFTER_OPERATION_S2 = "The '{0}' operation was completed, but the new object with the name '{1}' was not found";
        public const string ERROR_PATH_NOT_FOUND_S = "No object with the name '{0}' was found";
        public const string ERROR_SERVER_ENCOUNTERED_ERROR = "The server has encountered an error";
        public const string ERROR_USER_SESSION_HAS_EXPIRED = "User session has expired";
        public const string ERROR_VERSION_NOT_FOUND_S2 = "The version of '{0}' dated {1} has not been found";
        public const string ERROR_SHARE_NOT_FOUND_S = "Share '{0}' has not been found";
        public const string EXCEPTION_IN_PATH_S = "An exception has occurred why handling a request to '{0}'";

        public const string CLIENT_AUTHENTICATION_IS_REQUIRED_FOR_MAIL = "Client authentication is required for sending anonymous messages";
        public const string INTERNAL_ERROR = "Internal error";
    }

    // The StatusCodes class exists in ASP .NET Core, but not in .NET Framework
    public class StatusCodes
    {
        public const int Status200OK = 200;
        public const int Status201Created = 201;
        public const int Status204NoContent = 204;

        public const int Status304NotModified = 304;

        public const int Status400BadRequest = 400;
        public const int Status401Unauthorized = 401;
        public const int Status409Conflict = 409;
        public const int Status418ImATeapot = 418;

        public const int Status500InternalServerError = 500;
        public const int Status501NotImplemented = 501;
    }

    public class StrUtils
    {
        /// <summary>
        /// This function separates the path+basename from the extension
        /// </summary>
        /// <param name="path">the path to split</param>
        /// <param name="basepath">the path and a base name without an extension</param>
        /// <param name="ext">an extension with a leading dot ('.')</param>
        public static void SplitPathNameAndExt(string path, out string basepath, out string ext)
        {
            basepath = string.Empty;
            ext = string.Empty;
            if (string.IsNullOrEmpty(path))
                return;
            int idxSlash = path.LastIndexOf('/');
            int idxDot = path.LastIndexOf('.');
            if (idxSlash >= idxDot)
            {
                basepath = path;
                return;
            }
            basepath = path.Substring(0, idxDot);
            ext = path.Substring(idxDot);
        }

        private static void JsonPropertyToDictionary(JsonElement element, Dictionary<string, object> dict, string objectName = "", string propertyName = "")
        {
            object obj = null;
            string elementName;
            JsonElement childElement;

            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    // DateTime parsing is used only for testing because trying to convert every parameter is time-consuming
#if DEBUG
                    DateTime dt;
                    if (element.TryGetDateTime(out dt))
                        obj = dt;
                    else
#endif
                        obj = element.GetString(); // we don't decode the value because it is already decoded
                    break;
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long longValue))
                        obj = longValue;
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    obj = element.GetBoolean();
                    break;
                case JsonValueKind.Object:
                    if (!string.IsNullOrEmpty(objectName))
                        elementName = string.Join(".", objectName, propertyName);
                    else
                        elementName = propertyName;
                    JsonObjectToDictionary(element, dict, elementName);
                    break;
                case JsonValueKind.Array:
                    int arrLen = element.GetArrayLength();

                    // Add the number of elements as a "Count" property
                    if (!string.IsNullOrEmpty(objectName))
                        elementName = string.Join(".", objectName, propertyName, "Count");
                    else
                        elementName = string.Join(".", propertyName, "Count");

                    dict[elementName] = arrLen;

                    // Add every element of the array
                    for (int i = 0; i < arrLen; i++)
                    {
                        childElement = element[i];

                        if (!string.IsNullOrEmpty(objectName))
                            elementName = string.Concat(objectName, ".", propertyName);
                        else
                            elementName = propertyName;

                        elementName = string.Concat(elementName, "[", i.ToString(), "]");

                        if (childElement.ValueKind == JsonValueKind.Object)
                            JsonObjectToDictionary(childElement, dict, elementName);
                        else
                            JsonPropertyToDictionary(childElement, dict, string.Empty, elementName);
                    }

                    break;
            }

            // For primitive types, we have set obj above, so now, we add the value to the dictionary. Complex types have been added separately.
            if (obj != null)
            {
                if (!string.IsNullOrEmpty(objectName))
                    elementName = string.Join(".", objectName, propertyName);
                else
                    elementName = propertyName;

                dict[elementName] = obj;
            }
        }

        /// <summary>
        /// Converts a JSON object into a dictionary.
        ///
        /// The method is designed to product a dictionary with all elements, including array elements and subobjects, accessible via a string key.
        ///
        /// The method intentionally works only with objects - there should be no simple types accepted in API request parameters sent as JSON.
        /// The API should be extensible, which means that when an object is accepted, we can add, change, or remove its properties, which is not possible with primitive types.
        /// </summary>
        /// <param name="element">a JSON element with an object.</param>
        /// <param name="dict">a dictionary to store properties</param>
        /// <param name="objectName">The prefix for element names (used for recursive storing)</param>
        internal static void JsonObjectToDictionary(JsonElement element, Dictionary<string, object> dict, string objectName = "")
        {
            JsonElement.ObjectEnumerator enumer = element.EnumerateObject();

            JsonProperty current;
            while (enumer.MoveNext())
            {
                current = enumer.Current;
                JsonPropertyToDictionary(current.Value, dict, objectName, current.Name);
            }
        }

        public static void AppendListToJson(IList<Dictionary<string, object>> list, StringBuilder builder)
        {
            builder.Append("[\n");
            Dictionary<string, object> child;
            int listCount = list.Count;
            for (int i = 0; i < listCount; i++)
            {
                child = list[i];
                AppendObjectToJson(child, builder);
                if (i < listCount - 1)
                    builder.Append(",\n");
            }

            builder.Append("\n]");
        }

        public static void AppendStringArrayToJson(string[] array, StringBuilder builder)
        {
            builder.Append("[\n");
            string elem;
            int listCount = array.Length;
            for (int i = 0; i < listCount; i++)
            {
                elem = array[i];
                builder.Append(string.Join(StrUtils.JsonEncode(elem), "\"", "\""));
                if (i < listCount - 1)
                    builder.Append(",\n");
            }

            builder.Append("\n]");
        }

        /// <summary>
        /// Converts a dictionary with object properties to a JSON object.
        ///
        /// This method is not the opposite to JsonObjectToDictionary because it does not parse keys in "object.sub-object" and "arr[x].property" notations -
        /// doing this in a generic manner would require bulding an object tree in memory, which would slow down operations.
        ///
        /// Dictionary&lt;string, object&gt; and IList of such dictionaries are accepted in order to support embedding of object hierarchies.
        ///
        /// The method intentionally works only with objects - there should be no simple types returned in API responses as JSON.
        /// The API should be extensible, which means that when an object is returned, one can add, change, or remove its properties, which is not possible with primitive types.
        /// </summary>
        /// <param name="dict">A dictionary with object properties.</param>
        /// <param name="builder">The destination StringBuilder object, to which JSON is written. It is used to proxy the final JSON string, which is then sent using SendResponseAsync.</param>
        public static void AppendObjectToJson(Dictionary<string, object> dict, StringBuilder builder)
        {
            builder.Append("{\n");
            bool first = true;
            object obj;

            foreach (var entry in dict)
            {
                if (!first)
                    builder.Append(",\n");
                first = false;

                obj = entry.Value;

                builder.Append("\t\"");
                builder.Append(entry.Key);
                builder.Append("\": ");

                if (obj is string strobj)
                {
                    builder.Append('"');
                    builder.Append(JsonEncode(strobj));
                    builder.Append('"');
                }
                else
                if (obj is int intobj)
                {
                    builder.Append(intobj);
                }
                else
                if (obj is uint uintobj)
                {
                    builder.Append(uintobj);
                }
                else
                if (obj is long longobj)
                {
                    builder.Append(longobj);
                }
                else
                if (obj is ulong ulongobj)
                {
                    builder.Append(ulongobj);
                }
                else
                if (obj is bool boolobj)
                {
                    builder.Append(boolobj ? "true" : "false"); // the default Append(bool) method stores "True" and "False" with the uppercase first letter
                }
                else
                if (obj is DateTime dateobj)
                {
                    builder.Append('"');
                    builder.Append(JsonEncode(RenderDateISO8601(dateobj)));
                    builder.Append('"');
                }
                else
                if (obj is string[] strarr)
                {
                    AppendStringArrayToJson(strarr, builder);
                }
                else
                if (obj is IList<Dictionary<string, object>> listobj)
                {
                    AppendListToJson(listobj, builder);

                }
                else
                if (obj is Dictionary<string, object> dictobj)
                {
                    AppendObjectToJson(dictobj, builder);
                }
                else
                {
                    builder.Append("\"\"\n");
                }
            }

            builder.Append('}');
        }

        public static string JsonDecode(string jsonString)
        {
            StringBuilder decodedString = new StringBuilder(jsonString.Length);
            int i = 0;
            while (i < jsonString.Length)
            {
                char c = jsonString[i];
                if (c == '\\')
                {
                    i++;
                    if (i < jsonString.Length)
                    {
                        char nextChar = jsonString[i];
                        switch (nextChar)
                        {
                            case '"':
                                decodedString.Append('"');
                                break;
                            case '\\':
                                decodedString.Append('\\');
                                break;
                            case '/':
                                decodedString.Append('/');
                                break;
                            case 'b':
                                decodedString.Append('\b');
                                break;
                            case 'f':
                                decodedString.Append('\f');
                                break;
                            case 'n':
                                decodedString.Append('\n');
                                break;
                            case 'r':
                                decodedString.Append('\r');
                                break;
                            case 't':
                                decodedString.Append('\t');
                                break;
                            case 'u':
                                if (i + 4 < jsonString.Length)
                                {
                                    string hex = jsonString.Substring(i + 1, 4);
                                    try
                                    {
                                        int codePoint = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                                        decodedString.Append((char)(codePoint));

                                        i += 4;
                                    }
                                    catch (FormatException e)
                                    {
                                        // Invalid Unicode escape sequence
                                        decodedString.Append('\\').Append('u').Append(hex);
                                    }
                                }
                                else
                                {
                                    // Incomplete Unicode escape sequence
                                    decodedString.Append('\\').Append('u');
                                }
                                break;
                            default:
                                decodedString.Append('\\').Append(nextChar);
                                break;
                        }
                    }
                }
                else
                {
                    decodedString.Append(c);
                }
                i++;
            }
            return decodedString.ToString();
        }


        public static string JsonEncode(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            char c;
            StringBuilder builder = new StringBuilder(value.Length * 2);
            for (int i = 0; i < value.Length; i++)
            {
                c = value[i];
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '/':
                        builder.Append("\\/");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c >= 127)
                        {
                            builder.Append("\\u");
                            builder.Append(string.Format("{0:x4}", (int)c));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            return builder.ToString();
        }

        public static string RenderDateISO8601(DateTime date)
        {
            return date.ToString(APIConstants.DATE_FORMAT_ISO_8601_WITH_MS);
        }

        public static DateTime ParseDate(string date, string format)
        {
            if (date == null)
                return System.DateTime.MinValue;

            try
            {
                if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime result))
                    return result;
                else
                    return DateTime.MinValue;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        public static DateTime ParseDateISO8601(string date)
        {
            if (date == null)
                return DateTime.MinValue;

            DateTime result = ParseDate(date, APIConstants.DATE_FORMAT_ISO_8601_WITH_MS);
            if (result == DateTime.MinValue)
                result = ParseDate(date, APIConstants.DATE_FORMAT_ISO_8601);
            return result;
        }

        /// <summary>
        /// Converts contents of a dictionary into a string by concatenating names and values, and then concatenating name/source pairs.
        /// </summary>
        /// <param name="dictionary">A source dictionary to flatten</param>
        /// <param name="nameValueSeparator">A separator to use to concatenate a name and a source.</param>
        /// <param name="entrySeparator">A separator to use to concatenate name/source entries.</param>
        /// <param name="nameEncoder">An optional encoder function for a name (e.g., use UrlEncode when flattening the query parameters list)</param>
        /// <param name="valueEncoder">An optional encoder function for a source</param>
        /// <returns>The resulting string.</returns>
        public static string FlattenDictionary(Dictionary<string, string> dictionary, string nameValueSeparator, string entrySeparator, Func<string, string> nameEncoder, Func<string, string> valueEncoder)
        {
            StringBuilder builder = null;
            string value;
            string encodedValue;
            foreach (string key in dictionary.Keys)
            {
                value = dictionary[key];

                if (builder == null)
                    builder = new StringBuilder();
                else
                    builder.Append(entrySeparator); // there was some entry added earlier, so we add a separator

                builder.Append((nameEncoder != null) ? nameEncoder(key) : key);

                if (value != null)
                {
                    builder.Append(nameValueSeparator);
                    encodedValue = (valueEncoder != null) ? valueEncoder(value) : value;
                    builder.Append(encodedValue);
                }
            }

            if (builder == null)
                return string.Empty;
            else
                return builder.ToString();
        }
    }

    // This helper provides a way to asynchronously handle requests in ASP.NET Classic.
    // However, for whatever reason, it does not work in MyWorkDrive.WebClient (NullPointerException occurs in ASP.NET internals), thus it is of no use at the moment.
    /*
    internal class APIEventHandlerTaskAsyncHelper
    {

        HttpApplication _app;

        static APIEventHandlerTaskAsyncHelper _instance;

        public static APIEventHandlerTaskAsyncHelper CreateHelper(HttpApplication app)
        {
            if (_instance == null)
                _instance = new APIEventHandlerTaskAsyncHelper(app);
            else
                _instance._app = app;
            return _instance;
        }

        public APIEventHandlerTaskAsyncHelper(HttpApplication app)
        {
            _app = app;
        }

        public IAsyncResult BeginEventHandler(object sender, EventArgs args, AsyncCallback callback, object state)
        {
            HttpContext context = HttpContext.Current;
            var tcs = new TaskCompletionSource<bool>(state);
            if (context.Request.Path.StartsWith(APIEndpoints.PATH_BASE))
            {
                Task task = PostAuthorizeRequestInternalAsync(sender, args, context);

                task.ContinueWith(_ =>
                {
                    if (task.IsFaulted && task.Exception != null)
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    else
                    if (task.IsCanceled)
                        tcs.TrySetCanceled();
                    else
                        tcs.TrySetResult(true);

                    try
                    {
                        callback?.Invoke(tcs.Task);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
            else
                tcs.TrySetResult(false);

            return tcs.Task;
        }

        public void EndEventHandler(IAsyncResult result)
        {
            // Nothing to do here.
        }

        protected Task PostAuthorizeRequestAsync(object sender, EventArgs e)
        {
            return PostAuthorizeRequestInternalAsync(sender, e, HttpContext.Current);
        }

        protected virtual async Task PostAuthorizeRequestInternalAsync(object sender, EventArgs e, HttpContext context)
        {
            if (context.Request.Path.StartsWith(APIEndpoints.PATH_BASE))
            {
                context.SetSessionStateBehavior(SessionStateBehavior.Required);

                await APIHandler.ProcessRequestAsync(context);

                _app.CompleteRequest(); // this call is needed to prevent the ExtensionlessUrl handler from sending the request for the second time
            }
        }
    }
    */
}
