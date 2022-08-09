using K.Basis.Domain.Repositories;
using LightBlog.Infrastruct.Entities;
using LightBlog.Services.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LightBlog.Services
{
    public class UserService : IUserService<LightBlog.Infrastruct.Entities.User>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IRepository<LightBlog.Infrastruct.Entities.User, int> _userRepository;

        public UserService(IHttpContextAccessor httpContextAccessor, IRepository<LightBlog.Infrastruct.Entities.User, int> userRepository)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public string UserName 
        { 
            get 
            {
                //var jwtHandler = new JsonWebTokenHandler();
                //var token = _httpContextAccessor.HttpContext.GetTokenAsync("access_token").Result;
                //var JwtToken = jwtHandler.ReadJsonWebToken(token);
                //return JwtToken.GetClaim("name").Value??"";

                var userNameClaim = _httpContextAccessor.HttpContext.User.Claims
                    .Where(c => c.Type == "name")
                    .FirstOrDefault();
                return userNameClaim?.Value ?? "";
            }
        }

        public string UserId
        {
            get
            {
                var userClaims = _httpContextAccessor.HttpContext.User.Claims
                    .Where(c => c.Type == "sub")
                    .FirstOrDefault();
                return userClaims?.Value??"";
            }
        }

        public Task<LightBlog.Infrastruct.Entities.User> FindByUsername(string username)
        {
            return Task.Run(() =>
            {
                return _userRepository.Get().Where(x => x.Name == username).FirstOrDefault();
            });
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated;
        }

        public Task SignInAsync(LightBlog.Infrastruct.Entities.User user)
        {
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15),
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            return _httpContextAccessor.HttpContext.SignInAsync
            (
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );
        }

        public Task SignOutAsync()
        {
            return _httpContextAccessor.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<LightBlog.Infrastruct.Entities.User> SignUp(string userName, string password)
        {
            var existUser = await FindByUsername(userName.Trim());
            return await Task.Run(() => {
                if (existUser is null)
                {
                    var user = new LightBlog.Infrastruct.Entities.User(userName, password);
                    _userRepository.Add(user);
                    _userRepository.Complete();
                    return user;
                }
                else
                {
                    return null;
                }
            });
        }

        public async Task<bool> ValidateCredentials(LightBlog.Infrastruct.Entities.User user, string password)
        {
            var userIndb = await FindByUsername(user.Name);

            return await Task.Run(() =>
            {
                if (userIndb.Password == password)
                {
                    return true;
                }
                return false;
            });
        }
    }
}
