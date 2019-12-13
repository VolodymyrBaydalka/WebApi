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

        public Uri BaseAddress { get; set; }

        public JsonSerializerSettings JsonSerializerSettings { get; set; }

        public Func<HttpClient> CreateHttpClient { get; set; }

        public WebApiClient()
        {
        }

        public WebApiClient(string baseAddress)
        {
            this.BaseAddress = new Uri(baseAddress);
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

            using (var httpClient = OnCreateHttpClient())
            {
                var mediaType = webApi == null ? MediaType.Json : webApi.MediaType;
                var uriTemplate = webMethod == null ? null : webMethod.UriTemplate;
                var requestUri = BuildRequestUri(uriTemplate, methodInfo.Name, pathParameters, queryParameters);
                var formatters = CreateMediaTypeFormatterCollection();

                SetHeaders(httpClient, apiHeaders);
                SetHeaders(httpClient, methodHeaders);

                Task<HttpResponseMessage> responseTask = null;

                if (webMethod == null || webMethod is GetAttribute)
                {
                    responseTask = httpClient.GetAsync(requestUri);
                }
                else if (webMethod is PostAttribute)
                {
                    responseTask = httpClient.PostAsync(requestUri, SerializeContent(mediaType, bodyType, bodyParam, formatters));
                }
                else if (webMethod is PutAttribute)
                {
                    responseTask = httpClient.PutAsync(requestUri, SerializeContent(mediaType, bodyType, bodyParam, formatters));
                }
                else if (webMethod is DeleteAttribute)
                {
                    responseTask = httpClient.DeleteAsync(requestUri);
                }

                return HandleResponseTask(responseTask, methodInfo.ReturnType, formatters);
            }
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
                    var arrayValue = item.Value as ICollection;

                    if (arrayValue == null)
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

            return uriBuilder.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="responseTask"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        private object HandleResponseTask(Task<HttpResponseMessage> responseTask, Type returnType, MediaTypeFormatterCollection formatters)
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
                return response.Content.ReadAsAsync(returnType, formatters).Result;

            throw new WebApiStatusException(response.StatusCode, response.ReasonPhrase);
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
