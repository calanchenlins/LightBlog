using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Services.InDto
{
    public class CommentPostInDto
    {
        [Required]
        public int BlogId { get; set; }

        [StringLength(1000, MinimumLength = 5)]
        [Required]
        public string Content { get; set; }
    }
}
