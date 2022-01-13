// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
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
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using KaneBlake.Basis.Services;
using System.Runtime.CompilerServices;
using KaneBlake.AspNetCore.Extensions.MVC;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Localization;
using KaneBlake.AspNetCore.Extensions.MVC.Filters;

namespace KaneBlake.STS.Identity
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// dotnet new is4ui
    /// </summary>
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IUserService<User> _userService;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly ILogger _logger;
        private readonly ISigningCredentialStore _signingCredentialStore;
        private readonly IStringLocalizer<AccountController> _localizer;


        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            IUserService<User> userService,
            ILogger<AccountController> logger,
            ISigningCredentialStore signingCredentialStore,
            IStringLocalizer<AccountController> localizer
            )
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _signingCredentialStore = signingCredentialStore ?? throw new ArgumentNullException(nameof(signingCredentialStore));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }


        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null) 
            {
                var authenticationScheme = await _schemeProvider.GetSchemeAsync(context.IdP);
                if (!IdentityServerConstants.LocalIdentityProvider.Equals(authenticationScheme.Name)) 
                {
                    // this is meant to short circuit the UI and only trigger the one external IdP
                }
            }

            // Sign-in with External Identity Providers
            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var vm = new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                UserName = context?.LoginHint,
                // ToDo: Support External Identity Providers
            };

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(EncryptFormResourceFilterAttribute))]
        public async Task<ActionResult<ServiceResponse>> Login(LoginInputModel model, string button)
        {
            // check if we are in the context of an authorization request
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
                    var isNativeClient = !context.RedirectUri.StartsWith("https", StringComparison.Ordinal)
                        && !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
                    if (isNativeClient)
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        HttpContext.Response.StatusCode = 200;
                        HttpContext.Response.Headers["Location"] = "";
                        return View("Redirect", new RedirectViewModel { RedirectUrl = model.ReturnUrl });
                    }

                    return ServiceResponse.Redirect(model.ReturnUrl);
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return ServiceResponse.Redirect("/");
                }
            }

            var user = await _userService.FindByUsername(model.UserName);

            if (await _userService.ValidateCredentials(user, model.Password))
            {
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.Id.ToString(), user.Username, clientId: context?.Client.ClientId));

                await _userService.SignInAsync(user, model.RememberLogin);

                if (context != null)
                {
                    var isNativeClient = !context.RedirectUri.StartsWith("https", StringComparison.Ordinal)
                        && !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
                    if (isNativeClient)
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        HttpContext.Response.StatusCode = 200;
                        HttpContext.Response.Headers["Location"] = "";
                        return View("Redirect", new RedirectViewModel { RedirectUrl = model.ReturnUrl });
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
                    _logger.LogWarning("Login Filed: invalid return URL:{returnUrl}", model.ReturnUrl);

                    // delete local authentication cookie
                    await HttpContext.SignOutAsync();
                    // raise the logout event
                    await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));

                    return ServiceResponse.Redirect("/");
                }
            }

            await _events.RaiseAsync(new UserLoginFailureEvent(model.UserName, "invalid credentials", clientId: context?.Client.ClientId));
            
            ModelState.AddModelError(string.Empty, _localizer["Invalid username or password"]);

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
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(model.LogoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = model.LogoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && !IdentityServerConstants.LocalIdentityProvider.Equals(idp))
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
        public IActionResult AccessDenied()
        {
            return View();
        }


        /// <summary>
        /// Show SignUp page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult SignUp(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl?? "/";
            return View(new SignUpViewModel());
        }

        /// <summary>
        /// Handle signup page postback
        /// </summary>
        /// <param name="model"></param>
        /// <param name="ReturnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ServiceFilter(typeof(EncryptFormResourceFilterAttribute))]
        public async Task<ActionResult<ServiceResponse>> SignUp(SignUpViewModel model, string ReturnUrl = null)
        {
            ReturnUrl ??= Url.Content("~/");

            ViewData["ReturnUrl"] = ReturnUrl;

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
                    _logger.LogWarning("Login Filed: invalid return URL:{returnUrl}", ReturnUrl);

                    // delete local authentication cookie
                    await HttpContext.SignOutAsync();
                    // raise the logout event
                    await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));

                    return ServiceResponse.Redirect("/");
                }

            }

            ModelState.AddModelError(nameof(model.UserName), $"UserName '{model.UserName}' is already in use.");

            return ServiceResponse.BadRequest(new SerializableModelError(ModelState));
        }


        /// <summary>
        /// Verify user name is in use or not
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        [AcceptVerbs("GET")]
        public async Task<IActionResult> VerifyUserName(string UserName)
        {
            if (await _userService.UserNameExists(UserName))
            {
                return Json($"UserName {UserName} is already in use.");
            }

            return Json(true);
        }

        /// <summary>
        /// Get Rsa publick key for Form encryption submission
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs("GET")]
        [ResponseCache(Location = ResponseCacheLocation.Any, NoStore = false, Duration = 60 * 60 * 24)]
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

        /// <summary>
        /// switch UI language
        /// </summary>
        /// <param name="culture"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }

        /*****************************************/
        /* helper APIs for the AccountController */
        /*****************************************/
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


    }
}
