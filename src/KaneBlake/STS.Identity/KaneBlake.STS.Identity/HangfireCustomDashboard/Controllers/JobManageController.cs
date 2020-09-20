using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using CoreWeb.Util.Infrastruct;
using KaneBlake.Basis.Services;
using Hangfire;
using Hangfire.Logging;
using Hangfire.States;
using KaneBlake.STS.Identity.Quickstart;
using KaneBlake.STS.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

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
            //return Problem();
            await _jobManageService.RecurringJobAddOrUpdateAsync(recurringJobInDto);
            //return Problem();
            return Ok();
        }

        [HttpPost]
        [Route("BackgroundJob/add")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateBackgroundJob(BackgroundJobInDto backgroundJobInDto)
        {
            var t = backgroundJobInDto.EnqueueAt.ToUniversalTime();
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


        [HttpPost]
        [Route("testApi/Problem")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public async Task<IActionResult> ProblemC()
        {
            await Task.CompletedTask;
            return Problem();
        }

        [HttpPost]
        [Route("testApi/Problem400")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        //[Produces("application/xml")]
        public async Task<IActionResult> Problem400(Indto input)
        {
            //var ty = input.EnqueueAt.ToUniversalTime();
            await Task.CompletedTask;

            //var hj = input.EnqueueAt.Value;

            //input.EnqueueAt = default;
            //input.EnqueueAt = input.EnqueueAt.ToLocalTime();
            var tby = new DateTime(2019, 7, 26);
            var tgf = tby.ToUniversalTime();
            var gfss = tgf.ToLocalTime();
            var tjhj = tby.ToLocalTime();
            var tgfd = DateTime.Now;

            tby = DateTime.UtcNow;

            var tgtf = DateTimeOffset.Now;
            var gasa= DateTimeOffset.UtcNow;

            return new ObjectResult(ServiceResponse.OK(input));
        }

        [HttpPost]
        [Route("testApi/Problem404")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public async Task<IActionResult> Problem404()
        {
            await Task.CompletedTask;
            return NotFound();
        }

        [HttpPost]
        [Route("testApi/Problem204")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public async Task<IActionResult> Problem204()
        {
            await Task.CompletedTask;
            return NoContent();
        }

        [HttpPost]
        [Route("testApi/ServiceResponse")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        //[ServiceFilter(typeof(InjectResultActionFilter))]
        public async Task<ActionResult<ServiceResponse>> ServiceResponseOk()
        {
            await Task.CompletedTask;
            var res =  ServiceResponse.OK();
            res.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
            res.Extensions["complexType"] = new { a = "a", b = "b" };
            return res;
        }

        [HttpPost]
        [Route("testApi/ServiceResponse2")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        //[ServiceFilter(typeof(InjectResultActionFilter))]
        public ActionResult<ServiceResponse> ServiceResponseOk2()
        {
            var res = ServiceResponse.OK();
            res.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
            res.Extensions["complexType"] = new { a = "a", b = "b" };
            return res;
        }

        [HttpPost]
        [Route("testApi/ServiceResponse3")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        //[ServiceFilter(typeof(InjectResultActionFilter))]
        public ServiceResponse ServiceResponseOk3()
        {
            var res = ServiceResponse.OK();
            res.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
            res.Extensions["complexType"] = new { a = "a", b = "b" };
            return res;
        }

        [HttpPost]
        [Route("testApi/ServiceResponse4")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        //[ServiceFilter(typeof(InjectResultActionFilter))]
        public IActionResult ServiceResponseOk4()
        {
            var res = ServiceResponse.OK();
            res.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
            res.Extensions["complexType"] = new { a = "a", b = "b" };
            return new ObjectResult(res);
        }


        [HttpPost]
        [Route("testApi/ServiceResponseT")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public async Task<ActionResult<ServiceResponse>> ServiceResponseOkT()
        {
            await Task.CompletedTask;
            var res = ServiceResponse.OK(new { aa = "                aa测试中文bb           ", bb = "           bb             " });
            res.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
            res.Extensions["complexType"] = new { AdminRes = "a", b = "                      b                  ", c = DateTime.Now };
            var dict = new Dictionary<string, string>();
            dict.Add("ServiceResponseIn", "ServiceResponseIn");
            res.Extensions["dict"] = dict;
            //ObjectResult
            return ServiceResponse.OK("aascsacvs测试中文fdsvdvsdcv жаркоfsdvs");
        }

        [HttpPost]
        [Route("testApi/ServiceResponseIn")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public async Task<ActionResult<ServiceResponse>> ServiceResponseOkIn(ServiceResponse input)
        {
            await Task.CompletedTask;
            return input;
        }

        [HttpPost]
        [Route("testApi/ServiceResponseInT")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [AllowAnonymous]
        public async Task<ActionResult<ServiceResponse>> ServiceResponseOkInT(ServiceResponse<string> input)
        {
            await Task.CompletedTask;
            return input;
        }

    }

    public class Indto
    {
        [Required,MinLength(2)]
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public string Queue { get; set; }
        [Required]
        public DateTime? EnqueueAt { get; set; }
    }

    public class BackgroundJobInDto 
    {
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public string Queue { get; set; }
        public DateTime EnqueueAt { get; set; }
    }

}
