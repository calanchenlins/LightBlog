using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Models
{
    public class PostEditViewModel
    {
        public int BlogId { get; set; }

        public string Title { get; set; }

        public string EntryName { get; set; }

        public string Content { get; set; }

        public string Excerpt { get; set; }

        public string Tags { get; private set; }

        public bool IsPublished { get; set; } = true;
    }
}
