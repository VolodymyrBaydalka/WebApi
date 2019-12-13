using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace DuncanApps.WebApi
{
    static class Utils
    {
        public static MediaTypeFormatter FindType(this MediaTypeFormatterCollection collection, MediaType type)
        {
            switch (type)
            {
                case MediaType.Json:
                    return collection.JsonFormatter;

                case MediaType.Xml:
                    return collection.XmlFormatter;

                case MediaType.FormUrlEncoded:
                    return collection.FormUrlEncodedFormatter;
            }

            return null;
        }

        public static bool IsGenericOf(this Type type, Type generic)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == generic;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static StringBuilder AppendQuery(StringBuilder query, string param, string value)
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
        public static string CombineUri(string baseUri, string path)
        {
            return baseUri.TrimEnd('/') + '/' + path.TrimStart('/');
        }
    }
}
