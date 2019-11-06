using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Models
{
    public class PostsViewModel
    {
        public List<PostEntryViewModel> PostEntries { get; set; } = new List<PostEntryViewModel>();

        public int FirstPostId { get; set; }

        public int CurrPageIndex { get; set; }

        public bool HasNextPage { get; set; }

        public bool HasLastPage { get; set; }

        public class PostEntryViewModel
        {

            public int PostId { get; set; }

            public string Title { get; set; }

            public string AuthorName { get; set; }

            public string Excerpt { get; set; }

            public int PostViews { get; set; }

            public DateTime Published { get; set; }

            public int CommentCount { get; set; }
        }
    }
}
