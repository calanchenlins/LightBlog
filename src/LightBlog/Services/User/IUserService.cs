using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Services
{
    public interface IUserService<T>
    {
        /// <summary>
        /// 验证用户、密码
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<bool> ValidateCredentials(T user, string password);

        bool IsAuthenticated();

        string UserName { get; }

        string UserId { get; }

        /// <summary>
        /// 根据用户名查找用户
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        Task<T> FindByUsername(string username);

        /// <summary>
        /// 登入成功、发放凭证
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task SignInAsync(T user);

        Task SignOutAsync();

        Task<T> SignUp(string userName,string password);
    }
}
