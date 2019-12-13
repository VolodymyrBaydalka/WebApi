using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DuncanApps.WebApi
{
    [Serializable]
    public class WebApiException : Exception
    {
        public WebApiException(string message) : base(message)
        {
        }
    }

    [Serializable]
    public class WebApiStatusException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }

        public WebApiStatusException(HttpStatusCode statusCode, string message) : base(message)
        {
            this.StatusCode = statusCode;
        }
    }
}
