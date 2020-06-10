using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace KaneBlake.STS.Identity.HangfireCustomDashboard.Controllers
{
    public class HangfierController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
