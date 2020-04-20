using IdentityServer4.Models;
using IdentityServer4.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Common.IdentityServer4Config
{
    /// <summary>
    /// 自定义验证器(资源所有者密码授权模式)
    /// 1.验证用户账号、密码
    /// 2.动态增加声明,给声明赋值
    /// </summary>
    public class MyResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            if (context.UserName == "dd" && context.Password == "dd")
            {
                var Claims = new List<Claim>();
                Claims.Add(new Claim("catalogClaim_Type", "catalogClaim_Type_value"));
                Claims.Add(new Claim("role", "admin"));
                Claims.Add(new Claim("role2", "admin2"));
                // 将Claims加入context.Subject.Claims
                context.Result = new GrantValidationResult("subject名称", "authenticationMethod授权方式", Claims);
            }
            else
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid custom credential");
            }
            return Task.CompletedTask;
        }
    }
}
