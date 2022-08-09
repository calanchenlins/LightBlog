using Autofac;
using AutoMapper;
using K.Basis.Domain.Repositories;
using LightBlog.Infrastruct.Entities;
using LightBlog.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LightBlog.Models.PostsViewModel;

namespace LightBlog.Services.Cache
{
    public class HomeCacheServiceV2 : IHomeCacheService
    {

        private readonly ConcurrentDictionary<int, PostEntryViewModel> _cache;

        private readonly IMapper _mapper;

        private const int DefaultCapacity = 1000;

        private readonly IRepository<LightBlog.Infrastruct.Entities.Post, int> _postRepository;

        private int _lastIndex = 0;

        public HomeCacheServiceV2(IMapper mapper, IRepository<LightBlog.Infrastruct.Entities.Post, int> repository)
        {
            _cache = new ConcurrentDictionary<int, PostEntryViewModel>(Environment.ProcessorCount, DefaultCapacity);

            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            _postRepository = repository;

            var tmp = _postRepository.Get().OrderByDescending(p => p.Id).Take(DefaultCapacity * 2).ToList();

            foreach (var post in tmp)
            {
                _cache.TryAdd(post.Id, _mapper.Map<PostEntryViewModel>(post));
            }
        }


        public PostsViewModel GetPagePosts(int pageSize, int pageIndex)
        {
            var skipCount = pageSize * (pageIndex - 1);

            var validCount = pageSize;

            if (skipCount >= _cache.Count)
            {
                return new PostsViewModel();
            }

            if (validCount > _cache.Count - skipCount)
            {
                validCount = _cache.Count - skipCount;
            }

            var posts = _cache.OrderByDescending(p => p.Key).Skip(skipCount).Take(validCount).Select(p => p.Value).ToList();

            var postsView = new PostsViewModel
            {
                CurrPageIndex = pageIndex,
                HasLastPage = pageIndex > 1 ? true : false,
                HasNextPage = _cache.Count > skipCount + validCount ? true : false,
                PostEntries = _mapper.Map<List<PostEntryViewModel>>(posts)
            };

            return postsView;
        }

        public void AddOrUpdate(LightBlog.Infrastruct.Entities.Post post)
        {
            if (_cache.Count >= DefaultCapacity)
            {
                var postDrop = _cache.Keys.Min();
                _cache.TryRemove(postDrop, out PostEntryViewModel dropValue);
                _lastIndex = _cache.Keys.Min();
            }

            var postView = _mapper.Map<PostEntryViewModel>(post);

            _cache.AddOrUpdate(postView.PostId, postView, (key, value) => {
                value.Excerpt = post.Excerpt;
                value.Title = post.Title;
                return value;
            });
        }

        public void Delete(int id)
        {
            if (id >= _lastIndex)
            {
                _cache.TryRemove(id, out PostEntryViewModel delValue);
                if (id == _lastIndex)
                {
                    _lastIndex = _cache.Keys.Min();
                }
                if (_cache.Count < DefaultCapacity)
                {

                }
            }
        }

        public void AddComment(int id)
        {
            if (id >= _lastIndex)
            {
                _cache.TryGetValue(id, out PostEntryViewModel delValue);
                delValue.CommentCount++;
            }
        }
    }
}
