using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;

namespace KaneBlake.AspNetCore.Extensions.ConnectedServices
{
    public class WcfClientManager<T, TChannel> : IWcfClient<T, TChannel>
        where T : ClientBase<TChannel>, TChannel
        where TChannel : class
    {
        private readonly IWcfClientFactory _factory;

        private readonly ConcurrentDictionary<string, Lazy<T>> _cache = new ConcurrentDictionary<string, Lazy<T>>(StringComparer.Ordinal);

        public WcfClientManager(IWcfClientFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public virtual T Get(string EndpointAddress)
        {
            if (string.IsNullOrEmpty(EndpointAddress))
            {
                return default;
            }
            return _cache.GetOrAdd(EndpointAddress, new Lazy<T>(() => _factory.Create<T, TChannel>(EndpointAddress))).Value;
        }
    }
}
