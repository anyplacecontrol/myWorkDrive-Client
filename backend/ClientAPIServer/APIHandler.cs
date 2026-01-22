#if !NETCOREAPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
#endif
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace APIServer
{
    public class APIContext
    {
        private const string AUTH_TYPE_SESSIONID_SPACE = "SessionID ";
        private const string AUTH_TYPE_BEARER_SPACE = "Bearer ";

        private const int API_CONTEXT_POOL_SIZE = 16;

        private static List<APIContext> _contextPool = new List<APIContext>(API_CONTEXT_POOL_SIZE);

        private readonly Dictionary<string, object> _requestParams = new Dictionary<string, object>();
        private
#if NETCOREAPP
            object?
#else
            object
#endif
            _responseObject = null;

        private
#if NETCOREAPP
            HttpContext?
#else
            HttpContext
#endif
            _httpContext = null;

        public Dictionary<string, object> RequestParameters => _requestParams;


        // May be Dictionary<string, object> or List<Dictionary<string, object>>
        public object ResponseObject
        {
            get
            {
                return _responseObject;
            }
            set
            {
                if ((value is Dictionary<string, object>) || (value is List<Dictionary<string, object>>))
                    _responseObject = value;
                else
                    throw new ArgumentException("Only Dictionary<string, object> and List<Dictionary<string, object>> are accepted as ResponseObject objects");
            }
        }

        public
#if NETCOREAPP
            string?
#else
            string
#endif
            ContentType
        { get; set; }

        public int APIErrorCode { get; set; }

        public int HttpStatusCode { get; set; }

        public string ResponseMessage { get; set; }

        public bool ResponseSent { get; set; }

        public HttpContext Context
        {
            get
            {
                return _httpContext;
            }

            private set
            {
                _httpContext = value;
            }
        }

        public HttpRequest Request => _httpContext.Request;

        public HttpResponse Response => _httpContext.Response;

        /// <summary>
        /// Stores a session ID that is taken from the Authorization header.
        /// </summary>
        public string SessionID { get; set; }

        /// <summary>
        /// A GUID representation of the session ID
        /// </summary>
        public Guid SessionGUID { get; set; }

        /// <summary>
        /// Stores an authentication token that is taken from the Authorization header. Currently not used but can become of use when token-based authentication is added.
        /// </summary>
        public string AuthToken { get; set; }

        internal static APIContext GetContext(HttpContext context)
        {
            lock (_contextPool)
            {
                if (_contextPool.Count == 0)
                    return new APIContext(context);
                else
                {
                    int idx = _contextPool.Count - 1;
                    APIContext result = _contextPool[idx];
                    _contextPool.RemoveAt(idx);
                    result.Context = context;
                    return result;
                }
            }
        }

        internal static void ReturnContext(APIContext apiContext)
        {
            lock (_contextPool)
            {
                if (_contextPool.Count >= API_CONTEXT_POOL_SIZE)
                    return; // just throw away apiContext
                else
                {
                    apiContext.Reset();
                    _contextPool.Add(apiContext);
                }
            }
        }

        public APIContext(HttpContext context)
        {
            _httpContext = context;
        }

        private void Reset()
        {
            SessionID = string.Empty;
            SessionGUID = Guid.Empty;
            AuthToken = string.Empty;
            _responseObject = null;
            _httpContext = null;

            ContentType = string.Empty;
            APIErrorCode = 0;
            HttpStatusCode = 0;
            ResponseMessage = string.Empty;
            ResponseSent = false;

            _requestParams.Clear();
        }

        /// <summary>
        /// Populates the APIContext instance from the request.
        /// </summary>
        /// <param name="context">An HTTP context of the request.</param>
        /// <param name="apiContext">An APIContext instance to populate.</param>
        public static async Task PreProcessRequestAsync(HttpContext context, APIContext apiContext, CancellationToken cancellationToken = default)
        {
            try
            {
                // First, process query parameters, if any
                ProcessQueryParameters(context, apiContext);

                apiContext.ContentType = context.Request.ContentType;
                if (apiContext.ContentType == null)
                    apiContext.ContentType = string.Empty;

                // next, handle parameters posted in the body, if the request is POST
#if NETCOREAPP
                string method = context.Request.Method;
#else
                string method = context.Request.HttpMethod;
#endif
                if (method.Equals("POST", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    int idx = apiContext.ContentType.IndexOf(';');

                    string mediaType = (idx > 0) ? apiContext.ContentType.Substring(0, idx) : apiContext.ContentType;

                    if (APIConstants.CONTENT_TYPE_APPLICATION_JSON.Equals(mediaType))
                    {
                        await ProcessJsonRequestBodyAsync(context, apiContext, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    if (APIConstants.CONTENT_TYPE_APPLICATION_WWWFORM.Equals(mediaType) || APIConstants.CONTENT_TYPE_MULTIPART_FORMDATA.Equals(mediaType))
                    {
                        ProcessFormRequestBody(context, apiContext);
                    }
                }

                // one of the request parser methods could have set the error, in which case we can return early
                if (apiContext.APIErrorCode != 0)
                    return;

                // pick a session ID or an authentication token from the authorization header
                apiContext.SessionID = string.Empty;
                apiContext.AuthToken = string.Empty;


#if NETCOREAPP
                string? authHeader = null;
                if (context.Request.Headers.TryGetValue(APIConstants.HTTP_HEADER_AUTHORIZATION, out Microsoft.Extensions.Primitives.StringValues value) && value.Count > 0)
                {
                    authHeader = value[0];
                }
#else
                string authHeader = context.Request.Headers.Get(APIConstants.HTTP_HEADER_AUTHORIZATION);
#endif
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith(AUTH_TYPE_SESSIONID_SPACE) && authHeader.Length > AUTH_TYPE_SESSIONID_SPACE.Length)
                {
                    apiContext.SessionID = authHeader.Substring(AUTH_TYPE_SESSIONID_SPACE.Length).Trim();
                    if (!string.IsNullOrEmpty(apiContext.SessionID))
                    {
#if MOCK_SERVER
                        // In mock mode, accept any non-empty session ID
                        // Try to parse as GUID for compatibility, but don't fail if it's not
                        if (Guid.TryParse(apiContext.SessionID, out Guid parsedGuid))
                        {
                            apiContext.SessionGUID = parsedGuid;
                        }
                        else
                        {
                            // Generate a deterministic GUID from the session ID string for consistency
                            using (var md5 = System.Security.Cryptography.MD5.Create())
                            {
                                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(apiContext.SessionID));
                                apiContext.SessionGUID = new Guid(hash);
                            }
                        }
#else
                        // In production mode, require valid GUID format
                        if (Guid.TryParse(apiContext.SessionID, out Guid parsedGuid))
                        {
                            apiContext.SessionGUID = parsedGuid;
                        }
                        else
                        {
                            apiContext.SetError(
                                APIErrorCodes.INVALID_AUTH_FORMAT,
                                "SessionID must be a valid GUID");
                        }
#endif
                    }
                }
                else
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith(AUTH_TYPE_BEARER_SPACE) && authHeader.Length > AUTH_TYPE_BEARER_SPACE.Length)
                    apiContext.AuthToken = authHeader.Substring(AUTH_TYPE_BEARER_SPACE.Length);
            }
            catch (Exception ex)
            {
                apiContext.SetError(
                    APIErrorCodes.INTERNAL_REQUEST_INITIALIZATION_ERROR,
#if DEBUG
                    ExceptionToString(ex, APITextMessages.ERROR_INITIALIZATION_FAILED),
#else
                    APITextMessages.ERROR_INITIALIZATION_FAILED,
#endif
                    StatusCodes.Status500InternalServerError);
            }
        }

        public static string BuildErrorResponse(APIContext apiContext)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("{\n\t\"");
            builder.Append(APIConstants.REST_PROP_NAME_STATUS);
            builder.Append("\": \"");
            builder.Append(APIConstants.REST_JSON_RESPONSE_STATUS_FAILED);
            builder.Append('"');
            if (apiContext.APIErrorCode > 0)
            {
                builder.Append(",\n\t\"");
                builder.Append(APIConstants.REST_PROP_NAME_ERROR_CODE);
                builder.Append("\": ");
                builder.Append(apiContext.APIErrorCode);
            }

            builder.Append(",\n\t\"");
            builder.Append(APIConstants.REST_PROP_NAME_MESSAGE);
            builder.Append("\": \"");

            if (!string.IsNullOrEmpty(apiContext.ResponseMessage))
                builder.Append(StrUtils.JsonEncode(apiContext.ResponseMessage));
            else
                builder.Append("Undefined error");
#if DEBUG
            builder.Append(" (Error code: " + apiContext.APIErrorCode.ToString() + ')');
#endif
            if (apiContext.ResponseObject != null)
            {
                builder.Append("\",\n\t\"");
                builder.Append(APIConstants.REST_PROP_NAME_DETAILS);
                builder.Append("\": ");
                if (apiContext.ResponseObject is Dictionary<string, object>)
                    StrUtils.AppendObjectToJson((Dictionary<string, object>)apiContext.ResponseObject, builder);
                else
                if (apiContext.ResponseObject is List<Dictionary<string, object>>)
                    StrUtils.AppendListToJson((List<Dictionary<string, object>>)apiContext.ResponseObject, builder);
            }
            else
                builder.Append("\"\n}");
            return builder.ToString();
        }

        private static void ProcessQueryParameters(HttpContext httpContext, APIContext apiContext)
        {
#if NETCOREAPP
            string? value;
            var allKeys = httpContext.Request.Query.Keys;
#else
            string value;
            var allKeys = httpContext.Request.QueryString.AllKeys;
#endif
            foreach (var name in allKeys)
            {
#if NETCOREAPP
                if (httpContext.Request.Query.TryGetValue(name, out Microsoft.Extensions.Primitives.StringValues values) && values.Count > 0)
                    value = values[0] ?? string.Empty;
                else
                    value = string.Empty;
#else
                value = httpContext.Request.QueryString.Get(name);
#endif
                if ("true".Equals(value, StringComparison.OrdinalIgnoreCase))
                    apiContext._requestParams[name] = true;
                else
                if ("false".Equals(value, StringComparison.OrdinalIgnoreCase))
                    apiContext._requestParams[name] = false;
                else
                {
                    if (long.TryParse(value, out long intval))
                        apiContext._requestParams[name] = intval;
                    else
                        apiContext._requestParams[name] = value;
                }
            }
        }

        private static async Task ProcessJsonRequestBodyAsync(HttpContext httpContext, APIContext apiContext, CancellationToken cancellationToken = default)
        {
            if (httpContext.Request.ContentLength == 0)
                return;

#if NETCOREAPP
            Stream stream = httpContext.Request.Body;
#else
            Stream stream = httpContext.Request.GetBufferlessInputStream();

#endif
            if (stream == null)
            {
                apiContext.SetError(APIErrorCodes.FAILED_TO_LOAD_REQUEST, "Failed to load the client request", StatusCodes.Status500InternalServerError);
                return;
            }

            // We retrieve the body back for logging purposes, not provided here. If this is not needed, the code can be optimized.
#if NETCOREAPP
            string?[] jsonBodyBuf = new string?[1];
            JsonDocument? json
#else
            string[] jsonBodyBuf = new string[1];
            JsonDocument json
#endif
            = await LoadJSONRequestAsync(apiContext, stream, jsonBodyBuf, cancellationToken).ConfigureAwait(false);
            if (json != null)
                StrUtils.JsonObjectToDictionary(json.RootElement, apiContext._requestParams);
        }

        private static void ProcessFormRequestBody(HttpContext httpContext, APIContext apiContext)
        {
#if NETCOREAPP
            string? value;
            string name;
            foreach (var item in httpContext.Request.Form)
            {
                name = item.Key;
                if (item.Value.Count > 0)
                    value = item.Value[0] ?? string.Empty;
                else
                    value = "";
#else
            string value;
            foreach (string name in httpContext.Request.Form)
            {
                value = httpContext.Request.Form.Get(name);
#endif
                if ("true".Equals(value, StringComparison.OrdinalIgnoreCase))
                    apiContext._requestParams[name] = true;
                else
                if ("false".Equals(value, StringComparison.OrdinalIgnoreCase))
                    apiContext._requestParams[name] = false;
                else
                {
                    if (long.TryParse(value, out long intval))
                        apiContext._requestParams[name] = intval;
                    else
                        apiContext._requestParams[name] = value;
                }
            }
        }

        /// <summary>
        /// The method loads the JSON request from the stream.
        /// </summary>
        /// <param name="apiContext">Must contain the context.</param>
        /// <param name="jsonBody">set by the method to an original JSON body text.</param>
        /// <returns>A parsed document on success or null upon failure.</returns>
        private static async
#if NETCOREAPP
            Task<JsonDocument?>
#else
            Task<JsonDocument>
#endif
            LoadJSONRequestAsync(APIContext apiContext, Stream inputStream,
#if NETCOREAPP
                string?[]
#else
                string[]
#endif

                jsonBody, CancellationToken cancellationToken = default)
        {
            if (jsonBody.Length < 1)
                return null;

            jsonBody[0] = null;
#if NETCOREAPP
            JsonDocument?
#else
            JsonDocument
#endif
                doc = null;

            if (apiContext == null)
            {
                return null;
            }

            apiContext.HttpStatusCode = 0;
            try
            {
                StreamReader reader = new StreamReader(inputStream, UTF8Encoding.UTF8, false, 1024, leaveOpen: true);
                jsonBody[0] = await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                apiContext.SetError(
                    APIErrorCodes.FAILED_TO_LOAD_REQUEST,
#if DEBUG
                    ExceptionToString(ex, "Exception occurred when loading JSON in LoadJSONRequest"),
#else
                    APITextMessages.INTERNAL_ERROR,
#endif
                    StatusCodes.Status500InternalServerError);
                return null;
            }
            if (jsonBody[0] == null)
            {
                apiContext.SetError(APIErrorCodes.FAILED_TO_LOAD_REQUEST, "Failed to load the client request", StatusCodes.Status500InternalServerError);
                return null;
            }

            try
            {
                JsonDocumentOptions parseOptions = new JsonDocumentOptions()
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                };
                doc = JsonDocument.Parse(jsonBody[0], parseOptions);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                apiContext.SetError(
                    APIErrorCodes.FAILED_TO_PARSE_REQUEST,
#if DEBUG
                    ExceptionToString(ex, "An exception occurred when parsing JSON in LoadJSONRequest"),
#else
                    APITextMessages.INTERNAL_ERROR,
#endif
                    StatusCodes.Status500InternalServerError);
                return null;
            }

            return doc;
        }

        public static string ExceptionToString(Exception ex,
#if NETCOREAPP
            string?
#else
            string
#endif
            extraMessage = null)
        {
#if DEBUG
            if (!string.IsNullOrEmpty(extraMessage))
            {
                return string.Join("\n", extraMessage, ex.Message, ex.StackTrace);
            }
            else
            {
                return string.Join("\n", ex.Message, ex.StackTrace);
            }
#else
            if (!string.IsNullOrEmpty(extraMessage))
            {
                return extraMessage;
            }
            else
            {
                return APITextMessages.ERROR_SERVER_ENCOUNTERED_ERROR;
            }
#endif
        }

        public void SetError(int apiErrorCode, string responseMessage)
        {
            APIErrorCode = apiErrorCode;
            ResponseMessage = responseMessage;
            HttpStatusCode = APIErrorCodes.ErrorCodeToHttpResponse(apiErrorCode);
        }

        public void SetError(int apiErrorCode, string responseMessage, int httpStatusCode)
        {
            APIErrorCode = apiErrorCode;
            ResponseMessage = responseMessage;
            HttpStatusCode = httpStatusCode;
        }
    }

    /// <summary>
    /// This is the base class for the handlers.
    /// The class is a singleton, i.e., while one instance is created, it is expected that all method work with an APIContext parameter and don't use instance members.
    /// The methods are not static because some methods are virtual and must be overriden in descendants.
    /// </summary>
    public abstract class APIHandlerBase
    {
        protected static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly Dictionary<string, Func<APIContext, CancellationToken, Task>> _dispatchTable;

        public APIHandlerBase()
        {
            _dispatchTable = new Dictionary<string, Func<APIContext, CancellationToken, Task>>();
            AssignHandlers();
        }

        protected abstract void AssignHandlers();

        public virtual bool RecognizesPath(string path)
        {
            string lowerPath = path.ToLower();
            foreach (var key in _dispatchTable.Keys)
            {
                if (key.ToLower().Equals(lowerPath))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Validates the passed session ID or session token.
        /// </summary>
        /// <param name="apiContext">The APIContext instance that carries all information related to request processing.</param>
        public Task ValidateAccessAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            // Default implementation does nothing but can be used to validate a token, if one is used
            // Also, if a session ID is specified, maybe the validator can check it for validity quickly
            return Task.CompletedTask;
        }

        public static bool CheckSessionID(APIContext apiContext)
        {
#if MOCK_SERVER
            // In mock mode, just check that a session ID string is present
            if (string.IsNullOrEmpty(apiContext.SessionID))
            {
                apiContext.SetError(APIErrorCodes.NO_SESSION_ID, "Authorization header must contain 'SessionID <value>' where <value> is a session identifier");
                return false;
            }
#else
            // In production mode, validate it's a proper GUID
            if (Guid.Empty.Equals(apiContext.SessionGUID)
                /*
                 * When authentication tokens are used, uncomment this block
                && string.IsNullOrEmpty(apiContext.AuthToken)
                */
                )
            {
                apiContext.SetError(APIErrorCodes.NO_SESSION_ID, APITextMessages.ERROR_NO_SESSION_ID);

                // the response with the error code and message will be sent by the upper-level method.
                return false;
            }
#endif
            return true;
        }

        public async Task ProcessRequestAsync(HttpContext context)
        {
#if NETCOREAPP
            CancellationToken cancellationToken = context.RequestAborted;
#else
            CancellationToken cancellationToken = context.Response.ClientDisconnectedToken;
#endif
            // Pick the object from the pool
            APIContext apiContext = APIContext.GetContext(context);
            try
            {
                try
                {
                    // Initialize the context and handle possible initialization errors
                    await APIContext.PreProcessRequestAsync(context, apiContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    apiContext.SetError(
                        APIErrorCodes.INTERNAL_REQUEST_INITIALIZATION_ERROR,
#if DEBUG
                        APIContext.ExceptionToString(ex, APITextMessages.ERROR_INITIALIZATION_FAILED),
#else
                        APITextMessages.ERROR_INITIALIZATION_FAILED,
#endif
                        StatusCodes.Status500InternalServerError);

                }
                try
                {
                    // Check an access token etc.
                    if (apiContext.APIErrorCode == 0)
                        await ValidateAccessAsync(apiContext, cancellationToken).ConfigureAwait(false);

                    // if no error occurred earlier, dispatch the request
                    if (apiContext.APIErrorCode == 0)
                    {
                        await DispatchRequestAsync(apiContext, cancellationToken).ConfigureAwait(false);
                    }
                }
                // Here, one can insert some global error translation code that will convert exceptions to different API error codes and messages. The example is shown in ClientAPIHandler.InvokeHandlerAsync
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.Count == 1)
                    {
                        if (apiContext.APIErrorCode == 0)
                        {
                            //todo: log the exception
                            apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(ex.InnerExceptions[0], string.Format(APITextMessages.EXCEPTION_IN_PATH_S, apiContext.Request.Path)));
                        }
                    }
                    else
                    {
                        foreach (Exception iex in ex.InnerExceptions)
                        {
                            //todo: log the exception
                        }
                        if (apiContext.APIErrorCode == 0)
                        {
                            apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(ex, string.Format(APITextMessages.EXCEPTION_IN_PATH_S, apiContext.Request.Path)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (apiContext.APIErrorCode == 0)
                    {
                        //todo: log the exception
                        apiContext.SetError(APIErrorCodes.INTERNAL_EXCEPTION, APIContext.ExceptionToString(ex, string.Format(APITextMessages.EXCEPTION_IN_PATH_S, apiContext.Request.Path)));
                    }
                }

                // A handler of a particular request can call SendResponseAsync on its side, then we don't do anything here.
                // But if an error was returned or the response was prepared but not sent, send the appropriate response now.
                if (!apiContext.ResponseSent)
                {
                    if (apiContext.HttpStatusCode == StatusCodes.Status418ImATeapot)
                    {
                        // do nothing - this status indicates that the client connection is gone and there is nowhere to send the response to
                    }
                    else
                    if (apiContext.ResponseObject != null) // The use of ResponseObject is optional, it is just more convenient to initialize it instead of sending the response instantly in the handlers' code
                    {
                        StringBuilder builder = new StringBuilder();
                        if (apiContext.ResponseObject is List<Dictionary<string, object>> list)
                            StrUtils.AppendListToJson(list, builder);
                        else
                        if (apiContext.ResponseObject is Dictionary<string, object> dict)
                            StrUtils.AppendObjectToJson(dict, builder);

                        string response = builder.ToString();
                        await SendResponseAsync(apiContext, response, APIConstants.CONTENT_TYPE_APPLICATION_JSON, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    if (apiContext.HttpStatusCode >= StatusCodes.Status400BadRequest)
                    {
                        // Execution comes here only in the case of errors: successful responses are sent by the APIHandler
                        await SendErrorResponseAsync(apiContext, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException cex)
            {
                // do nothing - the request was cancelled due to the client connection being closed
            }
            catch (Exception ex)
            {
                // todo: log errors
            }
            finally
            {
                // Return the context to the pool
                APIContext.ReturnContext(apiContext);
            }
        }

        /// <summary>
        /// The request processing dispatcher.
        /// </summary>
        /// <param name="apiContext">The APIContext instance that carries all information related to request processing.</param>
        public Task DispatchRequestAsync(APIContext apiContext, CancellationToken cancellationToken = default)
        {
            foreach (var pathPair in _dispatchTable)
            {
                if (apiContext.Request.Path.Equals(pathPair.Key))
                {
                    return InvokeHandlerAsync(pathPair.Value, apiContext, cancellationToken);
                }
            }
            apiContext.SetError(APIErrorCodes.ENDPOINT_NOT_FOUND, string.Format(APITextMessages.ERROR_ENDPOINT_NOT_FOUND_S, apiContext.Request.Path));
            return Task.CompletedTask;
        }

        protected virtual Task InvokeHandlerAsync(Func<APIContext, CancellationToken, Task> callee, APIContext apiContext, CancellationToken cancellationToken = default)
        {
            return callee(apiContext, cancellationToken);
            // Here or in the descendant method, one can insert some global error translation code that will convert exceptions to different API error codes and messages. The example is shown in ClientAPIHandler.InvokeHandlerAsync
        }

        #region Response sending

        /// <summary>
        /// Sends a response based on the provided context data.
        /// </summary>
        /// <param name="context">The context object.</param>
        public static Task SendErrorResponseAsync(APIContext context, CancellationToken cancellationToken = default)
        {
            if (context.HttpStatusCode == StatusCodes.Status418ImATeapot) // This status indicates that the previous response could not be sent due to socket disconnection or a similar problem. Thus, no reason to try to send this error.
                return Task.CompletedTask;

            string responseText = APIContext.BuildErrorResponse(context);

            return SendResponseAsync(context, responseText, APIConstants.CONTENT_TYPE_APPLICATION_JSON, cancellationToken);
        }

        /// <summary>
        /// Sends a response based on the provided context data.
        /// </summary>
        /// <param name="context">The context object.</param>
        public static Task SendErrorResponseAsync(APIContext context, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            if (context.HttpStatusCode == StatusCodes.Status418ImATeapot) // This status indicates that the previous response could not be sent due to socket disconnection or a similar problem. Thus, no reason to try to send this error.
                return Task.CompletedTask;

            string responseText = APIContext.BuildErrorResponse(context);

            return SendResponseAsync(context, headers, responseText, APIConstants.CONTENT_TYPE_APPLICATION_JSON, cancellationToken);
        }

        /// <summary>
        /// Sends binary data as a response.
        /// </summary>
        public static Task SendResponseAsync(APIContext context, byte[] binaryData, string contentType, CancellationToken cancellationToken = default)
        {
            byte[] response;
            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (binaryData != null)
            {
                response = binaryData;
            }
            else
                response = null;

            if (!string.IsNullOrEmpty(contentType))
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_TYPE, contentType);
            else
            if (response != null)
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_TYPE, APIConstants.CONTENT_TYPE_APPLICATION_OCTET_STREAM);

            return SendResponseAsync(context, headers, response, cancellationToken);
        }

        /// <summary>
        /// Sends binary data as a response.
        /// </summary>
        public static Task SendResponseAsync(APIContext context, Stream stream, string contentType, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(contentType))
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_TYPE, contentType);
            else
            if (stream != null)
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_TYPE, APIConstants.CONTENT_TYPE_APPLICATION_OCTET_STREAM);

            return SendResponseAsync(context, headers, stream, cancellationToken);
        }

        /// <summary>
        /// Sends a text message as a response.
        /// </summary>
        public static Task SendResponseAsync(APIContext context, string message, string contentType, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            return SendResponseAsync(context, headers, message, contentType, cancellationToken);
        }

        /// <summary>
        /// Sends a text message as a response.
        /// </summary>
        public static Task SendResponseAsync(APIContext context, Dictionary<string, string> headers, string message, string contentType, CancellationToken cancellationToken = default)
        {
            if (headers == null)
                headers = new Dictionary<string, string>();
            byte[] response;
            if (message != null)
            {
                response = UTF8Encoding.UTF8.GetBytes(message);
            }
            else
                response = null;

            if (!string.IsNullOrEmpty(contentType))
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_TYPE, contentType);
            else
            if (response != null)
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_TYPE, APIConstants.CONTENT_TYPE_APPLICATION_JSON);

            return SendResponseAsync(context, headers, response, cancellationToken);
        }

        /// <summary>
        /// Sends a binary body with a set of headers as a response.
        /// </summary>
        public static async Task SendResponseAsync(APIContext context, Dictionary<string, string> headers, byte[] responseBody, CancellationToken cancellationToken = default)
        {
            string value;

            // Set Status code
            context.Response.StatusCode = context.HttpStatusCode == 0 ? StatusCodes.Status200OK : context.HttpStatusCode;

            context.ResponseSent = true;

            // Add common headers
            if (headers == null)
                headers = new Dictionary<string, string>();

            headers.Add(APIConstants.HTTP_HEADER_DATE, DateTime.UtcNow.ToString("r")); // RFC 1123 format used in HTTP
            headers.Add(APIConstants.HTTP_HEADER_SERVER, "MyWorkDrive"); // todo: pick the name from wherever it is defined

            if (context.HttpStatusCode != StatusCodes.Status204NoContent && !headers.ContainsKey(APIConstants.HTTP_HEADER_CONTENT_LENGTH))
            {
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_LENGTH, ((responseBody != null) ? responseBody.Length : 0).ToString());
            }

            if (context.HttpStatusCode == StatusCodes.Status401Unauthorized && !headers.ContainsKey(APIConstants.HTTP_HEADER_WWW_AUTHENTICATE))
            {
                headers.Add(APIConstants.HTTP_HEADER_WWW_AUTHENTICATE, APIConstants.HTTP_HEADER_VALUE_SESSIONID);
            }

            foreach (string header in headers.Keys)
            {
                value = headers[header];
#if NETCOREAPP
                context.Response.Headers.Append(header, value ?? "");
#else
                context.Response.Headers.Set(header, value != null ? value : "");
#endif
            }

            if (responseBody != null && responseBody.Length > 0)
            {
                try
                {
#if NETCOREAPP
                    Stream os = context.Response.Body;
#else
                    Stream os = context.Response.OutputStream;
#endif
                    if (os != null)
                    {
                        await os.WriteAsync(responseBody, 0, responseBody.Length, cancellationToken).ConfigureAwait(false);
                        await os.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // todo: log an error because in .NET Framework, OutputStream cannot be assigned (otherwise, we'd just assign our stream). So, OutputStream is required to be present
                    }
                }
                catch (Exception)
                {
                    // This occurs when the socket is disconnected, and this specific code is checked later.
                    context.HttpStatusCode = StatusCodes.Status418ImATeapot;
                }
            }

            return;
        }

        /// <summary>
        /// Sends a binary body with a set of headers as a response.
        /// </summary>
        /// <param name="stream">The response data as a stream. If specified, the stream must be reset to the position from which the copying should start.</param>
        public static async Task SendResponseAsync(APIContext context, Dictionary<string, string> headers, Stream stream, CancellationToken cancellationToken = default)
        {
            string value;

            long dataLength = stream != null ? stream.Length : 0;

            // Set Status code
            context.Response.StatusCode = context.HttpStatusCode == 0 ? StatusCodes.Status200OK : context.HttpStatusCode;

            context.ResponseSent = true;

            // Add common headers
            if (headers == null)
                headers = new Dictionary<string, string>();

            headers.Add(APIConstants.HTTP_HEADER_DATE, DateTime.UtcNow.ToString("r")); // RFC 1123 format used in HTTP
            headers.Add(APIConstants.HTTP_HEADER_SERVER, "MyWorkDrive"); // todo: pick the name from wherever it is defined

            if (context.HttpStatusCode != StatusCodes.Status204NoContent && !headers.ContainsKey(APIConstants.HTTP_HEADER_CONTENT_LENGTH))
            {
                headers.Add(APIConstants.HTTP_HEADER_CONTENT_LENGTH, dataLength.ToString());
            }

            if (context.HttpStatusCode == StatusCodes.Status401Unauthorized && !headers.ContainsKey(APIConstants.HTTP_HEADER_WWW_AUTHENTICATE))
            {
                headers.Add(APIConstants.HTTP_HEADER_WWW_AUTHENTICATE, APIConstants.HTTP_HEADER_VALUE_SESSIONID);
            }

            foreach (string header in headers.Keys)
            {
                value = headers[header];
#if NETCOREAPP
                context.Response.Headers.Append(header, value ?? "");
#else
                context.Response.Headers.Set(header, value != null ? value : "");
#endif
            }

            if (dataLength > 0 && stream != null)
            {
                try
                {
#if NETCOREAPP
                    Stream os = context.Response.Body;
#else
                    Stream os = context.Response.OutputStream;
#endif
                    if (os != null)
                    {
                        await stream.CopyToAsync(os, 81920, cancellationToken).ConfigureAwait(false);
                        await os.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // todo: log an error because in .NET Framework, OutputStream cannot be assigned (otherwise, we'd just assign our stream). So, OutputStream is required to be present
                    }
                }
                catch (Exception)
                {
                    // This occurs when the socket is disconnected, and this specific code is checked later.
                    context.HttpStatusCode = StatusCodes.Status418ImATeapot;
                }
            }

            return;
        }

        #endregion

        #region Helper functions

        protected static bool PickParameter(APIContext apiContext, string name, bool mustExist, bool mustBeFilled,
#if NETCOREAPP
            string? defaultValue, out string? value
#else
            string defaultValue, out string value
#endif
            )
        {
            object objval;
            if (apiContext.RequestParameters.TryGetValue(name, out objval))
            {
                if (objval == null)
                {
                    if (mustBeFilled)
                    {
                        apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, "", name));
                        value = null;
                        return false;
                    }
                    else
                    {
                        value = null;
                        return true;
                    }
                }
                if (!(objval is string strval))
                {
                    apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, objval.ToString(), name));
                    value = null;
                    return false;
                }
                else
                {
                    if (string.IsNullOrEmpty(strval) && mustBeFilled)
                    {
                        apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, "", name));
                        value = null;
                        return false;
                    }
                    else
                    {
                        value = strval;
                        return true;
                    }
                }
            }
            else
            if (mustExist)
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_MISSING_PARAM_S, name));
                value = null;
                return false;
            }
            else
            {
                value = defaultValue;
                return true;
            }
        }

        protected static bool PickParameter(APIContext apiContext, string name, bool mustExist, bool mustBeFilled,
#if NETCOREAPP
            DateTime? defaultValue, out DateTime? value
#else
            DateTime defaultValue, out DateTime value
#endif
            )
        {
            object objval;
            if (apiContext.RequestParameters.TryGetValue(name, out objval))
            {
                if (objval == null)
                {
                    if (mustBeFilled)
                    {
                        apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, "", name));
#if NETCOREAPP
                        value = null;
#else
                        value = DateTime.MinValue;
#endif
                        return false;
                    }
                    else
                    {
#if NETCOREAPP
                        value = null;
#else
                        value = DateTime.MinValue;
#endif
                        return true;
                    }
                }
                if (objval is DateTime dtval)
                {
                    value = dtval;
                    return true;
                }
                else
                if (!(objval is string strval))
                {
                    apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, objval.ToString(), name));
#if NETCOREAPP
                        value = null;
#else
                    value = DateTime.MinValue;
#endif
                    return false;
                }
                else
                if (string.IsNullOrEmpty(strval) && mustBeFilled)
                    {
                        apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, "", name));
#if NETCOREAPP
                    value = null;
#else
                        value = DateTime.MinValue;
#endif
                        return false;
                    }
                    else
                {
                    DateTime result;
                    try
                    {
                        result = StrUtils.ParseDateISO8601(strval);
                        if (result != DateTime.MinValue)
                        {
                            value = result;
                        return true;
                        }
                    }
                    catch
                    {
                        result = DateTime.MinValue;
                    }
                    try
                    {
                        result = DateTime.Parse(strval);
                        if (result != DateTime.MinValue)
                        {
                            value = result;
                            return true;
                        }
                    }
                    catch
                    {
                        result = DateTime.MinValue;
                    }
#if NETCOREAPP
                    value = null;
#else
                    value = DateTime.MinValue;
#endif
                    return false;
                }
            }
            else
            if (mustExist)
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_MISSING_PARAM_S, name));
#if NETCOREAPP
                value = null;
#else
                value = DateTime.MinValue;
#endif
                return false;
            }
            else
            {
                value = defaultValue;
                return true;
            }
        }

        protected static bool PickParameter(APIContext apiContext, string name, bool mustExist,
#if NETCOREAPP
            bool? defaultValue, out bool? value
#else
            bool defaultValue, out bool value
#endif
            )
        {
            object objval;
            if (apiContext.RequestParameters.TryGetValue(name, out objval))
            {
                if (objval == null)
                {
#if NETCOREAPP
                    value = null;
#else
                    value = false;
#endif
                    return true;
                }
                if (!(objval is bool boolval))
                {
                    apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, objval.ToString(), name));
#if NETCOREAPP
                    value = null;
#else
                    value = false;
#endif
                    return false;
                }
                else
                {
                    value = boolval;
                    return true;
                }
            }
            else
            if (mustExist)
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_MISSING_PARAM_S, name));
#if NETCOREAPP
                value = null;
#else
                value = false;
#endif
                return false;
            }
            else
            {
                value = defaultValue;
                return true;
            }
        }

        protected static bool PickParameter(APIContext apiContext, string name, bool mustExist,
#if NETCOREAPP
            long? defaultValue, out long? value
#else
            long defaultValue, out long value
#endif
            )
        {
            object objval;
            if (apiContext.RequestParameters.TryGetValue(name, out objval))
            {
                if (objval == null)
                {
#if NETCOREAPP
                    value = null;
#else
                    value = 0;
#endif
                    return true;
                }
                if (!(objval is long longval))
                {
                    apiContext.SetError(APIErrorCodes.INVALID_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_INVALID_PARAM_OS, objval.ToString(), name));
#if NETCOREAPP
                    value = null;
#else
                    value = 0;
#endif
                    return false;
                }
                else
                {
                    value = longval;
                    return true;
                }
            }
            else
            if (mustExist)
            {
                apiContext.SetError(APIErrorCodes.MISSING_PARAMETER_VALUE, string.Format(APITextMessages.ERROR_MISSING_PARAM_S, name));
#if NETCOREAPP
                value = null;
#else
                value = 0;
#endif
                return false;
            }
            else
            {
                value = defaultValue;
                return true;
            }
        }

        public static bool IsPathEncoded(string path)
        {
            string decodedPath = Uri.UnescapeDataString(path);
            return !decodedPath.Equals(path, StringComparison.Ordinal);
        }

        #endregion
    }
}
