using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.Services.Module
{
    /// <summary>
    /// Options class provides configuration for the <see cref="ApplicationServiceCacheEntryResolver"/>.
    /// </summary>
    public class ApplicationServiceOptions
    {
        public Dictionary<string, ServiceConfiguration> Services { get; set; }

        /// <summary>
        /// Represent configuration of application service
        /// </summary>
        public class ServiceConfiguration
        {
            /// <summary>
            /// The fully qualified name of application service request parameter's type
            /// </summary>
            public string Parameter { get; set; }

            /// <summary>
            /// The fully qualified name of application service returnValue's type
            /// </summary>
            public string ReturnValue { get; set; }

            /// <summary>
            /// The list of application service component
            /// </summary>
            public List<ComponentConfiguration> Components { get; set; }



            /// <summary>
            /// Represent configuration of component
            /// </summary>
            public class ComponentConfiguration
            {
                /// <summary>
                /// The order of component
                /// </summary>
                public int Order { get; set; } = -1;

                /// <summary>
                /// The name of component
                /// </summary>
                public string Name { get; set; }
            }
        }

    }
}
