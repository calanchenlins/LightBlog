using IdentityServer4.Models;
using IdentityServer4.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Common.IdentityServer4Config
{
    /// <summary>
    /// 将Claims以明文的方式写入token(base64解码)
    /// 对id_token没影响
    /// </summary>
    public class MyProfileService : IProfileService
    {
        //根据资源所有者账号、密码向/connect/token请求access_token时context.Subject.Claims不包括context.Client.Claims
        //根据access_token请求/connect/userinfo(scope:profile) 时,context.Subject.Claims已经包括了context.Client.Claims
        //根据刷新令牌请求/connect/token,不执行GetProfileDataAsync,通过refresh_token刷新访问令牌时,大部分token信息复用
        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            //context.Client.Claims:定义客户端时写入的Claims

            // 将context.Subject.Claims具有的Claims写入access_token的playload
            // 将context.Subject.Claims具有的Claims写入/connect/userinfo请求点返回数据
            context.IssuedClaims = context.Subject.Claims.ToList();


            //IssuedClaims默认为空
            //在id_token、access_token中和/connect/userinfo请求点返回数据一定存在的claim:sub
            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;
            return Task.CompletedTask;
        }
    }
}
