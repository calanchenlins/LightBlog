﻿using IdentityServer4;
using KaneBlake.Basis.Domain.Repositories;
using KaneBlake.STS.Identity.Infrastruct.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Services
{
    public class UserService : IUserService<User>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<User, int> _userRepository;
        private readonly ILogger _logger;

        public UserService(IHttpContextAccessor httpContextAccessor, IRepository<User, int> userRepository, ILogger<UserService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User> FindByUsername(string username)
        {
            return await _userRepository.Get().Where(x => x.Username == username).FirstOrDefaultAsync();
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated;
        }


        public Task SignInAsync(User user, bool rememberLogin)
        {
            _logger.LogDebug("SignInAsync");
            // only set explicit expiration here if user chooses "remember me". 
            // otherwise we rely upon expiration configured in cookie middleware.
            AuthenticationProperties props = null;
            if (AccountOptions.AllowRememberLogin && rememberLogin)
            {
                props = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                };
            };

            // issue authentication cookie with subject ID and username
            var isuser = new IdentityServerUser(user.Id.ToString())
            {
                DisplayName = user.Username
            };

            return _httpContextAccessor.HttpContext.SignInAsync(isuser, props);
        }

        public async Task<User> SignUp(string userName, string password)
        {
            try 
            {
                var user = User.Create(userName, password);
                _userRepository.Add(user);
                await _userRepository.CompleteAsync();
                return user;
            }
            catch(Exception ex) 
            {
                _logger.LogError(ex, "SignUp Failed!");
                return null;
            }
        }

        public async Task<bool> ValidateCredentials(User user, string password)
        {
            await Task.CompletedTask;
            return user is null ? false : user.ValidateCredentials(password);
        }

        public async Task<bool> UserNameExists(string userName)
        {
            var t = _userRepository.Get().ToList();
            var u = _userRepository.Get().Any(r => r.Username.Equals(userName));
            return await _userRepository.Get().AnyAsync(r => r.Username.Equals(userName));
        }
    }
}
