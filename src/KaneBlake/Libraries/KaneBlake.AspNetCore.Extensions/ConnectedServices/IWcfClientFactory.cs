using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;

namespace K.AspNetCore.Extensions.ConnectedServices
{
    public interface IWcfClientFactory
    {
        T Create<T, TChannel>(string EndpointAddress)
            where T : ClientBase<TChannel>, TChannel
            where TChannel : class;
    }
}
