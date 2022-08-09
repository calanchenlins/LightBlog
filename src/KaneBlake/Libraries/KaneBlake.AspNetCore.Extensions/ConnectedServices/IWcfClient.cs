using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;

namespace K.AspNetCore.Extensions.ConnectedServices
{
    public interface IWcfClient<T, TChannel>
        where T : ClientBase<TChannel>, TChannel
        where TChannel : class
    {
        T Get(string EndpointAddress);
    }
}
