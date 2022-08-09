// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Services;
using KaneBlake.AspNetCore.Extensions.MVC.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Controllers.Home
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger _logger;
        private static readonly MediaTypeHeaderValue _textHtmlMediaType = new MediaTypeHeaderValue("text/html");

        public HomeController(IIdentityServerInteractionService interaction, IWebHostEnvironment environment, ILogger<HomeController> logger)
        {
            _interaction = interaction;
            _environment = environment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (_environment.IsDevelopment())
            {
                // only show in development
                return View();
            }

            _logger.LogInformation("Homepage is disabled in production. Returning 404.");
            return NotFound();
        }

        /// <summary>
        /// Shows the error page for identityserver
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;

                if (!_environment.IsDevelopment())
                {
                    // only show in development
                    message.ErrorDescription = null;
                }
            }
            return View("Error", vm);
        }

        /// <summary>
        /// ErrorHandle for both API endpoints and MVC endpoints
        /// </summary>
        [Route("/error_handle")]
        [IgnoreAntiforgeryToken]
        [AllowAnonymous]
        public IActionResult ErrorHandle()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            _logger.LogError(exceptionHandlerPathFeature.Error, "An error occurred while processing your request in path:{0}", exceptionHandlerPathFeature.Path);
            if (HttpContext.Request.GetTypedHeaders().Accept.Any(a => a.IsSubsetOf(_textHtmlMediaType))) 
            {
                return View("error_handle");
            }
            return Problem();
        }

        [HttpOptions]
        [HttpPost]
        [Route("/csp_report")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CspReports() 
        {
            ReadResult readResult = await Request.BodyReader.ReadAsync();
            _logger.LogWarning("/csp_report:{0}", GetTextFromPipeReadBuffer(readResult.Buffer));
            return Ok();
        }

        private string GetTextFromPipeReadBuffer(ReadOnlySequence<byte> readOnlySequence) 
        {
            ReadOnlySpan<byte> span = readOnlySequence.IsSingleSegment ? readOnlySequence.First.Span : readOnlySequence.ToArray().AsSpan();
            return Encoding.UTF8.GetString(span);
        }
    }
}