using AutoMapper;
using LightBlog.Infrastruct.Entities;
using LightBlog.Models;
using LightBlog.Services.InDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LightBlog.Models.PostsViewModel;

namespace LightBlog.Services.AutoMapperConfig
{
    public class PostProfile : Profile
    {
        public PostProfile()
        {
            CreateMap<LightBlog.Infrastruct.Entities.Post, PostEntryViewModel>().ForMember(d=>d.PostId,opts=>opts.MapFrom(s=>s.Id));
            CreateMap<LightBlog.Infrastruct.Entities.Post, PostDetailViewModel>()
                .ForMember(d => d.PostId, opts => opts.MapFrom(s => s.Id))
                .ForMember(d => d.Comments, opts => opts.Ignore());
            CreateMap<LightBlog.Infrastruct.Entities.Post, PostEditViewModel>()
                .ForMember(d => d.BlogId, opts => opts.MapFrom(s => s.Id));

            CreateMap<Comment, PostCommentViewModel>();

            CreateMap<CreatPostInDto, LightBlog.Infrastruct.Entities.Post>();
        }
    }
}
