using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KaneBlake.Basis.Services;
using LightBlog.Infrastruct.Entities;
using LightBlog.Services;
using LightBlog.Services.InDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LightBlog.Controllers
{
    [Route("pm/[action]")]
    [Authorize]
    public class PostManageController : Controller
    {
        private readonly IPostService _postService;

        private readonly IUserService<User> _userService;

        public PostManageController(IPostService postService, IUserService<User> userService)
        {
            _postService = postService ?? throw new ArgumentNullException(nameof(postService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        [HttpGet]
        public IActionResult PubPost()
        {
            return View("EditPost");
        }

        [HttpGet]
        public IActionResult EditPost(int id)
        {
            var post = _postService.GetPostForEditById(id);
            if (post == null)
            {
                return NotFound();
            }
            return View("EditPost", post);
        }

        #region WebApi

        [HttpPost]
        public IActionResult SavePost(CreatPostInDto input)
        {
            var result = _postService.Create(input);
            if (result.OKStatus)
            {
                return Redirect($@"/blog/{_userService.UserName ?? ""}");
            }
            else
            {
                var ReturnUrl = "/pm/PubPost";
                return Redirect($@"/Account/Login?ReturnUrl={ReturnUrl}");
            }
        }

        [HttpPost]
        public IActionResult EditPost(EditPostInDto input)
        {
            var result = _postService.Edit(input.BlogId, input);
            if (result.OKStatus)
            {
                return Redirect($@"/blog/{_userService.UserName ?? ""}");
            }
            else
            {
                return Redirect($@"/Account/Login?ReturnUrl=/");
            }
        }


        [HttpPost]
        public IActionResult CommentPost([FromBody] CommentPostInDto input)
        {
            if (ModelState.IsValid)
            {
                var result = _postService.Commenting(input.BlogId, input);
                if (result.OKStatus)
                {
                    return Ok();
                }
                else
                {
                    return Redirect("/Account/Login?ReturnUrl=/");
                }
            }
            return BadRequest();
        }


        [HttpGet]
        public IActionResult DelPost(int id)
        {
            var result = _postService.Delete(id);
            if (result.OKStatus)
            {
                return Redirect($@"/blog/{_userService.UserName ?? ""}");
            }
            else
            {
                return Redirect($@"/Account/Login?ReturnUrl=/");
            }
        }

        #endregion

    }
}