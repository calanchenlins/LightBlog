using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;

namespace KaneBlake.CodeAnalysis.Test
{
    [AutoImplementation]
    public partial interface IParameters1
    {
        int MyProperty11 { get; set; }
        string MyProperty12 { get; set; }
        List<string> MyProperty13 { set; }
    }

    public partial interface IParameters1
    {
        public static Type ImplementationType { get; set; }
    }

    [AutoImplementation]
    public interface IParameters2
    {
        string MyProperty11 { get; set; }
        internal string MyProperty12 { get; set; }
        List<string> MyProperty13 { get; }
    }

    [AutoImplementation]
    public interface IParameters3 : IParameters1, IParameters2
    {
        internal protected int MyProperty31 { get; set; }
        string MyProperty32 { get; internal protected set; }
        List<string> MyProperty33 { get; set; }
        IList<string> MyProperty34 { get; set; }
    }

}
