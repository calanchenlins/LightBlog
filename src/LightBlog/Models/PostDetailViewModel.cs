using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Models
{
    public class PostDetailViewModel
    {
        public int PostId { get; set; }

        public string Title { get; set; }

        public string Excerpt { get; set; }

        public string AuthorName { get; set; }

        public string Content { get; set; }

        public int PostViews { get; set; }

        public DateTime Published { get; set; }

        public List<PostCommentViewModel> Comments { get; set; }
    }

    public class PostCommentViewModel
    {
        public string CommentatorName { get; set; }

        public string Content { get; set; }

        public DateTime CreatedTime { get; set; }
    }
}
