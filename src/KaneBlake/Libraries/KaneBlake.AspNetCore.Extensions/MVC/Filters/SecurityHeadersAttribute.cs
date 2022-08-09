using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace K.AspNetCore.Extensions.MVC.Filters
{
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var result = context.Result;
            if (result is ViewResult)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options"))
                {
                    context.HttpContext.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
                var csp = "default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';img-src 'self' data:;";
                // also consider adding upgrade-insecure-requests once you have HTTPS in place for production
                // csp += "upgrade-insecure-requests;";
                // also an example if you need client images to be displayed from twitter
                // csp += "img-src 'self' https://pbs.twimg.com;";
                csp += "script-src 'self' https://unpkg.com/;";

                csp += "report-uri /csp_report;";


                // 'report-to'、'report-uri' testPage: https://daniel.spilsbury.io/
                // chrome://net-export/   https://netlog-viewer.appspot.com/#import
                // https://stackoverflow.com/questions/60632559/how-to-add-report-to-content-security-policy-directly-in-web-config
                // https://community.brave.com/t/reporting-api-does-not-send-reports-in-dev-beta/74232
                // reports are queued indefinitely in localhost 
                if (!context.HttpContext.Request.Host.Host.Equals("localhost"))
                {
                    csp += "report-to csp-endpoint;";
                    if (!context.HttpContext.Response.Headers.ContainsKey("Report-To"))
                    {
                        context.HttpContext.Response.Headers.Add("Report-To", @"{""group"": ""csp-endpoint"",""max_age"": 10886400,""endpoints"": [{ ""url"": ""/csp_report"" }],""include_subdomains"":true}");
                    }
                }


                // once for standards compliant browsers
                if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("Content-Security-Policy", csp);
                }
                // and once again for IE
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", csp);
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
                var referrer_policy = "no-referrer";
                if (!context.HttpContext.Response.Headers.ContainsKey("Referrer-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("Referrer-Policy", referrer_policy);
                }
            }
        }
    }
}
