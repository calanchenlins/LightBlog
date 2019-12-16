using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Extensions.Logging.File
{
    public class FileLoggerOptions
    {
        public LogLevel LogLevel { get; set; } = LogLevel.None;

        public string FileName { get; set; }
    }
}
