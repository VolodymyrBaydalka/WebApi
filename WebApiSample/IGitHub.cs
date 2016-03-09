using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZV.WebApi;

namespace WebApiSample
{
    public interface IGitHub
    {
        [Get("rate_limit")]
        JObject RateLimit();
    }
}
