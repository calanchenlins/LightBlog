using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Basis.Common.Logging.File
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, FileLogger> _loggers;

        private readonly FileLoggerProcess _fileLoggerProcess = null;

        private readonly FileLoggerOptions _options;

        public FileLoggerProvider(IOptions<FileLoggerOptions> options, IOptionsMonitor<FileLoggerOptions> optionsMonitor)
        {
            _loggers = new ConcurrentDictionary<string, FileLogger>();
            _options = options.Value;
            _fileLoggerProcess = new FileLoggerProcess(_options?.FileName??"log.txt");
        }
        
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, 
                new FileLogger(categoryName, _fileLoggerProcess) { Options= _options }
                );
        }

        public void Dispose()
        {
            _loggers.Clear();
            _fileLoggerProcess.Dispose();
        }
    }
}
