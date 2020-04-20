using IdentityModel;
using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Infrastruct
{
    public class DataSeeder
    {
        /// <summary>
        /// 迁移IdentityServer的配置
        /// PersistedGrantDbContext、ConfigurationDbContext
        /// </summary>
        public static void Seed(ConfigurationDbContext context)
        {
            foreach (var r in context.Clients)
            {
                context.Clients.Remove(r);
            }
            foreach (var r in context.ApiResources)
            {
                context.ApiResources.Remove(r);
            }
            foreach (var r in context.IdentityResources)
            {
                context.IdentityResources.Remove(r);
            }
            if (!context.Clients.Any())
            {
                foreach (var client in GetClients())
                {
                    context.Clients.Add(client.ToEntity());
                }
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in GetIdentityResources())
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
            }

            if (!context.ApiResources.Any())
            {
                foreach (var resource in GetApis())
                {
                    context.ApiResources.Add(resource.ToEntity());
                }
            }
            context.SaveChanges();
        }


        /// <summary>
        /// 定义需要保护的资源(scope)
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new IdentityResource[]
            {
                //当scope为OpenId时，可以通过获取的访问令牌
                //http://localhost:7102/connect/userinfo 获取用户信息接口(默认只返回sub，如果需要用户其他信息，需要实现IProfileService接口)
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResources.Address()
            };
        }
        /// <summary>
        /// 需要保护的Api资源(也是scope)
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ApiResource> GetApis()
        {
            //定义该scope资源在资源所有者密码授权模式需要使用的声明
            var orderClaimTypes = new List<string>
            {
                JwtClaimTypes.Role
                ,"orderClaim_Type"
            };
            var catalogClaimTypes = new List<string>
            {
                JwtClaimTypes.Role,
                "catalogClaim_Type"

            };
            var OcelotGatewayClaimTypes = new List<string>
            {
                JwtClaimTypes.Role,
                "OcelotGatewayClaim_Type"
            };
            return new List<ApiResource>{
                // 定义微服务中需要用到的claims
                new ApiResource("sample.api", "示例Api")//catalogClaimTypes
            };
        }

        /// <summary>
        /// 定义客户端
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Client> GetClients()
        {
            var claims = new List<Claim>
            {
                // 在客户端凭证模式中,会在键上加"client_"前缀,再写入token
                new Claim("client_define_claim", "定义客户端时设置的claim,将会直接写入token!")
            };

            return new List<Client>
            {
                // 客户端授权模式
                new Client
                {
                    ClientId = "ClientCredentials",
                    ClientName="客户端授权模式",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    // 密码太长会导致验证客户端失败
                    ClientSecrets ={new Secret("iwiaXNzIjoibnVsbCIsImF1ZCI6WyJudWxsL3Jlc291cmNlcyIsIm9yZ".Sha256())},
                    // 客户端授权模式无法访问用户资源scope(openid、profile、email等),加入也无效
                    // 客户端凭证模式不支持刷新令牌( scope = offline_access )
                    AllowedScopes = {"orders" },
                    Claims =claims
                },

                // 资源所有者密码授权模式
                // 1.用于自家网站直接通过用户名密码登陆
                // 通过客户端Id、密码，用户Id、密码向IdentityServer4/connect/token请求访问令牌token
                new Client
                {
                    ClientId = "eshopOnVue",
                    ClientName="eshop前端",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    // 不启用客户端密码(前端无法安全保存客户端密码)
                    // 不能传递client_secret参数
                    RequireClientSecret=false,
                    AllowOfflineAccess=true,// 允许请求刷新令牌 
                    AlwaysIncludeUserClaimsInIdToken = false,//在id_token中写入claims,而不是通过access_token向/connect/userinfo请求用户声明
                    ClientSecrets ={ new Secret("c9439d7dff82dc97fc8db668564eed328b4e01a989dc1a1447751bd4d273108966bb505932b68ff34b7897b613a211d8c67a7857fb46086e5c541ca0e854565a".Sha256()) },
                    AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId,//向授权服务器表明该客户端发出的是OpenId请求
                        IdentityServerConstants.StandardScopes.Profile,// 获取用户信息的权限(/connect/userinfo)
                        IdentityServerConstants.StandardScopes.OfflineAccess,//简化模式、客户端凭证模式不支持刷新令牌
                        "orders"
                    },
                    // 将Claims写入ProfileDataRequestContext.Client.Claims
                    Claims =claims//只有资源所有者密码授权模式可以使用角色授权保护Api[Authorize(Roles = "superadmin")]??????
                },

                // 简化模式、授权码模式、混合模式:用于开放Api接口授权给第三方 https://oauth.net/2/browser-based-apps/
                // 最佳实践:禁止从授权终结点返回访问令牌给第三方应用
                // 简化模式:放弃使用(直接向授权终结点请求访问令牌,通过将acess_token追加到RedirectUrl锚点#,前端获取token)
                // 混合模式:向授权终结点请求id_token和code,只能通过code向token终结点请求访问令牌,必须设置 AllowAccessTokensViaBrowser =false,禁止向授权终结点请求访问令牌,设置RequirePkce = true
                // 授权码模式:state参数必须启用,设置RequirePkce = true

                new Client
                {
                    ClientId = "Swagger_UI",
                    ClientName = "Swagger UI客户端 授权码模式",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent=false,//跳过授权页
                    RequireClientSecret = false,
                    RequirePkce = true,//指定使用基于授权代码的授权类型的客户端是否必须发送校验密钥
                    AllowAccessTokensViaBrowser =false,// 混合模式下必须为false:禁止向授权终结点请求访问令牌，只能通过code向token终结点请求访问令牌
                    AllowOfflineAccess=true, //指定此客户端是否可以请求刷新令牌(请求scope:offline_access)
                    RedirectUris = {//登录成功后浏览器跳转地址(外部地址)，用来接收Code、令牌
                        //网关项目SwaggerUI跳转地址,网关需要跨域访问所有WebApi的Json文件
                        "http://localhost:6104/oauth2-redirect.html",
                        "http://localhost:6103/oauth2-redirect.html",
                        "http://localhost:6105/oauth2-redirect.html",
                        "http://localhost:9528/Callback",
                        "http://localhost:9528/SilentCallback"

                    },
                    PostLogoutRedirectUris = { // 登出后跳转地址
                        "http://localhost:9528/index.html"
                    },
                    AllowedCorsOrigins = {// 从前端向token终结点请求token时，需要开启跨域访问
                        "http://localhost:6104",//网关SwaggerUI
                        "http://localhost:6103",//ordering.api:SwaggerUI
                        "http://localhost:6105",//catalog.api:SwaggerUI
                        "http://localhost:9528"//VueSpa客户端

                    },

                    AllowedScopes ={// 允许的资源域
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "orders",
                        "catalog.api",
                        "OcelotGateway"
                    },
                    // 设置code、token的过期时间
                     AccessTokenLifetime = 60*1,
                     IdentityTokenLifetime= 60*1
                },
                
                
                new Client
                {
                    ClientId = "LightBlogMvc",
                    ClientName = "LightBlog MVC Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent=false,//跳过授权页
                    RequireClientSecret = false,
                    RequirePkce = true,//指定使用基于授权代码的授权类型的客户端是否必须发送校验密钥
                    AllowAccessTokensViaBrowser =false,// 混合模式下必须为false:禁止向授权终结点请求访问令牌，只能通过code向token终结点请求访问令牌
                    AllowOfflineAccess=true, //指定此客户端是否可以请求刷新令牌(请求scope:offline_access)
                    RedirectUris = {//登录成功后浏览器跳转地址(外部地址)，用来接收Code、令牌
                        "https://localhost:44310/signin-oidc"
                    },
                    PostLogoutRedirectUris = { // 登出后跳转地址
                        "https://localhost:44310/signout-callback-oidc"
                    },
                    //AllowedCorsOrigins = {// 从前端向token终结点请求token时，需要开启跨域访问
                    //    "http://localhost:6104",//网关SwaggerUI
                    //    "http://localhost:6103",//ordering.api:SwaggerUI
                    //    "http://localhost:6105",//catalog.api:SwaggerUI
                    //    "http://localhost:9528"//VueSpa客户端

                    //},

                    AllowedScopes ={// 允许的资源域
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "sample.api",
                    },
                    // 设置code、token的过期时间
                     AccessTokenLifetime = 60,
                     IdentityTokenLifetime= 60
                }




            };
        }
    }
}
