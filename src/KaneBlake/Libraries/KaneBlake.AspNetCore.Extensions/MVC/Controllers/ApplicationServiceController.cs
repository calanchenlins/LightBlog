using KaneBlake.AspNetCore.Extensions.Services.Module;
using KaneBlake.Basis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.MVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationServiceController: ControllerBase
    {
        private readonly ILogger<ApplicationServiceController> _logger;

        private readonly IApplicationServiceClient _srvClient;

        public ApplicationServiceController(ILogger<ApplicationServiceController> logger, IApplicationServiceClient srvClient)
        {
            _logger = logger;
            _srvClient = srvClient ?? throw new ArgumentNullException(nameof(srvClient));
        }


        /// <summary>
        /// Invoke applicationService with the specified name
        /// </summary>
        /// <param name="serviceName">name of service</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{serviceName}")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Submit(string serviceName, [FromBody]ServiceRequest request) 
        {
            return Ok(await _srvClient.InvokeAsync(serviceName, Request));
        }
    }
    public class ServiceRequest
    {
        [JsonExtensionData]
        public IDictionary<string, object> Body { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
}
