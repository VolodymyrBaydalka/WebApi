using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Threading.Tasks;

namespace DuncanApps.WebApi
{
    sealed class WebClientProxy : RealProxy
    {
        private readonly WebApiClient _client;

        public WebClientProxy(Type type, WebApiClient client)
            : base(type)
        {
            _client = client;
        }

        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage methodCall = (IMethodCallMessage)msg;

            try
            {
                var response = _client.Send((MethodInfo)methodCall.MethodBase, methodCall.Args);
                return new ReturnMessage(response, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception e)
            {
                return new ReturnMessage(e, methodCall);
            }
        }
    }
}
