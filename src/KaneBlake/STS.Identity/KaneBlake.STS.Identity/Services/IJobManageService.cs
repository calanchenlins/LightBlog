using Hangfire.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Services
{
    public interface IJobManageService
    {
        Task BackgroundJobCreateAsync(string targetTypeName, string targetMethodName, string queue = EnqueuedState.DefaultQueue);
        Task BackgroundJobCreateAsync(string targetTypeName, string targetMethodName, DateTime enqueueAt);
        Task RecurringJobAddOrUpdateAsync(RecurringJobInDto recurringJobInDto);
        IEnumerable<JobEntryViewModel> GetAllJobEntries();

        RecurringJobViewModel GetRecurringJobById(string id);
    }
}
