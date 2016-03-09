using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZV.WebApi
{
    internal class WebApiClient : RealProxy
    {
        private readonly static Regex _curlyBrackets = new Regex(@"\{(.+?)\}");

        public Uri BaseAddress { get; set; }
        public Func<Uri, HttpClient> HttpClientFactory { get; set; }

        public WebApiClient(Type type)
            : base(type)
        {
        }

        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage methodCall = (IMethodCallMessage)msg;

            try
            {
                var response = Send((MethodInfo)methodCall.MethodBase, methodCall.Args);

                return new ReturnMessage(response, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception e)
            {
                return new ReturnMessage(e, methodCall);
            }
        }

        public static HttpClient DefaultFactory(Uri baseAddress)
        {
            return new HttpClient() {
            };
        }

        public object Send(MethodInfo methodInfo, object[] parameterValues)
        {
            var webApi = methodInfo.DeclaringType.GetCustomAttribute<WebApiAttribute>();
            var apiHeaders = methodInfo.DeclaringType.GetCustomAttributes<HeaderAttribute>();

            var webMethod = methodInfo.GetCustomAttribute<MethodAttribute>();
            var methodHeaders = methodInfo.GetCustomAttributes<HeaderAttribute>();

            var parameters = methodInfo.GetParameters();
            var pathParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var queryParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var fieldParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            object bodyParam = fieldParameters;
            Type bodyType = fieldParameters.GetType();

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var value = parameterValues[i];

                var attr = param.GetCustomAttribute<ParameterAttribute>();

                if (attr == null || attr is QueryAttribute)
                {
                    var alias = attr == null || string.IsNullOrEmpty(attr.Alias) ? param.Name : attr.Alias;

                    queryParameters.Add(alias, value);
                }
                else if (attr is PathAttribute)
                {
                    var alias = string.IsNullOrEmpty(attr.Alias) ? param.Name : attr.Alias;
                    pathParameters.Add(alias, value);
                }
                else if (attr is BodyAttribute)
                {
                    if (bodyParam != fieldParameters || fieldParameters.Count != 0)
                        throw new NotSupportedException("Only one body parameter is supported.");

                    bodyParam = value;
                    bodyType = value == null ? param.ParameterType : value.GetType();
                }
                else if(attr is FieldAttribute)
                {
                    if (bodyParam != fieldParameters)
                        throw new NotSupportedException("Only one body parameter is supported.");

                    var alias = string.IsNullOrEmpty(attr.Alias) ? param.Name : attr.Alias;
                    fieldParameters.Add(alias, value);
                }
            }

            using (var httpClient = this.HttpClientFactory(this.BaseAddress))
            {
                var mediaType = webApi == null ? MediaType.Json : webApi.MediaType;
                var uriTemplate = webMethod == null ? null : webMethod.UriTemplate;
                var requestUri = BuildRequestUri(uriTemplate, methodInfo.Name, pathParameters, queryParameters);

                SetHeaders(httpClient, apiHeaders);
                SetHeaders(httpClient, methodHeaders);

                Task<HttpResponseMessage> responseTask = null;

                if (webMethod == null || webMethod is GetAttribute)
                {
                    responseTask = httpClient.GetAsync(requestUri);
                }
                else if(webMethod is PostAttribute)
                {
                    responseTask = httpClient.PostAsync(requestUri, PrepareHttpContent(mediaType, bodyType, bodyParam));
                }
                else if (webMethod is PutAttribute)
                {
                    responseTask = httpClient.PutAsync(requestUri, PrepareHttpContent(mediaType, bodyType, bodyParam));
                }
                else if (webMethod is DeleteAttribute)
                {
                    responseTask = httpClient.DeleteAsync(requestUri);
                }

                return HandleResponseTask(responseTask, methodInfo.ReturnType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mediaType"></param>
        /// <param name="bodyParams"></param>
        /// <returns></returns>
        private HttpContent PrepareHttpContent(MediaType mediaType, Type bodyType, object body)
        {
            MediaTypeFormatter mediaTypeFormatter = null;

            switch (mediaType)
            {
                case MediaType.Json:
                    mediaTypeFormatter = new JsonMediaTypeFormatter();
                    break;

                case MediaType.Xml:
                    mediaTypeFormatter = new XmlMediaTypeFormatter();
                    break;

                case MediaType.FormUrlEncoded:
                    mediaTypeFormatter = new FormUrlEncodedMediaTypeFormatter();
                    break;
            }

            return new ObjectContent(bodyType, body, mediaTypeFormatter);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uriTemplate"></param>
        /// <param name="methodName"></param>
        /// <param name="pathParams"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        private string BuildRequestUri(string uriTemplate, string methodName, Dictionary<string, object> pathParams, Dictionary<string, object> queryParams)
        {
            var uriBuilder = new UriBuilder(this.BaseAddress);

            if (!string.IsNullOrEmpty(uriTemplate))
            {
                methodName = _curlyBrackets.Replace(uriTemplate, x => {
                    return Convert.ToString(pathParams[x.Groups[1].Value]);
                });
            }

            uriBuilder.Path = CombineUri(uriBuilder.Path, methodName);

            if (queryParams.Count > 0)
            {
                var queryBuider = new StringBuilder(uriBuilder.Query);

                foreach (var item in queryParams)
                {
                    var arrayValue = item.Value as ICollection;

                    if (arrayValue == null)
                    {
                        AppendQuery(queryBuider, item.Key, Convert.ToString(item.Value));
                    }
                    else
                    {
                        foreach (var value in arrayValue)
                        {
                            AppendQuery(queryBuider, item.Key, Convert.ToString(value));
                        }
                    }
                }

                uriBuilder.Query = queryBuider.ToString();
            }

            return uriBuilder.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="responseTask"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        private object HandleResponseTask(Task<HttpResponseMessage> responseTask, Type returnType)
        {
            if (returnType == typeof(Task<HttpResponseMessage>))
            {
                return responseTask;
            }
            else if (returnType == typeof(HttpResponseMessage))
            {
                return responseTask.Result;
            }

            var response = responseTask.Result;

            if (response.IsSuccessStatusCode)
                return response.Content.ReadAsAsync(returnType).Result;

            throw new WebApiStatusException(response.StatusCode, response.ReasonPhrase);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static StringBuilder AppendQuery(StringBuilder query, string param, string value)
        {
            if (query.Length != 0)
                query.Append("&");

            return query.Append(param).Append("=").Append(Uri.EscapeDataString(value));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string CombineUri(string baseUri, string path)
        {
            return baseUri.TrimEnd('/') + '/' + path.TrimStart('/');
        }

        private static void SetHeaders(HttpClient client, IEnumerable<HeaderAttribute> headers)
        {
            if (headers == null)
                return;

            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Values);
            }
        }
    }
}
