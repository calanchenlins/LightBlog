using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace LightBlog.Common.Logging.File
{
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;

        private readonly FileLoggerProcess _fileLoggerProcess;

        internal FileLoggerOptions Options { get; set; }

        public FileLogger(string categoryName, FileLoggerProcess fileLoggerProcess)
        {
            _categoryName = categoryName;
            _fileLoggerProcess = fileLoggerProcess;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= Options.LogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            if (exception != null)
            {
                var t = "";
            }
            var StackTrace = exception==null ?"": $@"
            *************************************************************************************
            {exception.StackTrace ?? ""}
            *************************************************************************************";
            var message = formatter(state, exception);
            _fileLoggerProcess.EnqueueMessage($@"{logLevel}[{_categoryName}]{message}{StackTrace}");
        }
    }
}
