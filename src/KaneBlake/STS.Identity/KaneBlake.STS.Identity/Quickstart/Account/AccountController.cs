// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using KaneBlake.STS.Identity.Infrastruct.Entities;
using KaneBlake.STS.Identity.Services;
using KaneBlake.Basis.Common.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Buffers.Text;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using KaneBlake.STS.Identity.Quickstart;
using Hangfire;
using Hangfire.Common;
using System.Linq.Expressions;
using CoreWeb.Util.Infrastruct;
using Microsoft.AspNetCore.WebUtilities;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using KaneBlake.Basis.Services;
using System.Runtime.CompilerServices;
using KaneBlake.AspNetCore.Extensions.MVC;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc.Localization;

namespace KaneBlake.STS.Identity
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// dotnet new is4ui
    /// </summary>
    [SecurityHeaders]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserService<User> _userService;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly ILogger _logger;
        private readonly ISigningCredentialStore _signingCredentialStore;
        private readonly IEnumerable<IStringLocalizerFactory> _stringLocalizerFactory;
        private readonly IStringLocalizer<AccountController> S;
        private readonly IHtmlLocalizer<AccountController> H;
        private readonly IViewLocalizer T;


        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            IUserService<User> userService,
            ILogger<AccountController> logger,
            ISigningCredentialStore signingCredentialStore,
            IStringLocalizer<AccountController> stringLocalizer,
            IHtmlLocalizer<AccountController> htmlLocalizer,
            IViewLocalizer viewLocalizer,
            IEnumerable<IStringLocalizerFactory> stringLocalizerFactory,
            IStringLocalizerFactory factory
            )
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _signingCredentialStore = signingCredentialStore ?? throw new ArgumentNullException(nameof(signingCredentialStore));
            _stringLocalizerFactory = stringLocalizerFactory;
            S = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
            H = htmlLocalizer;
            T = viewLocalizer;
        }

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl)
        {
            //var a = H["123"];
            //var b = S["456"];
            //var C = T["789"];
            //var c = _viewLocalizer.GetAllStrings().ToList();


            // build a model so we know what to show on the login page
            var vm = await BuildLoginViewModelAsync(returnUrl);

            if (vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return RedirectToAction("Challenge", "External", new { provider = vm.ExternalLoginScheme, returnUrl });
            }

            return View(vm);
        }


        [HttpPost]
        [AllowAnonymous]
        [ServiceFilter(typeof(EncryptFormResourceFilterAttribute))]
        public async Task<ActionResult<ServiceResponse>> Login(LoginInputModel model, string button)
        {

            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            // the user clicked the "cancel" button
            if (button != "login")
            {
                if (context != null)
                {
                    // if the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.
                    await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    if (await _clientStore.IsPkceClientAsync(context?.Client.ClientId))
                    {
                        // if the client is PKCE then we assume it's native, so this change in how to
                        // return the response is for better UX for the end user.
                        //return this.LoadingPage("Redirect", model.ReturnUrl);
                    }
                    return ServiceResponse.Redirect(model.ReturnUrl);
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return ServiceResponse.Redirect("/");
                }
            }

            if (ModelState.IsValid)
            {
                var user = await _userService.FindByUsername(model.UserName);

                // validate username/password against in-memory store
                if (await _userService.ValidateCredentials(user, model.Password))
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.Id.ToString(), user.Username, clientId: context?.Client.ClientId));

                    await _userService.SignInAsync(user, model.RememberLogin);

                    if (context != null)
                    {
                        if (await _clientStore.IsPkceClientAsync(context?.Client.ClientId))
                        {
                            // if the client is PKCE then we assume it's native, so this change in how to
                            // return the response is for better UX for the end user.
                            //return this.LoadingPage("Redirect", model.ReturnUrl);
                            
                        }

                        // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                        return ServiceResponse.Redirect(model.ReturnUrl);
                    }

                    // request for a local page
                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return ServiceResponse.Redirect(model.ReturnUrl);
                    }
                    else if (string.IsNullOrEmpty(model.ReturnUrl))
                    {
                        return ServiceResponse.Redirect("/");
                    }
                    else
                    {

                        // user might have clicked on a malicious link - should be logged
                        // never throw new Exception() in business logic
                        _logger.LogWarning("invalid return URL:{0}", model.ReturnUrl);
                        await HttpContext.SignOutAsync();
                        return ServiceResponse.Redirect("/");
                        // throw new Exception("invalid return URL");
                    }
                }

                await _events.RaiseAsync(new UserLoginFailureEvent(model.UserName, "invalid credentials", clientId: context?.Client.ClientId));
                ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
            }

            return ServiceResponse.BadRequest(new SerializableModelError(ModelState));
        }


        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }


        /// <summary>
        /// Show SignUp page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignUp(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl?? "/";
            return View(new SignUpViewModel());
        }

        [AcceptVerbs("GET")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyUserName(string UserName)
        {
            if (await _userService.UserNameExists(UserName))
            {
                return Json($"UserName {UserName} is already in use.");
            }

            return Json(true);
        }

        //[ResponseCache(Location =ResponseCacheLocation.Any,NoStore =false)]//Duration =86400,
        [AcceptVerbs("GET")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicKey()
        {
            var securityKey = (await _signingCredentialStore.GetSigningCredentialsAsync()).Key;
            if (securityKey is X509SecurityKey x509SecurityKey)
            {
                var publickey = x509SecurityKey.Certificate.ExportRSAPKCS8PublicKey();
                return Content(publickey);
            }
            return NoContent();
        }

        [HttpPost]
        [AllowAnonymous]
        [ServiceFilter(typeof(EncryptFormResourceFilterAttribute))]
        public async Task<ActionResult<ServiceResponse>> SignUp(SignUpViewModel model, string ReturnUrl = null)
        {
            ReturnUrl ??= Url.Content("~/");

            ViewData["ReturnUrl"] = ReturnUrl;

            if (!ModelState.IsValid) return BadRequest(ModelState);// 返回了 400 错误,会在浏览器控制台报错.;

            var user = await _userService.SignUp(model.UserName, model.Password);
            if (user != null) 
            {
                await _userService.SignInAsync(user, true);

                // request for a local page
                if (Url.IsLocalUrl(ReturnUrl))
                {
                    return ServiceResponse.Redirect(ReturnUrl);
                }
                else if (string.IsNullOrEmpty(ReturnUrl))
                {
                    return ServiceResponse.Redirect("/");
                }
                else
                {

                    // user might have clicked on a malicious link - should be logged
                    // never throw new Exception() in business logic
                    _logger.LogWarning("invalid return URL:{0}", ReturnUrl);
                    await HttpContext.SignOutAsync();
                    return ServiceResponse.Redirect("/");
                    // throw new Exception("invalid return URL");
                }

            }
            ModelState.AddModelError(nameof(model.UserName), $"UserName '{model.UserName}' is already in use.");

            // If we got this far, something failed, redisplay form
            return ServiceResponse.BadRequest(new SerializableModelError(ModelState));
        }

        /// <summary>
        /// 注册页面Api
        /// </summary>
        /// <returns></returns>
        //[AllowAnonymous]
        //[HttpPost]
        //public async Task<IActionResult> SignUp(string Name = "", string Password = "")
        //{
        //    if (Name.Trim() == "" || Password.Trim() == "")
        //    {
        //        ViewBag.LoginInfor = "无效的注册信息";
        //        return View(ViewBag);
        //    }
        //    var user = await _userService.SignUp(Name, Password);
        //    if (user is null)
        //    {
        //        return View(new UserSingUpInDto() { InvalidInfo = "用户名已存在" });
        //    }
        //    else
        //    {
        //        await _userService.SignInAsync(user);
        //        return Redirect("/");
        //    }
        //}

        /*****************************************/
        /* helper APIs for the AccountController */
        /*****************************************/
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Action(nameof(HomeController.Index), nameof(HomeController));

            }
            if (Request.Headers.Any(h => "x-requested-with".Equals(h.Key) && h.Value.Any(v => "XMLHttpRequest".Equals(v)))) 
            {
                Response.Headers.Add("transparent-redirect", returnUrl);
                return Ok();
            }
            return Redirect(returnUrl);
        }
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    UserName = context?.LoginHint,
                };

                if (!local)
                {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null ||
                            (x.Name.Equals(AccountOptions.WindowsAuthenticationSchemeName, StringComparison.OrdinalIgnoreCase))
                )
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context?.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                UserName = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.UserName = model.UserName;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }
    }
}
