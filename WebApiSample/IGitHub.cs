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
        RateLimits RateLimit();

        [Get("rate_limit")]
        JObject RateLimitJson();

    }

    public class RateLimits
    {
        public Limits Rate { get; set; }

        public Dictionary<string, Limits> Resources { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("Rate: {0}", this.Rate);

            foreach (var res in Resources)
            {
                sb.AppendLine();
                sb.AppendFormat("{0}: {1}", res.Key, res.Value);
            }

            return sb.ToString();
        }
    }

    public class Limits
    {
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public long Reset { get; set; }

        public override string ToString()
        {
            return string.Format("{0} of {1}", this.Remaining, this.Limit);
        }
    }
}
