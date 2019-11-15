using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Basis.Diagnostics.Abstractions
{
    public interface IDiagnosticProcessor
    {
        string ListenerName { get; }
    }
}
