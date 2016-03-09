using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ZV.WebApi
{
    public class WebApi
    {
        public static T Client<T>(string baseAddress, Func<Uri, HttpClient> factory = null)
        {
            var type = typeof(T);

            if (!type.IsInterface)
                throw new ArgumentException("Only interfaces are supported");

            if (factory == null)
                factory = WebApiClient.DefaultFactory;

            var client = new WebApiClient(type);

            client.BaseAddress = new Uri(baseAddress);
            client.HttpClientFactory = factory;

            return (T)client.GetTransparentProxy();
        }
    }
}
