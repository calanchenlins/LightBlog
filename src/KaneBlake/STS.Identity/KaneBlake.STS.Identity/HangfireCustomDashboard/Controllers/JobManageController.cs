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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace KaneBlake.STS.Identity.HangfireCustomDashboard.Controllers
{
    [Route("/hangfire/api/[controller]")]
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
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddRecurringJob(string connStr,string Tsql)
        {
            Expression<Func<int,int>> methodCall2 = (t) => SqlClientHelp.ExecuteTSqlInTran(connStr, Tsql, null);
            return Ok();
        }

    }
}
