using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Hangfire.States;
using KaneBlake.STS.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaneBlake.STS.Identity.HangfireCustomDashboard.Controllers
{
    [Route("/hangfireapi/Manage")]
    [ApiController]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class JobManageController : ControllerBase
    {
        private readonly IJobManageService _jobManageService;

        public JobManageController(IJobManageService jobManageService)
        {
            _jobManageService = jobManageService ?? throw new ArgumentNullException(nameof(jobManageService));
        }

        [HttpPost]
        [Route("RecurringJob/add")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddRecurringJob(RecurringJobInDto recurringJobInDto)
        {
            await _jobManageService.RecurringJobAddOrUpdateAsync(recurringJobInDto);
            return Ok();
        }

        [HttpPost]
        [Route("BackgroundJob/add")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateBackgroundJob(BackgroundJobInDto backgroundJobInDto)
        {
            if (backgroundJobInDto.EnqueueAt == default)
            {
                backgroundJobInDto.Queue = string.IsNullOrEmpty(backgroundJobInDto.Queue) ? EnqueuedState.DefaultQueue : backgroundJobInDto.Queue;
                await _jobManageService.BackgroundJobCreateAsync(backgroundJobInDto.TypeName, backgroundJobInDto.MethodName, backgroundJobInDto.Queue);
            }
            else 
            {
                await _jobManageService.BackgroundJobCreateAsync(backgroundJobInDto.TypeName, backgroundJobInDto.MethodName, backgroundJobInDto.EnqueueAt);
            }
            return Ok();
        }

        [HttpGet]
        [Route("RecurringJob/{id:required}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public RecurringJobViewModel GetRecurringJob(string id)
        {
            return _jobManageService.GetRecurringJobById(id);
        }

        /// <summary>
        /// 获取作业模板
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("JobEntries")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public IEnumerable<JobEntryViewModel> GetAllJobEntries()
        {
            return _jobManageService.GetAllJobEntries();
        }
    }

    public class BackgroundJobInDto 
    {
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public string Queue { get; set; }
        public DateTime EnqueueAt { get; set; }
    }

}
