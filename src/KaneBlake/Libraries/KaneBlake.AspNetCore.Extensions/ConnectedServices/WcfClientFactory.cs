using System;
using System.Collections.Generic;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace K.AspNetCore.Extensions.ConnectedServices
{
    public class WcfClientFactory : IWcfClientFactory
    {
        private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;

        public WcfClientFactory(IHttpMessageHandlerFactory httpMessageHandlerFactory)
        {
            _httpMessageHandlerFactory = httpMessageHandlerFactory ?? throw new ArgumentNullException(nameof(httpMessageHandlerFactory));
        }

        public T Create<T, TChannel>(string EndpointAddress) where T : ClientBase<TChannel>, TChannel where TChannel : class
        {
            var client = (T)Activator.CreateInstance(typeof(T), EndpointAddress);
            client.Endpoint.EndpointBehaviors.Add(new CustomEndpointBehavior(_httpMessageHandlerFactory));
            Task.Factory.FromAsync(((ICommunicationObject)client).BeginOpen(null, null), new Action<IAsyncResult>(((ICommunicationObject)client).EndOpen)).ConfigureAwait(false).GetAwaiter().GetResult();
            return client;
        }
    }
}
