using KaneBlake.Basis.Extensions.Logging.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Extensions.Logging
{
    public static class LoggerExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, IConfiguration configuration)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
            builder.Services.Configure<FileLoggerOptions>(configuration.GetSection("File"));
            return builder;
        }
    }
}
