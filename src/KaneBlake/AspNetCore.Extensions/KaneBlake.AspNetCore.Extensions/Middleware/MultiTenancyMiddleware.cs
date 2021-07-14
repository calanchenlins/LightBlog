using KaneBlake.AspNetCore.Extensions.MultiTenancy;
using KaneBlake.Basis.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Middleware
{
    public class MultiTenancyMiddleware<T> where T : class
    {
        private readonly RequestDelegate _next;
        private readonly MultiTenancyOptions<T> _options;
        private readonly ILogger _logger;

        public MultiTenancyMiddleware(RequestDelegate next, IOptions<MultiTenancyOptions<T>> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _logger = loggerFactory?.CreateLogger<MultiTenancyMiddleware<T>>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _options = options.Value;
        }


        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            TenantInfo<T> tenantInfo = null;

            if (_options.RequestTenantProviders != null)
            {
                foreach (var provider in _options.RequestTenantProviders)
                {
                    var providerTenantResult = await provider.DetermineProviderTenantResult(context);

                    if (providerTenantResult != null)
                    {
                        tenantInfo = await _options.TenantService.GetTenantInfoAsync(providerTenantResult.TenantId);
                        if (tenantInfo != null) 
                        {
                            TenantInfo<T>.CurrentTenant = tenantInfo;
                            break;
                        }
                    }
                }
            }

            if (tenantInfo == null) 
            {
                _logger.LogInformation("Resolved null tenantInfo from http request.");
            }

            await _next(context);
        }
    }
}
