using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.Build.Core.Localization
{
    public class DataStructure
    {
        public string ProjectDirectory { get; set; }
        public Project Project { get; set; }
        public IList<LocalizableEntry> LocalizerEntries { get; private set; }

        private readonly List<IVisitor> Visitors;

        public DataStructure(string projectDirectory)
        {
            ProjectDirectory = projectDirectory ?? throw new ArgumentNullException(nameof(projectDirectory));
            LocalizerEntries = new List<LocalizableEntry>();
            Visitors = new List<IVisitor>();
        }

        public virtual void AcceptVisitor(IVisitor visitor)
        {
            Visitors.Add(visitor);
        }

        public virtual async Task ExecuteAsync()
        {
            var orderedVisitors = Visitors.OrderBy(v => v.Order);
            foreach (var visitor in orderedVisitors)
            {
                await visitor.Visit(this);
            }
        }
    }
}
