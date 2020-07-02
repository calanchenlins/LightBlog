using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LightBlog.Models;
using static LightBlog.Models.PostsViewModel;
using LightBlog.Services;
using System.Net;
using KaneBlake.Basis.Extensions.Logging.File;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using LightBlog.Services.Cache;
using CoreWeb.Util.Services;

namespace LightBlog.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPostService _postService;

        private readonly FileLoggerOptions _options;

        private readonly IHomeCacheService _homeCacheService;

        private readonly ILogger _logger;

        public HomeController(IPostService postService, IOptions<FileLoggerOptions> options, ILogger<HomeController> logger, IHomeCacheService homeCacheService)
        {
            _options = options.Value;
            _postService = postService ?? throw new ArgumentNullException(nameof(postService));
            _homeCacheService = homeCacheService ?? throw new ArgumentNullException(nameof(homeCacheService));
            _logger = logger;
        }

        /// <summary>
        /// 主页面
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            //var result = _postService.GetPagePosts(0, 5, 0);
            //return View(result.Response);
            var res = _homeCacheService.GetPagePosts(5, 1);
            return View(res);
        }

        /// <summary>
        /// 博文详情页面
        /// </summary>
        /// <param name="BloggerName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("blog/{BloggerName:required}/{id:int:min(1)}")]
        public IActionResult PostView(string BloggerName,int id)
        {
            var postDetail = _postService.GetPostById(id);
            return View("/Views/Home/PostDetail.cshtml", postDetail.Data);
        }

        /// <summary>
        /// 用户博客主页面
        /// </summary>
        /// <param name="BloggerName"></param>
        /// <returns></returns>
        [Route("blog/{BloggerName:required}")]
        public IActionResult BlogView(string BloggerName)
        {
            var postDetail = _postService.GetPagePostsByUser(BloggerName,0,5,0);
            return View("Index", postDetail.Data);
        }

        #region Api
        /// <summary>
        /// 主页面、用户主页面，分页数据接口
        /// </summary>
        /// <param name="BloggerName"></param>
        /// <param name="startIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("PagePosts")]
        [ProducesResponseType(typeof(IEnumerable<PostsViewModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public IActionResult PagePostsApi([FromForm]string BloggerName = "",[FromForm]int startIndex = 0 , [FromForm]int pageSize = 5, [FromForm]int pageIndex = 1)
        {
            ServiceResponse<PostsViewModel> postDetail;
            if ((BloggerName?.Trim()??"") == "")
            {
                var res = _homeCacheService.GetPagePosts(pageSize, pageIndex);
                postDetail = ServiceResponse.OK(res);
            }
            else
            {
                postDetail = _postService.GetPagePostsByUser(BloggerName, startIndex, pageSize, pageIndex);
            }
            if (postDetail.OKStatus)
            {
                return Ok(postDetail.Data);
            }
            else
            {
                return BadRequest();
            }
        }
        #endregion

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
