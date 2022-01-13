using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Extensions.Profiling
{
    /// <summary>
    /// Sets up logging options for <see cref="MiniProfilerBaseOptions"/>.
    /// </summary>
    public class MiniProfilerLogConfigureOptions : IConfigureOptions<MiniProfilerBaseOptions>
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes an instance of <see cref="MiniProfilerLogConfigureOptions"/>.
        /// </summary>
        /// <param name="loggerFactory"></param>
        public MiniProfilerLogConfigureOptions(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Add the <see cref="LogStorage"/> to <see cref="MiniProfilerBaseOptions"/>.
        /// </summary>
        /// <param name="options"></param>
        public void Configure(MiniProfilerBaseOptions options)
        {
            if (options.Storage != null)
            {
                options.Storage = new LogStorage(options.Storage, _loggerFactory);
            }
        }
    }
}
