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

        ServiceResponse<PostDetailViewModel> GetPostById(int blogId);

        ServiceResponse<PostEditViewModel> GetPostForEditById(int blogId);

        ServiceResponse<bool> Create(CreatPostInDto input);

        ServiceResponse<bool> Edit(int BlogId,EditPostInDto input);

        ServiceResponse<bool> Delete(int BlogId);

        ServiceResponse<bool> Commenting(int BlogId,CommentPostInDto input);
    }
}
