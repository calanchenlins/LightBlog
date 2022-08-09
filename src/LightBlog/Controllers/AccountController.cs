using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LightBlog.Infrastruct.Entities;
using LightBlog.Services.Post;
using LightBlog.Services.InDto;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LightBlog.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {

        private readonly IUserService<User> _userService;

        public AccountController(IUserService<User> userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// 返回登陆页面
        /// </summary>
        /// <param name="returnUrl">从</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Login(string ReturnUrl)
        {
            return Redirect("/");
            var user = User as ClaimsPrincipal;

            var token = await HttpContext.GetTokenAsync("access_token");

            if (token != null)
            {
                ViewBag.access_token = token;
            }

            ViewBag.ReturnUrl = ReturnUrl;
            await Task.CompletedTask;
            return View(ViewBag);
        }



        /// <summary>
        /// 返回注册页面
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> SignUp()
        {
            await Task.CompletedTask;
            return View(new UserSingUpInDto());
        }


        /// <summary>
        /// 注册页面Api
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SignUp(string Name="", string Password="")
        {
            if (Name.Trim() == "" || Password.Trim() == "")
            {
                ViewBag.LoginInfor = "无效的注册信息";
                return View(ViewBag);
            }
            var user = await _userService.SignUp(Name, Password);
            if (user is null)
            {
                return View(new UserSingUpInDto() { InvalidInfo= "用户名已存在" });
            }
            else
            {
                await _userService.SignInAsync(user);
                return Redirect("/");
            }
        }

        /// <summary>
        /// 登陆API
        /// </summary>
        /// <param name="Username"></param>
        /// <param name="pas"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string Username, string Password, string ReturnUrl, string RememberLogin, string button)
        {
            var user = await _userService.FindByUsername(Username);
            if (user == null)
            {
                ViewBag.ReturnUrl = ReturnUrl;
                ViewBag.LoginInfor = "无效的登陆信息";
                return View(ViewBag);
            }
            if (await _userService.ValidateCredentials(user, Password))
            {
                await _userService.SignInAsync(user);
                if (CheckReturnUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }
                else
                {
                    return Redirect("/");
                }
            }
            else
            {
                ViewBag.ReturnUrl = ReturnUrl;
                ViewBag.LoginInfor = "无效的登陆信息";
                return View(ViewBag);
            }
        }

        /// <summary>
        /// 检查ReturnUrl是否有效
        /// </summary>
        /// <param name="ReturnUrl"></param>
        /// <returns></returns>
        private bool CheckReturnUrl(string ReturnUrl)
        {
            return true;
        }

        /// <summary>
        /// 登出
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOut()
        {

            if (_userService.IsAuthenticated())
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync("oidc");
                HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            // "Catalog" because UrlHelper doesn't support nameof() for controllers
            // https://github.com/aspnet/Mvc/issues/5853
            var homeUrl = Url.Action(nameof(HomeController.Index), "Home");
            return new SignOutResult(CookieAuthenticationDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = homeUrl });
            //return Redirect("/");
        }
    }
}