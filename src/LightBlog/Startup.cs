using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using AutoMapper;
using KaneBlake.Basis.Common.Diagnostics.Abstractions;
using KaneBlake.Basis.Domain.Repositories;
using LightBlog.Common.AOP.CommonCache;
using LightBlog.Common.Diagnostics;
using LightBlog.Infrastruct.Context;
using LightBlog.Infrastruct.Entities;
using LightBlog.Infrastruct.Repository;
using LightBlog.Services;
using LightBlog.Services.Cache;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Authentication;

namespace LightBlog
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IWebHostEnvironment Env { get; set; }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });

            //services.Configure<CookiePolicyOptions>(Configuration.GetSection("File"));

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            var identityUrl = "https://localhost:44350";
            var callBackUrl = "/";

            // prevent from mapping "sub" claim to nameidentifier.
            // 清除token中的claim到.netCore中的claim的映射
            // 关闭了JWT的Claim 类型映射, 以便允许 well-known claims
            // 否则amr和sub声明的键名会被改变
            //JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "oidc";
            })
                // code 验证成功之后，将凭证写入cookie
                // 在cookie有效期内不需要重新申请code, 直接通过凭证获取token ???
                .AddCookie(setup => setup.ExpireTimeSpan = TimeSpan.FromHours(2))
                .AddOpenIdConnect("oidc", options =>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = identityUrl.ToString();
                    options.SignedOutRedirectUri = callBackUrl.ToString();
                    options.ClientId = "LightBlogMvc";
                    options.ResponseType = "code";
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.RequireHttpsMetadata = false;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    //options.ClaimActions.MapUniqueJsonKey("tenantId", "tenantId");
                    //options.Scope.Add("sample.api");
                });


            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:5000";
                options.InstanceName = "LightBlogCache";
            });

            services.AddMemoryCache(opts=> {
                opts.ExpirationScanFrequency = new TimeSpan(TimeSpan.TicksPerDay);
            });
            var gh = Configuration["LightBlogDb"];
            services.AddDbContext<PostDbContext>(options =>
            {
                options.UseSqlServer(Configuration["LightBlogDb"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
            });

            services.AddSingleton<IHomeCacheService, HomeCacheServiceV2>();


            services.AddSingleton<IDiagnosticProcessor, HomeCacheDiagnosticProcessor>();

            services.AddTransient<IRepository<Post, int>, PostRepository>();

            //services.AddSingleton<IHomeCacheService, HomeCacheService>(sp=> {
            //    var mapper = sp.GetRequiredService<IMapper>();
            //    var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
            //    return new HomeCacheService(mapper, iLifetimeScope);
            //});

            

            services.AddDbContext<UserDbContext>(options =>
            {
                options.UseSqlServer(Configuration["LightBlogDb"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
            });

            services.AddTransient<IRepository<User, int>, UserRepository>();
            services.AddTransient<IUserService<User>, UserService>();

            //var InstrumentationKey = Configuration["ApplicationInsightsInstrumentationKey"];

            services.AddOpenTelemetryTracing(
                (builder) => builder
                        .AddAspNetCoreInstrumentation()
                        .AddSqlClientInstrumentation(options => options.SetDbStatementForText = true)
                        .AddConsoleExporter()
                        );

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                  .AddNewtonsoftJson(options =>
                  {
                      options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                  });

            services.AddWebOptimizer(pipeline =>
            {
                if (Env.IsProduction())
                {
                    pipeline.MinifyCssFiles();
                    pipeline.MinifyJsFiles();
                    pipeline.MinifyHtmlFiles();
                }
                // JavaScriptBundle CssBundle
            });


        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<QueryCacheAOP>();
            builder.RegisterType<UpdateCacheAOP>();

            var IntercepTypes = new List<Type>
            {
                typeof(QueryCacheAOP),
                typeof(UpdateCacheAOP)
            };
            builder.RegisterType<PostService>()
                .As<IPostService>()
                .InstancePerLifetimeScope()
                .EnableInterfaceInterceptors()
                .InterceptedBy(IntercepTypes.ToArray());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // always use 'throw;' it maintain stack trace details
                // never use 'throw ex;' because it doesn’t maintain the stack trace details
                // https://source.dot.net/#Microsoft.AspNetCore.Diagnostics/ExceptionHandler/ExceptionHandlerMiddleware.cs
                // https://github.com/dotnet-architecture/eShopOnContainers/blob/dev/src/Services/Catalog/Catalog.API/Infrastructure/Filters/HttpGlobalExceptionFilter.cs
                app.UseExceptionHandler("/Home/Error");
                // Use Ngix to add_header Strict-Transport-Security "max-age=63072000; includeSubdomains; preload";
                //app.UseHsts(); // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            }

            // Use Ngix to Redirect Http Request
            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();


            app.UseCookiePolicy();

            // 增加了 oidc 认证之后, 启用认证中间件, 进行身份验证并颁发凭证
            // 对于 IdSrv4 组件来说,会在 host/signin-oidc 终结点提供接受code、写入cookie的服务
            app.UseAuthentication(); // 认证 signin-oidc
            app.UseAuthorization(); // 授权
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });


            //DiagnosticListener.AllListeners.Subscribe(app.ApplicationServices.GetRequiredService<DiagnosticProcessorObserver>());
        }

        private void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                // TODO: Use your User Agent library of choice here.
                options.SameSite = SameSiteMode.Unspecified;
            }
        }

    }
}
