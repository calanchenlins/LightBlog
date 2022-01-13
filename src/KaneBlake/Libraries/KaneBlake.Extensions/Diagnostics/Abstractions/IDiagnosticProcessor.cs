using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Extensions.Diagnostics.Abstractions
{
    public interface IDiagnosticProcessor
    {
        string ListenerName { get; }
    }
}
