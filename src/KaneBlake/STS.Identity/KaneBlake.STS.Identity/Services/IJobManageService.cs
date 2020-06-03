﻿using Hangfire.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Services
{
    public interface IJobManageService
    {
        void RecurringJobAddOrUpdate(string id, string targetTypeName, string targetMethodName, string cronExpression, TimeZoneInfo timeZone = null, string queue = EnqueuedState.DefaultQueue);
    }
}
