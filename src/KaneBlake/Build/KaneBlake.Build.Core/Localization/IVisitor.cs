using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.Build.Core.Localization
{
    public interface IVisitor
    {
        int Order { get; set; }
        Task Visit(DataStructure dataStructure);
    }
}
