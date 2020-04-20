using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace KaneBlake.STS.Identity.Controllers
{
    public class AccountController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;

        private readonly IDistributedCache _cache;

        public AccountController(IIdentityServerInteractionService interaction, IDistributedCache cache)
        {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 返回登陆页面
        /// </summary>
        /// <param name="returnUrl">从</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Login(string ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            await Task.CompletedTask;
            return View(ViewBag);
        }


        /// <summary>
        /// 登陆API
        /// </summary>
        /// <param name="Username"></param>
        /// <param name="pas"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Login(string Username, string Password, string ReturnUrl, string RememberLogin, string button, string __RequestVerificationToken)
        {
            // validate username/password against in-memory store
            if (true)
            {
                // check if we are in the context of an authorization request
                //根据ReturnUrl获取授权上下文
                //不能带host地址，所以在配置IdSrv的登录页面时使用相对地址
                var context = await _interaction.GetAuthorizationContextAsync(ReturnUrl);

                var props = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2),
                };
                // issue authentication cookie with subject ID and username
                // 用户登录成功后,根据subId颁发一个证书（加密的用户凭证）,用来标识用户的身份。
                
                await HttpContext.SignInAsync("用户唯一Id", "用户名", props, new Claim("claim2", "声明1"), new Claim("claim2", "声明2"));

                // make sure the returnUrl is still valid, and if yes - redirect back to authorize endpoint
                if (_interaction.IsValidReturnUrl(ReturnUrl))
                {
                    //return Redirect(ReturnUrl);
                    //ViewBag.RedirectUrl = ReturnUrl;
                    //return View("Redirect", ViewBag);
                    return Redirect(ReturnUrl);
                }
                //验证在 SignInAsync 中颁发的证书，并返回一个 AuthenticateResult 对象，表示用户的身份
                //await HttpContext.AuthenticateAsync();

                //用来获取 AuthenticationProperties 中保存的额外信息
                var accessToken = await HttpContext.GetTokenAsync("access_token");

                if (Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }
                else if (string.IsNullOrEmpty(ReturnUrl))
                {
                    return Redirect("~/");
                }
                else
                {
                    // user might have clicked on a malicious link - should be logged
                    throw new Exception("invalid return URL");
                }
            }
        }

        /// <summary>
        /// 登出页面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                // if the user is not authenticated, then just show logged out page
                return await LogoutPost(logoutId);
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                //it's safe to automatically sign-out
                return await LogoutPost(logoutId);
            }
            ViewBag.logoutId = logoutId;
            return View();
        }


        /// <summary>
        /// 登出Api
        /// </summary>
        [HttpPost] //[ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutPost(string logoutId)
        {
            var idp = User?.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;

            if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
            {
                if (logoutId == null)
                {
                    // if there's no current logout context, we need to create one
                    // this captures necessary info from the current logged in user
                    // before we signout and redirect away to the external IdP for signout
                    logoutId = await _interaction.CreateLogoutContextAsync();
                }

                string url = "/Account/Logout?logoutId=" + logoutId;

                try
                {

                    // hack: try/catch to handle social providers that throw
                    await HttpContext.SignOutAsync(idp, new AuthenticationProperties
                    {
                        RedirectUri = url
                    });
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            // delete authentication cookie
            await HttpContext.SignOutAsync();

            //await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            return Redirect(logout?.PostLogoutRedirectUri);
        }

        [HttpPost]
        public async Task<ContentResult> GetToken()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            return Content(accessToken);
        }
    }
}