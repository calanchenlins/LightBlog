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
using KaneBlake.Basis.Extensions.Diagnostics.Abstractions;
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
using OpenTelemetry.Trace.Configuration;

namespace LightBlog
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.Configure<CookiePolicyOptions>(Configuration.GetSection("File"));

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:6378";
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

            var InstrumentationKey = Configuration["ApplicationInsightsInstrumentationKey"];

            services.AddOpenTelemetry(builder => {
                builder.AddRequestCollector()
                .AddDependencyCollector()
                .UseApplicationInsights(config => { config.InstrumentationKey = Configuration["ApplicationInsightsInstrumentationKey"]; });
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                  .AddNewtonsoftJson(options =>
                  {
                      options.SerializerSettings.ContractResolver = new DefaultContractResolver();
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

            // Use Ngix to Redirec Http Request
            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();


            app.UseAuthentication();
            


            app.UseRouting();
            app.UseAuthorization(); // 放在 UseAuthentication 之后
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });


            //DiagnosticListener.AllListeners.Subscribe(app.ApplicationServices.GetRequiredService<DiagnosticProcessorObserver>());
        }
    }
}
