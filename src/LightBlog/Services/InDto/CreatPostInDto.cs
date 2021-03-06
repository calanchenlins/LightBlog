﻿using System;
using System.ComponentModel.DataAnnotations;

namespace LightBlog.Services.InDto
{
    public class CreatPostInDto
    {
        [StringLength(160)]
        [Required(ErrorMessage = "'{0}' 不可为空")]
        [Display(Name = "标题")]
        public string Title { get; set; }

        [StringLength(160)]
        public string EntryName { get;  set; }

        [Required(ErrorMessage = "'{0}' 不可为空")]
        [Display(Name = "文章内容")]
        public string Content { get;  set; }

        [StringLength(2000)]
        public string Excerpt { get;  set; }

        [StringLength(500)]
        public string Tags { get; private set; }

        [Required]
        public bool IsPublished { get;  set; } = true;
    }
}
