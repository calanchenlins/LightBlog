using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace KaneBlake.AspNetCore.Extensions.ConnectedServices
{
    /// <summary>
    /// https://medium.com/trueengineering/realization-of-the-connections-pool-with-wcf-for-net-core-with-usage-of-httpclientfactory-c2cb2676423e
    /// https://github.com/dotnet/wcf/issues/3230
    /// </summary>
    public class CustomEndpointBehavior : IEndpointBehavior
    {
        private readonly Func<HttpMessageHandler> _httpHandler;

        public CustomEndpointBehavior(IHttpMessageHandlerFactory factory)
        {
            factory.CreateHandler();
            _httpHandler = () => factory.CreateHandler();
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            bindingParameters.Add(new Func<HttpClientHandler, HttpMessageHandler>(handler => _httpHandler()));
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
