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
            var client = WebApi.Client<IGitHub>("https://api.github.com");
            var result = client.RateLimit();
            var json = client.RateLimitJson();

            Console.WriteLine(result);
            Console.WriteLine(json);

            Console.ReadKey();
        }
    }
}
