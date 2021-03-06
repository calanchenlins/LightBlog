﻿using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Basis.Common.Diagnostics
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class DiagnosticAdapterName : Attribute
    {
        public string Name { get; }

        public DiagnosticAdapterName(string name)
        {
            Name = name;
        }
    }
}
