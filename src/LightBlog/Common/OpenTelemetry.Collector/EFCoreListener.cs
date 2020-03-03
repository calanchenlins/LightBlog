using OpenTelemetry.Collector;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Common.OpenTelemetry.Collector
{
    public class EFCoreListener : ListenerHandler
    {
        public EFCoreListener(string name, Tracer tracer)
            : base(name, tracer)
        {
        }
        public override void OnStartActivity(Activity activity, object payload)
        {
            throw new NotImplementedException();
        }
    }
}
