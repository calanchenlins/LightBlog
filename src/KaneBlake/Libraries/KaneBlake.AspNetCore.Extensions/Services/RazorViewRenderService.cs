using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace K.AspNetCore.Extensions.Services
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model = null, bool isMainPage = true);
    }

    public class RazorViewRenderService : IViewRenderService
    {
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly IServiceProvider _serviceProvider;
        private readonly MvcViewOptions _viewOptions;

        public RazorViewRenderService(IActionContextAccessor actionContextAccessor, IHttpContextAccessor httpContextAccessor, ICompositeViewEngine viewEngine, IServiceProvider serviceProvider, IOptions<MvcViewOptions> viewOptions)
        {
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _viewEngine = viewEngine ?? throw new ArgumentNullException(nameof(viewEngine));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _viewOptions = viewOptions?.Value ?? throw new ArgumentNullException(nameof(viewOptions));
        }

        public async Task<string> RenderToStringAsync(string viewName, object model = null, bool isMainPage = true)
        {
            var actionContext = _actionContextAccessor.ActionContext;
            if (actionContext == null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    httpContext = new DefaultHttpContext() { RequestServices = _serviceProvider };
                }

                actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
            }

            var tempDataFactory = _serviceProvider.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory?.GetTempData(actionContext.HttpContext);
            if (tempData == null)
            {
                var tempDataProvider = _serviceProvider.GetRequiredService<ITempDataProvider>();
                tempData = new TempDataDictionary(actionContext.HttpContext, tempDataProvider);
            }

            var viewEngineResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage);
            if (!viewEngineResult.Success)
            {
                viewEngineResult = _viewEngine.FindView(actionContext, viewName, isMainPage);
            }
            viewEngineResult.EnsureSuccessful(originalLocations: null);
            var view = viewEngineResult.View;

            using var sw = new StringWriter();
            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };
            var viewContext = new ViewContext(actionContext, view, viewDictionary, tempData, sw, _viewOptions.HtmlHelperOptions);

            await view.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
