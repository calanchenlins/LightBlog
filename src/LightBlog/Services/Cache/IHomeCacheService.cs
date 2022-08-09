using LightBlog.Infrastruct.Entities;
using LightBlog.Models;

namespace LightBlog.Services.Cache
{
    public interface IHomeCacheService
    {
        void AddOrUpdate(LightBlog.Infrastruct.Entities.Post post);

        void Delete(int id);

        void AddComment(int id);

        PostsViewModel GetPagePosts(int pageSize, int pageIndex);
    }
}