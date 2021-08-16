using KaneBlake.AspNetCore.Extensions.Services;
using KaneBlake.Basis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.MVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationServiceController: ControllerBase
    {
        private readonly ILogger<ApplicationServiceController> _logger;

        private readonly ApplicationService _srv;

        public ApplicationServiceController(ILogger<ApplicationServiceController> logger, ApplicationService srv)
        {
            _logger = logger;
            _srv = srv;
        }


        [HttpPost]
        [Route("/{serviceName}")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Submit(string serviceName) 
        {
            return Ok(await _srv.InvokeAsync(serviceName, Request));
        }


    }
}
