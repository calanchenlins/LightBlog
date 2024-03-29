﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using K.Basis.Services;
using K.Basis.Domain.Repositories;
using LightBlog.Common.AOP.CommonCache;
using LightBlog.Common.Diagnostics;
using LightBlog.Infrastruct.Entities;
using LightBlog.Models;
using LightBlog.Services.InDto;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using static LightBlog.Models.PostsViewModel;
using LightBlog.Services.User;
using LightBlog.Services.Post;

namespace LightBlog.Services
{
    // todo:增加服务上下文 ServiceContext 承载Http中的非DTO数据(用户信息、账套信息等等)
    public class PostService : IPostService
    {
        private readonly IMapper _mapper;

        private readonly IRepository<LightBlog.Infrastruct.Entities.Post, int> _postRepository;

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Diagnostics 的优势在于可以修改传递的对象
        /// </summary>
        private static readonly DiagnosticListener s_diagnosticListener =
    new DiagnosticListener(LightBlogDiagnosticListenerExtensions.DiagnosticListenerName);

        public PostService(IRepository<LightBlog.Infrastruct.Entities.Post, int> postRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public AuthenticatedUser GetAuthenticatedUser()
        {
            var user = new AuthenticatedUser();
            if (_httpContextAccessor.HttpContext.User.Identity != null)
            {
                user.IsAuthenticated = _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated;
                user.UserName = _httpContextAccessor.HttpContext.User.Identity.Name;
                var userClaims = _httpContextAccessor.HttpContext.User.Claims
                    .Where(c => c.Type == ClaimTypes.NameIdentifier)
                    .FirstOrDefault();
                int.TryParse(userClaims?.Value ?? "", out int userId);
                user.UserId = userId;
            }
            return user;
        }


        public ServiceResponse Commenting(int blogId, CommentPostInDto input)
        {
            var post = _postRepository.Get()
                .Where(x => x.Id == input.BlogId)
                .Include(x=>x.Comments)
                .FirstOrDefault();
            var user = GetAuthenticatedUser();
            if (user.IsAuthenticated)
            {
                post.AddComment(input.Content, user.UserId, user.UserName);
                _postRepository.Complete();
                s_diagnosticListener.WriteAddPostCommentAfter(post.Id);
                return ServiceResponse.OK();
            }
            return ServiceResponse.Unauthorized();
        }

        public ServiceResponse Create(CreatPostInDto input)
        {
            var user = GetAuthenticatedUser();
            if (user.IsAuthenticated)
            {
                var post = _mapper.Map<LightBlog.Infrastruct.Entities.Post>(input);
                post.SetAuthor(user.UserId, user.UserName);
                _postRepository.Add(post);
                _postRepository.Complete();
                s_diagnosticListener.WriteAddOrUpdatePostAfter(post);
                return ServiceResponse.OK();
            }
            return ServiceResponse.Unauthorized();
        }


        public ServiceResponse Delete(int BlogId)
        {
            var post = _postRepository.Get()
                .Where(x => x.Id == BlogId)
                .FirstOrDefault();
            if ((post?.AuthorId ?? -1) == GetAuthenticatedUser().UserId)
            {
                _postRepository.Remove(post);
                _postRepository.Complete();
                s_diagnosticListener.WriteDeletePostAfter(post.Id);
                return ServiceResponse.OK();
            }
            return ServiceResponse.Unauthorized();
        }


        public ServiceResponse Edit(int BlogId, EditPostInDto input)
        {
            var post = _postRepository.Get()
                .Where(x => x.Id == input.BlogId)
                .FirstOrDefault();
            if ((post?.AuthorId ?? -1) == GetAuthenticatedUser().UserId)
            {
                post?.Edit(input.Title, input.EntryName, input.Content, input.Excerpt);
                _postRepository.Complete();
                s_diagnosticListener.WriteAddOrUpdatePostAfter(post);
                return ServiceResponse.OK();
            }
            return ServiceResponse.Unauthorized();
        }


        public ServiceResponse<PostDetailViewModel> GetPostById(int blogId)
        {
            var post = _postRepository.Get()
                .Where(x => x.Id == blogId)
                .Include(c => c.Comments)
                .FirstOrDefault();
            var postDeatil = _mapper.Map<PostDetailViewModel>(post);
            var comments = _mapper.Map<List<PostCommentViewModel>>(post.Comments);
            postDeatil.Comments = comments;
            return ServiceResponse.OK(postDeatil);
        }

        public PostEditViewModel GetPostForEditById(int blogId)
        {
            var post = _postRepository.Get()
                .Where(x => x.Id == blogId)
                .FirstOrDefault();
            if ((post?.AuthorId ?? -1) == GetAuthenticatedUser().UserId)
            {
                var postDeatil = _mapper.Map<PostEditViewModel>(post);
                return postDeatil;
            }
            return null;
        }

        public ServiceResponse<PostsViewModel> GetPagePostsByUser(string authorName, int startIndex = 0, int pageSize = 10, int pageIndex = 0)
        {
            if (startIndex <= 0)
            {
                startIndex = _postRepository.Get().Where(p=>p.AuthorName==authorName.Trim()).OrderByDescending(p => p.Id).FirstOrDefault()?.Id??0;
            }
            var AllItems = _postRepository.Get()
                .Where(p => p.Id <= startIndex && p.AuthorName==authorName.Trim())
                .OrderByDescending(c => c.Id).Count();
            int vaildCount = 0;
            if (AllItems < pageSize * (pageIndex + 1))
            {
                vaildCount = AllItems - pageSize * pageIndex;
                if (vaildCount <= 0)
                {
                    return ServiceResponse.OK(new PostsViewModel());
                }
            }
            else
            {
                vaildCount = pageSize;
            }
            var blogOnPage = _postRepository.Get()
                .Where(p => p.Id <= startIndex && p.AuthorName == authorName.Trim())
                .OrderByDescending(c => c.Id).Skip(pageSize * pageIndex)
                .Take(vaildCount)
                .ToList();
            var postsView = new PostsViewModel();
            var postEntries = _mapper.Map<List<PostEntryViewModel>>(blogOnPage);
            postsView.FirstPostId = startIndex;
            postsView.CurrPageIndex = pageIndex;
            postsView.HasNextPage = AllItems > pageSize * (pageIndex + 1);
            postsView.HasLastPage = pageIndex <= 0 ? false : true;
            postsView.PostEntries = postEntries;
            return ServiceResponse.OK(postsView);
        }

        public ServiceResponse<PostsViewModel> GetPagePosts(int startIndex = 0, int pageSize = 10, int pageIndex = 0)
        {
            if (startIndex <= 0)
            {
                startIndex = _postRepository.Get().OrderByDescending(p => p.Id).FirstOrDefault().Id;
            }
            var AllItems = _postRepository.Get().Where(c=>c.Id<=startIndex).OrderByDescending(c => c.Id).Count();
            int vaildCount = 0;
            if (AllItems < pageSize*(pageIndex+1))
            {
                vaildCount = AllItems - pageSize * pageIndex;
                if (vaildCount <= 0)
                {
                    return ServiceResponse.OK(new PostsViewModel());
                }
            }
            else
            {
                vaildCount = pageSize;
            }
            var blogOnPage = _postRepository.Get()
                .Where(c => c.Id <= startIndex)
                .OrderByDescending(c => c.Id).Skip(pageSize * pageIndex)
                .Take(vaildCount)
                .ToList();
            var postsView = new PostsViewModel();
            var postEntries = _mapper.Map<List<PostEntryViewModel>>(blogOnPage);
            postsView.FirstPostId = startIndex;
            postsView.CurrPageIndex = pageIndex;
            postsView.HasNextPage = AllItems > pageSize * (pageIndex + 1);
            postsView.HasLastPage = pageIndex<=0?false:true;
            postsView.PostEntries = postEntries;
            return ServiceResponse.OK(postsView);
        }

    }
}
