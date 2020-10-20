using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Build.Core.Localization
{
    public class LocalizableEntry
    {
        public string SourceReference { get; set; }

        public string SourceCode { get; set; }

        private string _ContextId = null;

        public string ContextId { get => _ContextId; set => _ContextId = string.IsNullOrWhiteSpace(value) ? null : value; }

        public string Id { get; set; }
    }
}
