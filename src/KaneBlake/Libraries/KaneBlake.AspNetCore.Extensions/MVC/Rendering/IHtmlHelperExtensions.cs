using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using WebOptimizer;

namespace K.AspNetCore.Extensions.MVC.Rendering
{
    public static class IHtmlHelperExtensions
    {
        private const string JscriptDeferRazorViewdata = "__jsdfrz";

        private const string JscriptIncludeViewdata = "__jsrq";

        private static string _linkTagFormat = "<link href=\"{0}\" rel=\"stylesheet\"/>";

        private static string _scriptTagFormat = "<script type=\"text/javascript\" src=\"{0}\"></script>";

        public static void DeferScript(this IHtmlHelper html, string scriptLocation)
        {
            string jsTag = "<script type=\"text/javascript\" src=\"" + scriptLocation + "\"></script>";

            if (html.ViewContext.HttpContext.Items.TryGetValue(JscriptIncludeViewdata, out object jscriptsObject) && jscriptsObject is List<string> jscripts && !jscripts.Contains(jsTag))
            {
                jscripts.Add(jsTag);
            }
            else
            {
                html.ViewContext.HttpContext.Items.TryAdd(JscriptIncludeViewdata, new List<string> { jsTag });
            }
        }

        public static void Script(this IHtmlHelper html, Func<int, HelperResult> script)
        {
            if (html.ViewContext.HttpContext.Items.TryGetValue(JscriptDeferRazorViewdata, out object jsActionsObject) && jsActionsObject is List<Func<int, HelperResult>> jsActions)
            {
                jsActions.Add(script);
            }
            else
            {
                html.ViewContext.HttpContext.Items.TryAdd(JscriptIncludeViewdata, new List<Func<int, HelperResult>> { script });
            }
        }

        public static IHtmlContent RenderScripts(this IHtmlHelper html)
        {
            html.ViewContext.HttpContext.Items.TryGetValue(JscriptIncludeViewdata, out object jscriptsObject);
            html.ViewContext.HttpContext.Items.TryGetValue(JscriptDeferRazorViewdata, out object jsActionsObject);

            return new HelperResult(writer => {
                if (jsActionsObject is List<Func<int, HelperResult>> jsActions)
                {
                    jsActions.ForEach(jsAction => jsAction.Invoke(0).WriteTo(writer, HtmlEncoder.Default));
                }
                var temp = string.Empty;
                if (jscriptsObject is List<string> jscripts)
                {
                    temp = string.Join("\r\n", jscripts.ToArray());
                }
                return writer.WriteAsync(temp);
            });
        }

        public static HtmlString RenderBundleAssets(this IHtmlHelper html, params string[] routes)
        {
            var temp = new StringBuilder();
            var pipeline = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IAssetPipeline>();
            foreach (var route in routes)
            {
                if (pipeline.TryGetAssetFromRoute(route, out var asset))
                {
                    if (asset.ContentType.Contains("text/javascript"))
                    {
                        temp.AppendFormat(_scriptTagFormat, asset.Route);
                    }
                    else if (asset.ContentType.Contains("text/css"))
                    {
                        temp.AppendFormat(_linkTagFormat, asset.Route);
                    }
                }
            }
            return new HtmlString(temp.ToString());
        }
    }
}
