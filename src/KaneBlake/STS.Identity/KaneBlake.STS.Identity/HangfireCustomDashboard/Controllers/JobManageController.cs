using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using CoreWeb.Util.Infrastruct;
using Hangfire;
using Hangfire.Logging;
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
    [Route("/hangfireapi/[controller]")]
    [ApiController]
    public class JobManageController : ControllerBase
    {
        private readonly IJobManageService _jobManageService;

        public JobManageController(IJobManageService jobManageService)
        {
            _jobManageService = jobManageService ?? throw new ArgumentNullException(nameof(jobManageService));
        }

        [HttpPost]
        [Route("RecurringJob/add")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddRecurringJob([FromForm] string TN, [FromForm] string MN, [FromForm] string CE)
        {
            //await _serviceProvider.RecurringJobAddOrUpdateAsync(Guid.NewGuid().ToString(), "CoreWeb.Util.Infrastruct.SqlClientHelp, KaneBlake.Basis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "ExecuteTSqlInTran", Cron.Minutely());
            await _jobManageService.RecurringJobAddOrUpdateAsync(Guid.NewGuid().ToString(), TN, MN, CE);
            return Ok();
        }

        [HttpPost]
        [Route("RecurringJob/all")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public IActionResult GetAllJobEntries()
        {
            return Ok(_jobManageService.GetAllJobEntries());
        }

    }
}
