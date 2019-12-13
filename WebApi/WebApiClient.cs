using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DuncanApps.WebApi
{
    public class WebApiClient
    {
        private readonly static Regex _curlyBrackets = new Regex(@"\{(.+?)\}");
        private readonly static MethodInfo _sendAndReadMethod;

        public Uri BaseAddress { get; set; }

        public JsonSerializerSettings JsonSerializerSettings { get; set; }

        public Func<HttpClient> CreateHttpClient { get; set; }

        static WebApiClient()
        {
            _sendAndReadMethod = typeof(WebApiClient).GetMethod(nameof(SendAndReadContentAsync), BindingFlags.NonPublic | BindingFlags.Static);
        }

        public WebApiClient(string baseAddress = null)
        {
            this.BaseAddress = baseAddress == null ? null : new Uri(baseAddress, UriKind.RelativeOrAbsolute);
        }

        protected virtual HttpClient OnCreateHttpClient()
        {
            return CreateHttpClient == null ? new HttpClient() : CreateHttpClient();
        }

        protected HttpContent SerializeContent(MediaType mediaType, Type bodyType, object body, MediaTypeFormatterCollection formatters)
        {
            return new ObjectContent(bodyType, body, formatters.FindType(mediaType));
        }

        public T Build<T>()
        {
            var type = typeof(T);

            if (!type.IsInterface)
                throw new ArgumentException("Only interfaces are supported");

            return (T)new WebClientProxy(type, this).GetTransparentProxy();
        }

        protected MediaTypeFormatterCollection CreateMediaTypeFormatterCollection()
        {
            var jsonMediaTypeFormatter = new JsonMediaTypeFormatter();

            if (this.JsonSerializerSettings != null)
                jsonMediaTypeFormatter.SerializerSettings = this.JsonSerializerSettings;

            return new MediaTypeFormatterCollection(new MediaTypeFormatter[] {
                new XmlMediaTypeFormatter(),
                new FormUrlEncodedMediaTypeFormatter(),
                jsonMediaTypeFormatter
            });
        }

        internal object Send(MethodInfo methodInfo, object[] parameterValues)
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
                else if (attr is FieldAttribute)
                {
                    if (bodyParam != fieldParameters)
                        throw new NotSupportedException("Only one body parameter is supported.");

                    var alias = string.IsNullOrEmpty(attr.Alias) ? param.Name : attr.Alias;
                    fieldParameters.Add(alias, value);
                }
            }


            var mediaType = webApi == null ? MediaType.Json : webApi.MediaType;
            var uriTemplate = webMethod?.UriTemplate;
            var requestUri = BuildRequestUri(uriTemplate, methodInfo.Name, pathParameters, queryParameters);
            var formatters = CreateMediaTypeFormatterCollection();

            var request = new HttpRequestMessage
            {
                RequestUri = requestUri
            };

            SetHeaders(request, apiHeaders);
            SetHeaders(request, methodHeaders);

            if (webMethod == null || webMethod is GetAttribute)
            {
                request.Method = HttpMethod.Get;
            }
            else if (webMethod is PostAttribute)
            {
                request.Method = HttpMethod.Post;
                request.Content = SerializeContent(mediaType, bodyType, bodyParam, formatters);
            }
            else if (webMethod is PutAttribute)
            {
                request.Method = HttpMethod.Put;
                request.Content = SerializeContent(mediaType, bodyType, bodyParam, formatters);
            }
            else if (webMethod is DeleteAttribute)
            {
                request.Method = HttpMethod.Delete;
            }

            return SendInternal(request, methodInfo.ReturnType, formatters);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uriTemplate"></param>
        /// <param name="methodName"></param>
        /// <param name="pathParams"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        private Uri BuildRequestUri(string uriTemplate, string methodName, Dictionary<string, object> pathParams, Dictionary<string, object> queryParams)
        {
            var uriBuilder = new UriBuilder(BaseAddress);

            if (!string.IsNullOrEmpty(uriTemplate))
            {
                methodName = _curlyBrackets.Replace(uriTemplate, x =>
                {
                    return Convert.ToString(pathParams[x.Groups[1].Value]);
                });
            }

            uriBuilder.Path = Utils.CombineUri(uriBuilder.Path, methodName);

            if (queryParams.Count > 0)
            {
                var queryBuider = new StringBuilder(uriBuilder.Query);

                foreach (var item in queryParams)
                {
                    if (!(item.Value is ICollection arrayValue))
                    {
                        Utils.AppendQuery(queryBuider, item.Key, Convert.ToString(item.Value));
                    }
                    else
                    {
                        foreach (var value in arrayValue)
                        {
                            Utils.AppendQuery(queryBuider, item.Key, Convert.ToString(value));
                        }
                    }
                }

                uriBuilder.Query = queryBuider.ToString();
            }

            return new Uri(uriBuilder.ToString(), UriKind.RelativeOrAbsolute);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="responseTask"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        private object SendInternal(HttpRequestMessage request, Type returnType, MediaTypeFormatterCollection formatters)
        {
            if (returnType.IsGenericOf(typeof(Task<>)))
            {
                var generic = _sendAndReadMethod.MakeGenericMethod(returnType.GetGenericArguments()[0]);
                return generic.Invoke(null, new object[] { this, request, formatters });
            }

            return SendAndReadContentAsync(request, returnType, formatters)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async Task<T> SendAndReadContentAsync<T>(WebApiClient webApiClient, HttpRequestMessage request, MediaTypeFormatterCollection formatters)
        {
            using (var client = webApiClient.OnCreateHttpClient())
            {
                var response = await client.SendAsync(request).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsAsync<T>(formatters).ConfigureAwait(false);

                throw new WebApiStatusException(response.StatusCode, response.ReasonPhrase);
            }
        }

        private async Task<object> SendAndReadContentAsync(HttpRequestMessage request, Type returnType, MediaTypeFormatterCollection formatters)
        {
            using (var client = OnCreateHttpClient())
            {
                var response = await client.SendAsync(request).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsAsync(returnType, formatters).ConfigureAwait(false);

                throw new WebApiStatusException(response.StatusCode, response.ReasonPhrase);
            }
        }

        private static void SetHeaders(HttpRequestMessage request, IEnumerable<HeaderAttribute> headers)
        {
            if (headers == null)
                return;

            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Values);
            }
        }
    }
}
