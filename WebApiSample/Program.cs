using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using ZV.WebApi;

namespace WebApiSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = WebApi.Client<IGitHub>("https://api.github.com", (uri) => {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "WebApi"); // GitHub requires User-Agent
                return httpClient;
            });
            var result = client.RateLimit();

            Console.WriteLine(result);
            Console.ReadKey();
        }
    }
}
