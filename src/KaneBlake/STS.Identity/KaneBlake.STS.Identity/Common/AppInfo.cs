using KaneBlake.Basis.Extensions.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Common
{
    public class AppInfo
    {
        private AppInfo() { }

        public static AppInfo Instance { get; private set; }

        static AppInfo()
        {
            LoginUrl = "/Account/Login";
            HangfirePath = "/hangfire";
            HangfireLoginUrl = UriHelper.BuildRelative(path: new PathString(LoginUrl), query: QueryString.Create("ReturnUrl", HangfirePath));
            TextHtmlMediaType = new MediaTypeHeaderValue("text/html");
            ApplicationProblemJsonMediaType = new MediaTypeHeaderValue("application/problem+json");
            Instance = new AppInfo
            {
                Certificate = CertificateExtensions.GetX509Certificate(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certs", "IdentityServerCredential.pfx"))
            };

        }

        public X509Certificate2 Certificate { get; private set; }

        public static string LoginUrl { get; private set; }

        public static string HangfirePath { get; private set; }

        public static string HangfireLoginUrl { get; private set; }

        public static MediaTypeHeaderValue TextHtmlMediaType { get; private set; }

        public static MediaTypeHeaderValue ApplicationProblemJsonMediaType { get; private set; }
    }
}
