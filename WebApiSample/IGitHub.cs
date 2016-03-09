using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZV.WebApi;

namespace WebApiSample
{
    [Header("User-Agent", "WebApi")]
    public interface IGitHub
    {
        [Get("rate_limit")]
        JObject RateLimit();
    }
}
