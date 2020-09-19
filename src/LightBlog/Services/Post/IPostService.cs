using KaneBlake.Basis.Services;
using LightBlog.Common.AOP.CommonCache;
using LightBlog.Infrastruct.Entities;
using LightBlog.Models;
using LightBlog.Services.InDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Services
{
    public interface IPostService : IAuthService<AuthenticatedUser>
    {
        ServiceResponse<PostsViewModel> GetPagePosts(int startIndex = 0, int pageSize = 5, int pageIndex = 1);

        ServiceResponse<PostsViewModel> GetPagePostsByUser(string authorName,int startIndex = 0, int pageSize = 5, int pageIndex = 1);

        [Caching(QueryKeys = new string[] { "blogId" })]
        ServiceResponse<PostDetailViewModel> GetPostById(int blogId);

        PostEditViewModel GetPostForEditById(int blogId);

        ServiceResponse Create(CreatPostInDto input);

        [CachingSet(QuryTypeName = nameof(PostService), QueryMethodName = nameof(GetPostById), QueryKeys = new string[] { "BlogId" })]
        ServiceResponse Edit(int BlogId,EditPostInDto input);

        [CachingSet(QuryTypeName = nameof(PostService), QueryMethodName = nameof(GetPostById), QueryKeys = new string[] { "BlogId" })]
        ServiceResponse Delete(int BlogId);

        [CachingSet(QuryTypeName = nameof(PostService), QueryMethodName = nameof(GetPostById), QueryKeys = new string[] { "blogId" })]
        [CachingSet(QuryTypeName = nameof(PostService), QueryMethodName = nameof(GetPostById), QueryKeys = new string[] { "blogId" })]
        ServiceResponse Commenting(int BlogId,CommentPostInDto input);
    }
}
