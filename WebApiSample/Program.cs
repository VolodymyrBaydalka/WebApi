using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using DuncanApps.WebApi;

namespace WebApiSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new WebApiClient("https://api.github.com").Build<IGitHub>();
            var result = client.RateLimit();
            var json = client.RateLimitJson();

            Console.WriteLine(result);
            Console.WriteLine(json);

            Console.ReadKey();
        }
    }
}
