﻿using KaneBlake.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.Extensions.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    [AutoInjection(ServiceLifetime.Singleton, typeof(IHostedService))]
    public interface IAutoInjectionHostedService : IHostedService { }
}
